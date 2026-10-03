using Newtonsoft.Json;
using LabFusion.Player;
using LabFusion.Entities;

namespace AvatarAnimator.FusionLab
{
    [Serializable]
    public class OtherPlayerStates
    {
        [JsonProperty("SmallId")]
        public byte m_smallId;
        [JsonProperty("States")]
        public List<PlayerStateChange> m_States = new();

        public OtherPlayerStates(byte smallId, PlayerStateChange change = null, List<PlayerStateChange> states = null)
        {
            m_smallId = smallId;
            if (null != change) m_States.Add(change);
            if (null != states) m_States.AddRange(states);
        }

        public static string Serialize(OtherPlayerStates obj) => JsonConvert.SerializeObject(obj, Formatting.None);
        public static OtherPlayerStates Deserialize(string json) => JsonConvert.DeserializeObject<OtherPlayerStates>(json);
    }

    public class OtherPlayerState
    {
        public int m_Layer;
        public string m_State;
        public OtherPlayerState(int layer, string state) { m_Layer = layer; m_State = state; }
    }

    public class OtherPlayerAnimator
    {
        private readonly List<EntityData> m_mirrorAnimators = new();
        private readonly Dictionary<int, OtherPlayerState> m_States = new();
        private readonly OtherPlayerData m_player = null;

        public OtherPlayerAnimator(NetworkPlayer player, PlayerID playerId)
        {
            m_player = OtherPlayerData.Create(player, playerId);
            UpdateStates();
        }

        public void SetAnimatorState(OtherPlayerStates states)
        {
            if (null == states || !m_player.IsValid)
            {
                Logger.Dbg?.Warn($"Cannot change state {null == states} {!m_player.IsValid}");
                Logger.Dbg?.Info(m_player.DebugEntityData());
                return;
            }

            var now = DateTime.Now;
            foreach (var state in states.m_States)
            {
                // Sync animation
                float nTime = Utils.ComputNTime(now, state.now, state.m_nTime, state.m_Duration, state.m_Speed);
                m_player.Animator.Play(state.m_State, state.m_Layer, nTime);
                foreach (var mirror in m_mirrorAnimators) mirror.Animator.Play(state.m_State, state.m_Layer, nTime);
                Logger.Dbg?.Info("OtherPlayerAnimator PlayState");
                m_States[state.m_Layer].m_State = state.m_State;
            }
        }

        public void OnAvatarChanged()
        {
            m_player.UpdateAvatar();
            foreach (var mirror in m_mirrorAnimators) mirror.UpdateAvatar();
            UpdateStates();
        }
        private void UpdateStates()
        {
            m_States.Clear();

            if (!m_player.IsValid) return;
            foreach (var layer in m_player.Data.ListLayer)
            {
                if ("" != layer.StartState)
                    m_States.Add(layer.LayerIndex, new(layer.LayerIndex, layer.StartState));
            }
        }

        public void AddMirror(EntityData data)
        {
            m_mirrorAnimators.Add(data);

            if (!m_player.IsValid) return;
            foreach (var layer in m_States)
            {
                var state = m_player.Animator.GetCurrentAnimatorStateInfo(layer.Value.m_Layer);
                data.Animator.Play(layer.Value.m_State, layer.Value.m_Layer, state.normalizedTime);
                Logger.Dbg?.Info($"AddMirror {layer.Value.m_State} {layer.Value.m_Layer} {state.normalizedTime}");
            }
        }
        public void RemoveMirror(EntityData data) { m_mirrorAnimators.Remove(data); }
        public void ClearMirrors() { m_mirrorAnimators.Clear(); }
        public bool IsMe() => m_player.IsMe();
    }

    public class OtherPlayerData : EntityData
    {
        protected PlayerID m_PlayerId;
        protected NetworkPlayer m_NetworkPlayer;

        public OtherPlayerData() { }
        public static OtherPlayerData Create(NetworkPlayer player, PlayerID id)
        {
            OtherPlayerData data = new();
            data.m_Source = EntityDataSources.OtherPlayer;
            data.m_PlayerId = id;
            data.m_NetworkPlayer = player;
            data.m_RigManager = player.RigRefs.RigManager;
            data.m_id = data.m_PlayerId.SmallID;
            data.UpdateAvatar();
            return data;
        }

        public bool IsMe() => m_PlayerId.IsMe;
        protected override void SetAvatar() { m_Avatar = m_RigManager.avatar; }
    }
}
