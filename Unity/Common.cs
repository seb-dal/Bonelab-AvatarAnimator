using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarAnimation
{
    public class Const
    {
        public static string prefix = "AvatarAnimation=";
    }

    public enum InputType
    {
        Keyboard,
        ControllerRight,
        ControllerLeft
    }

    [Serializable]
    public class StateNode
    {
        public IList<Transition> transitions;
    }

    [Serializable]
    public class Transition
    {
        public string stateNodeTo;
        public string trigger;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TransitionInput? input1;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TransitionInput? input2;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TransitionInput? input3;
    }

    [Serializable]
    public class TransitionInput
    {
        public string name;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public KeyCode? code;
        public InputType type;
    }

    [Serializable]
    public class AvatarAnimationJson
    {
        public Dictionary<string, StateNode> states; // Name -> State
        public string startState;
    }
}