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
    public delegate bool FindFunc<in T>(T arg);
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
                        Logger.Warn($"Player RigManager with Avatar {rig.AvatarCrate.Barcode.ToString()} doesn't have a Player Id");
                        foreach (var p in NetworkPlayer.Players)
                        {
                            Logger.Dbg?.Data($"Id:{p.PlayerID.SmallID} {p.RigRefs.RigManager.AvatarCrate.Barcode.ToString()} {Utils.RefEquals(rig, p.RigRefs.RigManager)}");
                            Logger.Dbg?.Data($"{Equals(rig, p.RigRefs.RigManager)} {p.RigRefs.RigManager.ToString()} {p.RigRefs.RigManager.Pointer} {rig.ToString()} {rig.Pointer}");

                            Logger.Dbg?.Data($"{rig.AvatarCrate.Barcode.ToString()}  |  {p.RigRefs.RigManager.AvatarCrate.Barcode.ToString()}");
                            Logger.Dbg?.Data($"{Debug.ToString(rig.avatar.handSchematicLf)}");
                            Logger.Dbg?.Data($"{Debug.ToString(p.RigRefs.RigManager.avatar.handSchematicLf)}");
                            Logger.Dbg?.Data($"");
                            Logger.Dbg?.Data($"{Debug.ToString(rig.avatar.handSchematicRt)}");
                            Logger.Dbg?.Data($"{Debug.ToString(p.RigRefs.RigManager.avatar.handSchematicRt)}");
                            Logger.Dbg?.Data($"");
                            Logger.Dbg?.Data($"{rig.avatar.wristRt.ToString()}");
                            Logger.Dbg?.Data($"{p.RigRefs.RigManager.avatar.wristRt.ToString()}");
                            Logger.Dbg?.Data($"");
                            Logger.Dbg?.Data($"{rig.avatar.wristLf.ToString()}");
                            Logger.Dbg?.Data($"{p.RigRefs.RigManager.avatar.wristLf.ToString()}");

                            if (Player.RigManager == rig && p.PlayerID.IsMe) return p.PlayerID.SmallID;

                            if (Utils.RefEquals(rig, p.RigRefs.RigManager)) return p.PlayerID.SmallID;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Err(e.ToString());
                }
                return 0;
            };

            Scanner.OnNew += OnNew;
            Scanner.OnRemoved += OnRemoved;
            Scanner.OnClear += OnClear;
            ModuleMessageManager.RegisterHandler<PlayerStateChangeMessageModule>();
            MultiplayerHooking.OnPlayerJoined += OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft += OnPlayerLeft;
            MultiplayerHooking.OnJoinedServer += OnJoinedServer;
            MultiplayerHooking.OnDisconnected += OnDisconnected;
            PlayerAnimator.OnAvatarStateChanged += OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent += OnPlayerMetadataChangedEvent;
            Hooking.OnLevelLoaded += OnLevelLoaded;

            NetworkPlayer.OnNetworkRigCreated += (NetworkPlayer _1, RigManager _2) => { Logger.Dbg?.Debug("OnNetworkRigCreated"); };
            NetworkPlayer.OnNetworkPlayerRegistered += (NetworkPlayer player) =>
            {
                Logger.Dbg?.Debug("OnNetworkPlayerRegistered");
                playersConnecting.Add(player.PlayerID.SmallID, player);
                if (1 == playersConnecting.Count) Core.OnUpdateEvt += PlayerConnecting;
            };

        }

        protected override void OnModuleUnregistered()
        {
            Logger.Msg("Module unregistered");
            players.Clear();
            PlayerStateChangeMessageModule.WaitingList.Clear();
            Scanner.OnNew -= OnNew;
            Scanner.OnRemoved -= OnRemoved;
            Scanner.OnClear -= OnClear;
            MultiplayerHooking.OnPlayerJoined -= OnPlayerJoined;
            MultiplayerHooking.OnPlayerLeft -= OnPlayerLeft;
            MultiplayerHooking.OnJoinedServer -= OnJoinedServer;
            MultiplayerHooking.OnDisconnected -= OnDisconnected;
            PlayerAnimator.OnAvatarStateChanged -= OnAvatarStateChanged;
            PlayerID.OnMetadataChangedEvent -= OnPlayerMetadataChangedEvent;
            Hooking.OnLevelLoaded -= OnLevelLoaded;
        }

        public static void ChangeOtherPlayerState(OtherPlayerState states)
        {
            if (!players.ContainsKey(states.m_smallId)) return;
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
            }
        }

        ////

        private void OnNew(ScannedData data)
        {
            if (!players.ContainsKey(data.Id)) return;
            Logger.Dbg?.Info($"Add Mirror to Other Player {data.Id}");
            var other = players[data.Id];
            other.Mirrors.Add(data);
        }
        private void OnRemoved(ScannedData data)
        {
            if (!players.ContainsKey(data.Id)) return;
            var other = players[data.Id];
            other.Mirrors.Remove(data);
        }
        private void OnClear()
        {
            foreach (var other in players) { other.Value.Mirrors.Clear(); }
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
                if (!p.PlayerID.IsMe) // "PlayerID.IsMe" is done by OnLevelLoaded
                {
                    players.Add(p.PlayerID.SmallID, new(p.PlayerID));
                }
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
            PlayerStateChangeMessageModule.SendMessage(new(PlayerAnimator.Id, change));
        }
        private void OnPlayerMetadataChangedEvent(PlayerID playerId, string key, string value)
        {
            if (!players.ContainsKey(playerId.SmallID)) return;
            var other = players[playerId.SmallID];
            if (key == "AvatarBarcode" || key.Contains("Avatar"))
            {
                Logger.Dbg?.Data($"key:{key} value:{value}");
                if (playerId.IsMe)
                {
                    Logger.Dbg?.Info($"Player avatar changed");
                    CorePrivate.UpdatePlayerAvatar();
                }
                else
                {
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
            CorePrivate.UpdatePlayerAvatar();
        }
    }

    public class PlayerStateChangeMessageModule : ModuleMessageHandler
    {
        private static readonly List<OtherPlayerState> m_waitingList = new();
        public static List<OtherPlayerState> WaitingList { get => m_waitingList; }

        public static void SendMessage(OtherPlayerState d)
        {
            var data = new MyNetSerializable() { m_data = OtherPlayerState.Serialize(d), };
            Logger.Dbg?.Data($"Msg sent '{data.m_data}'");
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
