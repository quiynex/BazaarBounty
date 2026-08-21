using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;

namespace BazaarBounty;

[Flags]
public enum ECharacterGameState
{
    // movement state
    Idle = 0b1,
    Walk = 0b10,
    Dash = 0b100,

    // attack state
    MeleeAttack = 0b1000,
    RangedAttack = 0b10000,

    // hit state
    OnHit = 0b100000,
    Dead = 0b1000000,
    Destroyed = 0b10000000,
    Stunned = 0b100000000
}

public class CharacterGameState
{
    private ECharacterGameState state;
    public ECharacterGameState State => state;
    public bool IsDestroyed => state.HasFlag(ECharacterGameState.Destroyed);
    public bool IsDead => state.HasFlag(ECharacterGameState.Dead);

    public bool IsAttacking => state.HasFlag(ECharacterGameState.MeleeAttack) || state.HasFlag(ECharacterGameState.RangedAttack);
    public bool IsMeleeAttacking => state.HasFlag(ECharacterGameState.MeleeAttack);
    public bool IsRangedAttacking => state.HasFlag(ECharacterGameState.RangedAttack);

    public bool IsHit => state.HasFlag(ECharacterGameState.OnHit);
    public bool IsMoving => state.HasFlag(ECharacterGameState.Walk) || state.HasFlag(ECharacterGameState.Dash);

    public bool IsStunned => state.HasFlag(ECharacterGameState.Stunned);

    public CharacterGameState()
    {
        state = ECharacterGameState.Idle;
    }

    public void ResetMoveState()
    {
        // Cannot reset move state if character is dashing
        if (state.HasFlag(ECharacterGameState.Dash))
        {
            return;
        }
        state &= ~ECharacterGameState.Walk;
        state |= ECharacterGameState.Idle;
    }

    public bool SwitchState(ECharacterGameState newState)
    {
        // Force switch to dead or destroyed state
        if (newState.HasFlag(ECharacterGameState.Dead) || newState.HasFlag(ECharacterGameState.Destroyed))
        {
            state = newState;
            return true;
        }

        // Cannot switch state if current state is dead or destroyed
        if (state.HasFlag(ECharacterGameState.Dead) || state.HasFlag(ECharacterGameState.Destroyed))
            return false;

        // Change Walk State
        if (newState.HasFlag(ECharacterGameState.Walk) || newState.HasFlag(ECharacterGameState.Dash))
        {
            if (state.HasFlag(ECharacterGameState.Dash)) return false;
            state &= ~ECharacterGameState.Idle & ~ECharacterGameState.Walk;
        }

        // Change Attack State
        if (newState.HasFlag(ECharacterGameState.MeleeAttack))
        {
            if (IsMeleeAttacking) return false; // Cannot attack while attacking
        }

        if (newState.HasFlag(ECharacterGameState.RangedAttack))
        {
            if (IsRangedAttacking) return false; // Cannot attack while attacking
        }

        // Change Hit State
        if (newState.HasFlag(ECharacterGameState.OnHit))
        {
            if (IsHit) return false; // Cannot hit while being hit
        }

        // Change Stun State
        if (newState.HasFlag(ECharacterGameState.Stunned))
        {
            if (IsStunned) return false; // Cannot get stunned while being stunned
        }

        state |= newState;
        return true;
    }

    public bool ClearState(ECharacterGameState oldState)
    {
        if (!state.HasFlag(oldState)) return false;
        state &= ~oldState;
        return true;
    }
}

public class Character : ICollisionActor
{
    // Common character attributes
    SoundEffect dashSound;
    protected int damage;
    protected int maxHealth;
    protected int currHealth;
    protected int maxBullet;
    protected int currBullet;
    protected float speed;
    protected float melee_defense = 1;
    protected float ranged_defense = 1;

    protected bool autoReflection;

    // Dash related
    protected bool dashReady;
    protected float dashSpeed;
    protected float dashTime;
    protected float dashCooldownTime;
    protected Utils.Timer dashTimer;
    protected float dashDamage;
    protected float stunTime;
    protected Vector2 position;
    protected CharacterType type;
    protected CharacterGameState state;

    // Movement and Direction related
    protected Vector2 moveDirection = Vector2.Zero;
    protected Vector2 lookDirection = new(1, 0);
    private Vector2 tempMoveDirection = Vector2.Zero;
    private Vector2 tempLookDirection = Vector2.Zero;

    // Bounding box related stuff here
    protected IShapeF bounds;
    protected float characterWidth;
    protected float characterHeight;
    protected float updateWidth;
    protected float updateHeight;

    private CharacterControlType controlType;

    public Character(Vector2 position)
    {
        this.position = position;
        state = new CharacterGameState();
        bounds = new RectangleF();
    }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        if (collisionInfo.Other is CollidableTiles tile)
        {
            switch (tile.TileCategory)
            {
                case "impenetrable":
                    position -= collisionInfo.PenetrationVector;
                    break;
                case "unwalkable":
                    if (this is not Bat) {
                        position -= collisionInfo.PenetrationVector;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Called when the character is hit by a weapon or projectile
    /// </summary>
    /// <param name="hitDirection">Direction from the character to the hit object</param>
    protected virtual void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
    }

    public virtual void Update(GameTime gameTime)
    {
        // MOVEMENT
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        if (tempMoveDirection.LengthSquared() > 0.0f)
        {
            tempMoveDirection.Normalize();
            moveDirection = tempMoveDirection;
        }

        float moveSpeed = State.HasFlag(ECharacterGameState.Dash) ? dashSpeed : speed;
        position += tempMoveDirection * moveSpeed * (float)dt;

        tempMoveDirection = Vector2.Zero;

        // LOOKING
        if (tempLookDirection.LengthSquared() > 0.0f)
        {
            tempLookDirection.Normalize();
            lookDirection = tempLookDirection;
        }

        tempLookDirection = Vector2.Zero;

        // BOUNDS
        bounds.Position = new Point2(position.X - updateWidth, position.Y - updateHeight);
    }

    public void DebugDraw(SpriteBatch spriteBatch)
    {
        if (BazaarBountyGame.Settings.Debug.Mode)
            spriteBatch.DrawRectangle((RectangleF)bounds, new Color(Color.Red, 0.5f),
                layerDepth: BazaarBountyGame.Settings.Debug.Depth);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        DebugDraw(spriteBatch);
    }

    public virtual void Dash()
    {
        if (!dashReady) return;
        if (state.SwitchState(ECharacterGameState.Dash))
        {
            dashSound.Play();
            dashReady = false;
            Utils.Timer.Delay(dashTime, () => state.ClearState(ECharacterGameState.Dash));
            dashTimer = new Utils.Timer(dashCooldownTime + dashTime, () => dashReady = true);
            Utils.TimerPool.GetInstance().RegisterDelay(dashTimer);
        }
    }

    public virtual void Move(Vector2 dir)
    {
        tempMoveDirection += dir;
    }

    public virtual void Look(Vector2 dir)
    {
        tempLookDirection += dir;
    }

    public virtual void LoadContent()
    {
        dashSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>($"SoundEffects/Player/dash");
    }

    public virtual void MeleeAttack()
    {
        state.SwitchState(ECharacterGameState.MeleeAttack);
    }

    public virtual void FinishMeleeAttack()
    {
        state.ClearState(ECharacterGameState.MeleeAttack);
    }

    public virtual void RangedAttack()
    {
        state.SwitchState(ECharacterGameState.RangedAttack);
    }

    public virtual void FinishRangedAttack()
    {
        state.ClearState(ECharacterGameState.RangedAttack);
    }

    // Getter and Setter
    public Vector2 Position
    {
        get => position;
        set => position = value;
    }

    public Vector2 LookDirection
    {
        get => lookDirection;
        set => lookDirection = value;
    }

    public int MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public int CurrHealth
    {
        get => currHealth;
        set => currHealth = value;
    }

    public int MaxBullet
    {
        get => maxBullet;
        set => maxBullet = value;
    }

    public int CurrBullet
    {
        get => currBullet;
        set => currBullet = value;
    }

    public CharacterControlType ControlType
    {
        get => controlType;
        set => controlType = value;
    }
    public float Speed
    {
        get => speed;
        set => speed = value;
    }
    public bool AutoReflection
    {
        get => autoReflection;
        set => autoReflection = value;
    }
    public float DashSpeed
    {
        get => dashSpeed;
        set => dashSpeed = value;
    }
    public float DashCDTime
    {
        get => dashCooldownTime;
        set => dashCooldownTime = value;
    }
    public float DashDamage
    {
        get => dashDamage;
        set => dashDamage = value;
    }
    public float Melee_defense
    {
        get => melee_defense;
        set => melee_defense = value;
    }
    public float Ranged_defense
    {
        get => ranged_defense;
        set => ranged_defense = value;
    }

    public double DashTime => dashTime;
    public double DashElapsedTime => dashTimer?.timeElapsed ?? 0;
    public int Damage => damage;
    public CharacterType Type => type;
    public IShapeF Bounds => bounds;
    public CharacterGameState CharacterGameState => state;
    public ECharacterGameState State => state.State;
}

public enum CharacterType
{
    Player,
    Melee,
    Slime,
    Ranged,
    Flying,
    Boss
}

public enum CharacterControlType
{
    Controller,
    MnK,
    AI
}