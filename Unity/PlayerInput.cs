#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarAnimator
{
    public static class PlayerInput
    {
        public static bool IsTriggered(ConditionInput trans)
        {

            if (InputType.Unset == trans.Type) return false;
            switch (trans.Type)
            {
                case InputType.Keyboard:
                    if (Input.GetKeyDown(trans.KeyCode))
                    {
                        if (KeyCode.None == trans.KeyCode2) return true;
                        return Input.GetKey(trans.KeyCode2);
                    }
                    return false;
                case InputType.Controller:
                    return false;
            }
            return false;
        }
    }
}
#endif // UNITY_EDITOR
