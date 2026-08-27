using MelonLoader;
using System.Reflection;
using LabFusion.SDK.Modules;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Extensions;
using Il2CppSLZ.Marrow;
using LabFusion.Utilities;
using LabFusion.Player;
using LabFusion.Entities;
using BoneLib;

namespace AvatarAnimator.FusionLab
{
    public class AvatarAnimatorFusionModule : LabFusion.SDK.Modules.Module
    {
        public override string Name => BuildInfo.Name;
        public override string Author => BuildInfo.Author;
        public override Version Version => new(BuildInfo.Version);
        public override ConsoleColor Color => ConsoleColor.Red;

        private static readonly Dictionary<byte, OtherPlayerAnimator> otherplayers = new();

        protected override void OnModuleRegistered()
        {
            Logger.Msg("Module registered");
            Utils.GetPlayerId = (RigManager rig) =>
            {
                try
                {
                    if (NetworkPlayerManager.TryGetPlayer(rig, out var player))
                    {
                        return player.PlayerID.SmallID;
                    }
                    else
                    {
                        Logger.Err($"Player with Avatar {rig.AvatarCrate.Barcode.ToString()} doesn't have a Player Id");
                    }
                }
                catch (Exception e)
                {
                    Logger.Err(e.ToString());
                }
                return 0;
            };

            Scanner.OnNew += Scanner_OnNew;
            Scanner.OnRemoved += Scanner_OnRemoved;
            Scanner.OnClear += Scanner_OnClear;
            ModuleMessageManager.RegisterHandler<PlayerStateChangeMessageModule>();
            MultiplayerHooking.OnPlayerJoined += MultiplayerHooking_OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft += MultiplayerHooking_OnPlayerLeft;
            PlayerAnimator.OnAvatarStateChanged += PlayerAnimator_OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent += PlayerID_OnMetadataChangedEvent;
            Hooking.OnLevelLoaded += Hooking_OnLevelLoaded;
        }

        protected override void OnModuleUnregistered()
        {
            Logger.Msg("Module unregistered");
            otherplayers.Clear();
            PlayerStateChangeMessageModule.WaitingList.Clear();
            Scanner.OnNew -= Scanner_OnNew;
            Scanner.OnRemoved -= Scanner_OnRemoved;
            Scanner.OnClear -= Scanner_OnClear;
            MultiplayerHooking.OnPlayerJoined -= MultiplayerHooking_OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft -= MultiplayerHooking_OnPlayerLeft;
            PlayerAnimator.OnAvatarStateChanged -= PlayerAnimator_OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent -= PlayerID_OnMetadataChangedEvent;
            Hooking.OnLevelLoaded -= Hooking_OnLevelLoaded;
        }

        public static void ChangeOtherPlayerState(OtherPlayerState states)
        {
            if (!otherplayers.ContainsKey(states.m_smallId)) return;
            otherplayers[states.m_smallId].SetAnimatorState(states);
        }

        ////

        private void Scanner_OnNew(ScannedData data)
        {
            if (!otherplayers.ContainsKey(data.Id)) return;
            Logger.Dbg?.Info($"Add Mirror to Other Player {data.Id}");
            var other = otherplayers[data.Id];
            other.Mirrors.Add(data);
        }
        private void Scanner_OnRemoved(ScannedData data)
        {
            if (!otherplayers.ContainsKey(data.Id)) return;
            var other = otherplayers[data.Id];
            other.Mirrors.Remove(data);
        }
        private void Scanner_OnClear()
        {
            foreach (var other in otherplayers) { other.Value.Mirrors.Clear(); }
        }
        private void MultiplayerHooking_OnPlayerJoined(PlayerID id)
        {
            Logger.Dbg?.Info($"Player '{id.SmallID}' Join");
            if (id.IsMe) return;
            OtherPlayerAnimator other = new(id);
            otherplayers.Add(id.SmallID, other);
        }
        private void MultiplayerHooking_OnPlayerLeft(PlayerID id)
        {
            Logger.Dbg?.Info($"Player '{id.SmallID}' Left");
            if (id.IsMe) return;
            otherplayers.Remove(id.SmallID);
        }
        private void PlayerAnimator_OnAvatarStateChanged(PlayerStateChange change)
        {
            PlayerStateChangeMessageModule.SendMessage(new(PlayerAnimator.Id, change));
        }
        private void PlayerID_OnMetadataChangedEvent(PlayerID playerId, string key, string value)
        {
            if (playerId.IsMe) return;
            if (!otherplayers.ContainsKey(playerId.SmallID)) return;
            var other = otherplayers[playerId.SmallID];
            if (key == "AvatarBarcode" || key.Contains("Avatar"))
            {
                Logger.Dbg?.Info($"Other Player '{playerId.SmallID}' avatar changed");
                other.OnAvatarChanged();
            }
        }
        private void Hooking_OnLevelLoaded(LevelInfo _)
        {
            Logger.Dbg?.Info($"Level finish to loading, use {PlayerStateChangeMessageModule.WaitingList.Count} stored messages");
            foreach (var states in PlayerStateChangeMessageModule.WaitingList)
            {
                ChangeOtherPlayerState(states);
            }
            PlayerStateChangeMessageModule.WaitingList.Clear();
        }
    }

    public class PlayerStateChangeMessageModule : ModuleMessageHandler
    {
        private static readonly List<OtherPlayerState> m_waitingList = new();
        public static List<OtherPlayerState> WaitingList { get => m_waitingList; }

        public static void SendMessage(OtherPlayerState d)
        {
            var data = new MyNetSerializable() { m_data = OtherPlayerState.Serialize(d), };
            Logger.Dbg?.Data($"Msg send '{data.m_data}'");
            MessageRelay.RelayModule<PlayerStateChangeMessageModule, MyNetSerializable>(data, new MessageRoute(RelayType.ToOtherClients, NetworkChannel.Reliable));
        }
        protected override void OnHandleMessage(ReceivedMessage received)
        {
            var data = received.ReadData<MyNetSerializable>();
            Logger.Dbg?.Info($"Msg received '{data.m_data}'");
            var state = OtherPlayerState.Deserialize(data.m_data);
            if (PlayerAnimator.Id == state.m_smallId) return;
            if (Core.IsLevelLoading)
            {
                Logger.Dbg?.Info($"Level didn't finish to load, store Message data");
                m_waitingList.Add(state);
                return;
            }
            AvatarAnimatorFusionModule.ChangeOtherPlayerState(state);
        }
    }

    public class MyNetSerializable : INetSerializable
    {
        public int? GetSize() => m_data.GetSize();
        public string m_data;
        public void Serialize(INetSerializer serializer) { serializer.SerializeValue(ref m_data); }
    }
}
