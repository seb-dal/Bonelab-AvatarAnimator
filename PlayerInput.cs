using UnityEngine;
using Input = UnityEngine.Input;
using BoneLib;

namespace AvatarAnimator
{
    public class PlayerInput
    {
        public static bool IsTriggered(ConditionInput trans)
        {
            if (InputType.Unset == trans.Type) return false;
            switch (trans.Type)
            {
                case InputType.Keyboard:
                    if (KeyCode.None == trans.KeyCode2)
                        return Input.GetKeyDown(trans.KeyCode);
                    return Input.GetKeyDown(trans.KeyCode) && Input.GetKey(trans.KeyCode2);
                case InputType.Controller:
                    if (ControllerInputs.None == trans.ControllerInput2)
                        return ControlerTriggerDown(trans.ControllerInput);
                    return ControlerTriggerDown(trans.ControllerInput)
                        && ControlerTriggerDown(trans.ControllerInput2, false);
            }
            return false;
        }

        public static bool ControlerTriggerDown(ControllerInputs input, bool down = true)
        {
            switch (input)
            {
                case ControllerInputs.A:
                    if (down) return Player.RightController.GetAButtonDown();
                    return Player.RightController.GetAButton();

                case ControllerInputs.B:
                    if (down) return Player.RightController.GetBButtonDown();
                    return Player.RightController.GetBButton();

                case ControllerInputs.X:
                    if (down) return Player.LeftController.GetAButtonDown();
                    return Player.LeftController.GetAButton();

                case ControllerInputs.Y:
                    if (down) return Player.LeftController.GetBButtonDown();
                    return Player.LeftController.GetBButton();

                case ControllerInputs.LeftGrip:
                    return Player.LeftController.GetGripForce() > 0.8;

                case ControllerInputs.RightGrip:
                    return Player.RightController.GetGripForce() > 0.8;

                case ControllerInputs.LeftTrigger:
                    return Player.LeftController.GetGrabbedState();

                case ControllerInputs.RightTrigger:
                    return Player.RightController.GetGrabbedState();

                case ControllerInputs.Menu:
                    if (down) return Player.LeftController.GetMenuButtonDown();
                    return Player.LeftController.GetMenuButton();

                case ControllerInputs.SecondaryMenu:
                    if (down) return Player.RightController.GetSecondaryMenuButtonDown();
                    return Player.RightController.GetSecondaryMenuButtonDown();

                case ControllerInputs.LeftThumbStick:
                    if (down) return Player.LeftController.GetThumbStickDown();
                    return Player.LeftController.GetThumbStick();

                case ControllerInputs.RightThumbStick:
                    if (down) return Player.RightController.GetThumbStickDown();
                    return Player.RightController.GetThumbStick();

                case ControllerInputs.LeftTouchPad:
                    if (down) return Player.LeftController.GetTouchPadDown();
                    return Player.LeftController.GetTouchPad();

                case ControllerInputs.RightTouchPad:
                    if (down) return Player.RightController.GetTouchPadDown();
                    return Player.RightController.GetTouchPad();
            }
            Logger.Warn($"Invalid Controller input name '{input}'");
            return false;
        }
    }
}
