using BoneLib;
using Newtonsoft.Json;

namespace AvatarAnimator
{
    public class PlayerStateChange
    {
        public int m_Layer;
        public string m_State;
        public PlayerStateChange(int layer, string state) { m_Layer = layer; m_State = state; }
    }
    public static class PlayerAnimator
    {
        public class LayerAnimator
        {
            public readonly int m_LayerIndex;
            public StateNode m_CurrentState = null;
            public string m_CurrentStateName;
            public DateTime? m_Transition = null;
            public DateTime? m_ConditionDelayTimer = null;
            public DateTime? m_ConditionDelayWaitEndClip = null;

            public LayerAnimator(int layerIndex) { m_LayerIndex = layerIndex; }
        }

        public static event Action<PlayerStateChange> OnAvatarStateChanged;

        private static readonly List<ScannedData> m_mirrorAnimators = new();
        private static readonly Dictionary<int, int> m_LayerIndexToIndex = new();
        private static readonly List<LayerAnimator> m_Layers = new();
        private static ScannedData m_player = null;

        private static long m_frame = 0;
        /// <summary> For sharing input across multiple transitions </summary>
        private static readonly Dictionary<string, long> m_inputPressed = new();
        /// <summary> Store values for level change </summary>
        private static readonly Dictionary<string, int> m_StoreValues = new();

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
                m_frame = 0;
                m_inputPressed.Clear();
                m_StoreValues.Clear();
                m_Layers.Clear();
                m_LayerIndexToIndex.Clear();

                m_player = player;
                if (!m_player.HasAvatarAnimatorData)
                {
                    Logger.Dbg.Info("PlayerAvatarChange: Current Animator doesn't have data");
                    return;
                }
                // Initialise Values
                foreach (var trans in m_player.Data.TransitionsData)
                {
                    switch (trans.Value.Type)
                    {
                        case ConditionType.Input:
                            foreach (var input in trans.Value.Inputs) m_inputPressed.Add(input.InputName, -1);
                            break;
                        case ConditionType.Random:
                        case ConditionType.Cyclic:
                            m_StoreValues.Add(trans.Key, -1);
                            break;
                        default: break;
                    }
                }
                // Initialise and get layers/states values 
                int i = 0;
                foreach (var layer in m_player.Data.ListLayer)
                {
                    m_Layers.Add(new(layer.LayerIndex));
                    m_LayerIndexToIndex.Add(layer.LayerIndex, i);
                    SetCurentState(layer.LayerIndex, layer.StartState);
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
                // Restore Values
                foreach (var trans in m_player.Data.TransitionsData)
                {
                    switch (trans.Value.Type)
                    {
                        case ConditionType.Random:
                        case ConditionType.Cyclic:
                            m_player.Animator.SetInteger(trans.Key, m_StoreValues[trans.Key]);
                            break;
                        default: break;
                    }
                }
                // Set back the Player state before level change
                foreach (var layer in m_Layers) { PlayState(layer.m_LayerIndex, layer.m_CurrentStateName, false); }
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

        public static void SetCurentState(int layer, string state, bool updateValues = true)
        {
            if (!m_LayerIndexToIndex.ContainsKey(layer)) return;
            int indexLayer = m_LayerIndexToIndex[layer];
            m_Layers[indexLayer].m_CurrentStateName = state;
            m_Layers[indexLayer].m_CurrentState = m_player.Data.ListLayer[indexLayer].States[state];
            foreach (var anim in m_mirrorAnimators) anim.Animator.Play(state, layer);
            Logger.Msg($"Player: {m_player.Barcode.ToString()} Current state change to {state}");
            Logger.Dbg?.Data(JsonConvert.SerializeObject(m_Layers[indexLayer].m_CurrentState, Formatting.None));
            OnAvatarStateChanged?.Invoke(new(layer, state));

            if (!updateValues) return;
            // Only update values if there will be used
            foreach (var trans in m_Layers[indexLayer].m_CurrentState.Transitions)
            {
                foreach (var cond in trans.Conditions)
                {
                    switch (cond.Type)
                    {
                        case ConditionType.Random:
                            {
                                TransitionConditionData data = m_player.Data.TransitionsData[cond.Name];
                                var value = Utils.RandomInt(data.Min, data.Max);
                                m_StoreValues[cond.Name] = value;
                                m_player.Animator.SetInteger(cond.Name, value);
                            }
                            break;
                        case ConditionType.Cyclic:
                            {
                                TransitionConditionData data = m_player.Data.TransitionsData[cond.Name];
                                var value = (m_player.Animator.GetInteger(cond.Name) + 1) % data.Max;
                                m_StoreValues[cond.Name] = value;
                                m_player.Animator.SetInteger(cond.Name, value);
                            }
                            break;
                    }
                }
            }
        }

        public static void PlayState(int layer, string state, bool updateValues = true)
        {
            m_player.Animator.Play(state, layer);
            SetCurentState(layer, state, updateValues);
        }

        public static void Update()
        {
            if (!HasAvatarAnimatorData) return;
            m_frame += 1;
            foreach (var layer in m_Layers)
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
                        SetCurentState(layer.m_LayerIndex, trans.NextState);
                        break;
                    }
                }
            }
        }

        private static bool IsConditionValid(LayerAnimator layer, TransitionCondition cond)
        {
            if (null == cond) return true;
            switch (cond.Type)
            {
                case ConditionType.Input:
                    {
                        TransitionConditionData data = m_player.Data.TransitionsData[cond.Name];
                        foreach (var input in data.Inputs)
                        {
                            if (m_frame == m_inputPressed[input.InputName]) return true; // for shared input across multiple transition
                            if (PlayerInput.IsTriggered(input))
                            {
                                m_player.Animator.SetTrigger(cond.Name);
                                m_inputPressed[input.InputName] = m_frame;
                                return true;
                            }
                        }
                        return false;
                    }
                case ConditionType.Health:
                    {
                        var health = Player.RigManager.health;
                        float healthValue = health.curr_Health / health.max_Health;
                        m_player.Animator.SetFloat(cond.Name, healthValue);
                        return Utils.Is(cond.Mode, healthValue, cond.Threshold);
                    }
                case ConditionType.Random:
                    {
                        return Utils.Is(cond.Mode, m_player.Animator.GetInteger(cond.Name), (int)cond.Threshold);
                    }
                case ConditionType.Timer:
                    {
                        if (null == layer.m_ConditionDelayTimer) layer.m_ConditionDelayTimer = DateTime.Now.AddSeconds(cond.Threshold);
                        if (DateTime.Now > layer.m_ConditionDelayTimer)
                        {
                            m_player.Animator.SetTrigger(cond.Name);
                            return true;
                        }
                        return false;
                    }
                case ConditionType.WaitEndClip:
                    {
                        if (null == layer.m_ConditionDelayWaitEndClip) layer.m_ConditionDelayWaitEndClip = DateTime.Now.AddSeconds(layer.m_CurrentState.ClipDuration / Math.Abs(layer.m_CurrentState.Speed));
                        if (DateTime.Now > layer.m_ConditionDelayWaitEndClip)
                        {
                            m_player.Animator.SetTrigger(cond.Name);
                            return true;
                        }
                        return false;
                    }
                case ConditionType.Cyclic:
                    {
                        return Utils.Is(cond.Mode, m_player.Animator.GetInteger(cond.Name), (int)cond.Threshold);
                    }
            }
            return true;
        }
    }
}

