using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;

namespace BazaarBounty;

public class Goblin : MeleeEnemy
{
    public override bool DamageState => damageState;

    public Goblin(Vector2 position) : base(position)
    {
        // set the properties of the goblin
        type = CharacterType.Melee;


        // Set properties from settings
        var setting = BazaarBountyGame.Settings.Enemy.Goblin.Green;
        speed = setting.Speed;
        maxHealth = setting.MaxHealth;
        currHealth = maxHealth;
        damage = setting.Damage;
        stunTime = setting.StunTime;
        // attackRange = setting.AttackRange;
        // attackGapTime = setting.AttackGapTime;
        // minAttackRange = setting.MinAttackRange;

        // set the bounds of the goblin
        characterWidth = 50;
        characterHeight = 58;
        updateWidth = characterWidth / 2;
        updateHeight = characterHeight / 2 - 12;
        bounds = new RectangleF(position.X - characterWidth / 2, position.Y - characterHeight / 2, characterWidth,
            characterHeight);

        Vector2 scale = new Vector2(0.8f, 0.8f);
        // set the animations of the goblin
        idleAnimation = new TextureAnimation("Characters/Enemies/Goblin/Idle", 0.2f, 4, true, scale);
        movingAnimation = new TextureAnimation("Characters/Enemies/Goblin/Run", 0.2f, 8, true, scale);
        attackAnimation = new TextureAnimation("Characters/Enemies/Goblin/Attack", 0.1f, 8, false, scale);
        takeHitAnimation = new TextureAnimation("Characters/Enemies/Goblin/Take Hit", stunTime / 4, 4, false, scale);
        deathAnimation = new TextureAnimation("Characters/Enemies/Goblin/Death", 0.1f, 4, false, scale);
    }

    public override void LoadContent()
    {
        hurtSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/goblin_onhit");
        deathSound = BazaarBountyGame.GetGameInstance().Content.Load<SoundEffect>("SoundEffects/Enemies/goblin_death");
        base.LoadContent();
    }

    public override void MeleeAttack()
    {
        if (state.SwitchState(ECharacterGameState.MeleeAttack))
        {
            attackAnimation.Play();
            // The first 6 frames are the sword swing up, no damage is dealt
            Utils.Timer.Delay(0.6f, () => { damageState = true; });
        }
    }

    public override void FinishMeleeAttack()
    {
        base.FinishMeleeAttack();
        damageState = false;
    }
}