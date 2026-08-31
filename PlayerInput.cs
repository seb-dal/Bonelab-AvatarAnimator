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
        public static bool IsTriggered(ConditionInput trans)
        {
            if (InputType.Unset == trans.Type) return false;
            switch (trans.Type)
            {
                case InputType.Keyboard:
                    if (KeyCode.None == trans.KeyCode2)
                        return LocalInput.GetKeyDown(trans.KeyCode);
                    return LocalInput.GetKeyDown(trans.KeyCode) && Input.GetKey(trans.KeyCode2);
                case InputType.Controller:
                    if (ControllerInputs.None == trans.ControllerInput2)
                        return LocalInput.GetControllerTriggerDown(trans.ControllerInput);
                    return LocalInput.GetControllerTriggerDown(trans.ControllerInput)
                        && LocalInput.GetControllerTrigger(trans.ControllerInput2);
            }
            return false;
        }
    }
}
