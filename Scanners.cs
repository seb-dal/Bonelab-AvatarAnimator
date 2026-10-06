using AvatarAnimator.Deserialize;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using UnityEngine;

namespace AvatarAnimator
{
    public delegate byte PlayerIdGetterFunc(RigManager rig);

    public enum EntityDataSources
    {
        Invalid,
        Player,
        Mirror,
        OtherPlayer,
    }
    public class EntityData
    {
        protected AvatarAnimatorDataContainer m_Cont;
        protected AvatarAnimatorData m_Data = null;
        protected Il2CppSLZ.VRMK.Avatar m_Avatar;
        protected RigManager m_RigManager;
        protected Barcode m_Barcode;
        protected EntityDataSources m_Source;
        protected Mirror m_Mirror = null;
        protected byte m_id;

        /// <summary> Override by FusionLab Integration </summary>
        public static PlayerIdGetterFunc GetPlayerId = (RigManager _) => 0;

        public EntityData() { }
        public static EntityData Create()
        {
            EntityData data = new();
            data.m_Source = EntityDataSources.Player;
            data.m_RigManager = Player.RigManager;
            data.SetId();
            data.UpdateAvatar();
            return data;
        }
        public static EntityData Create(Mirror mirror)
        {
            EntityData data = new();
            data.m_Source = EntityDataSources.Mirror;
            data.m_Mirror = mirror;
            data.m_RigManager = mirror.rigManager;
            data.SetId();
            data.UpdateAvatar();
            return data;
        }

        public AvatarAnimatorDataContainer Container { get => m_Cont; }
        public Animator Animator { get => m_Cont?.m_Animator; }
        public AvatarAnimatorData Data { get => m_Data; }
        public bool HasAvatarAnimatorData { get => null != m_Cont && null != m_Data; }
        public Il2CppSLZ.VRMK.Avatar Avatar { get => m_Avatar; }
        public RigManager RigManager { get => m_RigManager; }
        public Barcode Barcode { get => m_Barcode; }
        public EntityDataSources Source { get => m_Source; }
        public Mirror Mirror { get => m_Mirror; }
        public byte Id { get => m_id; }

        private bool valid = false;
        public bool IsValid { get => valid; }

        public void MakeInvalid() { valid = false; }
        /// <summary> Set the m_Avatar from current data </summary>
        protected virtual void SetAvatar()
        {
            m_Avatar = m_Source switch
            {
                EntityDataSources.Player => Player.Avatar,
                EntityDataSources.Mirror => m_Mirror.Reflection,
                _ => m_RigManager.avatar,
            };
        }

        protected virtual void SetId()
        {
            try
            {
                m_id = GetPlayerId(m_RigManager);
            }
            catch (Exception e)
            {
                Logger.Dbg?.Warn(e.ToString());
                m_Source = EntityDataSources.Invalid;
            }
        }

        /// <summary> Update the avatar and the AvatarAnimatorDataContainer if found </summary>
        public void UpdateAvatar()
        {
            m_Barcode = m_RigManager?.AvatarCrate?.Barcode;
            SetAvatar();
            if (EntityDataSources.Invalid != m_Source)
                m_Cont = m_Avatar.gameObject.GetComponent<AvatarAnimatorDataContainer>();
            if (null != m_Cont)
            {
                try
                {
                    m_Data = AvatarAnimatorDataDeserializer.Deserialize(m_Cont.m_Version, m_Cont.m_SerializeData);
                    Logger.Msg($"AvatarAnimator '{Barcode.ToString()}' Data found version {m_Cont.m_Version}");
                }
                catch (Exception e)
                {
                    Logger.Warn(e.ToString());
                    m_Data = null;
                }
                Logger.Dbg?.Data(m_Cont.m_SerializeData);
            }

            valid = (null != m_Cont && null != m_Cont?.m_Animator && null != m_Data);
            Logger.Dbg?.Info(DebugEntityData());
        }

        public string DebugEntityData()
        {
            return $"id:'{m_id}', sc:{m_Source}, valid:{valid}, Rig:'{null != m_RigManager}', Avatar:'{null != m_Avatar}' {m_Barcode.ToString()}, Cont:'{null != m_Cont}', Anim:'{null != m_Cont?.m_Animator}'";
        }
    }

    /// <summary> Scan GameObjects for Mirrors </summary>
    public static class MirrorScanner
    {
        private static readonly List<EntityData> m_all = new();
        public static List<EntityData> All { get => m_all; }

        public static event Action<EntityData> OnNew;
        public static event Action<EntityData> OnRemoved;
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
                EntityData d = EntityData.Create(e);
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
        private static EntityData PlayerData = null;
        /// <summary> Avatar Change </summary>
        public static event Action<EntityData> OnAvatarChange;
        /// <summary> Level Change </summary>
        public static event Action<EntityData> OnAvatarSame;

        public static void GetAvatarAnimator()
        {
            Logger.Dbg?.Data($"OLD: {PlayerData?.Barcode?.ToString()} '{PlayerData?.Source}' '{null == PlayerData?.RigManager}'  -  NEW:{Player.RigManager.AvatarCrate.Barcode?.ToString()} '{Player.RigManager.AvatarCrate?.Crate?.Title}'");

            bool hasInvalidSource = EntityDataSources.Invalid == PlayerData?.Source;
            bool hasAvatarChange = Player.RigManager.AvatarCrate.Barcode != PlayerData?.Barcode; // if false => level change => new Data needed

            if (!hasAvatarChange || null == PlayerData || hasInvalidSource)
                PlayerData = EntityData.Create();
            else
                PlayerData.UpdateAvatar();

            Logger.Dbg?.Info($"GetPlayerAvatarAnimator hasAvatarChange:{hasAvatarChange}");
            if (hasAvatarChange)
                OnAvatarChange?.Invoke(PlayerData);
            else
                OnAvatarSame?.Invoke(PlayerData);
        }
    }
}
