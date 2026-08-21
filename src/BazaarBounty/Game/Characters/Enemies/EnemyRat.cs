using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using System;

namespace BazaarBounty;

public class Rat : MeleeEnemy
{
    protected float attackGapTime;
    protected float lastAttackTime;
    protected float scaleFactor;

    private TextureAnimation idleAnimationBr;
    private TextureAnimation idleAnimationDown;
    private TextureAnimation idleAnimationTop;
    private TextureAnimation idleAnimationTr;

    private TextureAnimation walkAnimationBr;
    private TextureAnimation walkAnimationDown;
    private TextureAnimation walkAnimationTop;
    private TextureAnimation walkAnimationTr;

    private TextureAnimation onHitAnimation;

    private Weapon weapon;
    public Weapon Weapon => weapon;

    public Rat(Vector2 position) : base(position)
    {
        type = CharacterType.Melee;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Rat.Brown;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;

        lastAttackTime = 0;

        scaleFactor = 0.7f;
        Vector2 textureScale = new Vector2(scaleFactor, scaleFactor);
        idleAnimationBr = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationDown = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTop = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTr = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_idle_anim", 0.2f, 1, true, textureScale);

        walkAnimationBr = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationDown = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTop = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTr = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_walk_anim", 0.2f, 2, true, textureScale);

        onHitAnimation = new TextureAnimation("Characters/Enemies/Rat/rat_swordman_onhit_anim", stunTime / 4.0f, 4, false, textureScale);

        weapon = new EnemyMeleeWeapon(this);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        idleAnimationBr.Update(gameTime);
        idleAnimationDown.Update(gameTime);
        idleAnimationTop.Update(gameTime);
        idleAnimationTr.Update(gameTime);

        walkAnimationBr.Update(gameTime);
        walkAnimationDown.Update(gameTime);
        walkAnimationTop.Update(gameTime);
        walkAnimationTr.Update(gameTime);

        onHitAnimation.Update(gameTime);

        weapon.Update(gameTime);
    }

    public override void LoadContent()
    {
        hurtSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/rat_onhit");
        deathSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/rat_death");

        base.LoadContent();
        idleAnimationBr.LoadContent();
        idleAnimationDown.LoadContent();
        idleAnimationTop.LoadContent();
        idleAnimationTr.LoadContent();

        walkAnimationBr.LoadContent();
        walkAnimationDown.LoadContent();
        walkAnimationTop.LoadContent();
        walkAnimationTr.LoadContent();

        onHitAnimation.LoadContent();

        weapon.LoadContent();

        Vector2 size = idleAnimationBr.Size;
        characterWidth = size.X  * scaleFactor / 2;
        characterHeight = size.Y * scaleFactor;
        updateHeight = characterHeight / 2;
        updateWidth = characterWidth / 2;
        bounds = new RectangleF(position.X - characterWidth / 2, position.Y - characterHeight / 2, 
            characterWidth, characterHeight);
    }

    private TextureAnimation GetAnimationFramesForDirection(ECharacterGameState gameState, string directionSuffix)
    {
        if (gameState.HasFlag(ECharacterGameState.OnHit))
        {
            return onHitAnimation;
        }
        else if (gameState.HasFlag(ECharacterGameState.Walk))
        {
            switch (directionSuffix)
            {
                case "br": return walkAnimationBr;
                case "down": return walkAnimationDown;
                case "top": return walkAnimationTop;
                case "tr": return walkAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
        // Add cases for other states as necessary   
        else
        {
            switch (directionSuffix)
            {
                case "br": return idleAnimationBr;
                case "down": return idleAnimationDown;
                case "top": return idleAnimationTop;
                case "tr": return idleAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DebugDraw(spriteBatch);
        // Convert lookDirection to an angle in degrees, normalized to [0, 360)
        float angle = MathHelper.ToDegrees((float)Math.Atan2(lookDirection.Y, lookDirection.X));
        angle = (angle + 360) % 360;

        // Calculate the index of the direction segment (0-7)
        int directionIndex = (int)((angle + 22.5) / 45) % 8;

        // Map directionIndex to direction suffix and check if we need to mirror the sprite
        string[] directionSuffixes = { "br", "br", "down", "br", "br", "tr", "top", "tr" };
        SpriteEffects[] spriteEffects = { SpriteEffects.None, SpriteEffects.None, SpriteEffects.None, SpriteEffects.FlipHorizontally, 
            SpriteEffects.FlipHorizontally, SpriteEffects.FlipHorizontally, SpriteEffects.None, SpriteEffects.None };
        string directionSuffix = directionSuffixes[directionIndex];
        SpriteEffects spriteEffect = spriteEffects[directionIndex];

        // Assuming a method to select the correct texture array based on the state and directionSuffix
        TextureAnimation currentAnimationFrames = GetAnimationFramesForDirection(State, directionSuffix);

        Color color = state.IsHit || state.IsDead ? Color.Red : Color.White;
        currentAnimationFrames.Draw(spriteBatch, position, 0, spriteEffect, color:color);
        weapon.Draw(spriteBatch);
    }
        
    public override void MeleeAttack()
    {
        if (state.SwitchState(ECharacterGameState.MeleeAttack))
        {
            weapon.Attack();
        }
    }

    protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
        Trigger();
        hitBackAnimation.Play();
        hitBackDirection = -hitDirection;
        hitBackDirection.Normalize();
        state.SwitchState(ECharacterGameState.OnHit);
        if (currHealth <= 0)
        {
            deathSound.Play();
            state.SwitchState(ECharacterGameState.Dead);
            Utils.Timer.Delay(stunTime, () => state.SwitchState(ECharacterGameState.Destroyed));
        }
        else
        {
            onHitAnimation.Play();
            hurtSound.Play();
            Utils.Timer.Delay(stunTime, () => state.ClearState(ECharacterGameState.OnHit));
        }
    }

    public float AttackGapTime => attackGapTime;
    public float LastAttackTime => lastAttackTime;
}

public class BlueRat : MeleeEnemy
{
    protected float attackGapTime;
    protected float lastAttackTime;
    protected float scaleFactor;

    private TextureAnimation idleAnimationBr;
    private TextureAnimation idleAnimationDown;
    private TextureAnimation idleAnimationTop;
    private TextureAnimation idleAnimationTr;

    private TextureAnimation walkAnimationBr;
    private TextureAnimation walkAnimationDown;
    private TextureAnimation walkAnimationTop;
    private TextureAnimation walkAnimationTr;

    private TextureAnimation onHitAnimation;

    private Weapon weapon;
    public Weapon Weapon => weapon;

    public BlueRat(Vector2 position) : base(position)
    {
        type = CharacterType.Melee;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Rat.Blue;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;


        // Set Rat properties
        speed = 100f;
        maxHealth = 10;
        currHealth = maxHealth;
        stunTime = 0.4f;
        attackRange = 50;
        attackGapTime = 1.0f;
        lastAttackTime = 0;

        scaleFactor = 0.7f;
        Vector2 textureScale = new Vector2(scaleFactor, scaleFactor);
        idleAnimationBr = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationDown = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTop = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTr = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);

        walkAnimationBr = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationDown = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTop = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTr = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);

        onHitAnimation = new TextureAnimation("Characters/Enemies/Rat/blue_rat_swordman_onhit_anim", stunTime / 4.0f, 4, false, textureScale);

        weapon = new EnemyMeleeWeapon(this);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        idleAnimationBr.Update(gameTime);
        idleAnimationDown.Update(gameTime);
        idleAnimationTop.Update(gameTime);
        idleAnimationTr.Update(gameTime);

        walkAnimationBr.Update(gameTime);
        walkAnimationDown.Update(gameTime);
        walkAnimationTop.Update(gameTime);
        walkAnimationTr.Update(gameTime);

        onHitAnimation.Update(gameTime);

        weapon.Update(gameTime);
    }

    public override void LoadContent()
    {
        base.LoadContent();
        idleAnimationBr.LoadContent();
        idleAnimationDown.LoadContent();
        idleAnimationTop.LoadContent();
        idleAnimationTr.LoadContent();

        walkAnimationBr.LoadContent();
        walkAnimationDown.LoadContent();
        walkAnimationTop.LoadContent();
        walkAnimationTr.LoadContent();

        onHitAnimation.LoadContent();

        weapon.LoadContent();

        Vector2 size = idleAnimationBr.Size;
        characterWidth = size.X  * scaleFactor / 2;
        characterHeight = size.Y * scaleFactor;
        updateHeight = characterHeight / 2;
        updateWidth = characterWidth / 2;
        bounds = new RectangleF(position.X - characterWidth / 2, position.Y - characterHeight / 2, 
            characterWidth, characterHeight);
    }

    private TextureAnimation GetAnimationFramesForDirection(ECharacterGameState gameState, string directionSuffix)
    {
        if (gameState.HasFlag(ECharacterGameState.OnHit))
        {
            return onHitAnimation;
        }
        else if (gameState.HasFlag(ECharacterGameState.Walk))
        {
            switch (directionSuffix)
            {
                case "br": return walkAnimationBr;
                case "down": return walkAnimationDown;
                case "top": return walkAnimationTop;
                case "tr": return walkAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
        // Add cases for other states as necessary   
        else
        {
            switch (directionSuffix)
            {
                case "br": return idleAnimationBr;
                case "down": return idleAnimationDown;
                case "top": return idleAnimationTop;
                case "tr": return idleAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DebugDraw(spriteBatch);
        // Convert lookDirection to an angle in degrees, normalized to [0, 360)
        float angle = MathHelper.ToDegrees((float)Math.Atan2(lookDirection.Y, lookDirection.X));
        angle = (angle + 360) % 360;

        // Calculate the index of the direction segment (0-7)
        int directionIndex = (int)((angle + 22.5) / 45) % 8;

        // Map directionIndex to direction suffix and check if we need to mirror the sprite
        string[] directionSuffixes = { "br", "br", "down", "br", "br", "tr", "top", "tr" };
        SpriteEffects[] spriteEffects = { SpriteEffects.None, SpriteEffects.None, SpriteEffects.None, SpriteEffects.FlipHorizontally, 
            SpriteEffects.FlipHorizontally, SpriteEffects.FlipHorizontally, SpriteEffects.None, SpriteEffects.None };
        string directionSuffix = directionSuffixes[directionIndex];
        SpriteEffects spriteEffect = spriteEffects[directionIndex];

        // Assuming a method to select the correct texture array based on the state and directionSuffix
        TextureAnimation currentAnimationFrames = GetAnimationFramesForDirection(State, directionSuffix);

        Color color = state.IsHit || state.IsDead ? Color.Red : Color.White;
        currentAnimationFrames.Draw(spriteBatch, position, 0, spriteEffect, color:color);
        weapon.Draw(spriteBatch);
    }
        
    public override void MeleeAttack()
    {
        if (state.SwitchState(ECharacterGameState.MeleeAttack))
        {
            weapon.Attack();
        }
    }

    protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
        Trigger();
        hitBackAnimation.Play();
        hitBackDirection = -hitDirection;
        hitBackDirection.Normalize();
        state.SwitchState(ECharacterGameState.OnHit);
        if (currHealth <= 0)
        {
            state.SwitchState(ECharacterGameState.Dead);
            Utils.Timer.Delay(stunTime, () => state.SwitchState(ECharacterGameState.Destroyed));
        }
        else
        {
            onHitAnimation.Play();
            hurtSound.Play();
            Utils.Timer.Delay(stunTime, () => state.ClearState(ECharacterGameState.OnHit));
        }
    }

    public float AttackGapTime => attackGapTime;
    public float LastAttackTime => lastAttackTime;
}

public class RedRat : MeleeEnemy
{
    protected float attackGapTime;
    protected float lastAttackTime;
    protected float scaleFactor;

    private TextureAnimation idleAnimationBr;
    private TextureAnimation idleAnimationDown;
    private TextureAnimation idleAnimationTop;
    private TextureAnimation idleAnimationTr;

    private TextureAnimation walkAnimationBr;
    private TextureAnimation walkAnimationDown;
    private TextureAnimation walkAnimationTop;
    private TextureAnimation walkAnimationTr;

    private TextureAnimation onHitAnimation;

    private Weapon weapon;
    public Weapon Weapon => weapon;

    public RedRat(Vector2 position) : base(position)
    {
        type = CharacterType.Melee;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Rat.Red;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        attackRange = setting.AttackRange;
        attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;


        // Set Rat properties
        speed = 100f;
        maxHealth = 10;
        currHealth = maxHealth;
        stunTime = 0.4f;
        attackRange = 50;
        attackGapTime = 1.0f;
        lastAttackTime = 0;

        scaleFactor = 0.7f;
        Vector2 textureScale = new Vector2(scaleFactor, scaleFactor);
        idleAnimationBr = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationDown = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTop = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);
        idleAnimationTr = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_idle_anim", 0.2f, 1, true, textureScale);

        walkAnimationBr = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationDown = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTop = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);
        walkAnimationTr = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_walk_anim", 0.2f, 2, true, textureScale);

        onHitAnimation = new TextureAnimation("Characters/Enemies/Rat/red_rat_swordman_onhit_anim", stunTime / 4.0f, 4, false, textureScale);

        weapon = new EnemyMeleeWeapon(this);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        idleAnimationBr.Update(gameTime);
        idleAnimationDown.Update(gameTime);
        idleAnimationTop.Update(gameTime);
        idleAnimationTr.Update(gameTime);

        walkAnimationBr.Update(gameTime);
        walkAnimationDown.Update(gameTime);
        walkAnimationTop.Update(gameTime);
        walkAnimationTr.Update(gameTime);

        onHitAnimation.Update(gameTime);

        weapon.Update(gameTime);
    }

    public override void LoadContent()
    {
        base.LoadContent();
        idleAnimationBr.LoadContent();
        idleAnimationDown.LoadContent();
        idleAnimationTop.LoadContent();
        idleAnimationTr.LoadContent();

        walkAnimationBr.LoadContent();
        walkAnimationDown.LoadContent();
        walkAnimationTop.LoadContent();
        walkAnimationTr.LoadContent();

        onHitAnimation.LoadContent();

        weapon.LoadContent();

        Vector2 size = idleAnimationBr.Size;
        characterWidth = size.X  * scaleFactor / 2;
        characterHeight = size.Y * scaleFactor;
        updateHeight = characterHeight / 2;
        updateWidth = characterWidth / 2;
        bounds = new RectangleF(position.X - characterWidth / 2, position.Y - characterHeight / 2, 
            characterWidth, characterHeight);
    }

    private TextureAnimation GetAnimationFramesForDirection(ECharacterGameState gameState, string directionSuffix)
    {
        if (gameState.HasFlag(ECharacterGameState.OnHit))
        {
            return onHitAnimation;
        }
        else if (gameState.HasFlag(ECharacterGameState.Walk))
        {
            switch (directionSuffix)
            {
                case "br": return walkAnimationBr;
                case "down": return walkAnimationDown;
                case "top": return walkAnimationTop;
                case "tr": return walkAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
        // Add cases for other states as necessary   
        else
        {
            switch (directionSuffix)
            {
                case "br": return idleAnimationBr;
                case "down": return idleAnimationDown;
                case "top": return idleAnimationTop;
                case "tr": return idleAnimationTr;
                default: return null; // Handle this case as needed
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DebugDraw(spriteBatch);
        // Convert lookDirection to an angle in degrees, normalized to [0, 360)
        float angle = MathHelper.ToDegrees((float)Math.Atan2(lookDirection.Y, lookDirection.X));
        angle = (angle + 360) % 360;

        // Calculate the index of the direction segment (0-7)
        int directionIndex = (int)((angle + 22.5) / 45) % 8;

        // Map directionIndex to direction suffix and check if we need to mirror the sprite
        string[] directionSuffixes = { "br", "br", "down", "br", "br", "tr", "top", "tr" };
        SpriteEffects[] spriteEffects = { SpriteEffects.None, SpriteEffects.None, SpriteEffects.None, SpriteEffects.FlipHorizontally, 
            SpriteEffects.FlipHorizontally, SpriteEffects.FlipHorizontally, SpriteEffects.None, SpriteEffects.None };
        string directionSuffix = directionSuffixes[directionIndex];
        SpriteEffects spriteEffect = spriteEffects[directionIndex];

        // Assuming a method to select the correct texture array based on the state and directionSuffix
        TextureAnimation currentAnimationFrames = GetAnimationFramesForDirection(State, directionSuffix);

        Color color = state.IsHit || state.IsDead ? Color.Red : Color.White;
        currentAnimationFrames.Draw(spriteBatch, position, 0, spriteEffect, color:color);
        weapon.Draw(spriteBatch);
    }
        
    public override void MeleeAttack()
    {
        if (state.SwitchState(ECharacterGameState.MeleeAttack))
        {
            weapon.Attack();
        }
    }

    protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
        Trigger();
        hitBackAnimation.Play();
        hitBackDirection = -hitDirection;
        hitBackDirection.Normalize();
        state.SwitchState(ECharacterGameState.OnHit);
        generalHurtSound.Play();
        if (currHealth <= 0)
        {
            state.SwitchState(ECharacterGameState.Dead);
            Utils.Timer.Delay(stunTime, () => state.SwitchState(ECharacterGameState.Destroyed));
        }
        else
        {
            onHitAnimation.Play();
            hurtSound.Play();
            Utils.Timer.Delay(stunTime, () => state.ClearState(ECharacterGameState.OnHit));
        }
    }

    public float AttackGapTime => attackGapTime;
    public float LastAttackTime => lastAttackTime;
}