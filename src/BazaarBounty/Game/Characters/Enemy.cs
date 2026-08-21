using System;
using Assimp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace BazaarBounty;

public class EnemyCharacter : Character
{
    protected SoundEffect hurtSound = null;
    protected SoundEffect generalHurtSound = null;
    protected SoundEffect deathSound = null;

    protected bool triggered = false;

    // Enemy properties
    protected float rangeOfVision;
    protected float attackRange;
    protected float idleTime;
    protected float idleTimePoint;

    // Enemy Patrol AI related
    protected PatrolStates patrolState;
    protected Vector2 patrolOrigin;
    protected Vector2 patrolDestination;

    // Enemy animations
    protected TextureAnimation idleAnimation;
    protected TextureAnimation movingAnimation;
    protected TextureAnimation attackAnimation;
    protected TextureAnimation takeHitAnimation;
    protected TextureAnimation deathAnimation;
    protected TextureAnimation stunIndicatorAnimation;

    // hit back related
    protected float hitBackDistance = 50;
    protected Vector2 hitBackDirection;
    protected TransformationAnimation hitBackAnimation;

    public enum PatrolStates
    {
        Idle,
        Departure,
        Return
    }

    public EnemyCharacter(Vector2 position) : base(position)
    {
        // Set default properties
        attackRange = 40;
        rangeOfVision = 300;
        idleTime = 3;
        idleTimePoint = 0;
        // Set the default patrol origin and destination
        patrolOrigin = position;
        patrolDestination = BazaarBountyGame.levelManager.CurrentMap.SampleFloorPosition(); // Potentially dangerous, requires us to not use flooring "outside" the map.
        patrolState = PatrolStates.Departure;

        stunIndicatorAnimation = new TextureAnimation("VisualEffects/stun_indicator", 0.2f, 3);
    }

    public override void LoadContent()
    {
        base.LoadContent();
        idleAnimation?.LoadContent();
        movingAnimation?.LoadContent();
        attackAnimation?.LoadContent();
        takeHitAnimation?.LoadContent();
        deathAnimation?.LoadContent();
        stunIndicatorAnimation?.LoadContent();
        generalHurtSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/enemy_hurt");
        hurtSound ??= BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/enemy_hurt");
        deathSound ??= BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/enemy_hurt");
        if (attackAnimation != null) attackAnimation.OnAnimationFinished = FinishMeleeAttack;
        if (deathAnimation != null) deathAnimation.OnAnimationFinished = () => state.SwitchState(ECharacterGameState.Destroyed);
        if (takeHitAnimation != null) takeHitAnimation.OnAnimationFinished = () => { state.ClearState(ECharacterGameState.OnHit); state.ClearState(ECharacterGameState.Stunned); };
        hitBackAnimation = new TransformationAnimation("hitBack")
            .AddKeyframe(0.0f, new Vector2(0.0f), 0.0f, Vector2.One)
            .AddKeyframe(stunTime/3, new Vector2(0.5f), 0.0f, Vector2.One)
            .AddKeyframe(stunTime, new Vector2(1.0f), 0.0f, Vector2.One);
    }

    public override void Update(GameTime gameTime)
    {
        idleAnimation?.Update(gameTime);
        movingAnimation?.Update(gameTime);
        attackAnimation?.Update(gameTime);
        takeHitAnimation?.Update(gameTime);
        deathAnimation?.Update(gameTime);
        stunIndicatorAnimation?.Update(gameTime);

        hitBackAnimation.Update(gameTime);
        if (hitBackAnimation.isPlaying)
        {
            // Console.WriteLine("Hit back animation is playing");
            Transform2 delta = hitBackAnimation.DeltaTransform;
            // use translation track interpolation for progress representation
            float frameProgress = delta.Position.X;
            position += hitBackDirection * hitBackDistance * frameProgress;
        }


        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        SpriteEffects spriteEffect = SpriteEffects.None;
        if (lookDirection.X < 0)
        {
            spriteEffect = SpriteEffects.FlipHorizontally;
        }

        Color color = state.IsHit || state.IsDead ? Color.Red : Color.White;
        if (state.IsStunned)
            stunIndicatorAnimation.Draw(spriteBatch, position - new Vector2(0.0f, 25.0f), 0.0f);

        if (state.IsHit)
            takeHitAnimation.Draw(spriteBatch, position, 0f, spriteEffect, color: color);
        else if (state.IsAttacking)
            attackAnimation.Draw(spriteBatch, position, 0f, spriteEffect, color: color);
        else if (state.IsDead)
            deathAnimation.Draw(spriteBatch, position, 0f, spriteEffect, color: color);
        else if (state.IsMoving)
            movingAnimation.Draw(spriteBatch, position, 0f, spriteEffect, color: color);
        else // idle
            idleAnimation.Draw(spriteBatch, position, 0f, spriteEffect, color: color);
    }

    public override void Move(Vector2 dir)
    {
        // If the enemy is hit and attcking, it should not move
        if (!state.IsHit && !state.IsAttacking && state.SwitchState(ECharacterGameState.Walk))
            base.Move(dir);
    }

    public override void MeleeAttack()
    {
        if (state.SwitchState(ECharacterGameState.MeleeAttack))
        {
            attackAnimation.Play();
        }
    }

    private bool lastInVision;
    private double lastCheckVisionTime;
    public bool CheckPlayerInVision()
    {
        // check the visibility for every 1 second to avoid jittering problem
        if (BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds - lastCheckVisionTime < 1)
            return lastInVision;
        Vector2 dir2Player = BazaarBountyGame.player.Position - position;
        lastInVision = dir2Player.LengthSquared() <= rangeOfVision * rangeOfVision || triggered;
        lastCheckVisionTime = BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
        return lastInVision;
    }


    public override void OnCollision(CollisionEventArgs collisionInfo)
    {
        base.OnCollision(collisionInfo);
        if (collisionInfo.Other is CollidableTiles && !triggered)
        {
            // very naive way to keep patrol behavior functional
            // which is simple resetting patrol Destination whenever it touches the wall
            if (this.type != CharacterType.Flying ) {
                patrolDestination = position;
            }
        }
        // Cannot get hurt if already hit, dead or destroyed
        if (state.IsHit || state.IsDead || state.IsDestroyed)
            return;
        if (collisionInfo.Other is PlayerCharacter && 
            ((PlayerCharacter)collisionInfo.Other).DashDamage > 0 &&
            ((PlayerCharacter)collisionInfo.Other).State.HasFlag(ECharacterGameState.Dash)){
            // deal dash damage to enemy (if any)
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.EnemyHit, 0.4f);
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, (int)((PlayerCharacter)collisionInfo.Other).DashDamage, new HslColor(0f, 1f, 1f));
            currHealth = (int)(currHealth - ((PlayerCharacter)collisionInfo.Other).DashDamage);
            Vector2 hitDirection = ((PlayerCharacter)collisionInfo.Other).Position - position;
            OnHit(hitDirection);
        }
        if (collisionInfo.Other is MeleeWeapon.WeaponHitBox hitBox && hitBox.Weapon.DamageState
            && hitBox.Owner is PlayerCharacter && !state.IsHit && !state.IsDead)
        {
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.EnemyHit, 0.4f);
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, hitBox.Weapon.Damage, new HslColor(0f, 1f, 1f));
            currHealth -= hitBox.Weapon.Damage;
            Vector2 hitDirection = hitBox.Owner.Position - position;
            OnHit(hitDirection);
        }
        else if (collisionInfo.Other is Projectile projectile && projectile.Owner is PlayerCharacter)
        {
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.EnemyHit, 0.4f);
            BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, ((Projectile)collisionInfo.Other).Damage, new HslColor(0f, 1f, 1f));
            Vector2 hitDirection = projectile.Position - position;
            currHealth -= projectile.Damage;
            OnHit(hitDirection, true);
        }
    }

    protected void Trigger()
    {
        triggered = true;
        Utils.Timer.Delay(5.5f, () => { triggered = false; });
    }

    protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
        Trigger();
        if (!state.SwitchState(ECharacterGameState.OnHit))
            return;
        hitBackAnimation.Play();
        hitBackDirection = -hitDirection;
        hitBackDirection.Normalize();
        generalHurtSound.Play();
        if (currHealth <= 0)
        {
            state.SwitchState(ECharacterGameState.Dead);
            deathAnimation.Play();
            deathSound.Play();
        }
        else
        {
            takeHitAnimation.Play();
            hurtSound.Play();

            if (isParryStunned)
            {
                if (!state.SwitchState(ECharacterGameState.Stunned))
                    return;
            }
        }
    }

    public virtual bool CheckInAttackRange()
    {
        Vector2 dir = BazaarBountyGame.player.Position - position;
        float dist = dir.LengthSquared();
        return dist <= attackRange * attackRange;
    }

    // Getter and Setter
    public PatrolStates PatrolState
    {
        get => patrolState;
        set => patrolState = value;
    }

    public float IdleStartTime
    {
        get => idleTimePoint;
        set => idleTimePoint = value;
    }

    public float IdleTotalTime => idleTime;
    public Vector2 PatrolStartPoint => patrolOrigin;
    public Vector2 PatrolDestination => patrolDestination;
    public float AttackRange => attackRange;
}

public class MeleeEnemy : EnemyCharacter
{
    protected bool damageState;
    // By default, damage is caused when melee attacking
    public virtual bool DamageState => state.IsMeleeAttacking;

    public MeleeEnemy(Vector2 position) : base(position)
    {
        // The properties values are set in the derived class
    }
}

public class RangedEnemy : EnemyCharacter
{
    protected Weapon rangedWeapon;

    public RangedEnemy(Vector2 position) : base(position)
    {
        // override the default properties for ranged enemy
        attackRange = 200;
        rangeOfVision = 500;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        rangedWeapon.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        rangedWeapon.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        rangedWeapon.Draw(spriteBatch);
    }

    public override void RangedAttack()
    {
        if (state.SwitchState(ECharacterGameState.RangedAttack))
        {
            rangedWeapon.Attack();
            FinishRangedAttack();
        }
    }
}