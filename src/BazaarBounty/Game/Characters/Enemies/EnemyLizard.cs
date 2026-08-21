using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;

namespace BazaarBounty;

public class Lizard : RangedEnemy
{
    public Lizard(Vector2 position) : base(position)
    {
        type = CharacterType.Ranged;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Lizard.Purple;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        // attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;
        // rangeOfVision = setting.RangeOfVision;

        rangedWeapon = new EnemyMagicWeapon(this);
        // Initialize the animation
        idleAnimation = new TextureAnimation("Characters/Enemies/Lizard/purple_lizard_idle_anim", 0.2f, 4, true, Vector2.One, false);
        movingAnimation = new TextureAnimation("Characters/Enemies/Lizard/purple_lizard_walk_anim", 0.2f, 6, true, Vector2.One, false);
        attackAnimation = new TextureAnimation("Characters/Enemies/Lizard/purple_lizard_idle_anim", 0.1f, 4, true, Vector2.One, false);
        takeHitAnimation = new TextureAnimation("Characters/Enemies/Lizard/purple_lizard_idle_anim", (float)stunTime/4, 4, false, Vector2.One, false);
        deathAnimation = new TextureAnimation("Characters/Enemies/Lizard/purple_lizard_idle_anim", 0.1f, 4, false, Vector2.One, false);
        deathAnimation.OnAnimationFinished = () => state.SwitchState(ECharacterGameState.Destroyed);
        // Initialize the bounding box
        characterWidth = 13 * 3;
        characterHeight = 18 * 3;
        updateWidth = characterWidth / 2;
        updateHeight = characterHeight / 2;
        bounds = new RectangleF(position.X - updateWidth, position.Y - updateHeight, 
            characterWidth, characterHeight);
    }

    public override void LoadContent()
    {
        hurtSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/lizard_onhit");
        deathSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/lizard_death");
        base.LoadContent();
    }
}

public class BlueLizard : RangedEnemy
{
    public BlueLizard(Vector2 position) : base(position)
    {
        type = CharacterType.Ranged;
        
        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Lizard.Blue;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        // attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;
        // rangeOfVision = setting.RangeOfVision;

        rangedWeapon = new EnemyMagicTripleShotgun(this);
        idleAnimation = new TextureAnimation("Characters/Enemies/Lizard/blue_lizard_idle_anim", 0.2f, 4);
        movingAnimation = new TextureAnimation("Characters/Enemies/Lizard/blue_lizard_walk_anim", 0.2f, 6);
        attackAnimation = new TextureAnimation("Characters/Enemies/Lizard/blue_lizard_idle_anim", 0.1f, 4);
        takeHitAnimation = new TextureAnimation("Characters/Enemies/Lizard/blue_lizard_idle_anim", stunTime/4, 4, false);
        deathAnimation = new TextureAnimation("Characters/Enemies/Lizard/blue_lizard_idle_anim", 0.1f, 4, false);

        characterWidth = 15 * 3;
        characterHeight = 18 * 3;
        updateWidth = characterWidth / 2;
        updateHeight = characterHeight / 2;
        bounds = new RectangleF(position.X - updateWidth, position.Y - updateHeight, 
            characterWidth, characterHeight);
    }
}

public class RedLizard : RangedEnemy
{
    public RedLizard(Vector2 position) : base(position)
    {
        type = CharacterType.Ranged;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Lizard.Red;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        // attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;
        // rangeOfVision = setting.RangeOfVision;

        rangedWeapon = new EnemyMagicQuintupleShotgun(this);
        idleAnimation = new TextureAnimation("Characters/Enemies/Lizard/enraged_red_lizard_idle_anim", 0.2f, 4);
        movingAnimation = new TextureAnimation("Characters/Enemies/Lizard/enraged_red_lizard_walk_anim", 0.2f, 6);
        attackAnimation = new TextureAnimation("Characters/Enemies/Lizard/enraged_red_lizard_idle_anim", 0.1f, 4);
        takeHitAnimation = new TextureAnimation("Characters/Enemies/Lizard/enraged_red_lizard_idle_anim", stunTime/4, 4, false);
        deathAnimation = new TextureAnimation("Characters/Enemies/Lizard/enraged_red_lizard_idle_anim", 0.1f, 4, false);

        characterWidth = 15 * 3;
        characterHeight = 18 * 3;
        updateWidth = characterWidth / 2;
        updateHeight = characterHeight / 2;
        bounds = new RectangleF(position.X - updateWidth, position.Y - updateHeight, 
            characterWidth, characterHeight);
    }
}