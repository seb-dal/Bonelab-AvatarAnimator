using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;

namespace AvatarAnimator.FusionLab
{
    public class PlayerStateChangeMessageModule : ModuleMessageHandler
    {
        public class MyNetSerializable : INetSerializable
        {
            public int? GetSize() => m_data.GetSize();
            public string m_data;
            public void Serialize(INetSerializer serializer) { serializer.SerializeValue(ref m_data); }
        }

        private static readonly List<OtherPlayerState> m_waitingList = new();
        public static List<OtherPlayerState> WaitingList { get => m_waitingList; }

        public static void SendMessage(OtherPlayerState d)
        {
            var data = new MyNetSerializable() { m_data = OtherPlayerState.Serialize(d), };
            Logger.Dbg?.Data($"Msg sent '{data.m_data}'");
            MessageRelay.RelayModule<PlayerStateChangeMessageModule, MyNetSerializable>(data, new(RelayType.ToOtherClients, NetworkChannel.Reliable));
        }
        public static void SendMessageTo(byte smallId, OtherPlayerState d)
        {
            var data = new MyNetSerializable() { m_data = OtherPlayerState.Serialize(d), };
            Logger.Dbg?.Data($"Msg sent '{data.m_data}' to {smallId}");
            MessageRelay.RelayModule<PlayerStateChangeMessageModule, MyNetSerializable>(data, new(smallId, NetworkChannel.Reliable));
        }
        protected override void OnHandleMessage(ReceivedMessage received)
        {
            var data = received.ReadData<MyNetSerializable>();
            var state_s = OtherPlayerState.Deserialize(data.m_data);
            Logger.Dbg?.Data($"Msg received '{data.m_data}' from {state_s.m_smallId}");
            if (PlayerAnimator.Id == state_s.m_smallId) return;
            if (Core.IsLevelLoading)
            {
                Logger.Dbg?.Info($"Level didn't finish to load, store Message data");
                m_waitingList.Add(state_s);
                return;
            }
            AvatarAnimatorFusionModule.ChangeOtherPlayerState(state_s);
        }
    }
}
