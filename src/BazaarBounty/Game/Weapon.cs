using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using Microsoft.Xna.Framework.Audio;

namespace BazaarBounty;

public class Weapon
{
    // sound effect
    protected SoundEffect weaponSound;
    protected Texture2D weaponTexture;
    protected int damage;
    protected Character owner;
    public Character Owner => owner;

    public Weapon(Character owner)
    {
        this.owner = owner;
    }

    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    public virtual void LoadContent()
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
    //     if (BazaarBountyGame.Settings.Debug.Mode && bounds is not null && bounds is RectangleF)
    //         spriteBatch.DrawRectangle((RectangleF)bounds, Color.Red, layerDepth: BazaarBountyGame.Settings.Debug.Depth);
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Attack()
    {
    }

    // public virtual void OnCollision(CollisionEventArgs collisionInfo)
    // {
    // }

    public bool GetIsAttacking()
    {
        return owner.CharacterGameState.IsAttacking;
    }
}

public class MeleeWeapon : Weapon
{
    protected TextureAnimation weaponAttackAnimation;
    protected TransformationAnimation weaponAnimationLeft;
    protected TransformationAnimation weaponAnimationRight;
    protected TransformationAnimation weaponAnimationThrust;

    // The handle and the hand are assumed to be in the same position (attached)
    protected Vector2 handOffset; // attached position offset to the character
    protected Vector2 weaponOrigin; // position of the center of the weapon
    protected float tipOffset; // position of the tip of the weapon (assume along the center axis)
    protected float hiltOffset; // position of the hilt of the weapon (assume along the center axis)
    protected float weaponWidth; // width of the weapon along the x-axis
    protected bool damageState;
    public bool DamageState => damageState;

    public float ScaleFactor { get; set; }
    public float AtkCD { get; set; }

    protected WeaponHitBox hitBox1;  // on the tip of the weapon
    protected WeaponHitBox hitBox2;  // on the body of the weapon
    public WeaponHitBox HitBox1 => hitBox1;
    public WeaponHitBox HitBox2 => hitBox2;
    
    public void RegisterHitBox(CollisionComponent collisionComponent)
    {
        collisionComponent.Insert(hitBox1);
        collisionComponent.Insert(hitBox2);
    }

    public class WeaponHitBox : ICollisionActor
    {
        private float width;
        public float Width
        {
            get => width;
            set
            {
                width = value;
                Bounds = new RectangleF(Bounds.Position.X, Bounds.Position.Y, value, value);
            }
        }

        public IShapeF Bounds { get; private set; }
        public Character Owner { get; }
        public MeleeWeapon Weapon { get; }

        public WeaponHitBox(MeleeWeapon weapon, Character owner, Vector2 position, float W)
        {
            Weapon = weapon;
            Owner = owner;
            width = W;
            Bounds = new RectangleF(position.X, position.Y, width, width);
        }
        public void OnCollision(CollisionEventArgs collisionInfo)
        {
        }

        public void UpdateCenterPosition(Vector2 center)
        {
            Bounds.Position = center - new Vector2(Width / 2, Width / 2);
        }

        public void DebugDraw(SpriteBatch spriteBatch)
        {
            if (BazaarBountyGame.Settings.Debug.Mode)
                spriteBatch.DrawRectangle((RectangleF)Bounds, Color.Red, layerDepth:BazaarBountyGame.Settings.Debug.Depth);
        }
    }

    public MeleeWeapon(Character owner) : base(owner)
    {
        InitProperty();
        hitBox1 = new WeaponHitBox(this, owner, Vector2.Zero, 10 * ScaleFactor);
        hitBox2 = new WeaponHitBox(this, owner, Vector2.Zero, 10 * ScaleFactor);
        InitMeleeWeapon();
    }

    protected virtual void InitProperty()
    {
        damageState = false;
        damage = 3;
        AtkCD = 0.5f;
        ScaleFactor = 2.0f;
        // relative positions in the texture coordinates
        handOffset = new Vector2(8, 0);
        weaponOrigin = new Vector2(0, 20);
        weaponWidth = 32;
        tipOffset = 18;
        hiltOffset = 6;
    }

    public void InitMeleeWeapon()
    {
        // weapon animation related
        Vector2 scale = Vector2.One * ScaleFactor;
        weaponAnimationRight = new TransformationAnimation("sword_swing_right")
            .AddKeyframe(0.05f, Vector2.Zero, -45, Vector2.One)
            .AddKeyframe(0.1f, Vector2.Zero, 60, Vector2.One)
            .AddKeyframe(AtkCD, Vector2.Zero, 0, Vector2.One);
        weaponAnimationLeft = new TransformationAnimation("sword_swing_left")
            .AddKeyframe(0.05f, Vector2.Zero, 45, Vector2.One)
            .AddKeyframe(0.1f, Vector2.Zero, -60, Vector2.One)
            .AddKeyframe(AtkCD, Vector2.Zero, 0, Vector2.One);
        weaponAttackAnimation = new TextureAnimation("WeaponsAndProjectiles/sword_slash", 0.05f, 4, false, scale);
        weaponAnimationLeft.OnAnimationFinished += owner.FinishMeleeAttack;
        weaponAnimationRight.OnAnimationFinished += owner.FinishMeleeAttack;
        hitBox1.Width = 10 * ScaleFactor;
        hitBox2.Width = 10 * ScaleFactor;
    }

    public override void LoadContent()
    {
        weaponAttackAnimation.LoadContent();
        weaponAnimationLeft.LoadContent();
        weaponAnimationRight.LoadContent();
        weaponAnimationLeft.OnAnimationFinished();
        weaponAnimationRight.OnAnimationFinished();
        weaponSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/sword_slash");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        // Get draw depth
        float weaponDepth = owner.LookDirection.Y < 0 ? 0.25f : 0.75f;
        // float weaponDepth = 0.9f;
        // Get position and rotation of the weapon
        Vector2 weaponPosition = GetWeaponPosition(); // top left corner of the weapon texture
        float weaponRotation = GetWeaponRotation();

        Transform2 updatedTransform = owner.LookDirection.X > 0
            ? weaponAnimationRight.CurrentLerpTransform
            : weaponAnimationLeft.CurrentLerpTransform;

        SpriteEffects spriteEffect = SpriteEffects.FlipVertically;
        if (owner.LookDirection.X > 0)
            spriteEffect |= SpriteEffects.FlipHorizontally;
        weaponAttackAnimation.Draw(spriteBatch, weaponPosition, weaponRotation, weaponOrigin, updatedTransform,
            spriteEffect, layer: weaponDepth);
        
        hitBox1.DebugDraw(spriteBatch);
        hitBox2.DebugDraw(spriteBatch);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        weaponAttackAnimation.Update(gameTime);
        weaponAnimationLeft.Update(gameTime);
        weaponAnimationRight.Update(gameTime);
        Transform2 updatedTransform = owner.LookDirection.X > 0
            ? weaponAnimationRight.CurrentLerpTransform
            : weaponAnimationLeft.CurrentLerpTransform;
        // update the bounds position with the animation transform
        Vector2 position = GetWeaponPosition();

        float rotationX = GetWeaponRotation();
        rotationX += updatedTransform.Rotation;
        float rotationY = rotationX + (float)Math.PI / 2;
        Vector2 directionX = new Vector2((float)Math.Cos(rotationX), (float)Math.Sin(rotationX));
        Vector2 directionY = new Vector2((float)Math.Cos(rotationY), (float)Math.Sin(rotationY));
        Vector2 boundCenterPosition = position + directionX * weaponWidth * ScaleFactor / 2;
        Vector2 hitBoxCenter1 =  boundCenterPosition - directionY * (tipOffset * ScaleFactor - hitBox1.Width / 2);
        Vector2 hitBoxCenter2 =  boundCenterPosition - directionY * (tipOffset * ScaleFactor - hitBox2.Width / 2 - hitBox1.Width);
        // Vector2 hitBoxCenter1 =  boundCenterPosition - directionY * (tipOffset * ScaleFactor);
        // Vector2 hitBoxCenter2 =  boundCenterPosition - directionY * (tipOffset * ScaleFactor - hitBox1.Width);
        hitBox1.UpdateCenterPosition(hitBoxCenter1);
        hitBox2.UpdateCenterPosition(hitBoxCenter2);
        // bounds.Position = boundCenterPosition - new Vector2(boundWidth / 2, boundHeight / 2);
    }

    public override void Attack()
    {
        base.Attack();

        if (!(weaponAnimationLeft.isPlaying || weaponAnimationRight.isPlaying))
            weaponSound.Play();

        if (owner.LookDirection.X > 0)
            weaponAnimationRight.Play();
        else
            weaponAnimationLeft.Play();
        Utils.Timer.Delay(0.05f, () =>
        {
            damageState = true;
            weaponAttackAnimation.Play();
        });
        Utils.Timer.Delay(0.1f, () => damageState = false);
    }

    // Get the top-left corner position of the weapon texture on the screen for drawing
    public Vector2 GetWeaponPosition()
    {
        var scaledHandOffset = handOffset * ScaleFactor;
        var scaledHiltOffset = hiltOffset * ScaleFactor;
        var scaledWeaponWidth = weaponWidth * ScaleFactor;

        Vector2 weaponPosition = owner.LookDirection.X > 0f
            ? owner.Position + scaledHandOffset
            : owner.Position - scaledHandOffset;
        Transform2 updatedTransform = owner.LookDirection.X > 0
            ? weaponAnimationRight.CurrentLerpTransform
            : weaponAnimationLeft.CurrentLerpTransform;
        float rotationX = GetWeaponRotation() + updatedTransform.Rotation;
        float rotationY = rotationX + (float)Math.PI / 2;
        Vector2 directionY = new Vector2((float)Math.Cos(rotationY), (float)Math.Sin(rotationY));
        Vector2 directionX = new Vector2((float)Math.Cos(rotationX), (float)Math.Sin(rotationX));
        weaponPosition -= directionY * scaledHiltOffset;
        weaponPosition -= directionX * scaledWeaponWidth / 2;
        // weaponPosition += new Vector2(-7,0);
        // Adjust the position to ensure the weapon rotates around its end or handle rather than its center if needed
        // You might need to tweak this based on how you want the weapon to rotate around the character
        // Vector2 positionAdjustment = Vector2.Transform(new Vector2(weaponOrigin.X, 0), Matrix.CreateRotationZ(weaponRotation));
        // weaponPosition += positionAdjustment + new Vector2(-7, 0);
        return weaponPosition;
    }

    public float GetWeaponRotation()
    {
        return (float)Math.Atan2(owner.LookDirection.Y, owner.LookDirection.X) + (float)Math.PI / 2;
    }
}

public class EnemyMeleeWeapon : MeleeWeapon
{
    public EnemyMeleeWeapon(Character owner) : base(owner)
    {
    }

    protected override void InitProperty()
    {
        damage = 10;
        AtkCD = 0.7f;
        ScaleFactor = 1.8f;
        // relative positions in the texture coordinates
        handOffset = new Vector2(12, 0);
        weaponOrigin = new Vector2(0, 20);
        hiltOffset = 2;
        weaponWidth = 32;
        tipOffset = 18;
    }
}

public class RangedWeapon : Weapon
{
    private SoundEffect gunShotSound;
    private SoundEffect emptyGunShot;
    protected Vector2 weaponPosition;
    protected Vector2 weaponOrigin;
    protected float coolDownTime;
    protected bool coolDown;
    protected bool empty_sound_coolDown;
    protected float bulletVelocity;
    protected int fruit_effect_magnification;
    protected bool inf_bullet;

    public RangedWeapon(Character owner, int dmg = 3, int fruit_magnification = 1, bool inf_bullets = false) : base(owner)
    {
        damage = dmg;
        coolDownTime = 1f;
        coolDown = false;
        empty_sound_coolDown = false;
        bulletVelocity = 1000f;
        fruit_effect_magnification = fruit_magnification;
        inf_bullet = inf_bullets;
    }

    public float BulletVelocity
    {
        get { return bulletVelocity; }
        set { bulletVelocity = value; }
    }

    public float CoolDownTime
    {
        get { return coolDownTime; }
        set { coolDownTime = value; }
    }

    public int FruitEffectMagnification
    {
        get { return fruit_effect_magnification; }
        set { fruit_effect_magnification = value; }
    }

    public bool InfBullet
    {
        get { return inf_bullet; }
        set { inf_bullet = value; }
    }

    public override void LoadContent()
    {
        base.LoadContent();
        BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
        weaponTexture = myGame.Content.Load<Texture2D>("WeaponsAndProjectiles/flaregun");
        gunShotSound = myGame.Content.Load<SoundEffect>("SoundEffects/gun_shot");
        emptyGunShot = myGame.Content.Load<SoundEffect>("SoundEffects/empty_gun_shot");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        // Offset used to determine the hand distance from centerpoint 
        Vector2 handPixelOffset = new Vector2(10, 0) * 1.6f;

        // Get rotation angle
        float weaponRotation = (float)Math.Atan2(owner.LookDirection.Y, owner.LookDirection.X) - MathF.PI / 2;

        // Get weapon position based on look direction and character position
        weaponPosition = owner.LookDirection.X <= 0f
            ? owner.Position + handPixelOffset
            : owner.Position - handPixelOffset;
        weaponPosition += new Vector2(-7, 0);

        // Origin point of weapon (Ex: the hilt)
        weaponOrigin = new Vector2(7, 3); //Center point of weapon

        // Get draw depth
        // float weaponDepth = owner.LookDirection.Y < 0 ? 0.25f : 0.75f;
        float weaponDepth = 0.75f;

        // Draw the weapon with rotation
        SpriteEffects spriteEffect = owner.LookDirection is { Y: 1f, X: 0f } ? SpriteEffects.FlipHorizontally :
            owner.LookDirection.X >= 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        spriteBatch.Draw(weaponTexture, weaponPosition, null, Color.White, weaponRotation, weaponOrigin,
            new Vector2(2, 2), spriteEffect, weaponDepth);
    }

    public override void Attack()
    {
        base.Attack();
        //automatically registered
        if ((owner.CurrBullet > 0 || inf_bullet) && !coolDown)
        {
            gunShotSound.Play();
            // Vec2(10, 17) is the size of the current ranged weapon asset
            // new Projectile(owner, "WeaponsAndProjectiles/bullet_anim", weaponPosition + weaponOrigin, owner.LookDirection);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position, owner.LookDirection, damage: damage);
            if(!inf_bullet)
                owner.CurrBullet -= 1;
            coolDown = true;
            Utils.Timer.Delay(coolDownTime, () => { coolDown = false; });
        }
        else if(owner.CurrBullet <= 0 && !empty_sound_coolDown){
            emptyGunShot.Play();
            empty_sound_coolDown = true;
            Utils.Timer.Delay(1, () => { empty_sound_coolDown = false; });
        }
    }
}

public class PlayerShotgun : RangedWeapon
{
    private SoundEffect gunShotSound;
    private SoundEffect emptyGunShot;
    protected float bulletRange;

    public PlayerShotgun(Character owner, int dmg = 6, int magnification = 1, bool inf_bullets = false) : base(owner)
    {
        coolDownTime = 1f;
        coolDown = false;
        empty_sound_coolDown = false;
        bulletVelocity = 3000f;
        bulletRange = -1f; // bullet range disabled (buggy)
        damage = dmg;
        fruit_effect_magnification = magnification;
        inf_bullet = inf_bullets;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
        weaponTexture = myGame.Content.Load<Texture2D>("WeaponsAndProjectiles/flaregun");
        gunShotSound = myGame.Content.Load<SoundEffect>("SoundEffects/shotgun");
        emptyGunShot = myGame.Content.Load<SoundEffect>("SoundEffects/empty_gun_shot");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        // Offset used to determine the hand distance from centerpoint 
        Vector2 handPixelOffset = new Vector2(10, 0) * 1.6f;

        // Get rotation angle
        float weaponRotation = (float)Math.Atan2(owner.LookDirection.Y, owner.LookDirection.X) - MathF.PI / 2;

        // Get weapon position based on look direction and character position
        weaponPosition = owner.LookDirection.X <= 0f
            ? owner.Position + handPixelOffset
            : owner.Position - handPixelOffset;
        weaponPosition += new Vector2(-7, 0);

        // Origin point of weapon (Ex: the hilt)
        weaponOrigin = new Vector2(7, 3); //Center point of weapon

        // Get draw depth
        float weaponDepth = owner.LookDirection.Y < 0 ? 0.25f : 0.75f;

        // Draw the weapon with rotation
        SpriteEffects spriteEffect = owner.LookDirection is { Y: 1f, X: 0f } ? SpriteEffects.FlipHorizontally :
            owner.LookDirection.X >= 0f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        spriteBatch.Draw(weaponTexture, weaponPosition, null, Color.White, weaponRotation, weaponOrigin,
            new Vector2(2, 2), spriteEffect, weaponDepth);
    }

    public override void Attack()
    {
        //automatically registered
        if ((owner.CurrBullet > 0 || inf_bullet) && !coolDown)
        {
            gunShotSound.Play();
            // Vec2(10, 17) is the size of the current ranged weapon asset
            // new Projectile(owner, "WeaponsAndProjectiles/bullet_anim", weaponPosition + weaponOrigin, owner.LookDirection);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(-MathF.PI / 8)), bulletVelocity,
                bulletRange, damage);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(-MathF.PI / 16)), bulletVelocity,
                bulletRange, damage);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position, owner.LookDirection,
                bulletVelocity, bulletRange, damage);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(MathF.PI / 8)), bulletVelocity,
                bulletRange, damage);
            new Projectile(owner, "WeaponsAndProjectiles/black_bullet_anim", owner.Position,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(MathF.PI / 16)), bulletVelocity,
                bulletRange, damage);
            if(!inf_bullet)
                owner.CurrBullet -= 5;
            coolDown = true;
            Utils.Timer.Delay(coolDownTime, () => { coolDown = false; });
        }
        else if(owner.CurrBullet <= 0 && !empty_sound_coolDown){
            emptyGunShot.Play();
            empty_sound_coolDown = true;
            Utils.Timer.Delay(1, () => { empty_sound_coolDown = false; });
        }
    }
}

public class EnemyMagicWeapon : Weapon
{
    private SoundEffect magicSound;

    protected Vector2 weaponPosition;
    protected Vector2 weaponOrigin;
    protected float coolDownTime;
    protected bool coolDown;
    protected bool firstShot;
    protected float bulletVelocity;
    protected Vector2 WeaponPosition => owner.Position;

    public EnemyMagicWeapon(Character owner) : base(owner)
    {
        coolDownTime = 1.5f;
        coolDown = false;
        firstShot = true;
        bulletVelocity = 750f;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
        magicSound = myGame.Content.Load<SoundEffect>("SoundEffects/magic_fire");
    }

    public override void Attack()
    {
        base.Attack();
        if (!coolDown && !firstShot)
        {
            magicSound.Play();
            new Projectile(owner, "WeaponsAndProjectiles/purple_bullet_anim",
                WeaponPosition + weaponOrigin, owner.LookDirection, bulletVelocity);
            coolDown = true;
            Utils.Timer.Delay(coolDownTime, () => { coolDown = false; });
        }
        else if (firstShot)
        {
            Utils.Timer.Delay(coolDownTime, () => { firstShot = false; });
        }
    }
}


public class EnemyMagicTripleShotgun : Weapon
{
    SoundEffect magicSound;

    protected Vector2 weaponPosition;
    protected Vector2 weaponOrigin;
    protected float coolDownTime;
    protected bool coolDown;
    protected bool firstShot;
    protected float bulletVelocity;
    protected Vector2 WeaponPosition => owner.Position;

    public EnemyMagicTripleShotgun(Character owner) : base(owner)
    {
        coolDownTime = 1.5f;
        coolDown = false;
        firstShot = true;
        bulletVelocity = 400f;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
        magicSound = myGame.Content.Load<SoundEffect>("SoundEffects/magic_fire");
    }

    public override void Attack()
    {
        base.Attack();
        if (!coolDown && !firstShot)
        {
            new Projectile(owner, "WeaponsAndProjectiles/blue_bullet_anim",
                WeaponPosition + weaponOrigin, owner.LookDirection, bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/blue_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(MathF.PI / 6)), bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/blue_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(-MathF.PI / 6)), bulletVelocity);
            magicSound.Play();
            coolDown = true;
            Utils.Timer.Delay(coolDownTime, () => { coolDown = false; });
        }
        else if (firstShot)
        {
            Utils.Timer.Delay(coolDownTime, () => { firstShot = false; });
        }
    }
}

public class EnemyMagicQuintupleShotgun : Weapon
{
    SoundEffect magicSound;

    protected Vector2 weaponPosition;
    protected Vector2 weaponOrigin;
    protected float coolDownTime;
    protected bool coolDown;
    protected bool firstShot;
    protected float bulletVelocity;
    protected Vector2 WeaponPosition => owner.Position;

    public EnemyMagicQuintupleShotgun(Character owner) : base(owner)
    {
        coolDownTime = 1.5f;
        coolDown = false;
        firstShot = true;
        bulletVelocity = 400f;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        BazaarBountyGame myGame = BazaarBountyGame.GetGameInstance();
        magicSound = myGame.Content.Load<SoundEffect>("SoundEffects/magic_fire");
    }

    public override void Attack()
    {
        base.Attack();
        if (!coolDown && !firstShot)
        {
            new Projectile(owner, "WeaponsAndProjectiles/red_bullet_anim",
                WeaponPosition + weaponOrigin, owner.LookDirection, bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/red_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(MathF.PI / 6)), bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/red_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(-MathF.PI / 6)), bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/red_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(MathF.PI / 12)), bulletVelocity);
            new Projectile(owner, "WeaponsAndProjectiles/red_bullet_anim",
                WeaponPosition + weaponOrigin,
                Vector2.Transform(owner.LookDirection, Matrix.CreateRotationZ(-MathF.PI / 12)), bulletVelocity);
            magicSound.Play();
            coolDown = true;
            Utils.Timer.Delay(coolDownTime, () => { coolDown = false; });
        }
        else if (firstShot)
        {
            Utils.Timer.Delay(coolDownTime, () => { firstShot = false; });
        }
    }
}