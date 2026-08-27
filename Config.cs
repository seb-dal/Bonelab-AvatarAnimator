using MelonLoader;
using MelonLoader.Preferences;

namespace AvatarAnimator
{
    public static class Config
    {
        public static int ScanMirrorsInterval;
        public static int PlayerAnimatorUpdateInterval;

        public static void Initialize()
        {
            var category = MelonPreferences.CreateCategory(BuildInfo.Name, "");
            Logger.DebugLogs = CreateAndGetEntry(category, "Debug_Logs", false);
            ScanMirrorsInterval = CreateAndGetEntry(category, "Scan_Mirrors_Interval", 20, "Delay in frame between each Mirror scan", validator: new ValueRange<int>(1, int.MaxValue));
            PlayerAnimatorUpdateInterval = CreateAndGetEntry(category, "Player_Animator_Update_Interval", 2, "Delay in frame between each Player animator update", validator: new ValueRange<int>(1, int.MaxValue));
            MelonPreferences.Save();
        }

        private static T CreateAndGetEntry<T>(MelonPreferences_Category cat, string identifier, T default_value, string description = null, ValueValidator validator = null)
        {
            cat.CreateEntry(identifier, default_value, null, description, false, false, validator);
            return cat.GetEntry<T>(identifier).Value;
        }
    }
}
