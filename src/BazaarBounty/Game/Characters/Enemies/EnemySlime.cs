using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;
using System;

namespace BazaarBounty;

public class Slime : MeleeEnemy
{
    // Slime has multiple animation directions, leave parent class animations empty
    protected TextureAnimation movingAnimationLr;
    protected TextureAnimation movingAnimationUp;
    protected TextureAnimation movingAnimationDn;

    protected TextureAnimation deadAnimationLr;
    protected TextureAnimation deadAnimationUp;
    protected TextureAnimation deadAnimationDn;

    protected TransformationAnimation attackAnimationLe;
    protected TransformationAnimation attackAnimationRi;
    protected TransformationAnimation attackAnimationTp;
    protected TransformationAnimation attackAnimationBt;

    protected Transform2 transform;
    protected double attackGapTime;
    protected double lastAttackTime;
    protected double minAttackRange;

    public double AttackGapTime => attackGapTime;
    public double LastAttackTime => lastAttackTime;

    public Slime(Vector2 position) : base(position)
    {
        type = CharacterType.Slime;
        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Slime.Green;
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
        var scale = new Vector2(1.5f,1.5f);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Slime/slime_move_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Slime/slime_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Slime/slime_move_front", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Slime/slime_death_front", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Slime/slime_move_back", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Slime/slime_death_back", 0.2f, 4, false, scale);
        
        Initialize();
    }

    protected void Initialize()
    {
        attackAnimationLe = new TransformationAnimation("dash_attack_le")
            .AddKeyframe(0.0f, new Vector2( 0,0), 0, Vector2.One)
            .AddKeyframe(0.2f, new Vector2(-attackRange,0), 0, Vector2.One)
            .AddKeyframe(0.4f, new Vector2( 0,0), 0, Vector2.One);
        attackAnimationRi = new TransformationAnimation("dash_attack_ri")
            .AddKeyframe(0.0f, new Vector2( 0,0), 0, Vector2.One)
            .AddKeyframe(0.2f, new Vector2( attackRange,0), 0, Vector2.One)
            .AddKeyframe(0.4f, new Vector2( 0,0), 0, Vector2.One);
        attackAnimationTp = new TransformationAnimation("dash_attack_tp")
            .AddKeyframe(0.0f, new Vector2(0, 0), 0, Vector2.One)
            .AddKeyframe(0.2f, new Vector2(0, -attackRange), 0, Vector2.One)
            .AddKeyframe(0.4f, new Vector2(0, 0), 0, Vector2.One);
        attackAnimationBt = new TransformationAnimation("dash_attack_bt")
            .AddKeyframe(0.0f, new Vector2(0, 0), 0, Vector2.One)
            .AddKeyframe(0.2f, new Vector2(0, attackRange), 0, Vector2.One)
            .AddKeyframe(0.4f, new Vector2(0, 0), 0, Vector2.One);

        attackAnimationBt.OnAnimationFinished = FinishMeleeAttack;
        attackAnimationLe.OnAnimationFinished = FinishMeleeAttack;
        attackAnimationRi.OnAnimationFinished = FinishMeleeAttack;
        attackAnimationTp.OnAnimationFinished = FinishMeleeAttack;

        deadAnimationDn.OnAnimationFinished = () => { state.SwitchState(ECharacterGameState.Destroyed); };
        deadAnimationLr.OnAnimationFinished = () => { state.SwitchState(ECharacterGameState.Destroyed); };
        deadAnimationUp.OnAnimationFinished = () => { state.SwitchState(ECharacterGameState.Destroyed); };
    }

    public override void LoadContent()
    {
        // load the audio for the slime
        hurtSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/slime_onhit");
        deathSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/slime_death");
        
        base.LoadContent();
        // idleAnimation.LoadContent();
        movingAnimationLr.LoadContent();
        movingAnimationDn.LoadContent();
        movingAnimationUp.LoadContent();
        deadAnimationLr.LoadContent();
        deadAnimationDn.LoadContent();
        deadAnimationUp.LoadContent();

        // set the bounds of the slime
        Vector2 textureSize = movingAnimationLr.Size;
        characterWidth = textureSize.X;
        characterHeight = textureSize.Y;
        updateWidth = characterWidth / 2;
        updateHeight = characterHeight / 2;
        bounds = new RectangleF(position.X - updateWidth, position.Y - updateHeight, characterWidth, characterHeight);
    }
        
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        // idleAnimation.Update(gameTime);
        movingAnimationLr.Update(gameTime);
        movingAnimationDn.Update(gameTime);
        movingAnimationUp.Update(gameTime);
        deadAnimationDn.Update(gameTime);
        deadAnimationLr.Update(gameTime);
        deadAnimationUp.Update(gameTime);

        attackAnimationLe.Update(gameTime);
        attackAnimationRi.Update(gameTime);
        attackAnimationTp.Update(gameTime);
        attackAnimationBt.Update(gameTime);

        if (State.HasFlag(ECharacterGameState.MeleeAttack))
        {
            if (attackAnimationTp.isPlaying) {
                transform = attackAnimationTp.CurrentLerpTransform;
            } else if (attackAnimationBt.isPlaying) {
                transform = attackAnimationBt.CurrentLerpTransform;
            } else if (attackAnimationLe.isPlaying) {
                transform = attackAnimationLe.CurrentLerpTransform;
            } else {
                transform = attackAnimationRi.CurrentLerpTransform;
            }

            bounds = new RectangleF(position.X - updateWidth + transform.Position.X, position.Y - updateHeight + transform.Position.Y, 
                characterWidth, characterHeight);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DebugDraw(spriteBatch);
        SpriteEffects spriteEffect = SpriteEffects.None;
        if(lookDirection.X < 0) {
            spriteEffect = SpriteEffects.FlipHorizontally;
        }

        Vector2 drawPosition = position;
        Color color = state.IsHit ? Color.Red : Color.White;
        

        if (State.HasFlag(ECharacterGameState.Dead))
        {
            if (deadAnimationUp.isPlaying)
                deadAnimationUp.Draw(spriteBatch, drawPosition, 0f, color: color);
            else if (deadAnimationDn.isPlaying)
                deadAnimationDn.Draw(spriteBatch, drawPosition, 0f, color: color);
            else
                deadAnimationLr.Draw(spriteBatch, drawPosition, 0f, spriteEffect, color: color);
        } 
        else
        {
            if (State.HasFlag(ECharacterGameState.Stunned))
                stunIndicatorAnimation.Draw(spriteBatch, position - new Vector2(0.0f, 40.0f), 0.0f);

            if (State.HasFlag(ECharacterGameState.MeleeAttack))
            {

                Vector2 realPosition = drawPosition + transform.Position;
                if (attackAnimationTp.isPlaying)
                    movingAnimationUp.Draw(spriteBatch, realPosition, 0f, color: color);
                else if (attackAnimationBt.isPlaying)
                    movingAnimationDn.Draw(spriteBatch, realPosition, 0f, color: color);
                else if (attackAnimationLe.isPlaying)
                    movingAnimationLr.Draw(spriteBatch, realPosition, 0f, SpriteEffects.FlipHorizontally, color: color);
                else
                    movingAnimationLr.Draw(spriteBatch, realPosition, 0f, color: color);
            }
            else
            {
                // other state using idle animations (same as moving animations here)
                if (lookDirection.Y < 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
                    movingAnimationUp.Draw(spriteBatch, drawPosition, 0f, spriteEffect, color: color);
                else if (lookDirection.Y > 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
                    movingAnimationDn.Draw(spriteBatch, drawPosition, 0f, spriteEffect, color: color);
                else
                    movingAnimationLr.Draw(spriteBatch, drawPosition, 0f, spriteEffect, color: color);
            }
        }
    }

    public override void MeleeAttack()
    {
        if (!state.SwitchState(ECharacterGameState.MeleeAttack))
            return;
        if (lookDirection.Y < 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
            attackAnimationTp.Play();
        else if (lookDirection.Y > 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
            attackAnimationBt.Play();
        else if (lookDirection.X < 0)
            attackAnimationLe.Play();
        else
            attackAnimationRi.Play();
    }
        
    public override void FinishMeleeAttack()
    {
        if (state.ClearState(ECharacterGameState.MeleeAttack))
            lastAttackTime = BazaarBountyGame.GetGameTime().TotalGameTime.TotalSeconds;
    }

    /// <summary>
    /// Check whether slime can attack player
    /// Slime has a minimum attack range to avoid slime move too close to player
    /// </summary>
    public override bool CheckInAttackRange()
    {
        Vector2 dir = BazaarBountyGame.player.Position - position;
        float dist = dir.LengthSquared();
        return dist <= attackRange * attackRange && dist > minAttackRange * minAttackRange;
    }

    protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
    {
        Trigger();
        if (!state.SwitchState(ECharacterGameState.OnHit))
            return;
        hitBackDirection = -hitDirection;
        hitBackDirection.Normalize();
        hitBackAnimation.Play();
        generalHurtSound.Play();
        if (currHealth <= 0)
        {
            deathSound.Play();
            state.SwitchState(ECharacterGameState.Dead);
            Animation currentDeathAnimation;
            if (lookDirection.Y < 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
                currentDeathAnimation = deadAnimationUp;
            else if (lookDirection.Y > 0 && Math.Abs(lookDirection.Y) > Math.Abs(lookDirection.X))
                currentDeathAnimation = deadAnimationDn;
            else
                currentDeathAnimation = deadAnimationLr;
            currentDeathAnimation.Play();
        }
        else
        {
            hurtSound.Play();
            movingAnimationLr.Stop();
            movingAnimationDn.Stop();
            movingAnimationUp.Stop();

            if (isParryStunned && state.SwitchState(ECharacterGameState.Stunned))
            {
                Utils.Timer.Delay(stunTime + 2.0f, () =>
                {
                    state.ClearState(ECharacterGameState.Stunned);
                    movingAnimationLr.Continue();
                    movingAnimationDn.Continue();
                    movingAnimationUp.Continue();
                });
            }

            Utils.Timer.Delay(stunTime, () =>
            {
                state.ClearState(ECharacterGameState.OnHit);
                if (!state.IsStunned)
                {
                    movingAnimationLr.Continue();
                    movingAnimationDn.Continue();
                    movingAnimationUp.Continue();
                }
            });
        }
    }
}

public class BlueSlime : Slime
{

    public BlueSlime(Vector2 position) : base(position)
    {
        type = CharacterType.Slime;

        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Slime.Blue;
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
        var scale = new Vector2(1.5f,1.5f);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Slime/blue_slime_move_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Slime/blue_slime_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Slime/blue_slime_move_front", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Slime/blue_slime_death_front", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Slime/blue_slime_move_back", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Slime/blue_slime_death_back", 0.2f, 4, false, scale);

        Initialize();
    }
}

public class RedSlime : Slime
{
    public RedSlime(Vector2 position) : base(position)
    {
        type = CharacterType.Slime;
        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Slime.Red;
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
        var scale = new Vector2(1.5f,1.5f);
        // idleAnimation = new TextureAnimation("Characters/Enemies/Slime/SmallSlime_Green_0", 0.2f, 4, true, scale);
        movingAnimationLr = new TextureAnimation("Characters/Enemies/Slime/red_slime_move_right", 0.2f, 4, true, scale);
        deadAnimationLr   = new TextureAnimation("Characters/Enemies/Slime/red_slime_death_right", 0.2f, 4, false, scale);
        movingAnimationDn = new TextureAnimation("Characters/Enemies/Slime/red_slime_move_front", 0.2f, 4, true, scale);
        deadAnimationDn   = new TextureAnimation("Characters/Enemies/Slime/red_slime_death_front", 0.2f, 4, false, scale);
        movingAnimationUp = new TextureAnimation("Characters/Enemies/Slime/red_slime_move_back", 0.2f, 4, true, scale);
        deadAnimationUp   = new TextureAnimation("Characters/Enemies/Slime/red_slime_death_back", 0.2f, 4, false, scale);

        Initialize();
    }
}