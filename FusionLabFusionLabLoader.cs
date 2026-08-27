using AvatarAnimator.FusionLab;
using MelonLoader;

namespace AvatarAnimator
{
    public static class FusionLabLoader
    {
        public static void Initialise()
        {
            if (MelonBase.FindMelon("LabFusion", "Lakatrazz") != null)
            {
                Logger.Msg("LabFusion detected - registering multiplayer module...");
                try
                {
                    AvatarAnimatorFusionModuleLoader.LoadModule();
                    Logger.Msg("Fusion module registered successfully!");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Err("Error registering Fusion module: " + ex.Message);
                    return;
                }
            }
            else Logger.Msg("LabFusion not detected - running in singleplayer mode");
        }
    }
}
