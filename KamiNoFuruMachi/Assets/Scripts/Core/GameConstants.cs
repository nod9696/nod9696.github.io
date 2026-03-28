// Assets/Scripts/Core/GameConstants.cs
namespace KamiNoFuruMachi
{
    /// <summary>
    /// プロジェクト全体で使用する定数を集約した静的クラス。
    /// シーン名・BGM ID・SE ID・スプライト ID を文字列定数として定義する。
    /// </summary>
    public static class GameConstants
    {
        // -------------------------------------------------------------------------
        // Scene Names
        // -------------------------------------------------------------------------
        public static class Scenes
        {
            public const string Title   = "TitleScene";
            public const string Game    = "GameScene";
            public const string Ending  = "EndingScene";
        }

        // -------------------------------------------------------------------------
        // BGM IDs
        // -------------------------------------------------------------------------
        public static class BGM
        {
            public const string WildernessNight   = "wilderness_night";
            public const string AbandonedChapel   = "abandoned_chapel";
            public const string Pursuit            = "pursuit";
            public const string Descent            = "descent";
            public const string Campfire           = "campfire";
            public const string EmotionalPiano     = "emotional_piano";
            public const string NainfallAmbient    = "nainfall_ambient";
        }

        // -------------------------------------------------------------------------
        // SE IDs
        // -------------------------------------------------------------------------
        public static class SE
        {
            public const string SteamPipe         = "steam_pipe";
            public const string Siren             = "siren";
            public const string CampfireCrackle   = "campfire_crackle";
            public const string ArmPulse          = "arm_pulse";
            public const string Footsteps         = "footsteps";
            public const string DoorOpen          = "door_open";
            public const string GlassBreak        = "glass_break";
        }

        // -------------------------------------------------------------------------
        // Sprite IDs — Character Variants
        // -------------------------------------------------------------------------
        public static class Sprites
        {
            // Kanata
            public const string KanataNormal    = "kanata_normal";
            public const string KanataArm       = "kanata_arm";
            public const string KanataSmile     = "kanata_smile";

            // Lilith
            public const string LilithNormal    = "lilith_normal";
            public const string LilithSurprised = "lilith_surprised";
            public const string LilithSmile     = "lilith_smile";

            // Eleonora
            public const string EleonoraNormal    = "eleonora_normal";
            public const string EleonoraAnxious   = "eleonora_anxious";
            public const string EleonoraSoliloquy = "eleonora_soliloquy";
        }

        // -------------------------------------------------------------------------
        // Save / PlayerPrefs Keys
        // -------------------------------------------------------------------------
        public static class SaveKeys
        {
            public const string SlotPrefix    = "SaveSlot_";
            public const string AutoSaveKey   = "AutoSave";
            public const int    SlotCount     = 5;
        }
    }
}
