// Assets/Scripts/Dialogue/DialogueCommand.cs
using System.Collections.Generic;

namespace KamiNoFuruMachi
{
    public static class DialogueCommand
    {
        public const string Text   = "text";
        public const string Choice = "choice";
        public const string Bg     = "bg";
        public const string Sprite = "sprite";
        public const string Bgm    = "bgm";
        public const string Se     = "se";
        public const string Effect = "effect";
        public const string Flag   = "flag";

        public static class EffectType
        {
            public const string Shake  = "shake";
            public const string Flash  = "flash";
            public const string Glitch = "glitch";
            public const string Fade   = "fade";
        }

        public static class Transition
        {
            public const string Fade      = "fade";
            public const string Cut       = "cut";
            public const string CrossFade = "crossfade";
        }

        public static class Position
        {
            public const string Left   = "left";
            public const string Center = "center";
            public const string Right  = "right";
        }

        public static bool IsKnownCommand(string cmd)
        {
            switch (cmd)
            {
                case Text: case Choice: case Bg: case Sprite:
                case Bgm:  case Se:    case Effect: case Flag:
                    return true;
                default: return false;
            }
        }

        public static bool IsKnownEffectType(string type)
        {
            switch (type)
            {
                case EffectType.Shake: case EffectType.Flash:
                case EffectType.Glitch: case EffectType.Fade:
                    return true;
                default: return false;
            }
        }

        public static bool Validate(ScenarioCommand command, out string reason)
        {
            if (command == null)               { reason = "command is null"; return false; }
            if (string.IsNullOrEmpty(command.cmd)) { reason = "cmd is empty"; return false; }
            if (!IsKnownCommand(command.cmd))  { reason = $"unknown cmd: {command.cmd}"; return false; }

            switch (command.cmd)
            {
                case Text:   if (string.IsNullOrEmpty(command.body)) { reason = "text requires body"; return false; } break;
                case Choice: if (command.options == null || command.options.Count == 0) { reason = "choice requires options"; return false; } break;
                case Bg:     if (string.IsNullOrEmpty(command.id)) { reason = "bg requires id"; return false; } break;
                case Bgm:    if (string.IsNullOrEmpty(command.id)) { reason = "bgm requires id"; return false; } break;
                case Se:     if (string.IsNullOrEmpty(command.id)) { reason = "se requires id"; return false; } break;
                case Effect: if (!IsKnownEffectType(command.type)) { reason = $"unknown effect type: {command.type}"; return false; } break;
                case Flag:   if (string.IsNullOrEmpty(command.key)) { reason = "flag requires key"; return false; } break;
            }

            reason = string.Empty;
            return true;
        }
    }
}
