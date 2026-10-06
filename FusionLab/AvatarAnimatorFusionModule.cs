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
        private static readonly Dictionary<byte, NetworkPlayer> waitingPlayers = new();
        private static readonly Dictionary<byte, string> PlayersAvatarName = new();

        protected override void OnModuleRegistered()
        {
            Logger.Msg("AvatarAnimatorFusionModule registered");
            EntityData.GetPlayerId = (RigManager rig) =>
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

        public static bool ChangeOtherPlayerState(OtherPlayerStates states)
        {
            if (!players.ContainsKey(states.m_smallId)) return true;
            var player = players[states.m_smallId];
            if (!player.IsValid) return false;
            Logger.Dbg?.Info("Change other Player animator state");
            player.SetAnimatorState(states);
            return true;
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
            if (other.IsMe) return;
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
            switch (key)
            {
                case PlayerMetadataChangedKeys.AvatarTitle:
                    {
                        Utils.AddOrReplace(PlayersAvatarName, playerId.SmallID, value);
                        if (IsLevelLoading) return;
                        if (!players.ContainsKey(playerId.SmallID)) return;
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
                        HandleWaitingMessage();
                    }
                    break;
                case PlayerMetadataChangedKeys.Loading:
                    {
                        if (!playerId.IsMe) return;
                        m_levelLoading = (Const.True == value);
                        Logger.Dbg?.Info($"Level loading = {IsLevelLoading}");
                        if (IsLevelLoading) return;

                        UpdateSystem.CallLaterOnce(() =>
                        {
                            if (waitingPlayers.Count > 0)
                            {
                                foreach (var player in waitingPlayers) { AddOrUpdatePlayer(player.Value); }
                                waitingPlayers.Clear();
                            }

                            foreach (var (id, name) in PlayersAvatarName)
                            {
                                if (Const.PolyBlankAvatar == name) continue;
                                if (!players.ContainsKey(id)) continue;
                                var player = players[id];
                                if (player.IsMe)
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
                            HandleWaitingMessage();
                        });

                    }
                    break;
            }
        }

        private void HandleWaitingMessage()
        {
            if (PlayerStateChangeMessageModule.WaitingList.Count == 0) return;

            Logger.Dbg?.Info($"Level finish to loading, use {PlayerStateChangeMessageModule.WaitingList.Count} stored messages");
            for (int i = 0; i < PlayerStateChangeMessageModule.WaitingList.Count; i++)
            {
                var states = PlayerStateChangeMessageModule.WaitingList[i];
                if (ChangeOtherPlayerState(states))
                {
                    PlayerStateChangeMessageModule.WaitingList.RemoveAt(i);
                    i -= 1;
                }
            }
        }

        private void OnLevelLoaded(LevelInfo _)
        {
            Logger.Dbg?.Debug("OnLevelLoaded");
        }
        private void OnNetworkRigCreated(NetworkPlayer player, RigManager _2)
        {
            Logger.Dbg?.Debug("OnNetworkRigCreated");
            if (IsLevelLoading)
            {
                Utils.AddOrReplace(waitingPlayers, player.PlayerID.SmallID, player);
                return;
            }

            UpdateSystem.CallLaterOnce(() =>
            {
                if (IsLevelLoading) return;
                AddOrUpdatePlayer(player);
            });
        }
        private void OnNetworkPlayerRegistered(NetworkPlayer player)
        {
            Logger.Dbg?.Debug("OnNetworkPlayerRegistered");
            if (IsLevelLoading)
            {
                Utils.AddOrReplace(waitingPlayers, player.PlayerID.SmallID, player);
                return;
            }

            UpdateSystem.CallLaterOnce(() =>
            {
                if (IsLevelLoading) return;
                AddOrUpdatePlayer(player);

                if (player.PlayerID.IsMe) return;
                var states = PlayerAnimator.GetPlayerStates();
                if (states.Count <= 0) return;
                PlayerStateChangeMessageModule.SendMessageTo(player.PlayerID.SmallID, new(PlayerAnimator.Id, null, states));
            });

        }
        private void AddOrUpdatePlayer(NetworkPlayer player)
        {
            var smallId = player.PlayerID.SmallID;
            if (players.ContainsKey(smallId))
            {
                Logger.Dbg?.Info($"Player '{smallId}' update");
                players.Remove(smallId);
            }
            else
            {
                Logger.Dbg?.Info($"Player '{smallId}' Join");
            }
            players.Add(smallId, new(player, player.PlayerID));
        }
    }
}
