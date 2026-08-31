using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using Newtonsoft.Json;

namespace AvatarAnimator
{
    [CustomEditor(typeof(AvatarAnimatorDataContainer))]
    [DisallowMultipleComponent]
    public class AvatarAnimatorDataEditor : Editor
    {
        AvatarAnimatorDataContainer container;
        public Animator anim;
        private readonly GuiLogger Logger = new();

        private void OnEnable()
        {
            container = (AvatarAnimatorDataContainer)target;
            anim = container.GetComponent<Animator>();
        }
        public override void OnInspectorGUI()
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(container.gameObject))
            {
                anim = (Animator)EditorGUILayout.ObjectField(anim, typeof(Animator), true);
                GUILayout.Label("" == container.m_Data.Version ? $"No data found" : $"Data found, version:'{container.m_Data.Version}' generated:'{container.m_Data.Date}'");
                GUI.enabled = null != anim;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("How To Use"))
                {
                    Logger.Reset();
                    Help();
                }
                if (GUILayout.Button("Populate Data"))
                {
                    Logger.Reset();
                    container.m_Data = null;
                    container.m_CompactedData = null;
                    try
                    {
                        container.PopulateData(anim, CollectData());
                    }
                    catch (Exception e) { Logger.MsgErr(e.ToString()); }
                }
                if (null != container.m_Data)
                {
                    if (GUILayout.Button("Test Data"))
                    {
                        Logger.Reset();
                        Logger.Msg(JsonConvert.SerializeObject(container.m_Data, Formatting.Indented));
                    }
                    if (GUILayout.Button("Compact Data"))
                    {
                        Logger.Reset();
                        container.CompactData();
                        Logger.MsgInfo("Data has been compacted");
                    }
                }
                GUILayout.EndHorizontal();
                Logger.Gui();
                GUI.enabled = true;
            }

            DrawDefaultInspector();
        }

        public void Help()
        {
            Application.OpenURL("https://github.com/seb-dal/Bonelab-AvatarAnimator/wiki/How-to-use-in-Unity");
        }


        private static readonly string LayerName = "AvatarAnimator";
        private static readonly string TransitionInputsSeparator = ";";
        private static readonly string InputTypeSeparator = ":";
        private static readonly string SecondaryInputSeparator = "+";
        private static readonly Regex isRandomType = new(@"Random\((?:(\d+)|(\d+),[ ]*(\d+))\)", RegexOptions.IgnoreCase);
        private static readonly string isHealth = "Health";
        private static readonly string isInput = "Input=";
        private static readonly string isTimer = "Timer";
        private static readonly string isWaitEndClip = "WaitEndClip";
        private static readonly Regex isCyclic = new(@"Cyclic\((\d+)\)", RegexOptions.IgnoreCase);

        private static readonly string version = "1.0";

        private AvatarAnimatorData CollectData()
        {
            AvatarAnimatorData data = new()
            {
                Version = version,
                Date = DateTime.Now.ToString(),
                TransitionsData = new(),
                ListLayer = new(),
            };
            int layerIndex = -1;
            if (anim.runtimeAnimatorController is AnimatorController ac)
            {
                Logger.Msg($"Layer: '{LayerName}'");
                foreach (AnimatorControllerLayer layer in ac.layers)
                {
                    layerIndex += 1;
                    if (!layer.name.StartsWith(LayerName, StringComparison.CurrentCultureIgnoreCase)) continue;
                    AnimatorStateMachine stateMachine = layer.stateMachine;
                    LayerData layerData = new()
                    {
                        Name = LayerName,
                        StartState = stateMachine.defaultState.name,
                        LayerIndex = layerIndex,
                        States = new(),
                    };
                    data.ListLayer.Add(layerData);

                    foreach (ChildAnimatorState childState in stateMachine.states)
                    {
                        Logger.Msg($"  State: {childState.state.name}");
                        var motion = childState.state.motion;
                        StateNode state = new()
                        {
                            ClipDuration = motion?.averageDuration ?? 0,
                            ClipIsLooping = motion?.isLooping ?? false,
                            Speed = childState.state.speed,
                            Transitions = new(),
                        };
                        layerData.States.Add(childState.state.name, state);

                        foreach (AnimatorStateTransition transition in childState.state.transitions)
                        {
                            Logger.Msg($"    Transition: '{transition.name}' {childState.state.name} -> {transition.destinationState.name}");
                            Transition trans = new()
                            {
                                NextState = transition.destinationState.name,
                                Duration = transition.duration,
                                ExitTime = transition.exitTime,
                                HasExitTime = transition.hasExitTime,
                                Conditions = new(),
                            };
                            state.Transitions.Add(trans);
                            foreach (AnimatorCondition cond in transition.conditions)
                            {
                                Logger.Msg($"      Condition: {cond.parameter}");
                                var res = ToTransitionCondition(cond);
                                if (null == res) continue;
                                trans.Conditions.Add(res.First);
                                if (null != res.Second)
                                {
                                    if (!data.TransitionsData.ContainsKey(cond.parameter))
                                        data.TransitionsData.Add(cond.parameter, res.Second);
                                }
                            }
                        }
                    }
                }
                if (0 == data.ListLayer.Count)
                {
                    Logger.MsgErr($"Animator must have a Layer name '{LayerName}' with states");
                }
            }
            else { Logger.MsgErr("Animator must have a Controller"); }
            Logger.MsgInfo($"AvatarAnimatorDataContainer updated");
            return data;
        }

        private ConditionInput ToTransitionInput(string input)
        {
            input = input.Replace(" ", "");
            ConditionInput tInput = new();
            bool valide = true;
            var typeAndInput = input.Split(InputTypeSeparator, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                var type = Enum.Parse<InputType>(typeAndInput[0]);
                string name = typeAndInput[1];
                string name2 = "";
                if (name.Contains(SecondaryInputSeparator))
                {
                    var inputs = name.Split(SecondaryInputSeparator);
                    name = inputs[0];
                    name2 = inputs[1];
                }
                tInput.Type = type;
                tInput.InputName = name;
                tInput.InputName2 = name2;
                switch (type)
                {
                    case InputType.Controller:
                        {
                            tInput.ControllerInput = ParseControllerInputs(input, name);
                            tInput.ControllerInput2 = ParseControllerInputs(input, name2, true);
                            valide &= tInput.InputName != tInput.InputName2;
                            valide &= ControllerInputs.None != tInput.ControllerInput;
                            valide &= (ControllerInputs.None != tInput.ControllerInput2 || "" == name2);
                        }
                        break;
                    case InputType.Keyboard:
                        {
                            tInput.KeyCode = ParseKeyCode(input, name);
                            tInput.KeyCode2 = ParseKeyCode(input, name2, true);
                            valide &= tInput.KeyCode != tInput.KeyCode2;
                            valide &= KeyCode.None != tInput.KeyCode;
                            valide &= (KeyCode.None != tInput.KeyCode2 || "" == name2);
                        }
                        break;
                    default:
                        Logger.MsgErr($"Valide types are '{InputType.Keyboard}', '{InputType.Controller}'");
                        valide = false;
                        break;
                }
            }
            catch (Exception e)
            {
                if ("" != typeAndInput[0])
                {
                    Logger.MsgErr("ErrorMessage: " + e.Message);
                    Logger.MsgErr($"'{input}' doesn't have a valide InputType. '{typeAndInput[0]}' (<InputType>:<InputKey>)");
                    LogInputHelp();
                }

                valide = false;
            }

            if (!valide)
            {
                Logger.MsgErr($"'{input}' is not valid (name:'{tInput.InputName}' code:{tInput.KeyCode} name2:'{tInput.InputName2}' code2:{tInput.KeyCode2})");
                tInput.Type = InputType.Unset;
            }
            return tInput;
        }

        private KeyCode ParseKeyCode(string input, string name, bool isSecondary = false)
        {
            if ("" == name && isSecondary) return KeyCode.None;
            try { return Enum.Parse<KeyCode>(name, true); }
            catch (Exception)
            {
                Logger.MsgErr($"'{name}' in '{input}' is not a valide keyboard Key");
                return KeyCode.None;
            }
        }

        private ControllerInputs ParseControllerInputs(string input, string name, bool isSecondary = false)
        {
            if ("" == name && isSecondary) return ControllerInputs.None;
            try { return Enum.Parse<ControllerInputs>(name, true); }
            catch (Exception)
            {
                Logger.MsgErr($"'{name}' in '{input}' is not a valide Controller input");
                return ControllerInputs.None;
            }
        }

        private Pair<TransitionCondition, TransitionConditionData> ToTransitionCondition(AnimatorCondition cond)
        {
            TransitionCondition c = new()
            {
                Name = cond.parameter,
                Type = ConditionType.Unset,
            };
            TransitionConditionData d = null;

            if (isRandomType.Match(cond.parameter) is { Success: true } rand)
            {
                c.Type = ConditionType.Random;
                c.Mode = Utils.ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                d = new()
                {
                    Type = c.Type,
                };
                if (null != rand.Groups[1])
                {
                    d.Max = int.Parse(rand.Groups[1].Value);
                }
                else
                {
                    d.Min = int.Parse(rand.Groups[2].Value);
                    d.Max = int.Parse(rand.Groups[3].Value);
                }
                if ((d.Min) < cond.threshold || cond.threshold < d.Max)
                    Logger.MsgErr($"Random value must be between defined values");
                if ((d.Min) > d.Max)
                    Logger.MsgErr($"Random max must be greater that min");
            }
            else if (cond.parameter.Equals(isHealth, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Health;
                c.Mode = Utils.ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                if (c.Mode != ConditionMode.Greater && c.Mode != ConditionMode.Less)
                    Logger.MsgErr($"Health must be a float and use Greater or Less");
                if (0.0f < cond.threshold || cond.threshold < 1.0f)
                    Logger.MsgErr($"Health value must be between 0 and 1");
            }
            else if (cond.parameter.StartsWith(isInput, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Input;
                var inputs = cond.parameter.Substring(isInput.Length);
                d = new()
                {
                    Inputs = new(),
                    Type = c.Type,
                };
                foreach (string input in inputs.Split(TransitionInputsSeparator))
                {
                    var tInput = ToTransitionInput(input);
                    if (InputType.Unset == tInput.Type) continue;
                    d.Inputs.Add(tInput);
                }
            }
            else if (cond.parameter.StartsWith(isTimer, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Timer;
                c.Mode = Utils.ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                if (c.Mode != ConditionMode.Greater)
                    Logger.MsgErr($"Timer must only use Greater to work");
            }
            else if (cond.parameter.StartsWith(isWaitEndClip, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.WaitEndClip;
            }
            else if (isCyclic.Match(cond.parameter) is { Success: true } cycle)
            {
                c.Type = ConditionType.Cyclic;
                c.Mode = Utils.ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                d = new()
                {
                    Type = c.Type,
                };
                d.Max = int.Parse(cycle.Groups[1].Value);
                if (0 > d.Max)
                    Logger.MsgErr($"Cyclic value must greater that 0");
                if (c.Mode != ConditionMode.Equals)
                    Logger.MsgWarn($"Cyclic should only use Equals");
            }

            if (ConditionType.Unset == c.Type)
            {
                Logger.MsgErr($"Unknown condition {cond.parameter}");
                Logger.MsgInfo($"'Random(5)', 'Random(0,5)', '{isHealth}', '{isInput}...', '{isTimer}'");
                return null;
            }
            return new(c, d);
        }

        private void LogInputHelp()
        {
            Logger.Msg("Input=<Type>:<Key1>(+<Key2>);<Type>:<Key>");
            Logger.Msg($"<Type>: '{InputType.Keyboard}', '{InputType.Controller}'");
            Logger.Msg("<Key>:"
                + $"\n - '{InputType.Keyboard}': {Enum.GetValues(typeof(KeyCode))}"
                + $"\n - '{InputType.Controller}': {Enum.GetValues(typeof(ControllerInputs))}"
                );
            Logger.Msg("Exemple: Input=Keyboard.T;Controller.LeftThumbStick");
        }
    }
}


namespace AvatarAnimator
{
    public class GuiLogger
    {
        private readonly List<string> logs = new();

        public void Reset() => logs.Clear();

        public void Msg(string msg) => logs.Add(msg);
        public void MsgInfo(string msg) => logs.Add("<color=cyan>" + msg + "</color>");
        public void MsgWarn(string msg) => logs.Add("<color=yellow>" + msg + "</color>");
        public void MsgErr(string msg) => logs.Add("<color=red>" + msg + "</color>");

        private Vector2 gui_scrollPosition;
        private float gui_scrollViewHeight = 160f;
        private bool gui_isResizing;
        private Rect gui_resizerRect;
        public void Gui()
        {
            gui_scrollPosition = EditorGUILayout.BeginScrollView(gui_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(gui_scrollViewHeight));
            GUIStyle style = new();
            style.richText = true;
            style.normal.textColor = Color.white;
            foreach (var log in logs)
            {
                GUILayout.TextArea(log, style);
            }
            EditorGUILayout.EndScrollView();

            // Barre de redimensionnement
            gui_resizerRect = GUILayoutUtility.GetRect(gui_resizerRect.width, 5f);
            EditorGUIUtility.AddCursorRect(gui_resizerRect, MouseCursor.ResizeVertical);
            GUI.Box(gui_resizerRect, "", "WindowBottomResize");

            // Gestion logique du redimensionnement
            Event e = Event.current;
            if (e.type == EventType.MouseDown && gui_resizerRect.Contains(e.mousePosition)) gui_isResizing = true;
            if (gui_isResizing)
            {
                gui_scrollViewHeight = Mathf.Clamp(e.mousePosition.y, 50f, 400f);
            }
            if (e.type == EventType.MouseUp) gui_isResizing = false;
        }
    }
}


namespace AvatarAnimator
{
    public class Utils
    {
        public static ConditionMode ToConditionMode(AnimatorConditionMode mode) => Enum.Parse<ConditionMode>(mode.ToString(), true);

        public static int FloatToInt(float v) => (int)Math.Round(v);

        public static string ToStringAllEnumValues<T>()
        {
            string str = "[";
            var array = Enum.GetValues(typeof(T));
            T last = (T)array.GetValue(array.Length - 1);
            foreach (T e in array)
            {
                str += e.ToString() + (last.Equals(e) ? "" : ",");
            }
            return str + "]";
        }
    }

    public class Pair<T, U>
    {
        public Pair() { }
        public Pair(T first, U second) { First = first; Second = second; }
        public T First { get; set; }
        public U Second { get; set; }
    };
}
#endif // UNITY_EDITOR
