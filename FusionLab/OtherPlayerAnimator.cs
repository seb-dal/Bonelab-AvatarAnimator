using UnityEngine;
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
        [JsonProperty("Now")]
        public DateTime now;

        public OtherPlayerStates(byte smallId, PlayerStateChange change = null, List<PlayerStateChange> states = null)
        {
            m_smallId = smallId;
            if (null != change) m_States.Add(change);
            if (null != states) m_States.AddRange(states);
            now = DateTime.Now;
        }

        public static string Serialize(OtherPlayerStates obj) => JsonConvert.SerializeObject(obj, Formatting.None);
        public static OtherPlayerStates Deserialize(string json) => JsonConvert.DeserializeObject<OtherPlayerStates>(json);
    }

    public class OtherPlayerAnimator
    {
        private readonly List<ScannedData> m_mirrorAnimators = new();
        private readonly Dictionary<int, PlayerStateChange> m_States = new();
        private readonly ScannedDataFusion m_player = null;

        public OtherPlayerAnimator(NetworkPlayer player, PlayerID playerId)
        {
            m_player = ScannedDataFusion.Create(player, playerId);
            foreach (var layer in m_player.Data.ListLayer)
            {
                m_States.Add(layer.LayerIndex, new(layer.LayerIndex, layer.StartState));
            }
        }

        public void SetAnimatorState(OtherPlayerStates states)
        {
            if (null != states || !m_player.IsValid)
            {
                Logger.Dbg?.Warn("Cannot change state");
                return;
            }
            var diff = (float)(DateTime.Now - states.now).TotalSeconds;
            foreach (var state in states.m_States)
            {
                // Sync animation
                float nTime = state.m_nTime ?? 0.0f;
                float d = state.m_Duration ?? 0.0f;
                float s = state.m_Speed ?? 1.0f;
                if (d != 0.0f && s != 0.0f) nTime += diff / (d * s);
                m_player.Animator.Play(state.m_State, state.m_Layer, nTime);
                foreach (var mirror in m_mirrorAnimators) mirror.Animator.Play(state.m_State, state.m_Layer, nTime);
                m_States.Add(state.m_Layer, state);
            }
        }

        public void OnAvatarChanged()
        {
            m_player.UpdateAvatar();
            foreach (var mirror in m_mirrorAnimators) mirror.UpdateAvatar();
        }

        public void AddMirror(ScannedData data)
        {
            m_mirrorAnimators.Add(data);
            foreach (var layer in m_States)
            {
                var state = m_player.Animator.GetCurrentAnimatorStateInfo(layer.Value.m_Layer);
                data.Animator.Play(layer.Value.m_State, layer.Value.m_Layer, state.normalizedTime);
            }
        }
        public void RemoveMirror(ScannedData data) { m_mirrorAnimators.Remove(data); }
        public void ClearMirrors() { m_mirrorAnimators.Clear(); }
        public bool IsMe() => m_player.IsMe();
    }

    public class ScannedDataFusion : ScannedData
    {
        protected PlayerID m_PlayerId;
        protected NetworkPlayer m_NetworkPlayer;

        public ScannedDataFusion() { }
        public static ScannedDataFusion Create(NetworkPlayer player, PlayerID id)
        {
            ScannedDataFusion data = new();
            data.m_Source = ScannedDataSources.OtherPlayer;
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
