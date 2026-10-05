using System;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections.Generic;

#if IS_AVATAR_ANIMATOR_CORE_MOD

#elif !UNITY_EDITOR

namespace AvatarAnimator
{
    public class AvatarAnimatorTestScript : MonoBehaviour { }
}

#elif UNITY_EDITOR

namespace AvatarAnimator
{
    public delegate void Action();
    public class AvatarAnimatorTestScript : MonoBehaviour
    {
        public static event Action OnUpdate;
        public void Update() { OnUpdate?.Invoke(); }
    }

    [CustomEditor(typeof(AvatarAnimatorTestScript))]
    [DisallowMultipleComponent]
    public class AvatarAnimatorTestEditor : Editor
    {
        private const int AlignButtons = 3;

        AvatarAnimatorTestScript container;

        private readonly List<AvatarAnimatorDataContainer> m_avatars = new();
        private readonly List<string> m_avatarsName = new();
        private string[] avatarsName = new string[] { };
        private List<string> m_currAvName;

        public int SelectedIndex = 0;

        public float Health = 1f;
        public string HealthRealName = "";
        public bool HasHealth = false;

        private void OnEnable()
        {
            container = (AvatarAnimatorTestScript)target;
            AvatarAnimatorTestScript.OnUpdate += Update;
            Start();
        }

        public void Start()
        {
            m_avatars.Clear();
            m_avatars.AddRange(GameObject.FindObjectsOfType<AvatarAnimatorDataContainer>());
            m_avatarsName.Clear();
            foreach (var avatar in m_avatars)
            {
                AvatarAnimatorData tmp = null;
                if ("" != avatar.m_SerializeData) tmp = JsonConvert.DeserializeObject<AvatarAnimatorData>(avatar.m_SerializeData);
                bool avatarData = null != avatar.m_Data?.ListLayer;
                if (avatarData && avatar.m_Data?.ListLayer.Count > 0)
                {
                    var tmpLay = avatar.m_Data?.ListLayer[0];
                    avatarData = null != tmpLay.States;
                }
                if (avatarData || null != tmp) m_avatarsName.Add(avatar.name);
                if (avatarData && null != tmp)
                {
                    if (DateTime.Parse(tmp.Date) > DateTime.Parse(avatar.m_Data.Date)) { avatar.m_Data = tmp; }
                }
                else if (!avatarData && null != tmp) { avatar.m_Data = tmp; }
            }

            PlayerAnimatorCore.OnStateChange += (int layer, string state, bool playState) =>
            {
                if (playState)
                {
                    var avatar = m_avatars[SelectedIndex];
                    avatar.m_Animator.Play(state, layer);
                }
                PlayerAnimatorCore.PlayState(layer, state);
            };

            PlayerAnimatorCore.getPlayerHealth = () => Health;

            SetActifAvatar(0);
        }

        public void Update()
        {
            if (!PlayerAnimatorCore.IsInitialized()) return;
            PlayerAnimatorCore.Update();
        }

        public void SetActifAvatar(int index)
        {
            SelectedIndex = index;
            var avatar = m_avatars[index];
            PlayerAnimatorCore.SetAvatar(avatar.m_Animator, avatar.m_Data);
            PlayerAnimatorCore.Clear();
            PlayerAnimatorCore.Initialize();

            HasHealth = false;
            foreach (var parms in avatar.m_Animator.parameters)
            {
                HasHealth = parms.name.Equals(Const.IsHealth, StringComparison.CurrentCultureIgnoreCase);
                if (HasHealth)
                {
                    Health = 1f;
                    HealthRealName = parms.name;
                    avatar.m_Animator.SetFloat(parms.name, 1f);
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (!Utils.ListEquals(m_currAvName, m_avatarsName, (string a, string b) => a == b))
            {
                avatarsName = m_avatarsName.ToArray();
                m_currAvName = new(avatarsName);
            }
            if (null == container || avatarsName.Length == 0) return;

            if (!EditorApplication.isPlaying)
            {
                GUILayout.Label("Only available in Play Mode");
                return;
            }

            var index = EditorGUILayout.Popup("Avatar", SelectedIndex, avatarsName);
            var avatar = m_avatars[index];
            GUILayout.Space(20);

            GUILayout.Label("Animations");
            if (SelectedIndex != index) { SetActifAvatar(index); }

            foreach (var lay in avatar.m_Data.ListLayer)
            {
                int i = 0;
                var curr = PlayerAnimatorCore.GetLayer(lay.LayerIndex);
                foreach (var state in lay.States)
                {
                    if (i % AlignButtons == 0) GUILayout.BeginHorizontal();
                    bool current = state.Key == curr.m_CurrentStateName;
                    bool res = GUILayout.Toggle(state.Key == curr.m_CurrentStateName, $"{lay.LayerIndex} - {state.Key}", "Button");
                    if (res && state.Key != curr.m_CurrentStateName)
                    {
                        avatar.m_Animator.Play(state.Key, lay.LayerIndex);
                        PlayerAnimatorCore.PlayState(lay.LayerIndex, state.Key);
                    }
                    i += 1;
                    if (i % AlignButtons == 0) GUILayout.EndHorizontal();
                }
                if (i % AlignButtons != 0) GUILayout.EndHorizontal();
            }

            if (HasHealth)
            {
                GUILayout.Space(20);
                GUILayout.Label($"Avatar Health ({Math.Round(Health * 100)}%)");
                var health = GUILayout.HorizontalSlider(Health, 0.0f, 1.0f);
                if (Health != health)
                {
                    avatar.m_Animator.SetFloat("Health", health);
                    Health = health;
                }
                GUILayout.Space(20);
            }

            GUILayout.Space(20);
            foreach (var trans in avatar.m_Data.TransitionsData)
            {
                switch (trans.Value.Type)
                {
                    case ConditionType.Random:
                    case ConditionType.Cyclic:
                        var currValue = avatar.m_Animator.GetInteger(trans.Key);
                        var newValue = Mathf.Clamp(EditorGUILayout.IntField(trans.Key, currValue), trans.Value.Min, trans.Value.Max);
                        if (currValue != newValue) { avatar.m_Animator.SetInteger(trans.Key, newValue); }
                        break;
                }

            }
        }
    }
}

#endif // UNITY_EDITOR