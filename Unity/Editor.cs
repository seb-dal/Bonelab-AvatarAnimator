#if UNITY_EDITOR

using System;
using UnityEngine;
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
        public Animator m_anim;
        public AvatarAnimatorData m_Data;

        private void OnEnable()
        {
            container = (AvatarAnimatorDataContainer)target;
            m_anim = container.GetComponent<Animator>();
        }
        public override void OnInspectorGUI()
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(container.gameObject))
            {
                m_anim = (Animator)EditorGUILayout.ObjectField(m_anim, typeof(Animator), true);
                GUILayout.Label("" == container.m_Version ? $"No data found" : $"Data found, version:'{container.m_Version}' generated:'{m_Data?.Date}'");
                GUI.enabled = null != m_anim;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("How To Use"))
                {
                    LoggerGUI.Reset();
                    Help();
                }
                if (GUILayout.Button("Populate Data"))
                {
                    LoggerGUI.Reset();
                    m_Data = null;
                    container.m_SerializeData = null;
                    try
                    {
                        m_Data = CollectData(m_anim);
                    }
                    catch (Exception e) { Logger.Err(e.ToString()); }
                }
                if (null != m_Data)
                {
                    if (GUILayout.Button("Test Data"))
                    {
                        LoggerGUI.Reset();
                        Logger.Msg(JsonConvert.SerializeObject(m_Data, Formatting.Indented));
                    }
                    if (GUILayout.Button("Serialize Data"))
                    {
                        LoggerGUI.Reset();
                        container.PopulateData(m_anim, JsonConvert.SerializeObject(m_Data, Formatting.None));
                        Logger.Info("Data has been serialized");
                    }
                }
                GUILayout.EndHorizontal();
                LoggerGUI.Gui();
                GUI.enabled = true;
            }

            DrawDefaultInspector();
        }

        public static void Help()
        {
            Application.OpenURL("https://github.com/seb-dal/Bonelab-AvatarAnimator/wiki/How-to-use-in-Unity");
        }


        public static AvatarAnimatorData CollectData(Animator anim)
        {
            AvatarAnimatorData data = new()
            {
                Date = DateTime.Now.ToString(),
                TransitionsData = new(),
                ListLayer = new(),
            };
            int layerIndex = -1;
            if (anim.runtimeAnimatorController is AnimatorController ac)
            {
                foreach (AnimatorControllerLayer layer in ac.layers)
                {
                    Logger.Msg($"Layer: '{layer.name}'");
                    layerIndex += 1;
                    if (!layer.name.StartsWith(Const.LayerName, StringComparison.CurrentCultureIgnoreCase)) continue;
                    AnimatorStateMachine stateMachine = layer.stateMachine;
                    LayerData layerData = new()
                    {
                        Name = layer.name,
                        StartState = stateMachine?.defaultState?.name ?? "",
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
                    Logger.Err($"Animator must have at least one Layer named '{Const.LayerName}' with states");
                }
            }
            else { Logger.Err("Animator must have a Controller"); }
            Logger.Info($"AvatarAnimatorDataContainer updated");
            return data;
        }

        private static ConditionInput ToTransitionInput(string input)
        {
            input = input.Replace(" ", "");
            ConditionInput tInput = new();
            bool valide = true;
            var typeAndInput = input.Split(Const.InputTypeSeparator, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                var type = Enum.Parse<InputType>(typeAndInput[0]);
                string name = typeAndInput[1];
                string name2 = "";
                if (name.Contains(Const.SecondaryInputSeparator))
                {
                    var inputs = name.Split(Const.SecondaryInputSeparator);
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
                        Logger.Err($"Valide types are '{InputType.Keyboard}', '{InputType.Controller}'");
                        valide = false;
                        break;
                }
            }
            catch (Exception e)
            {
                if ("" != typeAndInput[0])
                {
                    Logger.Err("ErrorMessage: " + e.Message);
                    Logger.Err($"'{input}' doesn't have a valide InputType. '{typeAndInput[0]}' (<InputType>:<InputKey>)");
                    LogInputHelp();
                }

                valide = false;
            }

            if (!valide)
            {
                Logger.Err($"'{input}' is not valid (name:'{tInput.InputName}' code:{tInput.KeyCode} name2:'{tInput.InputName2}' code2:{tInput.KeyCode2})");
                tInput.Type = InputType.Unset;
            }
            return tInput;
        }

        private static KeyCode ParseKeyCode(string input, string name, bool isSecondary = false)
        {
            if ("" == name && isSecondary) return KeyCode.None;
            try { return Enum.Parse<KeyCode>(name, true); }
            catch (Exception)
            {
                Logger.Err($"'{name}' in '{input}' is not a valide keyboard Key");
                return KeyCode.None;
            }
        }

        private static ControllerInputs ParseControllerInputs(string input, string name, bool isSecondary = false)
        {
            if ("" == name && isSecondary) return ControllerInputs.None;
            try { return Enum.Parse<ControllerInputs>(name, true); }
            catch (Exception)
            {
                Logger.Err($"'{name}' in '{input}' is not a valide Controller input");
                return ControllerInputs.None;
            }
        }

        public static ConditionMode ToConditionMode(AnimatorConditionMode mode) => Enum.Parse<ConditionMode>(mode.ToString(), true);

        private static Pair<TransitionCondition, TransitionConditionData> ToTransitionCondition(AnimatorCondition cond)
        {
            TransitionCondition c = new()
            {
                Name = cond.parameter,
                Type = ConditionType.Unset,
            };
            TransitionConditionData d = null;

            if (Const.IsRandomType.Match(cond.parameter) is { Success: true } rand)
            {
                c.Type = ConditionType.Random;
                c.Mode = ToConditionMode(cond.mode);
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
                if (d.Min > cond.threshold || cond.threshold > d.Max)
                    Logger.Err($"Random value must be between defined values (min:{d.Min}, max:{d.Max} value:{cond.threshold})");
                if (d.Min > d.Max)
                    Logger.Err($"Random max must be greater that min (min:{d.Min}, max:{d.Max})");
            }
            else if (cond.parameter.Equals(Const.IsHealth, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Health;
                c.Mode = ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                if (c.Mode != ConditionMode.Greater && c.Mode != ConditionMode.Less)
                    Logger.Err($"Health must be a float and use Greater or Less");
                if (0.0f > cond.threshold || cond.threshold > 1.0f)
                    Logger.Err($"Health value must be between 0 and 1");
            }
            else if (cond.parameter.StartsWith(Const.IsInput, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Input;
                var inputs = cond.parameter.Substring(Const.IsInput.Length);
                d = new()
                {
                    Inputs = new(),
                    Type = c.Type,
                };
                foreach (string input in inputs.Split(Const.TransitionInputsSeparator))
                {
                    var tInput = ToTransitionInput(input);
                    if (InputType.Unset == tInput.Type) continue;
                    d.Inputs.Add(tInput);
                }
            }
            else if (cond.parameter.StartsWith(Const.IsTimer, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.Timer;
                c.Mode = ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                if (c.Mode != ConditionMode.Greater)
                    Logger.Err($"Timer must only use Greater to work");
            }
            else if (cond.parameter.StartsWith(Const.IsWaitEndClip, StringComparison.CurrentCultureIgnoreCase))
            {
                c.Type = ConditionType.WaitEndClip;
            }
            else if (Const.IsCyclic.Match(cond.parameter) is { Success: true } cycle)
            {
                c.Type = ConditionType.Cyclic;
                c.Mode = ToConditionMode(cond.mode);
                c.Threshold = cond.threshold;
                d = new()
                {
                    Type = c.Type,
                };
                d.Max = int.Parse(cycle.Groups[1].Value);
                if (0 > d.Max)
                    Logger.Err($"Cyclic value must greater that 0");
                if (c.Mode != ConditionMode.Equals)
                    Logger.Warn($"Cyclic should only use Equals");
            }

            if (ConditionType.Unset == c.Type)
            {
                Logger.Err($"Unknown condition {cond.parameter}");
                Logger.Info($"'Random(5)', 'Random(0,5)', '{Const.IsHealth}', '{Const.IsInput}...', '{Const.IsTimer}'");
                return null;
            }
            return new(c, d);
        }

        private static void LogInputHelp()
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

#endif // UNITY_EDITOR
