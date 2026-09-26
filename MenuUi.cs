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
        private static readonly List<FunctionElement> ListAction = new();

        public static void Initialize()
        {
            m_modPage = Page.Root.CreatePage(BuildInfo.Name, Color.cyan, 0, true);

            m_AvatarStatePage = m_modPage.CreatePage("Play Avatar state", Color.green);
            m_ConfigPage = m_modPage.CreatePage("Configs", Color.white);

            m_ConfigPage.CreateBool("Debug logs", Color.white, Logger.DebugLogs, (bool on) => Config.SwitchDebugLog(on));
            var smiRange = Config.ScanMirrorsIntervalRange;
            m_ConfigPage.CreateInt("Scan Mirror Interval", Color.white, Config.ScanMirrorsInterval, 1, smiRange.MinValue, smiRange.MaxValue, (int interval) => Config.ChangeScanMirrorsInterval(interval));
            var pauiRange = Config.PlayerAnimatorUpdateIntervalRange;
            m_ConfigPage.CreateInt("Player animator update Interval", Color.white, Config.PlayerAnimatorUpdateInterval, 1, pauiRange.MinValue, pauiRange.MaxValue, (int interval) => Config.ChangeScanMirrorsInterval(interval));

            PlayerScanner.OnAvatarChange += MenuUi.OnAvatarChanged;
        }


        public static void OnAvatarChanged(ScannedData player)
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
                    AddButton($"{layer}) {stateName}", Color.white, () => PlayerAnimator.PlayState(layer, stateName));
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
