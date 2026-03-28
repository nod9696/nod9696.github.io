// Assets/Scripts/Dialogue/ScenarioData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KamiNoFuruMachi
{
    [Serializable]
    public class ChoiceOption
    {
        public string label;
        public string flag;
        public string value;
    }

    [Serializable]
    public class ScenarioCommand
    {
        public string cmd;
        [SerializeField] private string _char;
        public string @char { get => _char; set => _char = value; }
        public string body;
        public string id;
        public string pose;
        public string position;
        public string transition;
        public string type;
        public float  intensity;
        public float  duration;
        public float  fade;
        public string key;
        public string value;
        public string color;
        public List<ChoiceOption> options;
    }

    [Serializable]
    public class ScenarioData
    {
        public string chapterId;
        public string title;
        public List<ScenarioCommand> commands;

        public int CommandCount => commands != null ? commands.Count : 0;

        public ScenarioCommand GetCommand(int index)
        {
            if (commands == null || index < 0 || index >= commands.Count) return null;
            return commands[index];
        }
    }
}
