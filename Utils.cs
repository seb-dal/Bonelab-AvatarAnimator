using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using UnityEngine;
using static Il2CppSLZ.VRMK.Avatar;

namespace AvatarAnimator
{
    public delegate byte PlayerIdGetterFunc(RigManager rig);
    public delegate bool FindFunc<in T>(T arg);

    public static class Utils
    {
        private static readonly System.Random rnd = new();
        public static bool Is(ConditionMode? ope, float value, float threshold = 0)
        {
            return ope switch
            {
                ConditionMode.Greater => value > threshold,
                ConditionMode.Less => value < threshold,
                ConditionMode.If => 1.0f == value,
                ConditionMode.IfNot => 0.0f == value,
                ConditionMode.Equals => (int)value == (int)threshold,
                ConditionMode.NotEqual => (int)value != (int)threshold,
                _ => false,
            };
        }
        public static bool Is(ConditionMode? ope, int value, int threshold = 0)
        {
            return ope switch
            {
                ConditionMode.Greater => value > threshold,
                ConditionMode.Less => value < threshold,
                ConditionMode.If => 1 == value,
                ConditionMode.IfNot => 0 == value,
                ConditionMode.Equals => value == threshold,
                ConditionMode.NotEqual => value != threshold,
                _ => false,
            };
        }

        public static DateTime DateTimeNowPlusSecs(double sec) => DateTime.Now.AddSeconds(sec);

        public static int RandomInt(int min, int max) => rnd.Next(min, max);

        /// <summary> Object.ReferenceEquals but enforce same Type </summary>
        public static bool RefEquals<T>(T obj1, T obj2) => ReferenceEquals(obj1, obj2);

        /// <summary> Override by FusionLab Integration </summary>
        public static PlayerIdGetterFunc GetPlayerId = (RigManager _) => 0;

        public static float ComputNTime(DateTime now, DateTime playAt, float? m_nTime, float? m_Duration, float? m_Speed)
        {
            var diff = (float)(now - playAt).TotalSeconds;
            float nTime = m_nTime ?? 0.0f;
            float d = m_Duration ?? 0.0f;
            float s = m_Speed ?? 1.0f;
            Logger.Dbg?.Info($"Tdiff:{diff} nTime:{nTime} d:{d} s:{s}");
            if (d != 0.0f && s != 0.0f) nTime += diff / (d * (1 / s));
            Logger.Dbg?.Info($"nTime:{nTime}");
            return nTime;
        }

        public static void AddOrReplace<TKey, TValue>(Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (dic.ContainsKey(key)) dic[key] = value;
            else dic.Add(key, value);
        }
    }

    public static class Debug
    {
        public static void GetAvatarMetadata(Il2CppSLZ.VRMK.Avatar avatar)
        {
            if (avatar == null) return;
            RigManager rigManager = avatar.GetComponentInParent<RigManager>();
            if (rigManager != null && rigManager.AvatarCrate != null)
            {
                var crate = rigManager.AvatarCrate.Crate;
                string barcode = crate.Barcode.ToString(); // Ex: "vrad.Avatar.Heavy"
                string avatarName = crate.Title;

                Pallet pallet = crate.Pallet;
                string palletName = pallet != null ? pallet.Title : "Inconnue";

                Logger.Dbg?.Debug($"Avatar actif : {avatarName} (Barcode: {barcode}) | Palette: {palletName}");
            }
            else
            {
                Logger.Dbg?.Debug($"Avatar actif : {avatar.gameObject.name}");
            }
        }

        public static void InspectGameObject(GameObject target)
        {
            Transform trans = target.transform;
            for (int i = 0; i < trans.childCount; i++)
            {
                Transform child = trans.GetChild(i);
                Logger.Dbg?.Debug($"Child IL2CPP : {child.name}");
            }
            foreach (var comp in target.GetComponents<Component>())
            {
                if (comp == null) continue;
                var nativeType = comp.GetIl2CppType();
                Logger.Dbg?.Debug($" Component IL2CPP : {nativeType.FullName}");
            }

            foreach (var comp in target.GetComponentsInParent<Component>())
            {
                if (comp == null) continue;
                var nativeType = comp.GetIl2CppType();
                Logger.Dbg?.Debug($" Parent Component IL2CPP : {nativeType.FullName}");
            }
        }

        public static string ToString(HandSchematic hand)
        {
            return $"{hand.thumb1} ({hand.thumb2} {hand.thumb3})  {hand.index1} ({hand.index2} {hand.index3})   {hand.middle1} ({hand.middle2} {hand.middle3})   {hand.ring1} ({hand.ring2} {hand.ring3})   {hand.pinky1} ({hand.pinky2} {hand.pinky3})";
        }
    }
}

