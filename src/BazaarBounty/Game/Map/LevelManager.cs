using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BazaarBounty;

public class LevelManager
{
    public Map CurrentMap => currentMap;
    private Map currentMap;
    public EnemyController EnemyController => enemyController;
    private EnemyController enemyController;
    private bool loadNextFlag = false;
    public bool LoadNextFlag { set => loadNextFlag = value; }
    private Vector2 lastEnemyPosition;
    public Vector2 LastEnemyPosition { set => lastEnemyPosition = value; }

    // Determines map selection and enemy difficulty
    private static int nextLevelNumber = 1; 
    public static int NextLevelNumber => nextLevelNumber;

    private bool hasSpawnFruits = false; // only spawn fruits once when all enemies are dead

    // Holds Enemy Costs and their Spawn function
    private EnemyManager enemyManager;

    // Keeps map Queue and Map properties
    public MapManager MapManager => mapManager;
    private MapManager mapManager;
    private int lastLevelNumber = 21;

    // Manager used to load maps and manage enemy & player spawns.
    public LevelManager()
    {
        mapManager = new MapManager();
        enemyController = new EnemyController();
        enemyManager = new EnemyManager();
    }

    public void ResetManager(){
        mapManager = new MapManager();
        enemyController = new EnemyController();
        enemyManager = new EnemyManager();   
        nextLevelNumber = 1;
    }

    // Simple Budget Increase
    private float CalculateBudgetFromLevel(){
        var s = BazaarBountyGame.Settings.SpawnBudget;
        int start = s.Start;
        int end = s.End;

        return start + (end - start) * ((float)nextLevelNumber / (lastLevelNumber - 1));
    }

// Enemy Spawn logic.
    public void SpawnEnemies()
    {
        // Calculate enemy spawning budget
        float budget = CalculateBudgetFromLevel();

        // Get Spawning Zones 
        List<SpawningZone> spawningZones = new(mapManager.CurrentMapProperties.SpawningZones);

        // Shuffle tiles within each spawning zone
        Random rng = RandomProvider.GetRandom();
        List<List<Rectangle>> spawningZoneTilesList = new List<List<Rectangle>>();
        Console.WriteLine($"Spawning zone count: {spawningZones.Count}");
        for (int i = 1; i <= spawningZones.Count; i++)
        {
            spawningZoneTilesList.Add(CurrentMap.MapInfo.GetTilesOfType($"Enemy Spawn {i}")
                                                        .OrderBy(x => rng.Next())
                                                        .ToList());
            Console.WriteLine($"Get tiles from SpawningZone {i}");
        }
        
        Console.WriteLine("Done Adding Zones");

        // Loop over spawnable tiles and expend budget
        while (budget > 0)
        {
            // Select a random spawning zone
            int zoneIndex = rng.Next(spawningZones.Count);
            Console.WriteLine($"Spawning in Zone {zoneIndex+1}");
            SpawningZone selectedZone = spawningZones[zoneIndex];
            List<Rectangle> tiles = spawningZoneTilesList[zoneIndex];

            // Select a random tile from the selected zone
            if (tiles.Count == 0)
            {
                // Remove the zone if no tiles are left to spawn
                spawningZones.RemoveAt(zoneIndex);
                spawningZoneTilesList.RemoveAt(zoneIndex);
                if (spawningZones.Count == 0)
                {
                    break; // No more zones to spawn from
                }
                continue; // Select a new zone
            }

            Rectangle spawnTile = tiles[0];
            tiles.RemoveAt(0); // Remove the used tile

            // Select an enemy based on the probabilities defined in the selected zone
            EnemyType selectedEnemyType = selectedZone.SelectEnemy(rng);
            Console.WriteLine($"Chose enemy {selectedEnemyType}");
            if (!enemyManager.EnemyDataDict.TryGetValue(selectedEnemyType, out var enemyData))
            {
                Console.WriteLine($"Enemy data not found for type {selectedEnemyType}");
                continue; // Skip if enemy data is not found
            }

            // Spawn the enemy by adding the enemy to EnemyController and CollisionComp
            Vector2 spawnPosition = new Vector2((spawnTile.Left + spawnTile.Right) / 2,
                                                (spawnTile.Top + spawnTile.Bottom) / 2);
            EnemyCharacter enemy = enemyData.CreateFunc(spawnPosition);
            enemyController.AddCharacter(enemy);
            BazaarBountyGame.GetGameInstance().CollisionComp.Insert(enemy);
            
            // Add the enemy's weapon to the collision component if applicable
            if (enemy is Rat rat)
            {
                ((MeleeWeapon)rat.Weapon).RegisterHitBox(BazaarBountyGame.GetGameInstance().CollisionComp);
                // BazaarBountyGame.GetGameInstance().CollisionComp.Insert(rat.Weapon);
            }

            // Deduct the cost of the spawned enemy from the budget
            budget -= enemyData.Cost;

        }
        Console.WriteLine("Now add to controller.");


        // Add enemies to EnemyController
        foreach (var enemy in enemyController.Characters)
        {
            enemy.LoadContent();
        }
        Console.WriteLine("Finish spawning enemies.");
    }



    // Spawn Player in one of the appropriate tiles.
    public void SpawnPlayer()
    {
        // Console.WriteLine("SPAWNING PLAYER");
        var spawnTileList = CurrentMap.MapInfo.GetTilesOfType("Player Spawn");
        Random rng = RandomProvider.GetRandom();
        int idx = rng.Next(0, spawnTileList.Count);
        var spawnTile = spawnTileList[idx];
        BazaarBountyGame.player.Position = new Vector2((float)(spawnTile.Left+spawnTile.Right)/2, 
                                      (float)(spawnTile.Top+spawnTile.Bottom)/2);
    }

    // Fruit Spawning Logic

    public string SelectFruits(Dictionary<string, float> fruitDir){
        Random rng = new Random();
        // Calculate total probability using floating-point division
        float totalProbability = fruitDir.Values.Sum();

        // Calculate cumulative probabilities
        Dictionary<string, float> cumulativeProbabilities = new Dictionary<string, float>();
        float cumulativeProbability = 0;
        foreach (var pair in fruitDir)
        {
            cumulativeProbability += pair.Value / totalProbability;
            cumulativeProbabilities.Add(pair.Key, cumulativeProbability);
        }
        // Roulette wheel selection
        double randomValue = rng.NextDouble();
        string selectedFruit = null;
        foreach (var pair in cumulativeProbabilities)
        {
            if (randomValue <= pair.Value)
            {
                selectedFruit = pair.Key;
                break;
            }
        }


        return selectedFruit;
    }
    public void SpawnFruits(CollisionComponent collisionComponent, MapStage mapStage)
    {
        // Console.WriteLine("SPAWNING FRUITS");
        Dictionary<string, float> fruitDir = mapStage == MapStage.Stage1 ? 
        new Dictionary<string, float>()
        {
            {"Apple", BazaarBountyGame.Settings.Fruit.Apple.SelectionRatio},
            {"Watermelon", BazaarBountyGame.Settings.Fruit.Watermelon.SelectionRatio},
            {"Banana", BazaarBountyGame.Settings.Fruit.Banana.SelectionRatio},
            // {"Coconut", BazaarBountyGame.Settings.Fruit.Coconut.SelectionRatio},
            // {"Grape", BazaarBountyGame.Settings.Fruit.Grape.SelectionRatio},
            // {"Peach", BazaarBountyGame.Settings.Fruit.Peach.SelectionRatio},
            {"Mango", BazaarBountyGame.Settings.Fruit.Mango.SelectionRatio},
            {"Orange", BazaarBountyGame.Settings.Fruit.Orange.SelectionRatio},
            {"Cherry", BazaarBountyGame.Settings.Fruit.Cherry.SelectionRatio}
        } :
        new Dictionary<string, float>()
        {
            {"Apple", BazaarBountyGame.Settings.Fruit.Apple2.SelectionRatio},
            {"Watermelon", BazaarBountyGame.Settings.Fruit.Watermelon2.SelectionRatio},
            {"Banana", BazaarBountyGame.Settings.Fruit.Banana2.SelectionRatio},
            {"Coconut", BazaarBountyGame.Settings.Fruit.Coconut.SelectionRatio},
            {"Grape", BazaarBountyGame.Settings.Fruit.Grape.SelectionRatio},
            {"Peach", BazaarBountyGame.Settings.Fruit.Peach.SelectionRatio},
            {"Mango", BazaarBountyGame.Settings.Fruit.Mango.SelectionRatio},
            {"Orange", BazaarBountyGame.Settings.Fruit.Orange.SelectionRatio},
            {"Cherry", BazaarBountyGame.Settings.Fruit.Cherry2.SelectionRatio}
        };

        Random rng = new Random();
        int num_fruits = rng.NextDouble() <= 0.2 ? 3 : 2;

        List<string> fruitnames = new List<string>();
        for (int i=0;i<num_fruits;i++){
            string selectedFruit = SelectFruits(fruitDir);
            fruitnames.Add(selectedFruit);
            fruitDir.Remove(selectedFruit);
        }
         
        // Spawn the selected fruit
        if (fruitnames.Count > 0)
        {
            Vector2 spawnPosition = lastEnemyPosition + new Vector2(rng.Next(-25, 25), rng.Next(-25, 25));
            FruitBag fruit = new(fruitnames, spawnPosition, 32, 32, (MapStage)mapManager.GetCurrentStage(nextLevelNumber-2));
            FruitPool.GetInstance().FruitList.Add(fruit);
            collisionComponent.Insert(fruit);
        }
    }
    // Fruit Despawning Logic
    public void DespawnFruits(CollisionComponent collisionComponent){
        foreach(FruitBag fruit in FruitPool.GetInstance().FruitList){
            collisionComponent.Remove(fruit);
        }
        FruitPool.GetInstance().FruitList.Clear();
    }

    // Load the Next Map using helper functions and finally call LoadContent() on LevelManager.
    public void LoadNextMap()
    {
        if (currentMap != null)
        {
            // Handle Unloading
            Console.WriteLine("Unload Map");
            var collisionComponent = BazaarBountyGame.GetGameInstance().CollisionComp;
            currentMap.Unload(collisionComponent);
            DespawnFruits(collisionComponent);
            enemyController.ClearCharacters();
        }
        else{
            Console.WriteLine("Map NULL");
        }

        if(nextLevelNumber == lastLevelNumber){
            BazaarBountyGame.GetGameInstance().WinGame();
        }
        else{
            Console.WriteLine("Load New Map");
            // Select Map
            string mapName = mapManager.SelectNextMap(nextLevelNumber);
            // Create new Map
            currentMap = new Map(mapName, new Vector2(320, 0), 1.0f);
            // Increase nextLevelNumber
            nextLevelNumber += 1;
            // LoadContent
            LoadContent();    
        }

    }

    public void LoadContent(){
        hasSpawnFruits = false;

        // Call LoadContent on Map and set CollisionComponent
        CurrentMap.LoadContent();
        CurrentMap.LoadCollidableTiles(BazaarBountyGame.GetGameInstance().CollisionComp);

        // Spawn Enemies if Hostile
        if(mapManager.CurrentMapProperties.MapType == MapType.Hostile) SpawnEnemies();

        // Spawn Player
        SpawnPlayer();
    }

    public void Update(GameTime gameTime)
    {
        // spawn fruits when the level is cleared
        if (IsLevelClear() && !hasSpawnFruits){
            hasSpawnFruits = true;
            if(mapManager.CurrentMapProperties.MapType == MapType.Hostile){
                SpawnFruits(BazaarBountyGame.GetGameInstance().CollisionComp, (MapStage)mapManager.GetCurrentStage(nextLevelNumber-2));
            }
        }

        // update the last enemy position
        if (!IsLevelClear()){
            lastEnemyPosition = enemyController.Characters[0].Position;
        }

        // Flag to trigger LoadNextMap()
        if (loadNextFlag)
        {
            LoadNextMap();
            loadNextFlag = false;
        }

        CurrentMap.Update(gameTime);
        enemyController.Update(gameTime);
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        CurrentMap.Draw(spriteBatch);
        foreach (var enemy in enemyController.Characters)
        {
            enemy.Draw(spriteBatch);
        }
    }
    
    // Condition for level to be cleared
    public bool IsLevelClear()
    {
        return enemyController.Characters.Count == 0;
    }
}