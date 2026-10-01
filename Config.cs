using MelonLoader;
using MelonLoader.Preferences;

namespace AvatarAnimator
{
    public static class Config
    {
        private static readonly DelayTimeGate delay = new(20);
        private static bool SaveLaterActif = false;
        public static event Action<bool> OnSaveStateChange;

        private static MelonPreferences_Category cat;
        private static MelonPreferences_Entry<bool> debugLog;
        private static MelonPreferences_Entry<int> scanMirrorsInterval;
        public static int ScanMirrorsInterval { get => scanMirrorsInterval.Value; }
        public static ValueRange<int> ScanMirrorsIntervalRange { get => (ValueRange<int>)scanMirrorsInterval.Validator; }

        private static MelonPreferences_Entry<int> playerAnimatorUpdateInterval;
        public static int PlayerAnimatorUpdateInterval { get => playerAnimatorUpdateInterval.Value; }
        public static ValueRange<int> PlayerAnimatorUpdateIntervalRange { get => (ValueRange<int>)playerAnimatorUpdateInterval.Validator; }

        public static void Initialize()
        {
            cat = MelonPreferences.CreateCategory(BuildInfo.Name, "");
            debugLog = cat.CreateEntry("Debug_Logs", false);
            Logger.DebugLogs = debugLog.Value;
            scanMirrorsInterval = cat.CreateEntry("Scan_Mirrors_Interval", 20, "Delay in frame between each Mirror scan", validator: new ValueRange<int>(1, 60));
            playerAnimatorUpdateInterval = cat.CreateEntry("Player_Animator_Update_Interval", 2, "Delay in frame between each Player animator update", validator: new ValueRange<int>(1, 4));
            SavePreferences();
        }

        public static void SwitchDebugLog(bool on)
        {
            if (on == Logger.DebugLogs) return;
            Logger.DebugLogs = debugLog.Value = on;
            SavePreferencesLater();
        }
        public static void ChangeScanMirrorsInterval(int interval)
        {
            if (interval == scanMirrorsInterval.Value) return;
            CorePrivate.UpdateScanner.Interval = scanMirrorsInterval.Value = interval;
            SavePreferencesLater();
        }
        public static void ChangePlayerAnimatorUpdateInterval(int interval)
        {
            if (interval == playerAnimatorUpdateInterval.Value) return;
            CorePrivate.UpdateAnim.Interval = playerAnimatorUpdateInterval.Value = interval;
            SavePreferencesLater();
        }
        public static void SavePreferences()
        {
            MelonPreferences.Save();
            if (SaveLaterActif)
            {
                SaveLaterActif = false;
                OnSaveStateChange?.Invoke(SaveLaterActif);
                UpdateSystem.Remove(DelayUpdate);

            }
        }

        public static void SavePreferencesLater()
        {
            delay.Reset();
            if (SaveLaterActif) return;
            SaveLaterActif = true;
            OnSaveStateChange?.Invoke(SaveLaterActif);
            UpdateSystem.CallLater(DelayUpdate);
        }
        private static void DelayUpdate()
        {
            if (!delay.Now()) return;
            SavePreferences();
        }
    }
}
