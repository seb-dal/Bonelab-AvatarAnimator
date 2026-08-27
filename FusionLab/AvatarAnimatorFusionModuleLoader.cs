using LabFusion.SDK.Modules;

namespace AvatarAnimator.FusionLab
{
    public static class AvatarAnimatorFusionModuleLoader
    {
        public static void LoadModule()
        {
            ModuleManager.RegisterModule<AvatarAnimatorFusionModule>();
        }
    }
}
