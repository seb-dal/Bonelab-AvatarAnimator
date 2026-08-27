using UnityEngine;
using BoneLib.BoneMenu;

namespace AvatarAnimator
{
    // Use to allow players to force/manually change the Avatar Animator States
    public static class MenuUi
    {
        private static Page m_modPage;
        private static Page m_AvatarStatePage;
        private static readonly List<FunctionElement> ListAction = new();

        public static void Initialize()
        {
            m_modPage = Page.Root.CreatePage(BuildInfo.Name, Color.cyan, 0, true);
            m_AvatarStatePage = m_modPage.CreatePage("Play Avatar state", Color.green);
            Scanner.OnPlayerAvatarChange += MenuUi.OnAvatarChanged;
        }

        public static void OnAvatarChanged(ScannedData player)
        {
            Logger.Dbg?.Info($"Update MenuUi AvatarStatePage, removing {ListAction.Count} button");
            foreach (var elem in ListAction)
            {
                m_AvatarStatePage.Remove(elem);
            }
            ListAction.Clear();
            if (null == player?.Container?.m_Data)
            {
                Logger.Dbg?.Info($"MenuUi: No Avatar Animator data");
                ListAction.Add(m_AvatarStatePage.CreateFunction($"Avatar doesn't have Animation", Color.red, () => { }));
                return;
            }
            Logger.Dbg?.Info($"MenuUi: Avatar Animator data found");
            foreach (var lay in player.Data.m_ListLayer)
            {
                foreach (var state in lay.m_States)
                {
                    ListAction.Add(m_AvatarStatePage.CreateFunction($"{lay.m_layerIndex} {state.Key}", Color.gray, () => PlayerAnimator.PlayState(lay.m_layerIndex, state.Key)));
                }
            }
        }
    }
}
