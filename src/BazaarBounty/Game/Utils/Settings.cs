// using System;
// using System.IO;
// using System.Text.Json;
// using System.Reflection;
// using System.Collections.Generic;

// namespace BazaarBounty
// {
//     public static class Settings
//     {
//        // class definition here
//     }
//         public static class SettingsLoader
//         {
//             public static void LoadSettings(string filePath)
//             {
//                 // load here
//             }
//         }
//     }

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace BazaarBounty
{
    public static class SettingsLoader
    {
        public static Settings.Root LoadSettings(string filePath)
        {
            try
            {
                var jsonString = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<Settings.Root>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return null;
            }
        }
    }

    public class Settings
    {
        public class BaseEnemy
        {
            public float Speed { get; set; }
            public int MaxHealth { get; set; }
            public int Damage { get; set; }
            public float StunTime { get; set; }
            public float AttackRange { get; set; }
            public float AttackGapTime { get; set; }
            public float MinAttackRange { get; set; }
            public float RangeOfVision { get; set; }
            public int SpawnCost { get; set; }
        }

        public class Bat
        {
            public BaseEnemy Black { get; set; }
            public BaseEnemy Blue { get; set; }
            public BaseEnemy Red { get; set; }
        }
        public class Slime
        {
            public BaseEnemy Green { get; set; }
            public BaseEnemy Blue { get; set; }
            public BaseEnemy Red { get; set; }
        }
        public class Lizard
        {
            public BaseEnemy Purple { get; set; }
            public BaseEnemy Blue { get; set; }
            public BaseEnemy Red { get; set; }
        }
        public class Rat
        {
            public BaseEnemy Brown { get; set; }
            public BaseEnemy Blue { get; set; }
            public BaseEnemy Red { get; set; }
        }
        public class Goblin
        {
            public BaseEnemy Green { get; set; }
        }

        public class Enemy
        {
            public Bat Bat { get; set; }
            public Slime Slime { get; set; }
            public Lizard Lizard { get; set; }
            public Rat Rat { get; set; }
            public Goblin Goblin { get; set; }

        }
        public class BaseFruit{
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsPermanent { get; set; }
            public float Duration { get; set; }
            public int IntParam1 { get; set; }
            public int IntParam2 { get; set; }
            public float FloatParam1 { get; set; }
            public float FloatParam2 { get; set; }
            public float FloatParam3 { get; set; }
            public float SelectionRatio { get; set; }
        }

        public class Fruit{
            public BaseFruit Apple { get; set; }
            public BaseFruit Watermelon { get; set; }
            public BaseFruit Banana { get; set; }
            public BaseFruit Coconut { get; set; }
            public BaseFruit Grape { get; set; }
            public BaseFruit Peach { get; set; }
            public BaseFruit Mango { get; set; }
            public BaseFruit Orange { get; set; }
            public BaseFruit Cherry { get; set; }
            public BaseFruit Apple2 { get; set; }
            public BaseFruit Watermelon2 { get; set; }
            public BaseFruit Banana2 { get; set; }
            public BaseFruit Coconut2 { get; set; }
            public BaseFruit Grape2 { get; set; }
            public BaseFruit Peach2 { get; set; }
            public BaseFruit Mango2 { get; set; }
            public BaseFruit Orange2 { get; set; }
            public BaseFruit Cherry2 { get; set; }
        }

        public class Graphics
        {
            public int ScreenWidth { get; set; }
            public int ScreenHeight { get; set; }
            public bool Fullscreen { get; set; }
        }

        public class Debug
        {
            public bool Mode { get; set; }
            public float Depth { get; set; }
        }

        public class SpawnBudget
        {
            public int Start { get; set; }
            public int End { get; set; }
        }

        public class Root
        {
            public Enemy Enemy { get; set; }
            public Fruit Fruit { get; set; }
            public Graphics Graphics { get; set; }
            public Debug Debug { get; set; }
            public SpawnBudget SpawnBudget { get; set; }
        }
    }
}

