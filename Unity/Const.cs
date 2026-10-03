#if UNITY_EDITOR

using System.Text.RegularExpressions;

namespace AvatarAnimator
{
    public static class Const
    {
        public static readonly string LayerName = "AvatarAnimator";
        public static readonly string TransitionInputsSeparator = ";";
        public static readonly string InputTypeSeparator = ":";
        public static readonly string SecondaryInputSeparator = "+";

        public static readonly Regex IsRandomType = new(@"Random\((?:(\d+)|(\d+),[ ]*(\d+))\)", RegexOptions.IgnoreCase);
        public static readonly string IsHealth = "Health";
        public static readonly string IsInput = "Input=";
        public static readonly string IsTimer = "Timer";
        public static readonly string IsWaitEndClip = "WaitEndClip";
        public static readonly Regex IsCyclic = new(@"Cyclic\((\d+)\)", RegexOptions.IgnoreCase);
    }
}

#endif // UNITY_EDITOR