#if UNITY_EDITOR || IS_AVATAR_ANIMATOR_CORE_MOD

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarAnimator
{
    public delegate void StateChange(int layer, string state, bool playState);
    public delegate float PlayerHealthGetterFunc();

    public static class PlayerAnimatorCore
    {
        public class AnimatorLayer
        {
            public readonly int m_LayerIndex;
            public StateNode m_CurrentState = null;
            public string m_CurrentStateName;
            public DateTime? m_Transition = null;
            public DateTime? m_ConditionDelayTimer = null;
            public DateTime? m_ConditionDelayWaitEndClip = null;

            public DateTime? m_startAt;

            public AnimatorLayer(int layerIndex) { m_LayerIndex = layerIndex; }
        }


        private static readonly Dictionary<int, int> m_LayerIndexToIndex = new();
        private static readonly List<AnimatorLayer> m_Layers = new();
        public static List<AnimatorLayer> Layers => m_Layers;

        private static Animator m_Animator;
        private static AvatarAnimatorData m_Data;

        public static event StateChange OnStateChange;
        public static PlayerHealthGetterFunc getPlayerHealth = () => 1.0f;


        public static void SetAvatar(Animator animator, AvatarAnimatorData data)
        {
            m_Animator = animator;
            m_Data = data;
        }

        public static bool IsInitialized() => m_LayerIndexToIndex.Count > 0;

        public static void Initialize()
        {
            int i = 0;
            foreach (var layer in m_Data.ListLayer)
            {
                Layers.Add(new(layer.LayerIndex));
                m_LayerIndexToIndex.Add(layer.LayerIndex, i);
                OnStateChange?.Invoke(layer.LayerIndex, layer.StartState, false);
                i += 1;
            }
        }

        public static void Clear()
        {
            Layers.Clear();
            m_LayerIndexToIndex.Clear();
        }

        public static AnimatorLayer PlayState(int layer, string state, DateTime? time = null)
        {
            if (!m_LayerIndexToIndex.ContainsKey(layer)) return null;

            int indexLayer = m_LayerIndexToIndex[layer];
            var layerObj = m_Layers[indexLayer];
            layerObj.m_startAt = time ?? DateTime.Now;
            layerObj.m_CurrentStateName = state;
            layerObj.m_CurrentState = m_Data.ListLayer[indexLayer].States[state];
            return layerObj;
        }

        public static AnimatorLayer GetState(int layer)
        {
            int indexLayer = m_LayerIndexToIndex[layer];
            return m_Layers[indexLayer];
        }

        public static void Update()
        {
            foreach (var layer in Layers)
            {
                if (null != layer.m_Transition)
                {
                    if (DateTime.Now > layer.m_Transition)
                    {
                        layer.m_Transition = null;
                        layer.m_ConditionDelayTimer = null;
                        layer.m_ConditionDelayWaitEndClip = null;
                    }
                    return;
                }
                foreach (var trans in layer.m_CurrentState.Transitions)
                {
                    bool validate = true;
                    foreach (var cond in trans.Conditions)
                    {
                        validate = IsConditionValid(layer, cond);
                        if (!validate) break;
                    }
                    if (validate || trans.HasExitTime)
                    {
                        layer.m_Transition = Utils.DateTimeNowPlusSecs(trans.HasExitTime ? trans.ExitTime : trans.Duration);
                        OnStateChange?.Invoke(layer.m_LayerIndex, trans.NextState, false); // already playing by fulfilling all conditions
                        break;
                    }
                }
            }
        }

        private static bool IsConditionValid(AnimatorLayer layer, TransitionCondition cond)
        {
            if (null == cond) return true;
            switch (cond.Type)
            {
                case ConditionType.Input:
                    {
                        TransitionConditionData data = m_Data.TransitionsData[cond.Name];
                        foreach (var input in data.Inputs)
                        {
                            if (PlayerInput.IsTriggered(input))
                            {
                                m_Animator.SetTrigger(cond.Name);
                                return true;
                            }
                        }
                        return false;
                    }
                case ConditionType.Health:
                    {
                        var healthValue = getPlayerHealth();
                        m_Animator.SetFloat(cond.Name, healthValue);
                        return Utils.Is(cond.Mode, healthValue, cond.Threshold);
                    }
                case ConditionType.Random:
                    {
                        return Utils.Is(cond.Mode, m_Animator.GetInteger(cond.Name), (int)cond.Threshold);
                    }
                case ConditionType.Timer:
                    {
                        if (null == layer.m_ConditionDelayTimer) layer.m_ConditionDelayTimer = DateTime.Now.AddSeconds(cond.Threshold);
                        if (DateTime.Now > layer.m_ConditionDelayTimer)
                        {
                            m_Animator.SetTrigger(cond.Name);
                            return true;
                        }
                        return false;
                    }
                case ConditionType.WaitEndClip:
                    {
                        if (null == layer.m_ConditionDelayWaitEndClip) layer.m_ConditionDelayWaitEndClip = DateTime.Now.AddSeconds(layer.m_CurrentState.ClipDuration / Math.Abs(layer.m_CurrentState.Speed));
                        if (DateTime.Now > layer.m_ConditionDelayWaitEndClip)
                        {
                            m_Animator.SetTrigger(cond.Name);
                            return true;
                        }
                        return false;
                    }
                case ConditionType.Cyclic:
                    {
                        return Utils.Is(cond.Mode, m_Animator.GetInteger(cond.Name), (int)cond.Threshold);
                    }
            }
            return true;
        }
    }
}

#endif // UNITY_EDITOR || ENABLE_MOD