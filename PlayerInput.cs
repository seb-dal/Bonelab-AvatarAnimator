using UnityEngine;
using Input = UnityEngine.Input;
using BoneLib;

namespace AvatarAnimator
{
    /// <summary>
    /// Inputs ___Down() and ___Up() cannot be use safely because it is shared across the game and mods and the first one to use it consume the value.
    /// </summary>
    public static class LocalInput
    {
        private static readonly HashSet<KeyCode> m_KeyboardInputs = new();
        private static readonly HashSet<ControllerInputs> m_ControllerInputs = new();
        public static void Update()
        {
            foreach (var code in m_KeyboardInputs)
            {
                if (!Input.GetKey(code)) m_KeyboardInputs.Remove(code);
            }
            foreach (var code in m_ControllerInputs)
            {
                if (!GetControllerTrigger(code)) m_ControllerInputs.Remove(code);
            }
        }
        public static bool GetKeyDown(KeyCode code)
        {
            if (Input.GetKey(code) && !m_KeyboardInputs.Contains(code))
            {
                m_KeyboardInputs.Add(code);
                return true;
            }
            return false;
        }

        public static bool GetControllerTriggerDown(ControllerInputs input)
        {
            if (GetControllerTrigger(input) && !m_ControllerInputs.Contains(input))
            {
                m_ControllerInputs.Add(input);
                return true;
            }
            return false;
        }

        public static bool GetControllerTrigger(ControllerInputs input)
        {
            return input switch
            {
                ControllerInputs.A => Player.RightController.GetAButton(),
                ControllerInputs.B => Player.RightController.GetBButton(),
                ControllerInputs.X => Player.LeftController.GetAButton(),
                ControllerInputs.Y => Player.LeftController.GetBButton(),
                ControllerInputs.LeftGrip => Player.LeftController.GetGripForce() > 0.8,
                ControllerInputs.RightGrip => Player.RightController.GetGripForce() > 0.8,
                ControllerInputs.LeftTrigger => Player.LeftController.GetGrabbedState(),
                ControllerInputs.RightTrigger => Player.RightController.GetGrabbedState(),
                ControllerInputs.Menu => Player.LeftController.GetMenuButton(),
                ControllerInputs.SecondaryMenu => Player.RightController.GetSecondaryMenuButtonDown(),
                ControllerInputs.LeftThumbStick => Player.LeftController.GetThumbStick(),
                ControllerInputs.RightThumbStick => Player.RightController.GetThumbStick(),
                ControllerInputs.LeftTouchPad => Player.LeftController.GetTouchPad(),
                ControllerInputs.RightTouchPad => Player.RightController.GetTouchPad(),
                _ => throw new Exception($"Invalid Controller input name '{input}'")
            };
        }
    }

    public class PlayerInput
    {
        private static long m_frame = 0;
        /// <summary> For sharing input across multiple transitions </summary>
        private static readonly Dictionary<string, long> m_inputPressed = new();

        public static void Clear()
        {
            m_frame = 0;
            m_inputPressed.Clear();
        }
        public static void Initialise(TransitionConditionData data)
        {
            foreach (var input in data.Inputs) m_inputPressed.Add(input.InputName, -1);
        }

        public static void Next() { m_frame += 1; }

        public static bool IsTriggered(ConditionInput trans)
        {
            if (InputType.Unset == trans.Type) return false;
            switch (trans.Type)
            {
                case InputType.Keyboard:
                    if (LocalInput.GetKeyDown(trans.KeyCode) || m_frame == m_inputPressed[trans.InputName])
                    {
                        m_inputPressed[trans.InputName] = m_frame;
                        if (KeyCode.None == trans.KeyCode2) return true;
                        return Input.GetKey(trans.KeyCode2);
                    }
                    return false;
                case InputType.Controller:
                    if (LocalInput.GetControllerTriggerDown(trans.ControllerInput) || m_frame == m_inputPressed[trans.InputName])
                    {
                        m_inputPressed[trans.InputName] = m_frame;
                        if (ControllerInputs.None == trans.ControllerInput2) return true;
                        return LocalInput.GetControllerTrigger(trans.ControllerInput2);
                    }
                    return false;
            }
            return false;
        }
    }
}
