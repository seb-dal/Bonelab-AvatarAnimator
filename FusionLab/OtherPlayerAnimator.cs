using UnityEngine;
using Newtonsoft.Json;
using LabFusion.Player;
using LabFusion.Entities;

namespace AvatarAnimator.FusionLab
{
    [Serializable]
    public class OtherPlayerState
    {
        [JsonProperty("SmallId")]
        public byte m_smallId;
        [JsonProperty("States")]
        public List<PlayerStateChange> m_States = new();

        public OtherPlayerState(byte smallId, PlayerStateChange change = null, List<PlayerStateChange> states = null)
        {
            m_smallId = smallId;
            if (null != change) m_States.Add(change);
            if (null != states) m_States.AddRange(states);
        }

        public static string Serialize(OtherPlayerState obj) => JsonConvert.SerializeObject(obj, Formatting.None);
        public static OtherPlayerState Deserialize(string json) => JsonConvert.DeserializeObject<OtherPlayerState>(json);
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

        public void SetAnimatorState(OtherPlayerState states)
        {
            if (null != states || !m_player.IsValid)
            {
                Logger.Dbg?.Warn("Cannot change state");
                return;
            }
            foreach (var state in states.m_States)
            {
                m_player.Animator.Play(state.m_State, state.m_Layer);
                foreach (var mirror in m_mirrorAnimators) mirror.Animator.Play(state.m_State, state.m_Layer);
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
