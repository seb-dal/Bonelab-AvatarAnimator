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
    public class AvatarAnimatorTestScript : MonoBehaviour
    {
        private readonly List<AvatarAnimatorDataContainer> m_avatars = new();
        public List<AvatarAnimatorDataContainer> Avatars { get => m_avatars; }

        private readonly List<string> m_avatarsName = new();
        public List<string> AvatarsName { get => m_avatarsName; }

        public int m_selectedIndex = 0;
        public float m_health = 1f;

        public void Start()
        {
            m_avatars.Clear();
            m_avatars.AddRange(GameObject.FindObjectsOfType<AvatarAnimatorDataContainer>());
            m_avatarsName.Clear();
            foreach (var avatar in Avatars)
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
                    var avatar = Avatars[m_selectedIndex];
                    avatar.m_Animator.Play(state, layer);
                }
                PlayerAnimatorCore.PlayState(layer, state);
            };

            PlayerAnimatorCore.getPlayerHealth = () => m_health;

            SetActifAvatar(0);
        }

        public void Update()
        {
            if (!PlayerAnimatorCore.IsInitialized()) return;
            PlayerAnimatorCore.Update();
        }

        public void SetActifAvatar(int index)
        {
            m_selectedIndex = index;
            var avatar = Avatars[index];
            PlayerAnimatorCore.SetAvatar(avatar.m_Animator, avatar.m_Data);
            PlayerAnimatorCore.Clear();
            PlayerAnimatorCore.Initialize();
        }
    }

    [CustomEditor(typeof(AvatarAnimatorTestScript))]
    [DisallowMultipleComponent]
    public class AvatarAnimatorTestEditor : Editor
    {
        AvatarAnimatorTestScript container;
        private string[] avatarsName = new string[] { };
        private const int AlignButtons = 3;

        private void OnEnable()
        {
            container = (AvatarAnimatorTestScript)target;
        }
        public override void OnInspectorGUI()
        {
            if (avatarsName?.Length != container.AvatarsName.Count)
            {
                avatarsName = container.AvatarsName.ToArray();
            }
            if (null == container || avatarsName.Length == 0) return;
            var index = EditorGUILayout.Popup("Avatar", container.m_selectedIndex, avatarsName);
            var avatar = container.Avatars[index];

            GUILayout.Label($"Avatar Health ({Math.Round(container.m_health * 100)}%)");
            var health = GUILayout.HorizontalSlider(container.m_health, 0.0f, 1.0f);
            if (container.m_health != health)
            {
                avatar.m_Animator.SetFloat("Health", health);
                container.m_health = health;
            }
            GUILayout.Label("");

            GUILayout.Label("Animations");
            if (container.m_selectedIndex != index) { container.SetActifAvatar(index); }


            foreach (var lay in avatar.m_Data.ListLayer)
            {
                int i = 0;
                var curr = PlayerAnimatorCore.GetState(lay.LayerIndex);
                foreach (var state in lay.States)
                {
                    if (i % AlignButtons == 0) GUILayout.BeginHorizontal();
                    bool res = GUILayout.Toggle(state.Key == curr.m_CurrentStateName, $"{lay.LayerIndex} - {state.Key}");
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