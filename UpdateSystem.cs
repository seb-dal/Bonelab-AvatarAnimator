namespace AvatarAnimator
{
    public delegate bool FuncTmp();
    public delegate void Func();

    /// <summary>
    /// Allow to temporarily have functions being called by Core.OnUpdate()
    /// 
    /// Notes: 
    /// - Remove with Func arg must only be used by named function or the same lambda that was used to add not another one with the same inner body.
    /// </summary>
    public static class UpdateSystem
    {
        private static readonly Dictionary<string, Func> ToDoLaterOnce = new();
        private static readonly Dictionary<string, FuncTmp> ToDoLaterTmp = new();
        private static readonly Dictionary<string, Func> ToDoLater = new();

        public static void CallLaterOnce(string name, Func func) => AddOrReplace(ToDoLaterOnce, name, func);
        public static void CallLaterOnce(Func func) => CallLaterOnce($"{func.GetHashCode()}", func);
        public static bool RemoveLater(string name) => ToDoLaterOnce.Remove(name);
        public static bool RemoveLater(Func func) => RemoveLater($"{func.GetHashCode()}");

        public static void CallLaterTmp(string name, FuncTmp func) => AddOrReplace(ToDoLaterTmp, name, func);
        public static void CallLaterTmp(FuncTmp func) => CallLaterTmp($"{func.GetHashCode()}", func);
        public static bool RemoveTmp(string name) => ToDoLaterTmp.Remove(name);
        public static bool RemoveTmp(FuncTmp func) => RemoveTmp($"{func.GetHashCode()}");

        public static void CallLater(string name, Func func) => AddOrReplace(ToDoLater, name, func);
        public static void CallLater(Func func) => CallLater($"{func.GetHashCode()}", func);
        public static bool Remove(string name) => ToDoLater.Remove(name);
        public static bool Remove(Func func) => Remove($"{func.GetHashCode()}");

        public static void Update()
        {
            if (ToDoLaterOnce.Count > 0)
            {
                var array = ToDoLaterOnce.ToArray();
                ToDoLaterOnce.Clear();
                foreach (var func in array)
                {
                    try { func.Value(); }
                    catch (Exception e) { Logger.Err(e.ToString()); }
                }
            }

            if (ToDoLaterTmp.Count > 0)
            {
                foreach (var func in ToDoLaterTmp.ToArray())
                {
                    try { if (func.Value()) ToDoLaterTmp.Remove(func.Key); }
                    catch (Exception e) { Logger.Err(e.ToString()); }
                }
            }

            if (ToDoLater.Count > 0)
            {
                foreach (var func in ToDoLater.ToArray())
                {
                    try { func.Value(); }
                    catch (Exception e) { Logger.Err(e.ToString()); }
                }
            }
        }

        private static void AddOrReplace<TKey, TValue>(Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (dic.ContainsKey(key)) dic[key] = value;
            else dic.Add(key, value);
        }
    }
}
