#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace AvatarAnimator
{
    [CustomEditor(typeof(AvatarAnimator))]
    public class AvatarAnimator : EditorWindow
    {
        [SerializeField]
        public Animator anim;

        [MenuItem("Tools/AvatarAnimator")]
        static void Init()
        {
            AvatarAnimator window = (AvatarAnimator)GetWindow(typeof(AvatarAnimator));
        }

        void OnGUI()
        {
            anim = (Animator)EditorGUILayout.ObjectField(anim, typeof(Animator), true);

            if (GUILayout.Button("Add or Update AvatarAnimator data"))
            {
                Patch();
            }
        }


        /// <summary>
        /// 
        /// The mod AvatarAnimator need the entier AnimatorController graph but when packing Pallet Il2Cpp doesn't give acces to it anymore.
        /// So the Graph must be store in something that we can still have acces after packing.
        /// This may seem dirty but storing the graph in a parameter name 
        ///  - Doesn't break anything and easily reversable.
        ///  - Allow easy acces with Il2Cpp.
        ///  - Don't need the user to install any other files for the mod to work
        ///  
        /// The user Input to trigger a transition is store as a Transition Name to keep thing simple and readable.
        /// "(Keyboard/ControllerRight/ControllerLeft).(input name/code);"
        /// </summary>
        public void Patch()
        {
            AvatarAnimatorJson jsonClass = new()
            {
                states = new Dictionary<string, StateNode>(),
                startState = ""
            };

            if (anim.runtimeAnimatorController is AnimatorController ac)
            {
                foreach (AnimatorControllerLayer layer in ac.layers)
                {
                    AnimatorStateMachine stateMachine = layer.stateMachine;
                    jsonClass.startState = stateMachine.defaultState.name;

                    foreach (ChildAnimatorState childState in stateMachine.states)
                    {
                        StateNode state = new()
                        {
                            transitions = new List<Transition>()
                        };
                        jsonClass.states[childState.state.name] = state;

                        foreach (AnimatorStateTransition transition in childState.state.transitions)
                        {
                            Transition trans = new()
                            {
                                stateNodeTo = transition.destinationState.name,
                                trigger = ""
                            };
                            state.transitions.Add(trans);
                            int i = 0;
                            foreach (string input in transition.name.Split(";"))
                            {
                                if (i >= 3) break;
                                TransitionInput tInput = toTransitionInput(input);
                                if (null == tInput) continue;
                                switch (i)
                                {
                                    case 0:
                                        trans.input1 = tInput;
                                        break;
                                    case 1:
                                        trans.input2 = tInput;
                                        break;
                                    case 2:
                                        trans.input3 = tInput;
                                        break;
                                }
                                i += 1;
                            }

                            foreach (AnimatorCondition cond in transition.conditions)
                            {
                                trans.trigger = cond.parameter;
                            }
                        }
                    }
                }
                for (var i = 0; i < ac.parameters.Length; ++i)
                {
                    if (ac.parameters[i].name.StartsWith(Const.prefix))
                    {
                        ac.RemoveParameter(i);
                        break;
                    }
                }
                string json = Const.prefix + JsonConvert.SerializeObject(jsonClass, Formatting.None);
                ac.AddParameter(json, AnimatorControllerParameterType.Trigger);
                Debug.Log("AvatarAnimator data has been updated in Controller Parameters");
            }
            else
            {
                Debug.LogError("Animator must have a Controller");
            }
        }

        private TransitionInput toTransitionInput(string input)
        {
            TransitionInput tInput = null;
            bool valide = false;
            if (input.StartsWith(InputType.ControllerRight.ToString() + ".", StringComparison.CurrentCultureIgnoreCase))
            {
                tInput = new()
                {
                    name = input.Substring(InputType.ControllerRight.ToString().Length + 1),
                    type = InputType.ControllerRight,
                };
                valide = checkControllerInputName(input, tInput.name);
            }
            else if (input.StartsWith(InputType.ControllerLeft.ToString() + ".", StringComparison.CurrentCultureIgnoreCase))
            {
                tInput = new()
                {
                    name = input.Substring(InputType.ControllerLeft.ToString().Length + 1),
                    type = InputType.ControllerLeft,
                };
                valide = checkControllerInputName(input, tInput.name);
            }
            else if (input.StartsWith(InputType.Keyboard.ToString() + ".", StringComparison.CurrentCultureIgnoreCase))
            {
                string inputName = input.Substring(InputType.Keyboard.ToString().Length + 1);
                KeyCode code = KeyCode.None;
                try
                {
                    code = (KeyCode)Enum.Parse(typeof(KeyCode), inputName);
                    valide = true;
                }
                catch (Exception _)
                {
                    Debug.LogError($"'{input}' is not a valide keyboard Key");
                    valide = false;
                }
                tInput = new()
                {
                    name = inputName,
                    code = code,
                    type = InputType.Keyboard,
                };
            }
            else if ("" != input)
            {
                Debug.LogError($"'{input}' doesn't have a valide InputType");
                valide = false;
            }
            if (!valide) return null;
            return tInput;
        }
        private bool checkControllerInputName(string input, string name)
        {
            // https://docs.unity3d.com/6000.5/Documentation/Manual/xr_input.html
            switch (name)
            {
                case "primaryButton": // button pressed
                case "primaryTouch": // button touch but not pressed
                case "secondaryButton":
                case "secondaryTouch":
                case "gripButton":
                case "triggerButton":
                case "menuButton":
                case "primary2DAxisClick":
                case "primary2DAxisTouch":
                    return true;
                default:
                    Debug.LogError($"'{input}' is not a valide Controller button name");
                    return false;
            }
        }
    }
}

#endif