using MelonLoader;
using UnityEngine;
using BoneLib;
using Input = UnityEngine.Input;
using Il2CppSLZ.Marrow;
using Newtonsoft.Json;
using UnityEngine.InputSystem.XR;

namespace AvatarAnimator
{
    public static class BuildInfo
    {
        public const string Name = "AvatarAnimator";
        public const string Author = "D";
        public const string Company = "";
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class Core : MelonMod
    {
        public static MelonLogger.Instance Logger;
        // List of current Avatar Animators
        private readonly List<Animator> avatarAnimators = new();
        // List of mirror that doesn't have an Avatar reflexion yet
        private readonly List<Mirror> mirrors = new();
        // Disable OnUpdate() execution
        private bool enabled = false;
        // AnimatorController json graph of the current Avatar
        private AvatarAnimatorJson graph = null;
        // Current State of AnimatorController graph of the current Avatar
        private StateNode currentState = null;
        private string currentStateName;
        // Controller
        private XRController rightHand;
        private XRController leftHand;

        private int updateCounterMirror;
        private static int maxUpdateCounterMirror = 60;

        public override void OnInitializeMelon()
        {
            Logger = LoggerInstance;
            Hooking.OnSwitchAvatarPostfix += OnAvatarChanged;
            Hooking.OnLevelLoading += (LevelInfo _) =>
            {
                enabled = false;
            };
            Hooking.OnLevelLoaded += (LevelInfo _) =>
            {
                enabled = true;
            };
            rightHand = XRController.rightHand;
            leftHand = XRController.leftHand;
        }

        public override void OnUpdate()
        {
            if (!enabled || null == graph || avatarAnimators.Count() <= 0) return;

            // try to get Avatar Animator from mirror
            if (mirrors.Count > 0)
            {
                if (updateCounterMirror == 0)
                {
                    for (int i = mirrors.Count - 1; i >= 0; i--)
                    {
                        Mirror m = mirrors[i];
                        if (null != m.Reflection?.gameObject)
                        {
                            Animator animMirror = m.Reflection.gameObject.GetComponentInChildren<Animator>();
                            animMirror.Play(currentStateName, 0, 0);
                            avatarAnimators.Add(animMirror);
                            mirrors.RemoveAt(i);
                        }
                    }
                }
                updateCounterMirror = (updateCounterMirror + 1) % maxUpdateCounterMirror;
            }

            foreach (Transition trans in currentState.transitions)
            {
                if ("" == trans.trigger)
                {
                    setCurentState(trans.stateNodeTo);
                    break;
                }

                if (isTriggered(trans.input1) || isTriggered(trans.input2) || isTriggered(trans.input3))
                {
                    foreach (Animator anim in avatarAnimators)
                    {
                        anim.SetTrigger(trans.trigger);
                    }
                    setCurentState(trans.stateNodeTo);
                    break;
                }
            }
        }

        private void OnAvatarChanged(Il2CppSLZ.VRMK.Avatar avatar)
        {
            avatarAnimators.Clear();
            mirrors.Clear();
            var avatarAnimator = Player.Avatar.gameObject.GetComponentInChildren<Animator>();
            avatarAnimators.Add(avatarAnimator);

            foreach (AnimatorControllerParameter param in avatarAnimator.parameters)
            {
                if (param.name.StartsWith(Const.prefix))
                {
                    string json = param.name.Substring(Const.prefix.Length);
                    graph = JsonConvert.DeserializeObject<AvatarAnimatorJson>(json);
                    setCurentState(graph.startState);
                    Logger.Msg("Animator Controller Json Data found");
                    break;
                }
            }
            foreach (Mirror m in GameObject.FindObjectsOfType<Mirror>())
            {
                if (null != m.Reflection?.gameObject)
                    avatarAnimators.Add(m.Reflection.gameObject.GetComponentInChildren<Animator>());
                else
                    mirrors.Add(m);
            }
            Logger.Msg($"{avatarAnimators.Count} Animator found and {mirrors.Count} Mirrors");
        }

        private void setCurentState(string state)
        {
            currentStateName = state;
            currentState = graph.states[state];
            Logger.Msg($"Current state change to {state}");
        }

        private bool isTriggered(TransitionInput trans)
        {
            if (null == trans) return false;
            switch (trans.type)
            {
                case InputType.Keyboard:
                    return Input.GetKeyDown(trans.code ?? KeyCode.None);
                case InputType.ControllerRight:
                    return (rightHand[trans.name] as UnityEngine.InputSystem.Controls.ButtonControl)?.wasPressedThisFrame ?? false;
                case InputType.ControllerLeft:
                    return (leftHand[trans.name] as UnityEngine.InputSystem.Controls.ButtonControl)?.wasPressedThisFrame ?? false;
            }
            return false;
        }
    }
}
