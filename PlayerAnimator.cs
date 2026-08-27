using BoneLib;
using Newtonsoft.Json;

namespace AvatarAnimator
{
    public static class PlayerAnimator
    {
        public class LayerAnimator
        {
            public readonly int m_LayerIndex;
            public StateNode m_CurrentState = null;
            public string m_CurrentStateName;
            public DateTime? m_Transition = null;
            public DateTime? m_ConditionDelay = null;
            public int? m_RandomValue = null;

            public LayerAnimator(int layerIndex) { m_LayerIndex = layerIndex; }
        }

        public static event Action<PlayerStateChange> OnAvatarStateChanged;

        private static readonly List<ScannedData> m_mirrorAnimators = new();
        private static readonly Dictionary<int, int> m_LayerIndexToIndex = new();
        private static readonly List<LayerAnimator> m_Layers = new();
        private static ScannedData m_player = null;

        public static bool HasAvatarAnimatorData { get => null != m_player?.Container; }
        public static byte Id { get => m_player.Id; }

        public static void Initialize()
        {
            Scanner.OnClear += () =>
            {
                m_mirrorAnimators.Clear();
            };
            Scanner.OnPlayerAvatarChange += (ScannedData player) =>
            {
                m_Layers.Clear();
                m_LayerIndexToIndex.Clear();
                m_player = player;
                if (!m_player.HasAvatarAnimatorData)
                {
                    Logger.Dbg.Info("PlayerAvatarChange: Current Animator doesn't have data");
                    return;
                }
                int i = 0;
                foreach (var layer in m_player.Data.m_ListLayer)
                {
                    m_Layers.Add(new(layer.m_layerIndex));
                    m_LayerIndexToIndex.Add(layer.m_layerIndex, i);
                    SetCurentState(layer.m_layerIndex, layer.m_StartState);
                    i += 1;
                }
                // Avatar change in front of a mirror
                foreach (var mirror in m_mirrorAnimators) { mirror.UpdateAvatar(); }
            };
            Scanner.OnPlayerAvatarSame += (ScannedData player) =>
            {
                m_player = player;
                if (!m_player.HasAvatarAnimatorData)
                {
                    Logger.Dbg.Info("PlayerAvatarSame: Current Animator doesn't have data");
                    return;
                }
                // Set back the Player state before level change
                foreach (var layer in m_Layers) { PlayState(layer.m_LayerIndex, layer.m_CurrentStateName); }
            };

            Scanner.OnNew += (ScannedData data) =>
            {
                Logger.Dbg?.Info($"Data:({data.Barcode.ToString()} PlayerID:'{data.Id}') Player:({m_player.Barcode.ToString()} PlayerID:'{m_player.Id}')");
                if (data.Barcode != m_player.Barcode) return;
                if (data.Id != m_player.Id) return;
                Logger.Dbg?.Info($"Add Mirror to Player");
                m_mirrorAnimators.Add(data);
                // Set the Mirror entity States
                foreach (var layer in m_Layers) { data.Animator.Play(layer.m_CurrentStateName, layer.m_LayerIndex); }
            };

            Scanner.OnRemoved += (ScannedData data) =>
            {
                m_mirrorAnimators.Remove(data);
            };
        }

        public static void SetCurentState(int layer, string state)
        {
            if (!m_LayerIndexToIndex.ContainsKey(layer)) return;
            int indexLayer = m_LayerIndexToIndex[layer];
            m_Layers[indexLayer].m_CurrentStateName = state;
            m_Layers[indexLayer].m_CurrentState = m_player.Data.m_ListLayer[indexLayer].m_States[state];
            foreach (var anim in m_mirrorAnimators) anim.Animator.Play(state, layer);
            Logger.Msg($"Player: {m_player.Barcode.ToString()} Current state change to {state}");
            Logger.Dbg?.Data(JsonConvert.SerializeObject(m_Layers[indexLayer].m_CurrentState, Formatting.None));
            OnAvatarStateChanged?.Invoke(new(layer, state));
        }

        public static void PlayState(int layer, string state)
        {
            m_player.Animator.Play(state, layer);
            SetCurentState(layer, state);
        }

        public static void Update()
        {
            if (!HasAvatarAnimatorData) return;
            foreach (var layer in m_Layers)
            {
                if (null != layer.m_Transition)
                {
                    if (DateTime.Now > layer.m_Transition)
                    {
                        layer.m_Transition = null;
                        layer.m_ConditionDelay = null;
                        layer.m_RandomValue = null;
                    }
                    return;
                }
                foreach (var trans in layer.m_CurrentState.m_Transitions)
                {
                    bool validate = true;
                    foreach (var cond in trans.m_Conditions)
                    {
                        validate = IsConditionValid(layer, cond);
                        if (!validate) break;
                    }
                    if (validate)
                    {
                        layer.m_Transition = Utils.DateTimeNowPlusSecs(trans.m_Duration);
                        SetCurentState(layer.m_LayerIndex, trans.m_NextState);
                        break;
                    }
                }
            }
        }

        public static bool IsConditionValid(LayerAnimator layer, TransitionCondition cond)
        {
            if (null == cond) return true;
            switch (cond.m_Type)
            {
                case ConditionType.Input:
                    {
                        TransitionConditionData data = m_player.Data.m_TransitionsData[cond.m_Name];
                        foreach (var input in data.m_Inputs)
                        {
                            if (PlayerInput.IsTriggered(input))
                            {
                                m_player.Animator.SetTrigger(cond.m_Name);
                                return true;
                            }
                        }
                        return false;
                    }
                case ConditionType.Health:
                    {
                        var health = Player.RigManager.health;
                        float healthValue = health.curr_Health / health.max_Health;
                        m_player.Animator.SetFloat(cond.m_Name, healthValue);
                        return Utils.Is(cond.m_Mode, healthValue, cond.m_Threshold);
                    }
                case ConditionType.Random:
                    {
                        if (null == layer.m_RandomValue)
                        {
                            TransitionConditionData data = m_player.Data.m_TransitionsData[cond.m_Name];
                            layer.m_RandomValue = Utils.RandomInt(data.m_RandomMin, data.m_RandomMax);
                            m_player.Animator.SetInteger(cond.m_Name, layer.m_RandomValue ?? 0);
                        }
                        return Utils.Is(cond.m_Mode, (float)layer.m_RandomValue, cond.m_Threshold);
                    }
                case ConditionType.Timer:
                    {
                        if (null == layer.m_ConditionDelay) layer.m_ConditionDelay = DateTime.Now;
                        if (DateTime.Now > layer.m_ConditionDelay?.AddSeconds(cond.m_Threshold))
                        {
                            m_player.Animator.SetTrigger(cond.m_Name);
                            return true;
                        }
                        return false;
                    }
                case ConditionType.WaitEndClip:
                    {
                        if (null == layer.m_ConditionDelay) layer.m_ConditionDelay = DateTime.Now;
                        if (DateTime.Now > layer.m_ConditionDelay?.AddSeconds(layer.m_CurrentState.m_ClipDuration / Math.Abs(layer.m_CurrentState.m_Speed)))
                        {
                            m_player.Animator.SetTrigger(cond.m_Name);
                            return true;
                        }
                        return false;
                    }
            }
            return true;
        }
    }

    public class PlayerStateChange
    {
        public int m_Layer;
        public string m_State;
        public PlayerStateChange(int layer, string state) { m_Layer = layer; m_State = state; }
    }
}

