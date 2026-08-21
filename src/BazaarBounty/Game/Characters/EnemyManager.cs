using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using MonoGame.Extended;
using SharpFont.MultipleMasters;

namespace BazaarBounty;

public enum EnemyType
{
    Slime,
    BlueSlime,
    RedSlime,
    Lizard,
    BlueLizard,
    RedLizard,
    Bat,
    BlueBat,
    RedBat,
    Rat,
    BlueRat,
    RedRat,
    Goblin,
}

public class EnemyData
{
    public EnemyType Type { get; set; }
    public int Cost { get; set; }
    public Func<Vector2, EnemyCharacter> CreateFunc { get; set; }

    public EnemyData(EnemyType type, int cost, Func<Vector2, EnemyCharacter> createFunc)
    {
        Type = type;
        Cost = cost;
        CreateFunc = createFunc;
    }
}

// Helper Class to handle Enemy costs
public class EnemyManager
{
    public Dictionary<EnemyType, EnemyData> EnemyDataDict
    {
        get { return enemyData; }
        set { enemyData = value; }
    }

    private Dictionary<EnemyType, EnemyData> enemyData;
    // Enemy costs to be used in the spawning algorithm based on the budget.
    public EnemyManager(){
        var es = BazaarBountyGame.Settings.Enemy;
        enemyData = new Dictionary<EnemyType, EnemyData>
        {

            { EnemyType.Slime, new EnemyData(EnemyType.Slime, es.Slime.Green.SpawnCost , pos => new Slime(pos)) },
            { EnemyType.BlueSlime, new EnemyData(EnemyType.BlueSlime, es.Slime.Blue.SpawnCost, pos => new BlueSlime(pos)) },
            { EnemyType.RedSlime, new EnemyData(EnemyType.RedSlime, es.Slime.Red.SpawnCost, pos => new RedSlime(pos)) },
            { EnemyType.Lizard, new EnemyData(EnemyType.Lizard, es.Lizard.Purple.SpawnCost, pos => new Lizard(pos)) },
            { EnemyType.BlueLizard, new EnemyData(EnemyType.BlueLizard, es.Lizard.Blue.SpawnCost, pos => new BlueLizard(pos)) },
            { EnemyType.RedLizard, new EnemyData(EnemyType.RedLizard, es.Lizard.Red.SpawnCost, pos => new RedLizard(pos)) },
            { EnemyType.Bat, new EnemyData(EnemyType.Bat, es.Bat.Black.SpawnCost, pos => new Bat(pos)) },
            { EnemyType.BlueBat, new EnemyData(EnemyType.BlueBat, es.Bat.Blue.SpawnCost, pos => new BlueBat(pos)) },
            { EnemyType.RedBat, new EnemyData(EnemyType.RedBat, es.Bat.Red.SpawnCost, pos => new RedBat(pos)) },
            { EnemyType.Rat, new EnemyData(EnemyType.Rat, es.Rat.Brown.SpawnCost, pos => new Rat(pos)) },
            { EnemyType.BlueRat, new EnemyData(EnemyType.BlueRat, es.Rat.Blue.SpawnCost, pos => new BlueRat(pos)) },
            { EnemyType.RedRat, new EnemyData(EnemyType.RedRat, es.Rat.Red.SpawnCost, pos => new RedRat(pos)) },
            { EnemyType.Goblin, new EnemyData(EnemyType.Goblin, es.Goblin.Green.SpawnCost, pos => new Goblin(pos)) },

        };
    }
}
