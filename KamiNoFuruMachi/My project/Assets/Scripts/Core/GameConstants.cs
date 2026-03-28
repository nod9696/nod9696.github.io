// Assets/Scripts/Core/GameConstants.cs
namespace KamiNoFuruMachi
{
    public static class GameConstants
    {
        public static class Scenes
        {
            public const string Title   = "TitleScene";
            public const string Game    = "GameScene";
            public const string Ending  = "EndingScene";
        }

        public static class BGM
        {
            public const string WildernessNight  = "wilderness_night";
            public const string AbandonedChapel  = "abandoned_chapel";
            public const string Pursuit          = "pursuit";
            public const string Descent          = "descent";
            public const string Campfire         = "campfire";
            public const string EmotionalPiano   = "emotional_piano";
            public const string NainfallAmbient  = "nainfall_ambient";
        }

        public static class SE
        {
            public const string SteamPipe       = "steam_pipe";
            public const string Siren           = "siren";
            public const string CampfireCrackle = "campfire_crackle";
            public const string ArmPulse        = "arm_pulse";
            public const string Footsteps       = "footsteps";
            public const string DoorOpen        = "door_open";
            public const string GlassBreak      = "glass_break";
        }

        public static class Sprites
        {
            public const string KanataNormal   = "kanata_normal";
            public const string KanataArm      = "kanata_arm";
            public const string KanataSmile    = "kanata_smile";
            public const string LilithNormal   = "lilith_normal";
            public const string LilithSurprise = "lilith_surprised";
            public const string LilithSmile    = "lilith_smile";
            public const string EleonoraNormal    = "eleonora_normal";
            public const string EleonoraAnxious   = "eleonora_anxious";
            public const string EleonoraSoliloquy = "eleonora_soliloquy";
        }

        public static class SaveKeys
        {
            public const string SlotPrefix  = "SaveSlot_";
            public const string AutoSaveKey = "AutoSave";
            public const int    SlotCount   = 5;
        }
    }
}
