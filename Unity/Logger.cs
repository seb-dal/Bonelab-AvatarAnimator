#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Logger
{
    public class DebugLogger
    {
        public void Log(string msg) => LoggerGUI.Log(msg);

        public void Debug(string msg) => Log("<color=Green>" + msg + "</color>");
        public void Data(string msg) => Log("<color=Magenta>" + msg + "</color>");
        public void Info(string msg) => Log("<color=Cyan>" + msg + "</color>");
        public void Highlight(string msg) => Log("<color=DarkRed>" + msg + "</color>");
        public void StackTrace() => Log("<color=DarkRed>" + (new System.Diagnostics.StackTrace()).ToString() + "</color>");
        public void Warn(string msg) => Log("<color=Yellow>" + msg + "</color>");
        public void Err(string msg) => Log("<color=Red>" + msg + "</color>");
    }

    public static readonly DebugLogger Dbg = new();
    public static void Msg(string msg) => LoggerGUI.Log(msg);
    public static void Info(string msg) => LoggerGUI.Log("<color=cyan>" + msg + "</color>");
    public static void Warn(string msg) => LoggerGUI.Log("<color=yellow>" + msg + "</color>");
    public static void Err(string msg) => LoggerGUI.Log("<color=red>" + msg + "</color>");
}

public static class LoggerGUI
{
    private static readonly List<string> logs = new();

    public static void Log(string msg) => logs.Add(msg);
    public static void Reset() => logs.Clear();

    private static Vector2 gui_scrollPosition;
    private static float gui_scrollViewHeight = 160f;
    private static bool gui_isResizing;
    private static Rect gui_resizerRect;
    public static void Gui()
    {
        gui_scrollPosition = EditorGUILayout.BeginScrollView(gui_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(gui_scrollViewHeight));
        GUIStyle style = new();
        style.richText = true;
        style.normal.textColor = Color.white;
        foreach (var log in logs)
        {
            GUILayout.TextArea(log, style);
        }
        EditorGUILayout.EndScrollView();

        // Barre de redimensionnement
        gui_resizerRect = GUILayoutUtility.GetRect(gui_resizerRect.width, 5f);
        EditorGUIUtility.AddCursorRect(gui_resizerRect, MouseCursor.ResizeVertical);
        GUI.Box(gui_resizerRect, "", "WindowBottomResize");

        // Gestion logique du redimensionnement
        Event e = Event.current;
        if (e.type == EventType.MouseDown && gui_resizerRect.Contains(e.mousePosition)) gui_isResizing = true;
        if (gui_isResizing)
        {
            gui_scrollViewHeight = Mathf.Clamp(e.mousePosition.y, 50f, 400f);
        }
        if (e.type == EventType.MouseUp) gui_isResizing = false;
    }
}


#endif // UNITY_EDITOR