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
        public AnimatorType m_AnimatorType;
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
    public enum AnimatorType
    {
        Player,
        Npc,
        Object
    }
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
        public float m_ClipDuration;
        public bool m_ClipIsLooping;
        public float m_Speed;
        public List<Transition> m_Transitions;
    }

    [Serializable]
    public class Transition
    {
        public string m_NextState;
        public float m_Duration;
        public List<TransitionCondition> m_Conditions;
    }

    [Serializable]
    public class TransitionCondition
    {
        public string m_Name;
        public ConditionType m_Type;
        public ConditionMode m_Mode;
        public float m_Threshold;
    }

    [Serializable]
    public class TransitionConditionData
    {
        public int m_RandomMin;
        public int m_RandomMax;
        public List<ConditionInput> m_Inputs;
    }

    [Serializable]
    public class ConditionInput
    {
        public InputType m_Type;
        public string m_InputName;
        public KeyCode m_KeyCode;
        public ControllerInputs m_ControllerInput;
        public string m_InputName2;
        public KeyCode m_KeyCode2;
        public ControllerInputs m_ControllerInput2;
    }

    [Serializable]
    public class LayerData
    {
        public string m_Name;
        public Dictionary<string, StateNode> m_States;
        public int m_layerIndex;
        public string m_StartState;
    }

    [Serializable]
    public class AvatarAnimatorData
    {
        public List<LayerData> m_ListLayer;
        public Dictionary<string, TransitionConditionData> m_TransitionsData;
        public string m_Version;
        public string m_Date;
    }
}

