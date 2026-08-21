using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Containers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using MonoGame.Extended.TextureAtlases;
using Myra.Graphics2D.TextureAtlases;
using MonoGame.Extended.Sprites;



namespace BazaarBounty
{
    public enum EffectName
    {
        PlayerHit,
        EnemyHit,
        ProjectileHit,
        TextHit
        // Add more effect names as needed
    }
    public class ParticleEffectManager
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly List<ActiveParticleEffect> _activeEffects;
        private Texture2D _particleTexture;
        private TextureRegion2D defaultTextureRegion;

        public Dictionary<int, Texture2D> NumberTextures { get; private set; }

        public ParticleEffectManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _activeEffects = new List<ActiveParticleEffect>();

        }
        public void LoadContent()
        {
            _particleTexture = new Texture2D(_graphicsDevice, 1, 1);
            
            _particleTexture.SetData(new[] { Color.White });

            defaultTextureRegion = new TextureRegion2D(_particleTexture);


            var Content = BazaarBountyGame.GetGameInstance().Content;

            NumberTextures = new Dictionary<int, Texture2D>();

            for (int i = 0; i < 100; i++){
                NumberTextures[i] = Content.Load<Texture2D>($"UI/WhiteNumbers/{i}");
            } 
            NumberTextures[100] = Content.Load<Texture2D>($"UI/WhiteNumbers/99plus");

        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = _activeEffects[i];
                activeEffect.TimeElapsed += deltaTime;

                if (activeEffect.TimeElapsed >= activeEffect.Duration)
                {
                    _activeEffects[i].ParticleEffect.Dispose();
                    _activeEffects.RemoveAt(i);
                }
                else
                {
                    if(activeEffect.Character != null){
                        // Only update for characters, not wall
                        activeEffect.ParticleEffect.Position = activeEffect.Character.Position;
                    }
                    activeEffect.ParticleEffect.Update(deltaTime);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var activeEffect in _activeEffects)
            {
                activeEffect.ParticleEffect.Trigger(activeEffect.ParticleEffect.Position, 1.0f);
                spriteBatch.Draw(activeEffect.ParticleEffect);
            }
        }

        private class ActiveParticleEffect
        {
            public Character Character { get; }
            public ParticleEffect ParticleEffect { get; }
            public float Duration { get; }
            public float TimeElapsed { get; set; }

            public ActiveParticleEffect(ParticleEffect particleEffect, float duration, Character character = null)
            {
                Character = character;
                ParticleEffect = particleEffect;
                Duration = duration;
                TimeElapsed = 0;
            }
        }

        // For Walls        
        public void AddParticleEffect(Projectile projectile, EffectName effectName, float duration)
        {
            ParticleEffect particleEffect = effectName switch
            {
                EffectName.ProjectileHit => CreateWallHitEffect(projectile.Position, duration),
                // Add more effects as needed
                _ => throw new ArgumentException($"Particle effect '{effectName}' not found.")
            };

            _activeEffects.Add(new ActiveParticleEffect(particleEffect, duration));
        }

        // For Characters
        public void AddParticleEffect(Character character, EffectName effectName, float duration) 
        {
            ParticleEffect particleEffect = effectName switch
            {
                EffectName.PlayerHit => CreatePlayerHitEffect(character, duration),
                EffectName.EnemyHit => CreateEnemyHitEffect(character, duration),
                // Add more effects as needed
                _ => throw new ArgumentException($"Particle effect '{effectName}' not found.")
            };

            _activeEffects.Add(new ActiveParticleEffect(particleEffect, duration, character));
        }
        // For Damage Numbers
        public void AddParticleEffect(Character character, EffectName effectName, float duration, int damagenumber, HslColor hslColor) 
        {
            ParticleEffect particleEffect = effectName switch
            {
                EffectName.TextHit => CreateTextHitEffect(character, duration, damagenumber, hslColor),
                // Add more effects as needed
                _ => throw new ArgumentException($"Particle effect '{effectName}' not found.")
            };

            _activeEffects.Add(new ActiveParticleEffect(particleEffect, duration, character));
        }

        // PARTICLE EFFECT DEFINITIONS:
        public ParticleEffect CreatePlayerHitEffect(Character character, float duration){
            return new ParticleEffect(autoTrigger: false)
            {
                Position = character.Position,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(defaultTextureRegion, 100, TimeSpan.FromSeconds(duration),
                        Profile.Point())
                    {
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = new Range<float>(50f, 90f),
                            Quantity = 10,
                            Rotation = new Range<float>(-0.1f, 0.1f),
                            Scale = new Range<float>(3.0f, 5.0f)
                        },
                        Modifiers =
                        {
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new ColorInterpolator
                                    {
                                        StartValue = new HslColor(0f, 0.5f, 0.5f),
                                        EndValue = new HslColor(0.5f, 0.9f, 0.0f)
                                    },
                                    new OpacityInterpolator
                                    {
                                        StartValue = 1f,
                                        EndValue = 0f
                                    },
                                    new ScaleInterpolator { 
                                        StartValue = new Vector2(10,10),
                                        EndValue = new Vector2(1,1) }
                                    
                                }
                            },
                            // new RotationModifier { RotationRate = -2.1f },
                            //new RectangleContainerModifier { Width = 800, Height = 480 },
                            new DragModifier { Density = 1f, DragCoefficient = 1f }
                        }
                    }
                }
            };
        }

        public ParticleEffect CreateEnemyHitEffect(Character character, float duration){
            return new ParticleEffect(autoTrigger: false)
            {
                Position = character.Position,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(defaultTextureRegion, 100, TimeSpan.FromSeconds(duration),
                        Profile.Point())
                    {
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = new Range<float>(80f, 120f),
                            Quantity = 2,
                            Rotation = new Range<float>(-0.1f, 0.1f),
                            Scale = new Range<float>(3.0f, 5.0f)
                        },
                        Modifiers =
                        {
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new ColorInterpolator
                                    {
                                        StartValue = (character.Type == CharacterType.Slime) ? new HslColor(140f, 0.5f, 0.5f) : new HslColor(1f, 0.5f, 0.2f),
                                        EndValue =  (character.Type == CharacterType.Slime) ? new HslColor(140f, 0.5f, 0.5f) : new HslColor(1f, 0.5f, 0.2f)
                                    },
                                    new OpacityInterpolator
                                    {
                                        StartValue = 1f,
                                        EndValue = 0f
                                    },
                                    new ScaleInterpolator { 
                                        StartValue = new Vector2(10,10),
                                        EndValue = new Vector2(1,1) }
                                    
                                }
                            },
                            new DragModifier { Density = 1f, DragCoefficient = 1f }
                        }
                    }
                }
            };
        }
        public ParticleEffect CreateWallHitEffect(Vector2 position, float duration){
            return new ParticleEffect(autoTrigger: false)
            {
                Position = position,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(defaultTextureRegion, 100, TimeSpan.FromSeconds(duration),
                        Profile.Point())
                    {
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = new Range<float>(100f, 150f),
                            Quantity = 3,
                            Rotation = new Range<float>(-0.1f, 0.1f),
                            Scale = new Range<float>(3.0f, 4.0f)
                        },
                        Modifiers =
                        {
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new ColorInterpolator
                                    {
                                        StartValue = new HslColor(200f, 0.2f, 0.4f),
                                        EndValue =  new HslColor(200f, 0.2f, 0.2f)
                                    },
                                    new OpacityInterpolator
                                    {
                                        StartValue = 1f,
                                        EndValue = 0f
                                    },
                                    new ScaleInterpolator { 
                                        StartValue = new Vector2(5,5),
                                        EndValue = new Vector2(1,1) }
                                    
                                }
                            },
                            new DragModifier { Density = 1f, DragCoefficient = 1f }
                        }
                    }
                }
            };
        }
        public ParticleEffect CreateTextHitEffect(Character character, float duration, int damagenumber, HslColor hslColor){
            TextureRegion2D numberTextureRegion;
            if(damagenumber > 99){
                numberTextureRegion = new TextureRegion2D(NumberTextures[100]);  
            }
            else{
                numberTextureRegion = new TextureRegion2D(NumberTextures[damagenumber]);
            }
            
            return new ParticleEffect(autoTrigger: false)
            {
                Position = character.Position,
                Emitters = new List<ParticleEmitter>
                {
                    new ParticleEmitter(numberTextureRegion, 1, TimeSpan.FromSeconds(duration),
                        Profile.Box(1,1))
                    {
                        Parameters = new ParticleReleaseParameters
                        {
                            Speed = 30f,
                            Quantity = 1,
                            Scale = 2.0f,
                            Rotation= 0.0f
                        },
                        Modifiers =
                        {
                            new AgeModifier
                            {
                                Interpolators =
                                {
                                    new ColorInterpolator
                                    {
                                        StartValue = hslColor,
                                        EndValue =  hslColor
                                    },
                                    new OpacityInterpolator
                                    {
                                        StartValue = 1f,
                                        EndValue = 0f
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
