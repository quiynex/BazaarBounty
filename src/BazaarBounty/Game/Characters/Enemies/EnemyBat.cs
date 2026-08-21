using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using System;

namespace BazaarBounty;

public class Bat : Slime
{
    public Bat(Vector2 position) : base(position)
    {
        type = CharacterType.Flying;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Bat.Black;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        minAttackRange = setting.MinAttackRange;
        rangeOfVision = setting.RangeOfVision;


        // set the animations of the bat
        var scale = new Vector2(1.0f,1.0f);
        // idleAnimation = new TextureAnimation("Characters/Enemies/Slime/SmallSlime_Green_0", 0.2f, 4, true, scale);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Bat/bat_fly_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Bat/bat_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Bat/bat_fly_right", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Bat/bat_death_right", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Bat/bat_fly_right", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Bat/bat_death_right", 0.2f, 4, false, scale);

        Initialize();
    }

    public override void LoadContent()
    {
        base.LoadContent();
        // set the audio for the bat
        deathSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/bat_death");
    }
}

public class BlueBat : Bat
{
    public BlueBat(Vector2 position) : base(position)
    {
        type = CharacterType.Flying;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Bat.Blue;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        minAttackRange = setting.MinAttackRange;
        rangeOfVision = setting.RangeOfVision;


        // set the animations of the slime
        var scale = new Vector2(1.0f,1.0f);
        // idleAnimation = new TextureAnimation("Characters/Enemies/Slime/SmallSlime_Green_0", 0.2f, 4, true, scale);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Bat/bat_blue_fly_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Bat/bat_blue_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Bat/bat_blue_fly_right", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Bat/bat_blue_death_right", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Bat/bat_blue_fly_right", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Bat/bat_blue_death_right", 0.2f, 4, false, scale);
        Initialize();
    }
}

public class RedBat : Bat
{
    public RedBat(Vector2 position) : base(position)
    {
        type = CharacterType.Flying;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Bat.Red;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        minAttackRange = setting.MinAttackRange;
        rangeOfVision = setting.RangeOfVision;


        // set the animations of the slime
        var scale = new Vector2(1.0f,1.0f);
        // idleAnimation = new TextureAnimation("Characters/Enemies/Slime/SmallSlime_Green_0", 0.2f, 4, true, scale);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Bat/bat_red_fly_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Bat/bat_red_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Bat/bat_red_fly_right", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Bat/bat_red_death_right", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Bat/bat_red_fly_right", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Bat/bat_red_death_right", 0.2f, 4, false, scale);
        Initialize();
    }
}