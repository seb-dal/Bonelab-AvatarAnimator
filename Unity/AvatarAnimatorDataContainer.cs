using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AvatarAnimator
{
    [Serializable]
    public class AvatarAnimatorDataContainer : MonoBehaviour
    {
        public Animator m_Animator;
        public AvatarAnimatorData m_Data;
        public string m_CompactedData;

        public void PopulateData(Animator anim, AvatarAnimatorData data)
        {
            m_Animator = anim;
            m_Data = data;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void CompactData()
        {
            if (null == m_Data) throw new Exception("Cannot Compact null Data");
            m_CompactedData = JsonConvert.SerializeObject(m_Data, Formatting.None);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        public void UncompactData()
        {
            if (null == m_CompactedData) throw new Exception("Cannot Uncompact null CompactedData");
            m_Data = JsonConvert.DeserializeObject<AvatarAnimatorData>(m_CompactedData);
        }
    }
}


namespace AvatarAnimator
{
    public enum InputType
    {
        Unset,
        Keyboard,
        Controller
    }
    public enum ConditionMode
    {
        Unset,
        If,
        IfNot,
        Greater,
        Less,
        Equals,
        NotEqual
    }
    public enum ConditionType
    {
        Unset,
        Input,
        Random,
        Health,
        Timer,
        WaitEndClip,
        Cyclic,
    }

    public enum ControllerInputs
    {
        None,
        A, B,
        X, Y,
        LeftGrip, RightGrip,
        LeftTrigger, RightTrigger,
        Menu, SecondaryMenu,
        LeftThumbStick, RightThumbStick,
        LeftTouchPad, RightTouchPad,
    }

    [Serializable]
    public class StateNode
    {
        public float ClipDuration;
        public bool ClipIsLooping;
        public float Speed;
        public List<Transition> Transitions;
    }

    [Serializable]
    public class Transition
    {
        public string NextState;
        public float Duration;
        public float ExitTime;
        public bool HasExitTime;
        public List<TransitionCondition> Conditions;
    }

    [Serializable]
    public class TransitionCondition
    {
        public string Name;
        public ConditionType Type;
        public ConditionMode Mode;
        public float Threshold;
    }

    [Serializable]
    public class TransitionConditionData
    {
        public ConditionType Type;
        public int Min;
        public int Max;
        public List<ConditionInput> Inputs;
    }

    [Serializable]
    public class ConditionInput
    {
        public InputType Type;
        public string InputName;
        public KeyCode KeyCode;
        public ControllerInputs ControllerInput;
        public string InputName2;
        public KeyCode KeyCode2;
        public ControllerInputs ControllerInput2;
    }

    [Serializable]
    public class LayerData
    {
        public string Name;
        public Dictionary<string, StateNode> States;
        public int LayerIndex;
        public string StartState;
    }

    [Serializable]
    public class AvatarAnimatorData
    {
        public List<LayerData> ListLayer;
        public Dictionary<string, TransitionConditionData> TransitionsData;
        public string Version;
        public string Date;
    }
}

