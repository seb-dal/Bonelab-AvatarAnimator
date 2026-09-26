using UnityEngine;
using BoneLib.BoneMenu;

namespace AvatarAnimator
{
    // Use to allow players to force/manually change the Avatar Animator States
    public static class MenuUi
    {
        private static Page m_modPage;
        private static Page m_AvatarStatePage;
        private static Page m_ConfigPage;
        private static FunctionElement m_ConfigSaveButton;
        private static readonly List<FunctionElement> ListAction = new();

        public static void Initialize()
        {
            m_modPage = Page.Root.CreatePage(BuildInfo.Name, Color.cyan);

            m_AvatarStatePage = m_modPage.CreatePage("Play Avatar state", Color.green);
            PlayerScanner.OnAvatarChange += MenuUi.OnAvatarChanged;
            InitializeConfigPage();
        }

        private static void InitializeConfigPage()
        {
            m_ConfigPage = m_modPage.CreatePage("Configs", Color.white);

            m_ConfigSaveButton = m_ConfigPage.CreateFunction("Save", Color.white, () => Config.SavePreferences());
            SaveButtonState(false);
            m_ConfigSaveButton.ElementTooltip = "Save configuration changes \n Black: No change, Cyan: Change pending (auto-save after 20 sec)";

            var dbg = m_ConfigPage.CreateBool("Debug Logs", Color.white, Logger.DebugLogs, (bool on) => Config.SwitchDebugLog(on));
            dbg.ElementTooltip = "Enable Debug logs";

            var smiRange = Config.ScanMirrorsIntervalRange;
            var scan = m_ConfigPage.CreateInt("Scan Mirror Interval", Color.white, Config.ScanMirrorsInterval, 1, smiRange.MinValue, smiRange.MaxValue, (int interval) => Config.ChangeScanMirrorsInterval(interval));
            scan.ElementTooltip = $"Number of update before a scan for Mirror enities [{smiRange.MinValue}; {smiRange.MaxValue}] default: 20 \n This may affect performance if set too low.";

            var pauiRange = Config.PlayerAnimatorUpdateIntervalRange;
            var update = m_ConfigPage.CreateInt("Player animator update Interval", Color.white, Config.PlayerAnimatorUpdateInterval, 1, pauiRange.MinValue, pauiRange.MaxValue, (int interval) => Config.ChangePlayerAnimatorUpdateInterval(interval));
            update.ElementTooltip = $"Number of update before a an update for the player Animator [{pauiRange.MinValue}; {pauiRange.MaxValue}] default: 2";
        }
        public static void SaveButtonState(bool ModificationPending)
        {
            if (null == m_ConfigSaveButton) return;
            m_ConfigSaveButton.ElementColor = (ModificationPending ? Color.cyan : Color.black);
        }

        public static void OnAvatarChanged(EntityData player)
        {
            var removed = ListAction.Count;
            foreach (var elem in ListAction) { m_AvatarStatePage.Remove(elem); }
            ListAction.Clear();
            if (null == player?.Container?.m_Data)
            {
                Logger.Dbg?.Info($"MenuUi: No Avatar Animator data");
                AddButton($"Avatar doesn't have Animation", Color.red, () => { });
                return;
            }
            Logger.Dbg?.Info($"MenuUi: Avatar Animator data found");
            foreach (var lay in player.Data.ListLayer)
            {
                foreach (var state in lay.States)
                {
                    // Capture only the variables and not the container
                    var layer = lay.LayerIndex;
                    var stateName = state.Key;
                    AddButton($"{layer} - {stateName}", Color.white, () => PlayerAnimator.PlayState(layer, stateName));
                }
            }
            Logger.Dbg?.Info($"Update MenuUi AvatarStatePage, removed {removed}, Added {ListAction.Count} buttons");
        }
        private static void AddButton(string text, Color textColor, Action func)
        {
            ListAction.Add(m_AvatarStatePage.CreateFunction(text, textColor, func));
        }
    }
}
