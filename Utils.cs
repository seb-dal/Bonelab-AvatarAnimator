#if UNITY_EDITOR || IS_AVATAR_ANIMATOR_CORE_MOD

using System;
using System.Collections.Generic;

namespace AvatarAnimator
{
    public delegate bool FindFunc<in T>(T arg);
    public delegate bool Compare<in T>(T a, T b);
    
    public static class Utils
    {
        private static readonly System.Random rnd = new();
        public static bool Is(ConditionMode? ope, float value, float threshold = 0)
        {
            return ope switch
            {
                ConditionMode.Greater => value > threshold,
                ConditionMode.Less => value < threshold,
                ConditionMode.If => 1.0f == value,
                ConditionMode.IfNot => 0.0f == value,
                ConditionMode.Equals => (int)value == (int)threshold,
                ConditionMode.NotEqual => (int)value != (int)threshold,
                _ => false,
            };
        }
        public static bool Is(ConditionMode? ope, int value, int threshold = 0)
        {
            return ope switch
            {
                ConditionMode.Greater => value > threshold,
                ConditionMode.Less => value < threshold,
                ConditionMode.If => 1 == value,
                ConditionMode.IfNot => 0 == value,
                ConditionMode.Equals => value == threshold,
                ConditionMode.NotEqual => value != threshold,
                _ => false,
            };
        }

        public static DateTime DateTimeNowPlusSecs(double sec) => DateTime.Now.AddSeconds(sec);

        public static int RandomInt(int min, int max) => rnd.Next(min, max);

        /// <summary> Object.ReferenceEquals but enforce same Type </summary>
        public static bool RefEquals<T>(T obj1, T obj2) => ReferenceEquals(obj1, obj2);

        public static float ComputNTime(DateTime now, DateTime playAt, float? m_nTime, float? m_Duration, float? m_Speed)
        {
            var diff = (float)(now - playAt).TotalSeconds;
            float nTime = m_nTime ?? 0.0f;
            float d = m_Duration ?? 0.0f;
            float s = m_Speed ?? 1.0f;
            Logger.Dbg?.Info($"Tdiff:{diff} nTime:{nTime} d:{d} s:{s}");
            if (d != 0.0f && s != 0.0f) nTime += diff / (d * (1 / s));
            Logger.Dbg?.Info($"nTime:{nTime}");
            return nTime;
        }

        public static void AddOrReplace<TKey, TValue>(Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (dic.ContainsKey(key)) dic[key] = value;
            else dic.Add(key, value);
        }

        public static bool ListEquals<T>(List<T> a, List<T> b, Compare<T> comp)
        {
            if (a?.Count != b?.Count) return false;
            for (int i = 0; i > a.Count; ++i) { if (!comp(a[i], b[i])) return false; }
            return true;
        }
    }

    public class Pair<T, U>
    {
        public Pair() { }
        public Pair(T first, U second) { First = first; Second = second; }
        public T First { get; set; }
        public U Second { get; set; }
    };
}

#endif // UNITY_EDITOR || IS_AVATAR_ANIMATOR_CORE_MOD