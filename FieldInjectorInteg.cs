using FieldInjector;

namespace AvatarAnimator
{
    public class FieldInjectorInteg
    {
        public static void InjectFields()
        {
            Logger.Msg("AvatarAnimator components injection start!");
            // Required for "GetComponent"
            SerialisationHandler.Inject<AvatarAnimatorDataContainer>();
            Logger.Msg("AvatarAnimator components injected successfully!");
        }
    }
}
