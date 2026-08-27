using MelonLoader;

namespace AvatarAnimator
{
    public static class Logger
    {
        public class DebugLogger
        {
            public void Log(ConsoleColor color, string msg) { m_Logger.Msg(color, msg); }
            public void Log(string msg) { m_Logger.Msg(msg); }

            /// <summary> For class Debug functions </summary>
            public void Debug(string msg) { Log(ConsoleColor.Green, msg); }
            /// <summary> Generaly for raw data </summary>
            public void Data(string msg) { Log(ConsoleColor.Magenta, msg); }
            /// <summary> For Logging Data </summary>
            public void Info(string msg) { Log(ConsoleColor.Cyan, msg); }
            /// <summary> For Coding temporary logs that need to be more visible that other logs </summary>
            public void Highlight(string msg) { Log(ConsoleColor.DarkRed, msg); }
            /// <summary> For when you need to know how you manage to get here (i mean in the code) </summary>
            public void StackTrace() { Log(ConsoleColor.DarkRed, (new System.Diagnostics.StackTrace()).ToString()); }
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

        /// <summary> 
        /// Use Dbg nullable to add/remove without cost of computation of string debug Logs.
        /// Same as "if(Logger.DebugLogs) Logger.DbgInfo($"____")" but more compact and uniform.
        /// Better that "Logger.DbgInfo(() => $"_____")" lambda capture is costly. 
        /// </summary>
        public static DebugLogger Dbg { get => m_Dbg; }
        public static void Msg(string msg) { m_Logger.Msg(msg); }
        public static void Warn(string msg) { m_Logger.Warning(msg); }
        public static void Err(string msg) { m_Logger.Error(msg); }
    }
}
