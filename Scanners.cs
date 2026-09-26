using AvatarAnimator.Deserialize;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using UnityEngine;

namespace AvatarAnimator
{
    public enum ScannedDataSources
    {
        Invalid,
        Player,
        Mirror,
        OtherPlayer,
    }
    public class ScannedData
    {
        protected AvatarAnimatorDataContainer m_Cont;
        protected Il2CppSLZ.VRMK.Avatar m_Avatar;
        protected RigManager m_RigManager;
        protected Barcode m_Barcode;
        protected ScannedDataSources m_Source;
        protected Mirror m_Mirror = null;
        protected byte m_id;

        public ScannedData() { }
        public static ScannedData Create()
        {
            ScannedData data = new();
            data.m_Source = ScannedDataSources.Player;
            data.m_RigManager = Player.RigManager;
            SetId(ref data);
            data.UpdateAvatar();
            return data;
        }
        public static ScannedData Create(Mirror mirror)
        {
            ScannedData data = new();
            data.m_Source = ScannedDataSources.Mirror;
            data.m_Mirror = mirror;
            data.m_RigManager = mirror.rigManager;
            SetId(ref data);
            data.UpdateAvatar();
            return data;
        }
        private static void SetId(ref ScannedData data)
        {
            try
            {
                data.m_id = Utils.GetPlayerId(data.m_RigManager);
            }
            catch (Exception e)
            {
                Logger.Dbg?.Warn(e.ToString());
                data.m_Source = ScannedDataSources.Invalid;
            }
        }

        public AvatarAnimatorDataContainer Container { get => m_Cont; }
        public Animator Animator { get => m_Cont.m_Animator; }
        public AvatarAnimatorData Data { get => m_Cont.m_Data; }
        public bool HasAvatarAnimatorData { get => null != m_Cont?.m_Data; }
        public Il2CppSLZ.VRMK.Avatar Avatar { get => m_Avatar; }
        public RigManager RigManager { get => m_RigManager; }
        public Barcode Barcode { get => m_Barcode; }
        public ScannedDataSources Source { get => m_Source; }
        public Mirror Mirror { get => m_Mirror; }
        public byte Id { get => m_id; }
        public bool IsValid { get => ScannedDataSources.Invalid != m_Source; }


        /// <summary> Set the m_Avatar from current data </summary>
        protected virtual void SetAvatar()
        {
            m_Avatar = m_Source switch
            {
                ScannedDataSources.Player => Player.Avatar,
                ScannedDataSources.Mirror => m_Mirror.Reflection,
                _ => null,
            };
        }

        /// <summary> Update the avatar and the AvatarAnimatorDataContainer if found </summary>
        public void UpdateAvatar()
        {
            SetAvatar();
            m_Barcode = m_RigManager?.AvatarCrate?.Barcode;
            if (ScannedDataSources.Invalid != m_Source)
                m_Cont = m_Avatar.gameObject.GetComponent<AvatarAnimatorDataContainer>();
            if (null != m_Cont)
            {
                try
                {
                    m_Cont.m_Data = AvatarAnimatorDataDeserializer.Deserialize(m_Cont.m_Version, m_Cont.m_CompactedData);
                    Logger.Msg($"AvatarAnimator '{Barcode.ToString()}' Data found version {m_Cont.m_Version}");
                }
                catch (Exception e)
                {
                    m_Source = ScannedDataSources.Invalid;
                    Logger.Warn(e.ToString());
                }
                Logger.Dbg?.Data(m_Cont.m_CompactedData);
            }
            Logger.Dbg?.Info($"id:'{m_id}', sc:{m_Source}, Rig:'{null != m_RigManager}', Avatar:'{null != m_Avatar}', Cont:'{null != m_Cont}', Anim:'{null != m_Cont?.m_Animator}'");
        }
    }

    /// <summary> Scan GameObjects for Mirrors </summary>
    public static class MirrorScanner
    {
        private static readonly List<ScannedData> m_all = new();
        public static List<ScannedData> All { get => m_all; }

        public static event Action<ScannedData> OnNew;
        public static event Action<ScannedData> OnRemoved;
        public static event Action OnClear;

        public static void Clear()
        {
            m_all.Clear();
            OnClear?.Invoke();
        }

        /// <summary> Scan for added/remove "Mirror" entities </summary>
        public static void Scan()
        {
            // Carrying <AvatarAnimatorDataContainer> doesn't allow to get the RigManager link to the Avatar
            List<Mirror> scanned = new(GameObject.FindObjectsOfType<Mirror>());

            int removed = 0, add = 0;
            for (int i = m_all.Count - 1; i >= 0; --i)
            {
                var e = m_all[i];
                if (null != scanned.Find((e2) => Utils.RefEquals(e2.rigManager, e.RigManager))) continue;
                m_all.RemoveAt(i);
                OnRemoved?.Invoke(e);
                removed += 1;
            }

            foreach (var e in scanned)
            {
                if (null == e || null == e?.rigManager || null == e?.Reflection) continue;
                if (null != m_all.Find((e2) => Utils.RefEquals(e2.RigManager, e.rigManager))) continue;
                ScannedData d = ScannedData.Create(e);
                m_all.Add(d);
                OnNew?.Invoke(d);
                add += 1;
            }

            if (null != Logger.Dbg && (0 != removed || 0 != add))
                Logger.Dbg?.Info($"Scanner: Entities {removed} removed, {add} added, {m_all.Count} Total");
        }
    }

    public static class PlayerScanner
    {
        private static ScannedData PlayerData = null;
        /// <summary> Avatar Change </summary>
        public static event Action<ScannedData> OnAvatarChange;
        /// <summary> Level Change </summary>
        public static event Action<ScannedData> OnAvatarSame;

        public static void GetAvatarAnimator()
        {
            Logger.Dbg?.Data($"OLD: {PlayerData?.Barcode?.ToString()} '{PlayerData?.Source}' '{null == PlayerData?.RigManager}'  -  NEW:{Player.RigManager.AvatarCrate.Barcode?.ToString()} '{Player.RigManager.AvatarCrate?.Crate?.Title}'");
            if ("PolyBlank" == Player.RigManager.AvatarCrate?.Crate?.Title) return;

            bool wasInvalid = ScannedDataSources.Invalid == PlayerData?.Source;
            bool hasAvatarChange = Player.RigManager.AvatarCrate.Barcode != PlayerData?.Barcode; // if false => level change => new Data needed

            if (!hasAvatarChange || null == PlayerData || wasInvalid)
                PlayerData = ScannedData.Create();
            else
                PlayerData.UpdateAvatar();

            if (!PlayerData.IsValid) return;
            Logger.Dbg?.Info($"GetPlayerAvatarAnimator hasAvatarChange:{hasAvatarChange}");

            if (hasAvatarChange)
                OnAvatarChange?.Invoke(PlayerData);
            else
                OnAvatarSame?.Invoke(PlayerData);
        }
    }
}
