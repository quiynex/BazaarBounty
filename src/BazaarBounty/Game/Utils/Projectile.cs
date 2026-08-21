using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;
using System.Collections.Generic;

namespace BazaarBounty
{
    public class ProjectilePool
    {
        private static ProjectilePool instance;
        private ProjectilePool() { }
        public static ProjectilePool GetInstance()
        {
            if (instance == null)
            {
                instance = new ProjectilePool();
            }
            return instance;
        }

        private List<Projectile> projectiles = new();

        public void RegisterProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
            // add to collision component
            BazaarBountyGame.GetGameInstance().CollisionComp.Insert(projectile);
        }

        public void Update(GameTime gameTime)
        {
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            // remove from collision component
            foreach (Projectile projectile in projectiles)
            {
                if (projectile.Destroyed)
                    BazaarBountyGame.GetGameInstance().CollisionComp.Remove(projectile);
            }
            // remove all done delays
            projectiles.RemoveAll(projectile => projectile.Destroyed);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
        }
    }

    public class Projectile : ICollisionActor
    {
        protected Vector2 position;
        protected Vector2 initPosition;
        protected TextureAnimation projectileAnimation;
        protected float speed;
        protected float rangeLimit;
        protected Vector2 moveDirection = Vector2.Zero;
        protected int damage;
        protected Character owner;

        protected bool destroyed;
        protected float scaleFactor;


        protected float projWidth;
        protected float projHeight;
        protected float updateWidth;
        protected float updateHeight;

        protected IShapeF bounds;

        protected bool isParried = false;

        public Vector2 Position => position;
        public IShapeF Bounds => bounds;
        public bool IsParried => isParried;

        public Projectile(Character owner, string animationName, Vector2 initPosition, Vector2 moveDir, float speed = 1000f, float range=0, int damage = 3)
        {
            this.owner = owner;
            scaleFactor = 1.0f;
            projectileAnimation = new TextureAnimation(animationName, 0.1f, 4, true, new Vector2(scaleFactor, scaleFactor), false);
            position = initPosition; // just 4 test
            this.speed = speed;
            this.damage = damage;
            moveDirection = moveDir;
            rangeLimit = range;
            if (moveDirection != Vector2.Zero)
                moveDirection.Normalize();

            projWidth = 8 * scaleFactor;
            projHeight = 8 * scaleFactor;

            // hard coded parameters, need to understand coordinate systems to fix this
            updateWidth = projWidth  / 2; /// scaleFactor; /// 2 / scaleFactor;
            updateHeight = projHeight  / 2; /// scaleFactor; //- 12 * scaleFactor;// * scaleFactor;

            bounds = new RectangleF(position.X - projWidth / 2, position.Y - projHeight / 2, projWidth, projHeight);
            // bounds = new RectangleF(position.X, position.Y, projWidth, projHeight);
            // bounds = new RectangleF(0, 0, 0, 0);

            LoadContent();
            ProjectilePool.GetInstance().RegisterProjectile(this);
        }

        public int Damage => damage;

        public Character Owner
        {
            get => owner;
            set => owner = value;
        }
        public Vector2 MoveDirection
        {
            get => moveDirection;
            set => moveDirection = value;
        }

        public bool Destroyed => destroyed;

        public virtual void LoadContent()
        {
            BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
            projectileAnimation.LoadContent();
        }
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            // TODO
            projectileAnimation.Draw(spriteBatch, position, 0f);
            // projectileAnimation.Draw(spriteBatch, position, MathHelper.ToDegrees((float)Math.Atan2(moveDirection.Y, moveDirection.X)));
            // spriteBatch.DrawRectangle((RectangleF)bounds, Color.Red);
        }
        public virtual void Update(GameTime gameTime)
        {
            // MOVEMENT
            double dt = gameTime.ElapsedGameTime.TotalSeconds;
            position += moveDirection * speed * (float)dt;

            var oldPosition = position;

            Vector2 movedRange = initPosition - position;
            if(rangeLimit > 0 &&
                movedRange.LengthSquared() >= rangeLimit * rangeLimit)
            {
                destroyed = true;
            }

            // BOUNDS
            bounds.Position = new Point2(position.X - updateWidth, position.Y - updateHeight);
        }

        public void OnCollision(CollisionEventArgs collisionInfo)
        {
            if ((collisionInfo.Other.Equals(BazaarBountyGame.player) && owner != BazaarBountyGame.player) ||
                (collisionInfo.Other is EnemyCharacter && owner == BazaarBountyGame.player))
            {
                BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.ProjectileHit, 0.3f);
                destroyed = true;
            }
            if (collisionInfo.Other is CollidableTiles && ((CollidableTiles)collisionInfo.Other).TileCategory == "impenetrable")
            {
                BazaarBountyGame.particleEffectManager.AddParticleEffect(this, EffectName.ProjectileHit, 0.3f);
                destroyed = true;
            }
            // player parry
            if ((collisionInfo.Other is MeleeWeapon.WeaponHitBox hitBox) && (hitBox.Owner is PlayerCharacter) && 
                hitBox.Weapon.DamageState && !(owner is PlayerCharacter)) {
                owner = hitBox.Owner;
                // moveDirection = moveDirection*-1;
                moveDirection = owner.LookDirection;
                isParried = true;
            }
        }
    }
}