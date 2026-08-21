using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Particles.Modifiers;
using SharpDX.D3DCompiler;
using SharpFont.MultipleMasters;
using System;
using System.Collections.Generic;
using System.IO;

namespace BazaarBounty
{
    public delegate void EffectFunction(Character character);

    public class FruitPool
    {
        private static FruitPool instance;

        private FruitPool()
        {
        }

        public static FruitPool GetInstance()
        {
            if (instance == null)
            {
                instance = new FruitPool();
            }

            return instance;
        }

        private List<FruitBag> Fruits = new List<FruitBag>();

        public List<FruitBag> FruitList => Fruits;

        public void RegisterFruit(FruitBag Fruit)
        {
            Fruits.Add(Fruit);
        }

        public void Update(GameTime gameTime)
        {
            foreach (FruitBag fruit in Fruits)
            {
                fruit.Update(gameTime);
            }

            // remove from collision component
            var collisionComponent = BazaarBountyGame.GetGameInstance().CollisionComp;
            foreach (FruitBag fruit in Fruits)
            {
                if (fruit.Destroyed)
                    collisionComponent.Remove(fruit);
            }

            // remove all done delays
            Fruits.RemoveAll(fruit => fruit.Destroyed);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (FruitBag fruit in Fruits)
            {
                fruit.Draw(spriteBatch);
            }
        }
    }

    // public struct FruitEffectInfo
    // {
    //     public string effectName {get; set;}
    //     public string effectDescription {get; set;}
    //     public EffectFunction effectFunction;
    //     public FruitEffectInfo(string name, string description, EffectFunction func)
    //     {
    //         effectName = name;
    //         effectDescription = description;
    //         effectFunction = func;
    //     }
    // }


    public static class BuffFactory
    {
        public static List<string> buffNames = new()
        {
            "Apple",
            "Watermelon",
            "Banana",
            "Coconut",
            "Grape",
            "Peach",
            "Mango",
            "Orange",
            "Cherry"
        };

        public static Buff CreateBuff(string buffName, MapStage level_stage)
        {
            return buffName switch
            {
                "Apple" => level_stage == MapStage.Stage1 ? new AppleBuff() : new Apple2Buff(),
                "Watermelon" => level_stage == MapStage.Stage1 ? new WatermelonBuff() : new Watermelon2Buff(),
                "Banana" => level_stage == MapStage.Stage1 ? new BananaBuff() : new Banana2Buff(),
                "Coconut" => new CoconutBuff(),
                "Grape" => new GrapeBuff(),
                "Peach" => new PeachBuff(),
                "Mango" => level_stage == MapStage.Stage1 ? new MangoBuff() : new Mango2Buff(),
                "Orange" => new OrangeBuff(),
                "Cherry" => level_stage == MapStage.Stage1 ? new CherryBuff() : new Cherry2Buff(),
                _ => null,
            };
        }
    }

    public class Buff
    {
        protected bool isPermanent;
        protected float duration;
        protected Utils.Timer durationTimer;
        protected string buffName;
        protected string buffDescription;
        public string BuffName => buffName;
        public string BuffDescription => buffDescription;

        public bool IsPermanent => isPermanent;
        public bool IsExpired => !isPermanent && durationTimer.isDone;
        public float Duration => duration;
        public float RemainingTime => (float)durationTimer.duration - (float)durationTimer.timeElapsed;

        // Superclass constructor to make it less verbose to set settings
        protected Settings.Fruit setting;
        public Buff(){
            setting = BazaarBountyGame.Settings.Fruit;
        }

        public virtual void Apply(PlayerCharacter character)
        {
            if (!isPermanent)
            {
                character.TemporaryBuffs.Add(this);
                Utils.TimerPool.GetInstance().RegisterDelay(durationTimer);
            }
        }
    }

    public class AppleBuff : Buff
    {
        public AppleBuff()
        {
            isPermanent = setting.Apple.IsPermanent;
            buffName = setting.Apple.Name;
            buffDescription = setting.Apple.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // heal % health
            character.CurrHealth = Math.Min(character.MaxHealth,
            character.CurrHealth + (int)(character.MaxHealth * setting.Apple.FloatParam1));
        }
    }

    public class Apple2Buff : Buff
    {
        public Apple2Buff()
        {
            isPermanent = setting.Apple2.IsPermanent;
            buffName = setting.Apple2.Name;
            buffDescription = setting.Apple2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // heal % health
            character.MaxHealth += setting.Apple2.IntParam1; 
            character.CurrHealth = Math.Min(character.MaxHealth,
            character.CurrHealth + (int)(character.MaxHealth * setting.Apple2.FloatParam1));
        }
    }

    public class WatermelonBuff : Buff
    {
        public WatermelonBuff()
        {
            isPermanent = setting.Watermelon.IsPermanent;
            buffName = setting.Watermelon.Name;
            buffDescription = setting.Watermelon.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // restore bullets
            character.CurrBullet = Math.Min(character.MaxBullet, character.CurrBullet + setting.Watermelon.IntParam1);
        }
    }

    public class Watermelon2Buff : Buff
    {
        public Watermelon2Buff()
        {
            isPermanent = setting.Watermelon2.IsPermanent;
            buffName = setting.Watermelon2.Name;
            buffDescription = setting.Watermelon2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // restore bullets
            character.CurrBullet = Math.Min(character.MaxBullet, character.CurrBullet + setting.Watermelon2.IntParam1);
            character.SecondaryWeapon.Damage += setting.Watermelon2.IntParam2;
        }
    }

    public class BananaBuff : Buff
    {
        public BananaBuff()
        {
            isPermanent = setting.Banana.IsPermanent;
            duration = setting.Banana.Duration;
            buffName = setting.Banana.Name;
            buffDescription = setting.Banana.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            float modifier = setting.Banana.FloatParam1;
            // bullet velocity *= 2, CD time /= 2
            character.CurrBullet = character.MaxBullet;
            ((RangedWeapon)character.SecondaryWeapon).BulletVelocity *= modifier;
            ((RangedWeapon)character.SecondaryWeapon).CoolDownTime /= modifier;
            durationTimer = new Utils.Timer(duration, () =>
            {
                ((RangedWeapon)character.SecondaryWeapon).BulletVelocity /= modifier;
                ((RangedWeapon)character.SecondaryWeapon).CoolDownTime *= modifier;
            });
            base.Apply(character);
        }
    }

    public class Banana2Buff : Buff
    {
        public Banana2Buff()
        {
            isPermanent = setting.Banana2.IsPermanent;
            duration = setting.Banana2.Duration;
            buffName = setting.Banana2.Name;
            buffDescription = setting.Banana2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            float modifier = setting.Banana2.FloatParam1;
            // bullet velocity *= 2, CD time /= 2
            character.CurrBullet = character.MaxBullet;
            character.CurrHealth = (int)(character.CurrHealth * setting.Banana2.FloatParam2);
            character.PlayOnHitEffect();
            ((RangedWeapon)character.SecondaryWeapon).InfBullet = true;
            ((RangedWeapon)character.SecondaryWeapon).BulletVelocity *= modifier;
            ((RangedWeapon)character.SecondaryWeapon).CoolDownTime /= modifier;
            durationTimer = new Utils.Timer(duration, () =>
            {
                ((RangedWeapon)character.SecondaryWeapon).InfBullet = false;
                ((RangedWeapon)character.SecondaryWeapon).BulletVelocity /= modifier;
                ((RangedWeapon)character.SecondaryWeapon).CoolDownTime *= modifier;
            });
            base.Apply(character);
        }
    }

    public class CoconutBuff : Buff
    {
        public CoconutBuff()
        {
            isPermanent = setting.Coconut.IsPermanent;
            duration = setting.Coconut.Duration;
            buffName = setting.Coconut.Name;
            buffDescription = setting.Coconut.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // bullet velocity *= 2, CD time *= 2
            if (character.State.HasFlag(ECharacterGameState.MeleeAttack))
            {
                character.FinishMeleeAttack();
            }

            ((MeleeWeapon)character.PrimaryWeapon).ScaleFactor *= setting.Coconut.FloatParam1;
            ((MeleeWeapon)character.PrimaryWeapon).AtkCD *= setting.Coconut.FloatParam2;
            character.PrimaryWeapon.Damage *= setting.Coconut.IntParam1;
            ((MeleeWeapon)character.PrimaryWeapon).InitMeleeWeapon();
            ((MeleeWeapon)character.PrimaryWeapon).LoadContent();
            durationTimer = new Utils.Timer(duration, () =>
            {
                if (character.State.HasFlag(ECharacterGameState.MeleeAttack))
                {
                    character.FinishMeleeAttack();
                }

                ((MeleeWeapon)character.PrimaryWeapon).ScaleFactor /= setting.Coconut.FloatParam1;
                ((MeleeWeapon)character.PrimaryWeapon).AtkCD /= setting.Coconut.FloatParam2;
                character.PrimaryWeapon.Damage /= setting.Coconut.IntParam1;
                ((MeleeWeapon)character.PrimaryWeapon).InitMeleeWeapon();
                ((MeleeWeapon)character.PrimaryWeapon).LoadContent();
            });
            base.Apply(character);
        }
    }

    public class GrapeBuff : Buff
    {
        public GrapeBuff()
        {
            isPermanent = setting.Grape.IsPermanent;
            duration = setting.Grape.Duration;
            buffName = setting.Grape.Name;
            buffDescription = setting.Grape.Description;
        }

        public void BananaBuff(PlayerCharacter character, int magnification){
            float modifier = setting.Banana.FloatParam1;
            ((RangedWeapon)character.SecondaryWeapon).BulletVelocity *= (float)Math.Pow(modifier, magnification);
            ((RangedWeapon)character.SecondaryWeapon).CoolDownTime /= (float)Math.Pow(modifier, magnification);
        }

        public override void Apply(PlayerCharacter character)
        {
            // bullet velocity *= 2, CD time /= 2
            character.SpecialWeaponCount += 1;
            character.SecondaryWeapon = new PlayerShotgun(character, ((RangedWeapon)character.SecondaryWeapon).Damage * 2, ((RangedWeapon)character.SecondaryWeapon).FruitEffectMagnification);
            character.SecondaryWeapon.LoadContent();
            BananaBuff(character, ((RangedWeapon)character.SecondaryWeapon).FruitEffectMagnification - 1);
            character.MaxBullet = setting.Grape.IntParam1;
            character.CurrBullet = character.MaxBullet;
            durationTimer = new Utils.Timer(duration, () =>
            {
                character.SpecialWeaponCount -= 1;
                if(character.SpecialWeaponCount <= 0){
                    character.SecondaryWeapon = new RangedWeapon(character, ((RangedWeapon)character.SecondaryWeapon).Damage / 2, ((RangedWeapon)character.SecondaryWeapon).FruitEffectMagnification);
                    character.SecondaryWeapon.LoadContent();
                    BananaBuff(character, ((RangedWeapon)character.SecondaryWeapon).FruitEffectMagnification - 1);
                    character.MaxBullet = 20; // base max ammo count
                    character.CurrBullet = character.MaxBullet;
                }
            });
            base.Apply(character);
        }
    }

    public class PeachBuff : Buff
    {
        public PeachBuff()
        {
            isPermanent = setting.Peach.IsPermanent;
            duration = setting.Peach.Duration;
            buffName = setting.Peach.Name;
            buffDescription = setting.Peach.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // Resistance calculation in Player.cs, currently very overtuned.
            character.DashDamage += setting.Peach.IntParam1;
            character.DashSpeed *= setting.Peach.FloatParam1;
            character.DashCDTime *= setting.Peach.FloatParam1;
            character.Speed *= setting.Peach.FloatParam1;
            durationTimer = new Utils.Timer(duration, () =>
            {
                character.DashDamage -= setting.Peach.IntParam1;
                character.DashSpeed /= setting.Peach.FloatParam1;
                character.DashCDTime /= setting.Peach.FloatParam1;
                character.Speed /= setting.Peach.FloatParam1;
            });
            base.Apply(character);
        }
    }

    public class MangoBuff : Buff
    {
        public MangoBuff()
        {
            isPermanent = setting.Mango.IsPermanent;
            duration = setting.Mango.Duration;
            buffName = setting.Mango.Name;
            buffDescription = setting.Mango.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // Math is elsewhere
            character.AutoReflection = true;
            character.Ranged_defense *= setting.Mango.FloatParam1;
            durationTimer = new Utils.Timer(duration, () => { 
                character.AutoReflection = false;
                character.Ranged_defense /= setting.Mango.FloatParam1;
            });
            base.Apply(character);
        }
    }

    public class Mango2Buff : Buff
    {
        public Mango2Buff()
        {
            isPermanent = setting.Mango2.IsPermanent;
            duration = setting.Mango2.Duration;
            buffName = setting.Mango2.Name;
            buffDescription = setting.Mango2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            // Math is elsewhere
            character.AutoReflection = true;
            character.Ranged_defense *= setting.Mango2.FloatParam1;
            character.Melee_defense *= setting.Mango2.FloatParam2;
            durationTimer = new Utils.Timer(duration, () => { 
                character.AutoReflection = false;
                character.Ranged_defense /= setting.Mango2.FloatParam1;
                character.Melee_defense /= setting.Mango2.FloatParam2;
            });
            base.Apply(character);
        }
    }

    public class OrangeBuff : Buff
    {
        public OrangeBuff()
        {
            isPermanent = setting.Orange.IsPermanent;
            duration = setting.Orange.Duration;
            buffName = setting.Orange.Name;
            buffDescription = setting.Orange.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            character.Melee_defense *= setting.Orange.FloatParam1;
            character.Ranged_defense *= setting.Orange.FloatParam2;
            durationTimer = new Utils.Timer(duration, () =>
            {
                character.Melee_defense /= setting.Orange.FloatParam1;
                character.Ranged_defense /= setting.Orange.FloatParam2;
            });
            base.Apply(character);
        }
    }

    public class Orange2Buff : Buff
    {
        public Orange2Buff()
        {
            isPermanent = setting.Orange2.IsPermanent;
            duration = setting.Orange2.Duration;
            buffName = setting.Orange2.Name;
            buffDescription = setting.Orange2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            character.Melee_defense *= setting.Orange2.FloatParam1;
            character.Ranged_defense *= setting.Orange2.FloatParam2;
            durationTimer = new Utils.Timer(duration, () =>
            {
                character.Melee_defense /= setting.Orange2.FloatParam1;
                character.Ranged_defense /= setting.Orange2.FloatParam2;
            });
            base.Apply(character);
        }
    }

    public class CherryBuff : Buff
    {
        public CherryBuff()
        {
            isPermanent = setting.Cherry.IsPermanent;
            duration = setting.Cherry.Duration;
            buffName = setting.Cherry.Name;
            buffDescription = setting.Cherry.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            character.Melee_defense *= setting.Cherry.FloatParam1;
            character.Ranged_defense *= setting.Cherry.FloatParam1;
            character.MaxHealth = (int)(character.MaxHealth * setting.Cherry.FloatParam1);
            
            base.Apply(character);
        }
    }

    public class Cherry2Buff : Buff
    {
        public Cherry2Buff()
        {
            isPermanent = setting.Cherry2.IsPermanent;
            duration = setting.Cherry2.Duration;
            buffName = setting.Cherry2.Name;
            buffDescription = setting.Cherry2.Description;
        }

        public override void Apply(PlayerCharacter character)
        {
            character.Melee_defense *= setting.Cherry2.FloatParam1;
            character.Ranged_defense *= setting.Cherry2.FloatParam1;
            character.MaxHealth = (int)(character.MaxHealth * setting.Cherry2.FloatParam1);
            
            base.Apply(character);
        }
    }


    public class FruitBag : ICollisionActor
    {
        protected List<string> fruitNames;
        protected List<Buff> buffs;
        protected TextureAnimation fruitBagAnimation;
        protected Vector2 position;
        protected bool destroyed = false;
        protected float scaleFactor = 1.3f;
        protected float moveSpeed = 10f;
        protected float absorbed_distance = 150f;

        public FruitBag(List<string> fruitNames, Vector2 position, int width, int height, MapStage level_stage)
        {
            this.fruitNames = fruitNames;
            this.position = position;
            bounds = new RectangleF(position.X - width * scaleFactor / 2, position.Y - height * scaleFactor / 2,
                width * scaleFactor, height * scaleFactor);
            fruitBagAnimation = new TextureAnimation(Path.Combine("Fruits", "bag_of_fruits"), 1f, 1, true,
                new Vector2(scaleFactor, scaleFactor));

            buffs = new List<Buff>();
            foreach (var fruit in fruitNames)
            {
                buffs.Add(BuffFactory.CreateBuff(fruit, level_stage));
            }

            LoadContent();
        }

        protected IShapeF bounds;

        public bool Destroyed => destroyed;
        public IShapeF Bounds => bounds;

        public void LoadContent()
        {
            fruitBagAnimation.LoadContent();
        }

        public void Update(GameTime gameTime)
        {
            // automatically absorbed by nearby player
            double dt = gameTime.ElapsedGameTime.TotalSeconds;

            PlayerCharacter player = BazaarBountyGame.player;
            Vector2 dir = player.Position - position;
            if (dir.LengthSquared() <= absorbed_distance * absorbed_distance)
            {
                position += dir * moveSpeed * (float)dt;
                bounds.Position += dir * moveSpeed * (float)dt;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            fruitBagAnimation.Draw(spriteBatch, position, 0f);
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            if (collisionInfo.Other.Equals(BazaarBountyGame.player))
            {
                // pop out fruit power-up selection UI
                BazaarBountyGame.GetGameInstance().PowerUpSelection(buffs);
                destroyed = true;
            }
        }
    }
}