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
        public static bool IsLevelLoading { get => !enabled; }

        public static event Action OnUpdateEvt;

        public event Action<PlayerStateChange> OnAvatarStateChanged;
        public event Action<ScannedData> OnPlayerAvatarChange;
        public event Action<ScannedData> OnPlayerAvatarSame;
        public void PlayState(int layerIndex, string state) => PlayerAnimator.PlayState(layerIndex, state);

        public override void OnInitializeMelon()
        {
            Config.Initialize();
            CorePrivate.Initalize();
            Logger.Initialize(LoggerInstance);
            MenuUi.Initialize();
            PlayerAnimator.Initialize();
            FieldInjectorInteg.InjectFields();
            Hooking.OnLevelUnloaded += () =>
            {
                enabled = false;
                MirrorScanner.Clear();
            };
            Hooking.OnLevelLoading += (LevelInfo _) => { enabled = false; };
            Hooking.OnLevelLoaded += (LevelInfo _) =>
            {
                enabled = true;
                CorePrivate.UpdateScanner.Reset();
            };
            FusionLabLoader.Initialise();

            // Other mods hook
            PlayerAnimator.OnAvatarStateChanged += OnAvatarStateChanged;
            PlayerScanner.OnAvatarChange += OnPlayerAvatarChange;
            PlayerScanner.OnAvatarSame += OnPlayerAvatarSame;
        }

        public override void OnUpdate()
        {
            if (!enabled) return;
            LocalInput.Update();
            OnUpdateEvt?.Invoke();
            if (CorePrivate.UpdateScanner.Now()) MirrorScanner.Scan();
            if (CorePrivate.UpdateAnim.Now()) PlayerAnimator.Update();
        }
    }

    // Keep "public functions" public but remove from Core External API
    public class CorePrivate
    {
        private static UpdateTimeGate updateScanner;
        private static UpdateTimeGate updateAnim;
        private static bool switchAvatarHooked = false;
        private static bool updateAvatarChangeLater = false;

        public static UpdateTimeGate UpdateScanner { get => updateScanner; }
        public static UpdateTimeGate UpdateAnim { get => updateAnim; }

        public static void Initalize()
        {
            updateScanner = new UpdateTimeGate(Config.ScanMirrorsInterval);
            updateAnim = new UpdateTimeGate(Config.PlayerAnimatorUpdateInterval);
        }
        private static void UpdateAvatarChangeLater()
        {
            PlayerScanner.GetAvatarAnimator();
            Core.OnUpdateEvt -= UpdateAvatarChangeLater;
            updateAvatarChangeLater = false;
        }
        public static void UpdatePlayerAvatar()
        {
            if (updateAvatarChangeLater) return;
            Core.OnUpdateEvt += UpdateAvatarChangeLater;
            updateAvatarChangeLater = true;
        }

        private static void OnSwitchAvatarPostfix(Il2CppSLZ.VRMK.Avatar _) { UpdatePlayerAvatar(); }
        public static void SimplePlayerMonitoring()
        {
            if (switchAvatarHooked) return;
            Logger.Dbg?.Info("Simple PlayerMonitoring");
            Hooking.OnSwitchAvatarPostfix += OnSwitchAvatarPostfix;
            switchAvatarHooked = true;
        }
        public static void MultiPlayerMonitoring()
        {
            if (!switchAvatarHooked) return;
            Logger.Dbg?.Info("Multiplayer PlayerMonitoring");
            Hooking.OnSwitchAvatarPostfix -= OnSwitchAvatarPostfix;
            switchAvatarHooked = false;
        }
    }
}

