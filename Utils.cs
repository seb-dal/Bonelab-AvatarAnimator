
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Warehouse;
using UnityEngine;

namespace AvatarAnimator
{
    public delegate byte PlayerIdGetter(RigManager rig);

    public class Utils
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

        public static DateTime DateTimeNowPlusSecs(double sec) => DateTime.Now.AddSeconds(sec);

        public static int RandomInt(int min, int max) => rnd.Next(min, max);

        // Enforce same Type
        public static bool RefEquals<T>(T obj1, T obj2) => ReferenceEquals(obj1, obj2);

        public static PlayerIdGetter GetPlayerId = (RigManager _) => 0;
    }

    public class Debug
    {
        public static void GetAvatarMetadata(Il2CppSLZ.VRMK.Avatar avatar)
        {
            if (avatar == null) return;
            RigManager rigManager = avatar.GetComponentInParent<RigManager>();
            if (rigManager != null && rigManager.AvatarCrate != null)
            {
                var crate = rigManager.AvatarCrate.Crate;
                string barcode = crate.Barcode.ToString(); // Ex: "vrad.Avatar.Heavy"
                string avatarName = crate.Title;           // Nom d'affichage

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
            // Récupérer le transform natif mappé
            Transform trans = target.transform;

            // Parcourir la hiérarchie
            for (int i = 0; i < trans.childCount; i++)
            {
                Transform child = trans.GetChild(i);
                Logger.Dbg?.Debug($"Enfant IL2CPP : {child.name}");
            }

            // Récupérer les composants natifs
            foreach (var comp in target.GetComponents<Component>())
            {
                if (comp != null)
                {
                    // Il2CppType permet de récupérer le vrai type C++ sous-jacent
                    var nativeType = comp.GetIl2CppType();
                    Logger.Dbg?.Debug($" Composant IL2CPP : {nativeType.FullName}");
                }
            }

            foreach (var comp in target.GetComponentsInParent<Component>())
            {
                if (comp != null)
                {
                    // Il2CppType permet de récupérer le vrai type C++ sous-jacent
                    var nativeType = comp.GetIl2CppType();
                    Logger.Dbg?.Debug($" Parent Composant IL2CPP : {nativeType.FullName}");
                }
            }
        }
    }
}
