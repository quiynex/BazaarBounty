using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BazaarBounty
{
    public class PlayerCharacter : Character
    {
        TextureAnimation idleAnimationBr;
        TextureAnimation idleAnimationDown;
        TextureAnimation idleAnimationTop;
        TextureAnimation idleAnimationTr;

        TextureAnimation walkAnimationBr;
        TextureAnimation walkAnimationDown;
        TextureAnimation walkAnimationTop;
        TextureAnimation walkAnimationTr;

        
        List<SoundEffect> hurtSounds = new();
        List<SoundEffect> deathSounds = new();

        IndicatorArrow indicatorArrow = new();

        public List<Buff> TemporaryBuffs { get; } = new();

        protected Weapon primaryWeapon;
        protected Weapon secondaryWeapon;

        public Weapon PrimaryWeapon => primaryWeapon;

        public Weapon SecondaryWeapon
        {
            get => secondaryWeapon;
            set => secondaryWeapon = value;
        }

        protected int specialWeaponCount = 0;

        public int SpecialWeaponCount
        {
            get => specialWeaponCount;
            set => specialWeaponCount = value;
        }

        private class IndicatorArrow
        {
            private Texture2D texture;
            private readonly float radius = 96;

            public void LoadContent()
            {
                BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
                texture = myGame.Content.Load<Texture2D>("UI/arrow");
            }

            public void Draw(SpriteBatch spriteBatch, Vector2 origin, Vector2 direction)
            {
                float angle = (float)Math.Atan2(direction.Y, direction.X);
                Vector2 position = origin + direction * radius;
                Vector2 textureOrigin = new Vector2((float)texture.Width / 2, (float)texture.Height / 2);
                spriteBatch.Draw(texture, position, null, Color.White, angle, textureOrigin, 1.0f, SpriteEffects.None, 0.75f);
            }
        }

        public PlayerCharacter(Vector2 position) : base(position)
        {
            type = CharacterType.Player;
            ResetProperty();
            var textureScale = Vector2.One * 1.5f;
            characterWidth = 17 * 1;
            characterHeight = 22 * 1;
            updateWidth = characterWidth / 2 + 8;
            updateHeight = characterHeight / 2 + 10;

            idleAnimationBr = new TextureAnimation("Characters/Player/idle_anim", 0.1f, 8, true, textureScale);
            idleAnimationDown = new TextureAnimation("Characters/Player/idle_backward_anim", 0.1f, 8, true, textureScale);
            idleAnimationTop = new TextureAnimation("Characters/Player/idle_forward_anim", 0.1f, 8, true, textureScale);
            idleAnimationTr = new TextureAnimation("Characters/Player/idle_anim", 0.1f, 8, true, textureScale);

            walkAnimationBr = new TextureAnimation("Characters/Player/walk_anim", 0.1f, 10, true, textureScale);
            walkAnimationDown = new TextureAnimation("Characters/Player/walk_backward_anim", 0.1f, 10, true, textureScale);
            walkAnimationTop = new TextureAnimation("Characters/Player/walk_forward_anim", 0.1f, 10, true, textureScale);
            walkAnimationTr = new TextureAnimation("Characters/Player/walk_anim", 0.1f, 10, true, textureScale);

            primaryWeapon = new MeleeWeapon(this); // set owner
            secondaryWeapon = new RangedWeapon(this); // set owner
        }

        public void ResetProperty()
        {
            speed = 200f;
            dashReady = true;
            dashSpeed = 600f;
            dashTime = 0.2f;
            dashCooldownTime = 1.0f;
            dashDamage = 0f;
            maxHealth = 100;
            currHealth = maxHealth;
            maxBullet = 20;
            currBullet = maxBullet;
            autoReflection = false;
            melee_defense = 1.0f;
            ranged_defense = 1.0f;
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

            primaryWeapon.Update(gameTime);
            secondaryWeapon.Update(gameTime);

            // remove temporary buffs that have expired
            TemporaryBuffs.RemoveAll(buff => buff.IsExpired);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            indicatorArrow.LoadContent();

            idleAnimationBr.LoadContent();
            idleAnimationDown.LoadContent();
            idleAnimationTop.LoadContent();
            idleAnimationTr.LoadContent();

            walkAnimationBr.LoadContent();
            walkAnimationDown.LoadContent();
            walkAnimationTop.LoadContent();
            walkAnimationTr.LoadContent();

            for(int i=1;i<=8;i++)
                hurtSounds.Add(BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>($"SoundEffects/Player/player_hurt_{i}"));
            
            for(int i=1;i<=3;i++)
                deathSounds.Add(BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>($"SoundEffects/Player/player_death_{i}"));

            primaryWeapon.LoadContent();
            secondaryWeapon.LoadContent();

            Vector2 size = idleAnimationBr.Size;
            Vector2 margin = new Vector2(40, 10);
            characterWidth = size.X - margin.X;
            characterHeight = size.Y - margin.Y;
            updateHeight = characterHeight / 2;
            updateWidth = characterWidth / 2;
            bounds = new RectangleF(position.X - characterWidth / 2, position.Y - characterHeight / 2,
                                    characterWidth, characterHeight);
        }

        private TextureAnimation GetAnimationFramesForDirection(ECharacterGameState gameState, string directionSuffix)
        {
            if (State.HasFlag(ECharacterGameState.Walk))
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
            indicatorArrow.Draw(spriteBatch, position, lookDirection);
            base.Draw(spriteBatch);

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

            Color color = state.IsHit ? Color.Red : Color.White;
            currentAnimationFrames.Draw(spriteBatch, position, 0, spriteEffect, color: color);
            primaryWeapon.Draw(spriteBatch);
            secondaryWeapon.Draw(spriteBatch);
        }

        public override void MeleeAttack()
        {
            if (state.SwitchState(ECharacterGameState.MeleeAttack))
            {
                primaryWeapon.Attack();
            }
        }

        public override void RangedAttack()
        {
            if (state.SwitchState(ECharacterGameState.RangedAttack))
            {
                secondaryWeapon.Attack();
                FinishRangedAttack();
            }
        }

        public override void OnCollision(CollisionEventArgs collisionInfo)
        {
            base.OnCollision(collisionInfo);
            if (collisionInfo.Other is MeleeEnemy enemy && enemy.DamageState && !state.IsHit)
            {
                if (enemy is Rat) return;   // Rat is supposed to attack with weapon

                // immune to damage during dash
                if(State.HasFlag(ECharacterGameState.Dash)){
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, 0, new HslColor(0f, 0f, 0.7f));
                    return;
                }
                BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.PlayerHit, 0.5f);

                if(dashDamage > 0){ // Peach reduce melee damage (TODO: Change this if statement condition to a more robust condition)
                    int receivedDamage = (int)(enemy.Damage*0.7 / melee_defense);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.5f, 0.5f));
                    currHealth -=  receivedDamage;
                }
                else{
                    int receivedDamage = (int)(enemy.Damage / melee_defense);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.5f, 0.5f));
                    currHealth -= receivedDamage;
                }
                OnHit(collisionInfo.PenetrationVector);
            }
            else if (collisionInfo.Other is Projectile && ((Projectile)collisionInfo.Other).Owner != this)
            {
                if(State.HasFlag(ECharacterGameState.Dash)){
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, 0, new HslColor(0f, 0f, 0.7f));
                    return;
                }
                if (autoReflection){
                    ((Projectile)collisionInfo.Other).Owner = this;
                    ((Projectile)collisionInfo.Other).MoveDirection *= -1;
                    int receivedDamage = (int)(((Projectile)collisionInfo.Other).Damage / ranged_defense);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.75f, 0.5f));
                    currHealth -= receivedDamage;
                    OnHit(collisionInfo.PenetrationVector);
                }
                else{
                    int receivedDamage = (int)(((Projectile)collisionInfo.Other).Damage / ranged_defense);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.5f, 0.5f));
                    currHealth -= receivedDamage;
                    OnHit(collisionInfo.PenetrationVector);
                }
            }
            else if (collisionInfo.Other is MeleeWeapon.WeaponHitBox hitBox && hitBox.Weapon.DamageState && !(hitBox.Owner is PlayerCharacter) && !state.IsHit)
            {
                BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.PlayerHit, 0.5f);

                if(State.HasFlag(ECharacterGameState.Dash)){
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, 0, new HslColor(0f, 1f, 0.5f));
                    return;
                }
                
                if(dashDamage > 0){ // Peach reduce melee damage (TODO: Change this if statement condition to a more robust condition)
                    int receivedDamage = (int)(hitBox.Weapon.Damage * 0.7 / melee_defense);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.5f, 0.5f));
                    currHealth -=  receivedDamage;
                }
                else{
                    int receivedDamage = (int)(hitBox.Weapon.Damage / melee_defense);
                    Console.WriteLine(receivedDamage);
                    BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.TextHit, 2f, receivedDamage, new HslColor(0f, 0.5f, 0.5f));
                    currHealth -= receivedDamage;
                }
                OnHit(collisionInfo.PenetrationVector);
            }

            // Check for player entering the door
            if (collisionInfo.Other is CollidableTiles tile)
            {
                if (tile.TileType == "Door" && BazaarBountyGame.levelManager.IsLevelClear())
                {
                    // Console.WriteLine("Player entered the door! Loading next level...");
                    // Cannot load next level here, because cannot modify the enumerator while iterating
                    BazaarBountyGame.levelManager.LoadNextFlag = true;
                }
            }
        }

        protected override void OnHit(Vector2 hitDirection, bool isParryStunned = false)
        {
            Random rnd = new Random();
            if(currHealth > 0)
                hurtSounds[rnd.Next(0, 8)].Play();
            else
                deathSounds[rnd.Next(0, 3)].Play();
            state.SwitchState(ECharacterGameState.OnHit);
            Utils.Timer.Delay(0.5f, () => state.ClearState(ECharacterGameState.OnHit));
        }

        public void PlayOnHitEffect(){
            state.SwitchState(ECharacterGameState.OnHit);
            Utils.Timer.Delay(0.5f, () => state.ClearState(ECharacterGameState.OnHit));
        }
    }
}