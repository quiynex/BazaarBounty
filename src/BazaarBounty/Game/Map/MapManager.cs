using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using SharpFont.MultipleMasters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BazaarBounty;

// Map Types defining enemy spawn behaviour.
public enum MapType
{
    Hostile,
    Peaceful,
}

public enum MapStage
{
    Stage1,
    Stage2,
    NoStage // for special levels
}

public class EnemyWithSpawnProb
{
    public EnemyType Type { get; set; }
    public float Probability { get; set; }

    public EnemyWithSpawnProb(EnemyType enemyType, float probability)
    {
        Type = enemyType;
        Probability = probability;
    }
}

public class SpawningZone
{
    public List<EnemyWithSpawnProb> Enemies { get; set; }

    public SpawningZone(List<EnemyWithSpawnProb> enemies)
    {
        Enemies = enemies;
    }

    public EnemyType SelectEnemy(Random rng)
    {
        float totalProbability = Enemies.Sum(e => e.Probability);
        float randomValue = (float)rng.NextDouble() * totalProbability;

        float cumulativeProbability = 0;
        foreach (var enemy in Enemies)
        {
            cumulativeProbability += enemy.Probability;
            if (randomValue <= cumulativeProbability)
            {
                return enemy.Type;
            }
        }

        // Fallback in case something goes wrong
        return Enemies.Last().Type;
    }
}

public struct MapProperties
{
    public MapType MapType { get; set; }
    public MapStage MapStage { get; set; }
    public List<SpawningZone> SpawningZones { get; set; }
    public TileSwapDict TileSwapDictionary { get; set; }

    public MapProperties(MapType type, MapStage stage, List<SpawningZone> spawningZones, TileSwapDict tileSwapDictionary)
    {
        MapType = type;
        MapStage = stage;
        SpawningZones = spawningZones;
        TileSwapDictionary = tileSwapDictionary;
    }
}

// Helper class to handle map properties
public class MapManager{

    // Getter and setter for current Map Type
    public MapProperties CurrentMapProperties
    {
        get { return currentMapProperties; }
        set { currentMapProperties = value; }
    }
    private MapProperties currentMapProperties;
    
    // Recent map Queue
    private Queue<string> recentMaps = new Queue<string>();

    // Maximum number of maps to remember.
    private const int maxRecentMaps = 2;  

    // All maps with their respective types.
    public List<(string, MapProperties)> mapsWithProperties;
    // Mapping between Stage definitions and Maps
    public Dictionary<MapStage, (int, int)> stageDictionary; 
    // Special Maps to be loaded at currentLevelNumbers, overriding the map selection logic
    public Dictionary<int, string> specialMaps;
    public MapManager(){

        // Testing Tile Swapping (use local tileset index + 1 since 0 is reserved for nothing)
        TileSwapDict tileSwapDict = new TileSwapDict();

        // horizontal wall swap (mossy and broken)
        tileSwapDict.AddTileSwap(81, "bazaar_combined_tiles", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(81, "bazaar_combined_tiles", 0.70f),   // regular
            new TileWithProb(88, "bazaar_combined_tiles", 0.10f),   // mossy
            new TileWithProb(91, "bazaar_combined_tiles", 0.10f),   // broken 1
            new TileWithProb(92, "bazaar_combined_tiles", 0.10f)    // broken 2
        }));

        // vertical wall swap (mossy)
        tileSwapDict.AddTileSwap(84, "bazaar_combined_tiles", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(84, "bazaar_combined_tiles", 0.70f),   // regular
            new TileWithProb(89, "bazaar_combined_tiles", 0.30f)    // mossy
        }));
        tileSwapDict.AddTileSwap(85, "bazaar_combined_tiles", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(85, "bazaar_combined_tiles", 0.70f),   // regular
            new TileWithProb(90, "bazaar_combined_tiles", 0.30f)    // mossy
        }));

        // border trees
        tileSwapDict.AddTileSwap(1, "The_Roguelike_Assets2", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(1, "The_Roguelike_Assets2", 0.70f),    // green tree
            new TileWithProb(60, "The_Roguelike_Assets2", 0.30f),   // green duo trees
            new TileWithProb(241, "The_Roguelike_Assets2", 0.30f),  // trio bush
            new TileWithProb(300, "The_Roguelike_Assets2", 0.30f),   // mossy rock
        }));

        // farm plots
        tileSwapDict.AddTileSwap(709, "The_Roguelike_Assets2", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(650, "The_Roguelike_Assets2", 0.70f),
            new TileWithProb(651, "The_Roguelike_Assets2", 0.30f),
            new TileWithProb(663, "The_Roguelike_Assets2", 0.30f),
            new TileWithProb(664, "The_Roguelike_Assets2", 0.30f),
            new TileWithProb(665, "The_Roguelike_Assets2", 0.30f),
        }));

        tileSwapDict.AddTileSwap(366, "The_Roguelike_Assets2", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(366, "The_Roguelike_Assets2", 0.10f),  // light
            new TileWithProb(367, "The_Roguelike_Assets2", 0.90f),  // dark
        }));

        // white tile swap
        tileSwapDict.AddTileSwap(1, "bazaar_combined_tiles", new TileSwap(new List<TileWithProb>
        {
            new TileWithProb(1, "bazaar_combined_tiles", 0.97f),
            new TileWithProb(101, "bazaar_combined_tiles", 0.002f),
            new TileWithProb(102, "bazaar_combined_tiles", 0.002f),
            new TileWithProb(103, "bazaar_combined_tiles", 0.001f),
        }));

        mapsWithProperties = new List<(string, MapProperties)>
        {
            ("FinalTutorialMap.tmx", new MapProperties(
                MapType.Peaceful, 
                MapStage.NoStage, 
                new List<SpawningZone>{},
                tileSwapDict
            )),
            ("FinalMap1.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.2f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 0.2f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f),
                    })
                },
                tileSwapDict
            )),
            ("FinalMap2.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.5f)
                    })
                },
                tileSwapDict
            )),
            ("FinalMap3.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.2f)
                    })
                },
                tileSwapDict
            )),
            ("FinalMap4.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.2f),
                    }),
                },
                tileSwapDict
            )),

            // Spawns recycled  from map 3
            ("FinalMap5.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.2f)
                    })
                },
                tileSwapDict
            )),
            ("FinalMap6.tmx", new MapProperties(
                MapType.Hostile,
                MapStage.Stage1,
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.2f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f)
                    }),
                },
                tileSwapDict
            )),
            ("FinalMap7.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage1, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Rat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.Bat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f),
                    }),
                },
                tileSwapDict
            )),
            ("FinalStageTransitionMap.tmx", new MapProperties(
                MapType.Peaceful, 
                MapStage.NoStage, 
                new List<SpawningZone>{},
                tileSwapDict
            )),  
            ("FinalMap11.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage2, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedRat, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 1.0f),
                    })
                },
                tileSwapDict
            )),   
            ("FinalMap12.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage2, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f),        
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedBat, 0.5f),
                    })
                },
                tileSwapDict
            )),
            ("FinalMap13.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage2, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),        
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedBat, 0.5f),
                    })
                },
                tileSwapDict
            )),
            ("FinalMap14.tmx", new MapProperties(
                MapType.Hostile, 
                MapStage.Stage2, 
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),        
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedBat, 0.5f),
                    })
                },
                tileSwapDict
            )),
            ("FinalMap15.tmx", new MapProperties(
                MapType.Hostile,
                MapStage.Stage2,
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedBat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 1.0f),
                    })
                },
                tileSwapDict
            )),

            ("FinalMap16.tmx", new MapProperties(
                MapType.Hostile,
                MapStage.Stage2,
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Lizard, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.5f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.BlueLizard, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.BlueBat, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.RedBat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Slime, 0.5f),
                        new EnemyWithSpawnProb(EnemyType.BlueSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.Goblin, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.BlueRat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedBat, 1.0f),
                    }),
                },
                tileSwapDict
            )),
            ("FinalRedLizardMap.tmx", new MapProperties(
                MapType.Hostile,
                MapStage.NoStage,
                new List<SpawningZone>
                {
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedBat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedSlime, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedRat, 1.0f),
                        new EnemyWithSpawnProb(EnemyType.RedBat, 1.0f)
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedLizard, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedLizard, 1.0f),
                    }),
                    new SpawningZone(new List<EnemyWithSpawnProb>
                    {
                        new EnemyWithSpawnProb(EnemyType.RedLizard, 1.0f),
                    }),
                },
                tileSwapDict
            )),
        };

        stageDictionary = new Dictionary<MapStage, (int, int)>
        {
            { MapStage.Stage1, (1, 10) },
            { MapStage.Stage2, (11, 20) }
            // Add additional stages as needed
        };
        specialMaps = new Dictionary<int, string>
        {
            { 1, "FinalTutorialMap.tmx" },
            { 11, "FinalStageTransitionMap.tmx" },
            { 20, "FinalRedLizardMap.tmx" },
            // { 20, "BaseMap5.tmx" }
            // Add other special level configurations here
        };   
    }

public MapStage? GetCurrentStage(int currentLevelNumber){
    // Determine the appropriate stage based on the level number using stageDictionary
    foreach (var stage in stageDictionary)
    {
        if (currentLevelNumber >= stage.Value.Item1 && currentLevelNumber <= stage.Value.Item2)
        {
            return stage.Key;
        }
    }
    return null;
}

// Select Next Map based on Queue or if it is a special map, based on currentLevelNumber.
public string SelectNextMap(int currentLevelNumber)
{
    Console.WriteLine($"Select map for level {currentLevelNumber}.");
    if (specialMaps.TryGetValue(currentLevelNumber, out var mapName))
    {
        // If a special map is defined for this level, set and return it directly
        currentMapProperties = mapsWithProperties.First(m => m.Item1 == mapName).Item2;
        return mapName;
    }
    else
    {
        MapStage? currentStage = GetCurrentStage(currentLevelNumber);

        if (currentStage == null)
        {
            Console.WriteLine("No stage configuration found for this level, defaulting to the first available map.");
            return mapsWithProperties[0].Item1;
        }

        // Select from maps that are appropriate for the determined stage and not recently used
        var mapsToConsider = mapsWithProperties
                             .Where(map => map.Item2.MapStage == currentStage 
                                           && !recentMaps.Contains(map.Item1))
                             .ToList();

        if (mapsToConsider.Count == 0)
        {
            // If all maps are in recent history, reset the recentMaps queue and select randomly again
            Console.WriteLine($"No available maps for Stage {currentStage} not recently used, defaulting to random map of the stage.");
            recentMaps.Clear(); // Clear recent maps to reset the cycle
            mapsToConsider = mapsWithProperties
                             .Where(map => map.Item2.MapStage == currentStage)
                             .ToList();
        }

        // Select a map randomly from the filtered list
        Random rng = RandomProvider.GetRandom();
        int mapIndex = rng.Next(mapsToConsider.Count);
        UpdateRecentMaps(mapsToConsider[mapIndex].Item1); // Update recent maps
        currentMapProperties = mapsToConsider[mapIndex].Item2; // Set the current map properties
        return mapsToConsider[mapIndex].Item1;
    }
}


    // Update the Last Map Queue
    public void UpdateRecentMaps(string mapName)
    {
        if (recentMaps.Count >= maxRecentMaps)
            recentMaps.Dequeue();
        recentMaps.Enqueue(mapName);
    }
}