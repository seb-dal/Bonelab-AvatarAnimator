using MelonLoader;

namespace AvatarAnimator
{
    public static class Logger
    {
        public class DebugLogger
        {
            public void Log(ConsoleColor color, string msg) { m_Logger.Msg(color, msg); }
            public void Log(string msg) { m_Logger.Msg(msg); }

            public void Debug(string msg) { Log(ConsoleColor.Green, "[Dbg-Debug] " + msg); }
            public void Data(string msg) { Log(ConsoleColor.Magenta, "[Dbg-Data] " + msg); }
            public void Info(string msg) { Log(ConsoleColor.Cyan, "[Dbg-Info] " + msg); }
            public void Highlight(string msg) { Log(ConsoleColor.DarkRed, "[Dbg-High] " + msg); }
            public void StackTrace() { Log(ConsoleColor.DarkRed, "[StackTrace] " + (new System.Diagnostics.StackTrace()).ToString()); }
            public void Warn(string msg) { Log(ConsoleColor.Yellow, "[Dbg-W] " + msg); }
            public void Err(string msg) { Log(ConsoleColor.Red, "[Dbg-E] " + msg); }
        }

        private static MelonLogger.Instance m_Logger;
        private static DebugLogger m_Dbg;

        private static bool m_DebugLogs = false;
        public static bool DebugLogs
        {
            get => m_DebugLogs;
            set
            {
                m_DebugLogs = value;
                m_Dbg = (value ? new() : null);
            }
        }

        public static void Initialize(MelonLogger.Instance logger) { m_Logger = logger; }

        public static DebugLogger Dbg { get => m_Dbg; }
        public static void Msg(string msg) { m_Logger.Msg(msg); }
        public static void Warn(string msg) { m_Logger.Warning("[W] " + msg); }
        public static void Err(string msg) { m_Logger.Error("[E] " + msg); }
    }
}
