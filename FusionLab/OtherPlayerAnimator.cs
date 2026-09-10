using UnityEngine;
using Newtonsoft.Json;
using LabFusion.Player;
using LabFusion.Entities;

namespace AvatarAnimator.FusionLab
{
    public class OtherPlayerState
    {
        public byte m_smallId;
        public PlayerStateChange m_StateChange;

        public OtherPlayerState(byte smallId, PlayerStateChange change)
        {
            m_smallId = smallId;
            m_StateChange = change;
        }

        public static string Serialize(OtherPlayerState obj) => JsonConvert.SerializeObject(obj, Formatting.None);
        public static OtherPlayerState Deserialize(string json) => JsonConvert.DeserializeObject<OtherPlayerState>(json);
    }

    public class OtherPlayerAnimator
    {
        private readonly List<ScannedData> m_mirrorAnimators = new();
        private readonly ScannedDataFusion m_player = null;

        public List<ScannedData> Mirrors { get => m_mirrorAnimators; }

        public OtherPlayerAnimator(NetworkPlayer player, PlayerID playerId)
        {
            m_player = ScannedDataFusion.Create(player, playerId);
        }
        public OtherPlayerAnimator(PlayerID playerId)
        {
            m_player = ScannedDataFusion.Create(playerId);
        }

        public void SetAnimatorState(OtherPlayerState state)
        {
            if (null != state || !m_player.IsValid) return;
            m_player.Animator.Play(state.m_StateChange.m_State, state.m_StateChange.m_Layer);
            foreach (var mirror in m_mirrorAnimators) mirror.Animator.Play(state.m_StateChange.m_State, state.m_StateChange.m_Layer);
        }

        public void OnAvatarChanged()
        {
            m_player.UpdateAvatar();
            foreach (var mirror in m_mirrorAnimators) mirror.UpdateAvatar();
        }
    }

    public class ScannedDataFusion : ScannedData
    {
        protected PlayerID m_PlayerId;
        protected NetworkPlayer m_NetworkPlayer;

        public ScannedDataFusion() { }

        public static ScannedDataFusion Create(NetworkPlayer player, PlayerID id)
        {
            ScannedDataFusion data = new();
            data.m_PlayerId = id;
            data.Init(player);
            return data;
        }
        public static ScannedDataFusion Create(PlayerID id)
        {
            ScannedDataFusion data = new();
            data.m_PlayerId = id;
            if (NetworkPlayerManager.TryGetPlayer(id.SmallID, out var player))
            {
                data.Init(player);
            }
            else
            {
                data.m_Source = ScannedDataSources.Invalid;
                Logger.Err($"Player with id '{id.SmallID}' didn't give NetworkPlayer ;( ");
            }
            return data;
        }

        private void Init(NetworkPlayer player)
        {
            m_NetworkPlayer = player;
            m_RigManager = player.RigRefs.RigManager;
            m_id = m_PlayerId.SmallID;
            UpdateAvatar();
            m_Source = ScannedDataSources.OtherPlayer;
        }

        protected override void SetAvatar()
        {
            m_Avatar = m_RigManager.avatar;
        }
    }
}
