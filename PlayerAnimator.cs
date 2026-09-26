using BoneLib;
using Newtonsoft.Json;

namespace AvatarAnimator
{
    [Serializable]
    public class PlayerStateChange
    {
        [JsonProperty("Layer")]
        public int m_Layer;
        [JsonProperty("State")]
        public string m_State;

        [JsonProperty("nTime", NullValueHandling = NullValueHandling.Ignore)]
        public float? m_nTime = null;
        // In sec
        [JsonProperty("Duration", NullValueHandling = NullValueHandling.Ignore)]
        public float? m_Duration = null;
        [JsonProperty("Speed", NullValueHandling = NullValueHandling.Ignore)]
        public float? m_Speed = null;

        public PlayerStateChange() { }
        public PlayerStateChange(int layer, string state) { m_Layer = layer; m_State = state; }
        public PlayerStateChange(int layer, string state, float len, float speed)
        {
            m_Layer = layer; m_State = state;
            m_Duration = len; m_Speed = speed;
        }
        public PlayerStateChange(int layer, string state, float len, float speed, float nTime) : this(layer, state, len, speed)
        {
            m_nTime = nTime;
        }
    }

    public static class PlayerAnimator
    {
        public class AnimatorLayer
        {
            public readonly int m_LayerIndex;
            public StateNode m_CurrentState = null;
            public string m_CurrentStateName;
            public DateTime? m_Transition = null;
            public DateTime? m_ConditionDelayTimer = null;
            public DateTime? m_ConditionDelayWaitEndClip = null;

            public AnimatorLayer(int layerIndex) { m_LayerIndex = layerIndex; }
        }

        public static event Action<PlayerStateChange> OnAvatarStateChanged;

        private static readonly List<ScannedData> m_mirrorAnimators = new();
        private static readonly Dictionary<int, int> m_LayerIndexToIndex = new();
        private static readonly List<AnimatorLayer> m_Layers = new();
        private static ScannedData m_player = null;

        /// <summary> Store values for level change </summary>
        private static readonly Dictionary<string, int> m_StoreValues = new();

        public static bool IsValid { get => null != m_player?.Container && m_player.IsValid; }
        public static byte Id { get => m_player.Id; }

        public static void Initialize()
        {
            MirrorScanner.OnClear += () =>
            {
                m_mirrorAnimators.Clear();
            };
            PlayerScanner.OnAvatarChange += (ScannedData player) =>
            {
                Logger.Dbg?.Info("OnAvatarChange");
                PlayerInput.Clear();
                m_StoreValues.Clear();
                m_Layers.Clear();
                m_LayerIndexToIndex.Clear();

                m_player = player;
                if (!m_player.HasAvatarAnimatorData)
                {
                    Logger.Dbg?.Info("PlayerAvatarChange: Current Animator doesn't have data");
                    return;
                }
                // Initialise Values
                foreach (var trans in m_player.Data.TransitionsData)
                {
                    switch (trans.Value.Type)
                    {
                        case ConditionType.Input:
                            PlayerInput.Initialise(trans.Value);
                            break;
                        case ConditionType.Random:
                        case ConditionType.Cyclic:
                            m_StoreValues.Add(trans.Key, -1);
                            break;
                        default: break;
                    }
                }
                if (!IsValid) return;
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
            PlayerScanner.OnAvatarSame += (ScannedData player) =>
            {
                Logger.Dbg?.Info("OnAvatarSame");
                m_player = player;
                if (!m_player.HasAvatarAnimatorData)
                {
                    Logger.Dbg?.Info("PlayerAvatarSame: Current Animator doesn't have data");
                    return;
                }
                if (!IsValid) return;
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

            MirrorScanner.OnNew += (ScannedData data) =>
            {
                Logger.Dbg?.Info($"Data:({data.Barcode.ToString()} PlayerID:'{data.Id}') Player:({m_player.Barcode.ToString()} PlayerID:'{m_player.Id}')");
                if (data.Barcode != m_player.Barcode) return;
                if (data.Id != m_player.Id) return;
                Logger.Dbg?.Info($"Add Mirror to Player");
                m_mirrorAnimators.Add(data);
                // Set the Mirror entity States
                foreach (var layer in m_Layers)
                {
                    var state = m_player.Animator.GetCurrentAnimatorStateInfo(layer.m_LayerIndex);
                    data.Animator.Play(layer.m_CurrentStateName, layer.m_LayerIndex, state.normalizedTime);
                }
            };

            MirrorScanner.OnRemoved += (ScannedData data) =>
            {
                m_mirrorAnimators.Remove(data);
            };

            Logger.Msg($"Avatar animator data current version {AvatarAnimatorDataContainer.m_CurrentVersion}");
        }

        public static void SetCurentState(int layer, string state, bool updateValues = true)
        {
            if (!IsValid) return;
            if (!m_LayerIndexToIndex.ContainsKey(layer)) return;
            int indexLayer = m_LayerIndexToIndex[layer];
            var layerObj = m_Layers[indexLayer];
            layerObj.m_CurrentStateName = state;
            layerObj.m_CurrentState = m_player.Data.ListLayer[indexLayer].States[state];
            foreach (var anim in m_mirrorAnimators) anim.Animator.Play(state, layer);
            Logger.Msg($"Player: {m_player.Barcode.ToString()} Current state change to '{state}'");
            Logger.Dbg?.Data(JsonConvert.SerializeObject(layerObj.m_CurrentState, Formatting.None));
            OnAvatarStateChanged?.Invoke(new(layer, state, layerObj.m_CurrentState.ClipDuration, layerObj.m_CurrentState.Speed));

            if (!updateValues) return;
            // Only update values if they will be used
            HashSet<string> done = new(); // and only once
            foreach (var trans in layerObj.m_CurrentState.Transitions)
            {
                foreach (var cond in trans.Conditions)
                {
                    if (done.Contains(cond.Name)) continue;
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
                    done.Add(cond.Name);
                }
            }
        }

        public static void PlayState(int layer, string state, bool updateValues = true)
        {
            if (!IsValid) return;
            m_player.Animator.Play(state, layer);
            SetCurentState(layer, state, updateValues);
        }

        public static void Update()
        {
            if (!IsValid) return;
            PlayerInput.Next();
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

        private static bool IsConditionValid(AnimatorLayer layer, TransitionCondition cond)
        {
            if (null == cond) return true;
            switch (cond.Type)
            {
                case ConditionType.Input:
                    {
                        TransitionConditionData data = m_player.Data.TransitionsData[cond.Name];
                        foreach (var input in data.Inputs)
                        {
                            if (PlayerInput.IsTriggered(input))
                            {
                                m_player.Animator.SetTrigger(cond.Name);
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

        /// <summary> Get all Player states to be send </summary>
        /// <returns> List of all player states </returns>
        public static List<PlayerStateChange> GetPlayerStates()
        {
            List<PlayerStateChange> states = new();
            foreach (var state in m_Layers)
            {
                var st = m_player.Animator.GetCurrentAnimatorStateInfo(state.m_LayerIndex);
                states.Add(new(state.m_LayerIndex, state.m_CurrentStateName, st.length, st.m_Speed, st.m_NormalizedTime));
            }
            return states;
        }
    }
}

