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

        [JsonProperty("Now")]
        public DateTime now;

        public PlayerStateChange() { }
        public PlayerStateChange(int layer, string state, float len, float speed, float? nTime = null)
        {
            m_Layer = layer; m_State = state;
            m_Duration = len; m_Speed = speed;
            now = DateTime.Now;
            m_nTime = nTime;
        }
    }

    public static class PlayerAnimator
    {
        public static event Action<PlayerStateChange> OnAvatarStateChanged;

        private static readonly List<EntityData> m_mirrorAnimators = new();
        private static EntityData m_player = null;
        private static string m_oldAvatar = "";

        /// <summary> Store values for level change </summary>
        private static readonly Dictionary<string, int> m_StoreValues = new();

        public static bool IsValid { get => null != m_player?.Container && m_player.IsValid; }
        public static byte Id { get => m_player.Id; }

        public static void Initialize()
        {
            PlayerScanner.OnAvatarChange += AvatarChange;
            PlayerScanner.OnAvatarSame += SameAvatar;
            MirrorScanner.OnNew += AddMirror;
            MirrorScanner.OnRemoved += RemoveMirror;
            MirrorScanner.OnClear += ClearMirrors;
            PlayerAnimatorCore.getPlayerHealth = () =>
            {
                var health = Player.RigManager.health;
                float healthValue = health.curr_Health / health.max_Health;
                return healthValue;
            };
            PlayerAnimatorCore.OnStateChange += (int layer, string state, bool playState) =>
            {
                PlayState(layer, state, playState: playState);
            };

            Logger.Msg($"Avatar animator data current version {AvatarAnimatorDataContainer.m_CurrentApiVersion}");
        }

        private static void SetPlayer(EntityData player)
        {
            m_player = player;
            PlayerAnimatorCore.SetAvatar(m_player.Animator, m_player.Container.m_Data);
        }

        private static void AvatarChange(EntityData player)
        {
            Logger.Dbg?.Info("OnAvatarChange");
            PlayerInput.Clear();
            // Fusion Change the Avatar to PolyBlank when level is loading and shortly after
            var barcode = player.Barcode.ToString();
            if (Const.PolyBlankBarcode != barcode)
            {
                m_StoreValues.Clear();
                PlayerAnimatorCore.Clear();
                m_oldAvatar = barcode;
            }
            else if (barcode == m_oldAvatar)
            {
                SameAvatar(player);
                return;
            }

            SetPlayer(player);
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
            // Avatar change in front of a mirror
            foreach (var mirror in m_mirrorAnimators) { mirror.UpdateAvatar(); }
            if (!IsValid) return;
            // Initialise and get layers/states values 
            PlayerAnimatorCore.Initialize();
        }

        private static void SameAvatar(EntityData player)
        {
            Logger.Dbg?.Info("OnAvatarSame");
            SetPlayer(player);
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
            foreach (var layer in PlayerAnimatorCore.Layers)
            {
                PlayState(layer.m_LayerIndex, layer.m_CurrentStateName, time: layer.m_startAt, updateValues: false);
            }
        }

        private static void AddMirror(EntityData data)
        {
            Logger.Dbg?.Info($"Data:({data.Barcode.ToString()} PlayerID:'{data.Id}') Player:({m_player.Barcode.ToString()} PlayerID:'{m_player.Id}')");
            if (data.Barcode != m_player.Barcode) return;
            if (data.Id != m_player.Id) return;
            Logger.Dbg?.Info($"Add Mirror to Player");
            m_mirrorAnimators.Add(data);

            if (!IsValid) return;
            // Set the Mirror entity States
            foreach (var layer in PlayerAnimatorCore.Layers)
            {
                var state = m_player.Animator.GetCurrentAnimatorStateInfo(layer.m_LayerIndex);
                data.Animator.Play(layer.m_CurrentStateName, layer.m_LayerIndex, state.normalizedTime);
            }
        }
        private static void RemoveMirror(EntityData data) { m_mirrorAnimators.Remove(data); }
        private static void ClearMirrors() { m_mirrorAnimators.Clear(); }

        public static void PlayState(int layer, string state, DateTime? time = null, bool updateValues = true, bool playState = true)
        {
            if (!IsValid) return;

            var layerObj = PlayerAnimatorCore.PlayState(layer, state, time);
            Logger.Msg($"Player: {m_player.Barcode.ToString()} Current state change to '{state}'");
            Logger.Dbg?.Data(JsonConvert.SerializeObject(layerObj.m_CurrentState, Formatting.None));

            var currState = layerObj.m_CurrentState;
            var ttime = layerObj.m_startAt ?? DateTime.Now;
            var nTime = Utils.ComputNTime(DateTime.Now, ttime, 0, currState.ClipDuration, currState.Speed);
            if (playState) m_player.Animator.Play(state, layer, nTime);
            foreach (var anim in m_mirrorAnimators) anim.Animator.Play(state, layer, nTime);
            OnAvatarStateChanged?.Invoke(new(layer, state, layerObj.m_CurrentState.ClipDuration, layerObj.m_CurrentState.Speed, nTime));

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

        public static void Update()
        {
            if (!IsValid) return;
            PlayerInput.Next();
            PlayerAnimatorCore.Update();
        }

        /// <summary> Get all Player states to be send </summary>
        /// <returns> List of all player states </returns>
        public static List<PlayerStateChange> GetPlayerStates()
        {
            List<PlayerStateChange> states = new();
            if (!IsValid) return states;
            foreach (var state in PlayerAnimatorCore.Layers)
            {
                var st = m_player.Animator.GetCurrentAnimatorStateInfo(state.m_LayerIndex);
                states.Add(new(state.m_LayerIndex, state.m_CurrentStateName, st.length, st.m_Speed, st.m_NormalizedTime));
            }
            return states;
        }
    }
}

