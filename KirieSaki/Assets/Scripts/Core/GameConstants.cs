// Assets/Scripts/Core/GameConstants.cs — KirieSaki project
namespace KirieSaki
{
    public static class GameConstants
    {
        public static class Chapters
        {
            public const string Chapter00 = "chapter00"; // 序章
            public const string Chapter01 = "chapter01"; // 第一章
            public const string Chapter02 = "chapter02"; // 昼間のキリエ
            public const string Chapter03 = "chapter03"; // 夜の街
            public const string Chapter04 = "chapter04"; // サキの部屋
            public const string Chapter05 = "chapter05"; // 先を越した何か
            public const string Chapter06 = "chapter06"; // 後退の夜
            public const string Chapter07 = "chapter07"; // 川の名前
            public const string Chapter08 = "chapter08"; // 間違い電話
            public const string Chapter09 = "chapter09"; // 街の昼間
            public const string Chapter10 = "chapter10"; // 夜の数え方（独立章）
            public const string Chapter11 = "chapter11"; // 白が満ちる夜に
            public const string Chapter12 = "chapter12"; // 人間が最初に泣いた夜
            public const string Chapter13 = "chapter13"; // 終章
            public const string Chapter14 = "chapter14"; // 砂糖なしの朝に（後日談）
        }

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
