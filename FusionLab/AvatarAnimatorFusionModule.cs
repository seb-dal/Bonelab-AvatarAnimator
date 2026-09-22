using LabFusion.SDK.Modules;
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

        private static bool isOnline = false;

        private static readonly Dictionary<byte, OtherPlayerAnimator> players = new();

        private static readonly Dictionary<byte, NetworkPlayer> playersConnecting = new();

        protected override void OnModuleRegistered()
        {
            Logger.Msg("AvatarAnimatorFusionModule registered");
            Utils.GetPlayerId = (RigManager rig) =>
            {
                if (!isOnline) return 0;
                if (NetworkPlayerManager.TryGetPlayer(rig, out var player))
                    return player.PlayerID.SmallID;
                throw new Exception($"Player RigManager with Avatar {rig.AvatarCrate.Barcode.ToString()} doesn't have a Player Id");
            };

            ModuleMessageManager.RegisterHandler<PlayerStateChangeMessageModule>();

            MirrorScanner.OnNew += OnNew;
            MirrorScanner.OnRemoved += OnRemoved;
            MirrorScanner.OnClear += OnClear;
            MultiplayerHooking.OnPlayerJoined += OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft += OnPlayerLeft;
            MultiplayerHooking.OnStartedServer += OnJoinedServer;
            MultiplayerHooking.OnJoinedServer += OnJoinedServer;
            MultiplayerHooking.OnDisconnected += OnDisconnected;
            PlayerAnimator.OnAvatarStateChanged += OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent += OnPlayerMetadataChangedEvent;
            Hooking.OnLevelLoaded += OnLevelLoaded;
            NetworkPlayer.OnNetworkRigCreated += OnNetworkRigCreated;
            NetworkPlayer.OnNetworkPlayerRegistered += OnNetworkPlayerRegistered;

            // You cannot start the game in multiplayer
            CorePrivate.SimplePlayerMonitoring();
        }

        protected override void OnModuleUnregistered()
        {
            Logger.Msg("Module unregistered");
            players.Clear();
            PlayerStateChangeMessageModule.WaitingList.Clear();

            MirrorScanner.OnNew -= OnNew;
            MirrorScanner.OnRemoved -= OnRemoved;
            MirrorScanner.OnClear -= OnClear;
            MultiplayerHooking.OnPlayerJoined -= OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft -= OnPlayerLeft;
            MultiplayerHooking.OnJoinedServer -= OnJoinedServer;
            MultiplayerHooking.OnDisconnected -= OnDisconnected;
            PlayerAnimator.OnAvatarStateChanged -= OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent -= OnPlayerMetadataChangedEvent;
            Hooking.OnLevelLoaded -= OnLevelLoaded;
            NetworkPlayer.OnNetworkRigCreated -= OnNetworkRigCreated;
            NetworkPlayer.OnNetworkPlayerRegistered -= OnNetworkPlayerRegistered;
        }

        public static void ChangeOtherPlayerState(OtherPlayerState states)
        {
            if (!players.ContainsKey(states.m_smallId)) return;
            Logger.Dbg?.Info("Change other Player animator state");
            players[states.m_smallId].SetAnimatorState(states);
        }

        public static NetworkPlayer FindPlayer(FindFunc<NetworkPlayer> func)
        {
            foreach (var p in NetworkPlayer.Players)
            {
                if (func(p)) return p;
            }
            return null;
        }

        private static void PlayerConnecting()
        {
            if (0 == playersConnecting.Count)
            {
                Core.OnUpdateEvt -= PlayerConnecting;
                return;
            }
            for (int i = playersConnecting.Count - 1; i >= 0; i--)
            {
                var elem = playersConnecting.ElementAt(i);
                var player = elem.Value;
                if (null == player.RigRefs?.RigManager) continue;
                Logger.Dbg?.Info($"Player '{player.PlayerID.SmallID}' Join");
                players.Add(player.PlayerID.SmallID, new(player, player.PlayerID));
                playersConnecting.Remove(elem.Key);

                if (player.PlayerID.IsMe) continue;
                PlayerStateChangeMessageModule.SendMessageTo(player.PlayerID.SmallID, new(PlayerAnimator.Id, null, PlayerAnimator.GetPlayerStates()));
            }
        }

        ////

        private void OnNew(ScannedData data)
        {
            if (!players.ContainsKey(data.Id)) return;
            var other = players[data.Id];
            if (other.IsMe()) return;
            Logger.Dbg?.Info($"Add Mirror to Other Player {data.Id}");
            other.AddMirror(data);
        }
        private void OnRemoved(ScannedData data)
        {
            if (!players.ContainsKey(data.Id)) return;
            var other = players[data.Id];
            other.RemoveMirror(data);
        }
        private void OnClear()
        {
            foreach (var other in players) { other.Value.ClearMirrors(); }
        }
        private void OnPlayerJoined(PlayerID id)
        {
            Logger.Dbg?.Debug("OnPlayerJoined");
        }
        private void OnPlayerLeft(PlayerID id)
        {
            Logger.Dbg?.Debug("OnPlayerLeft");
            Logger.Dbg?.Info($"Player '{id.SmallID}' Left");
            if (id.IsMe) return; // done in OnDisconnected
            players.Remove(id.SmallID);
        }
        private void OnJoinedServer()
        {
            Logger.Dbg?.Debug("OnJoinedServer");
            isOnline = true;
            CorePrivate.MultiPlayerMonitoring();
            foreach (var p in NetworkPlayer.Players)
            {
                Logger.Dbg?.Info($"Player '{p.PlayerID.SmallID}'");
            }
        }
        private void OnDisconnected()
        {
            Logger.Dbg?.Debug("OnDisconnected");
            isOnline = false;
            players.Clear();
            CorePrivate.SimplePlayerMonitoring();
        }

        private void OnAvatarStateChanged(PlayerStateChange change)
        {
            if (!isOnline) return;
            PlayerStateChangeMessageModule.SendMessage(new(PlayerAnimator.Id, change));
        }
        private void OnPlayerMetadataChangedEvent(PlayerID playerId, string key, string value)
        {
            if (!players.ContainsKey(playerId.SmallID)) return;
            if ("AvatarTitle" == key)
            {
                Logger.Dbg?.Data($"key:'{key}' value:'{value}'");
                if (playerId.IsMe)
                {
                    Logger.Dbg?.Info($"Player avatar changed");
                    CorePrivate.UpdatePlayerAvatar();
                }
                else
                {
                    var other = players[playerId.SmallID];
                    Logger.Dbg?.Info($"Other Player '{playerId.SmallID}' avatar changed");
                    other.OnAvatarChanged();
                }
            }
        }
        private void OnLevelLoaded(LevelInfo _)
        {
            Logger.Dbg?.Info($"Level finish to loading, use {PlayerStateChangeMessageModule.WaitingList.Count} stored messages");
            foreach (var states in PlayerStateChangeMessageModule.WaitingList)
            {
                ChangeOtherPlayerState(states);
            }
            PlayerStateChangeMessageModule.WaitingList.Clear();
        }
        private void OnNetworkRigCreated(NetworkPlayer _1, RigManager _2)
        {
            Logger.Dbg?.Debug("OnNetworkRigCreated");
        }
        private void OnNetworkPlayerRegistered(NetworkPlayer player)
        {
            Logger.Dbg?.Debug("OnNetworkPlayerRegistered");
            playersConnecting.Add(player.PlayerID.SmallID, player);
            if (1 == playersConnecting.Count) Core.OnUpdateEvt += PlayerConnecting;
        }
    }
}
