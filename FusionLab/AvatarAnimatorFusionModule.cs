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
        public override ConsoleColor Color => ConsoleColor.DarkGreen;

        private static bool isOnline = false;

        private static readonly Dictionary<byte, OtherPlayerAnimator> players = new();

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

        public static void ChangeOtherPlayerState(OtherPlayerStates states)
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

        ////

        private void OnNew(EntityData data)
        {
            if (!players.ContainsKey(data.Id)) return;
            var other = players[data.Id];
            if (other.IsMe()) return;
            Logger.Dbg?.Info($"Add Mirror to Other Player {data.Id}");
            other.AddMirror(data);
        }
        private void OnRemoved(EntityData data)
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

        private static bool m_levelLoading = false;
        public static bool IsLevelLoading { get => m_levelLoading; }
        private void OnPlayerMetadataChangedEvent(PlayerID playerId, string key, string value)
        {
            Logger.Dbg?.Debug($"id:{playerId?.SmallID} key:'{key}' value:'{value}'");
            if (!players.ContainsKey(playerId.SmallID)) return;
            switch (key)
            {
                case PlayerMetadataChangedKeys.AvatarTitle:
                    {
                        if (IsLevelLoading) return;
                        if (playerId.IsMe)
                        {
                            Logger.Dbg?.Info($"Player avatar changed");
                            CorePrivate.UpdatePlayerAvatar();
                        }
                        else
                        {
                            UpdateSystem.CallLaterOnce(() =>
                            {
                                Logger.Dbg?.Info($"Other Player '{playerId.SmallID}' avatar changed");
                                players[playerId.SmallID].OnAvatarChanged();
                            });
                        }
                    }
                    break;
                case PlayerMetadataChangedKeys.Loading:
                    {
                        m_levelLoading = (Const.True == value);
                        if (!IsLevelLoading)
                        {
                            Logger.Dbg?.Info($"Level finish to loading, use {PlayerStateChangeMessageModule.WaitingList.Count} stored messages");

                            CorePrivate.UpdatePlayerAvatar();
                            foreach (var player in players) { player.Value.OnAvatarChanged(); }

                            foreach (var states in PlayerStateChangeMessageModule.WaitingList)
                            {
                                ChangeOtherPlayerState(states);
                            }
                            PlayerStateChangeMessageModule.WaitingList.Clear();
                        }
                    }
                    break;
            }
        }

        private void OnLevelLoaded(LevelInfo _)
        {
            Logger.Dbg?.Debug("OnLevelLoaded");
        }
        private void OnNetworkRigCreated(NetworkPlayer player, RigManager _2)
        {
            Logger.Dbg?.Debug("OnNetworkRigCreated");

            var smallId = player.PlayerID.SmallID;
            Logger.Dbg?.Info($"Player '{smallId}' rig update");
            if (players.ContainsKey(smallId)) players.Remove(smallId);
            players.Add(smallId, new(player, player.PlayerID));
        }
        private void OnNetworkPlayerRegistered(NetworkPlayer player)
        {
            Logger.Dbg?.Debug("OnNetworkPlayerRegistered");
            UpdateSystem.CallLaterOnce(() =>
            {
                if (null == player.RigRefs?.RigManager) return;
                var smallId = player.PlayerID.SmallID;
                Logger.Dbg?.Info($"Player '{smallId}' Join");
                if (players.ContainsKey(smallId)) players.Remove(smallId);
                players.Add(smallId, new(player, player.PlayerID));

                if (player.PlayerID.IsMe) return;
                var states = PlayerAnimator.GetPlayerStates();
                if (states.Count <= 0) return;
                PlayerStateChangeMessageModule.SendMessageTo(player.PlayerID.SmallID, new(PlayerAnimator.Id, null, states));
            });
        }
    }
}
