using MelonLoader;
using BoneLib;

namespace AvatarAnimator
{
    public static class BuildInfo
    {
        public const string Name = "AvatarAnimator";
        public const string Author = "D";
        public const string Company = "";
        public const string Version = "1.0.0";
        public const string DownloadLink = "";
    }

    public class Core : MelonMod
    {
        private static bool enabled = false;
        private static bool updateAvatarChangeLater = false;

        private static TimeGate updateScanner;
        private static TimeGate updateAnim;

        public static bool IsLevelLoading { get => !enabled; }

        public event Action<PlayerStateChange> OnAvatarStateChanged;
        public event Action<ScannedData> OnPlayerAvatarChange;
        public event Action<ScannedData> OnPlayerAvatarSame;

        public override void OnInitializeMelon()
        {
            Config.Initialize();
            updateScanner = new UpdateTimeGate(Config.ScanMirrorsInterval);
            updateAnim = new UpdateTimeGate(Config.PlayerAnimatorUpdateInterval);
            Logger.Initialize(LoggerInstance);
            MenuUi.Initialize();
            PlayerAnimator.Initialize();
            FieldInjectorInteg.InjectFields();
            // Barcode and RigManager are not yet updated here
            Hooking.OnSwitchAvatarPostfix += (Il2CppSLZ.VRMK.Avatar avatar) =>
            {
                updateAvatarChangeLater = true;
            };
            Hooking.OnLevelUnloaded += () =>
            {
                enabled = false;
                Scanner.Clear();
            };
            Hooking.OnLevelLoading += (LevelInfo _) =>
            {
                enabled = false;
            };
            Hooking.OnLevelLoaded += (LevelInfo _) =>
            {
                enabled = true;
                updateScanner.Reset();
            };
            FusionLabLoader.Initialise();

            PlayerAnimator.OnAvatarStateChanged += OnAvatarStateChanged;
            Scanner.OnPlayerAvatarChange += OnPlayerAvatarChange;
            Scanner.OnPlayerAvatarSame += OnPlayerAvatarSame;
        }

        public override void OnUpdate()
        {
            if (!enabled) return;
            LocalInput.Update();
            if (updateAvatarChangeLater)
            {
                updateAvatarChangeLater = false;
                Scanner.GetPlayerAvatarAnimator();
            }
            if (updateScanner.Now()) Scanner.ScanForAvatarAnimator();
            if (updateAnim.Now()) PlayerAnimator.Update();
        }
    }
}
