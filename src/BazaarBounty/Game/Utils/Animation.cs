using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tweening;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;


namespace BazaarBounty
{
    public abstract class Animation
    {
        public delegate void AnimationFinished();

        public AnimationFinished OnAnimationFinished;
        public string animationName;
        public bool isLooping;
        public bool isPlaying;
        public bool isFinished;
        protected float timeElapsed;
        protected float playSpeed = 1f;


        public abstract void LoadContent();
        public abstract void Update(GameTime gameTime);
        public abstract void Play();
        public abstract void Reset();

        public virtual void Stop()
        {
            isPlaying = false;
        }

        public virtual void Continue()
        {
            isPlaying = true;
        }
    }

    public class TextureAnimation : Animation
    {
        public int spriteWidth;
        public int spriteHeight;
        public Vector2 spriteScale;
        public Vector2 Size => new(spriteWidth * spriteScale.X, spriteHeight * spriteScale.Y);

        private Texture2D[] animationTextureFrames;
        private Texture2D animationTexture;
        private bool isSequence;
        private float frameDuration;
        private int frameCount;
        private int currentFrame;

        public TextureAnimation(string animationName, float frameDuration, int frameCount,
            bool isLooping = true, Vector2 scale = default, bool isSequence = false)
        {
            scale = scale == default ? Vector2.One : scale;
            this.animationName = animationName;
            this.frameDuration = frameDuration;
            this.frameCount = frameCount;
            this.isSequence = isSequence;
            this.isLooping = isLooping;
            // Looping animations are playing by default
            isPlaying = isLooping;
            isFinished = false;
            timeElapsed = 0;
            currentFrame = 0;
            spriteScale = scale;
            if (isSequence) animationTextureFrames = new Texture2D[frameCount];
        }

        public override void LoadContent()
        {
            BazaarBountyGame game = BazaarBountyGame.GetGameInstance();
            if (isSequence)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    animationTextureFrames[i] = game.Content.Load<Texture2D>(animationName + (i + 1));
                }

                // assume all the textures are the same size
                if (frameCount > 0 && spriteWidth == 0 && spriteHeight == 0)
                {
                    spriteWidth = animationTextureFrames[0].Width;
                    spriteHeight = animationTextureFrames[0].Height;
                }
            }
            else
            {
                animationTexture = game.Content.Load<Texture2D>(animationName);
                if (spriteWidth == 0 && spriteHeight == 0)
                {
                    spriteWidth = animationTexture.Width / frameCount;
                    spriteHeight = animationTexture.Height;
                }
            }
        }

        public override void Play()
        {
            // Cannot restart a playing animation
            if (isPlaying)
            {
                return;
            }

            isPlaying = true;
            isFinished = false;
            currentFrame = 0;
            timeElapsed = 0;
        }

        public override void Reset()
        {
            isPlaying = false;
            isFinished = false;
            currentFrame = 0;
            timeElapsed = 0;
        }

        public override void Stop()
        {
            isPlaying = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (isPlaying)
            {
                timeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (timeElapsed > frameDuration / playSpeed)
                {
                    timeElapsed = 0;
                    currentFrame++;
                    if (currentFrame == frameCount)
                    {
                        currentFrame = 0;
                        if (!isLooping)
                        {
                            isFinished = true;
                            isPlaying = false;
                            OnAnimationFinished?.Invoke();
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float angle,
            SpriteEffects spriteEffect = SpriteEffects.None, Color color = default)
        {
            Draw(spriteBatch, position, angle, Vector2.Zero, spriteEffect, color: color);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float angle, Vector2 origin,
            SpriteEffects spriteEffect = SpriteEffects.None, float layer = 0.5f, Color color = default)
        {
            color = color == default ? Color.White : color;
            Vector2 center = new Vector2((float)spriteWidth / 2, (float)spriteHeight / 2);
            Vector2 scaleCenter = center * spriteScale;
            if (isSequence)
            {
                spriteBatch.Draw(animationTextureFrames[currentFrame], position - scaleCenter, null, color,
                    angle, origin, spriteScale, spriteEffect, 0.5f);
            }
            else
            {
                spriteBatch.Draw(animationTexture, position - scaleCenter, new Rectangle(currentFrame * spriteWidth, 0,
                    spriteHeight, spriteWidth), color, angle, origin, spriteScale, spriteEffect, 0.5f);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 cornerPosition, float baseAngle, Vector2 origin,
            Transform2 transform, SpriteEffects spriteEffect = SpriteEffects.None,
            float layer = 0.5f, Color color = default)
        {
            color = color == default ? Color.White : color;
            Vector2 position = cornerPosition + transform.Position;
            float angle = baseAngle + transform.Rotation;
            Vector2 scale = spriteScale * transform.Scale;
            if (isSequence)
            {
                spriteBatch.Draw(animationTextureFrames[currentFrame], position, null, color,
                    angle, origin, scale, spriteEffect, 0.5f);
            }
            else
            {
                spriteBatch.Draw(animationTexture, cornerPosition, new Rectangle(currentFrame * spriteWidth, 0,
                    spriteHeight, spriteWidth), color, angle, origin, scale, spriteEffect, 0.5f);
            }
        }
        // public override void Draw(SpriteBatch spriteBatch, Vector2 basePosition, float baseAngle, Vector2 origin, 
        //                           SpriteEffects spriteEffect=SpriteEffects.None, float layer=0f, Color color=default)
        // {
        //     color = color == default ? Color.White : color;
        //     Vector2 position = basePosition + currentLerpTransform.Position;
        //     float angle = baseAngle + currentLerpTransform.Rotation;
        //     Vector2 scale = spriteScale * currentLerpTransform.Scale;
        //
        //     // Use the interpolated values to draw the sprite
        //     spriteBatch.Draw(animationTexture, position, null, color, angle, origin, scale, spriteEffect, layer);
        // }
    }

    public class TransformationAnimation : Animation
    {
        public class Keyframe : IComparable
        {
            public float Time { get; } // Finish time of the keyframe
            public Transform2 transform; // Target transform

            public Keyframe(float time, Vector2 position, float rotation, Vector2 scale)
            {
                Time = time;
                transform = new Transform2(position, scale, rotation);
            }

            public int CompareTo(object obj)
            {
                Keyframe other = obj as Keyframe;
                if (other == null) return 1;
                return Time.CompareTo(other.Time);
            }
        }

        private SortedSet<Keyframe> keyframes;
        private Keyframe lastFrame, currentFrame;
        private Transform2 currentLerpTransform;
        public Transform2 CurrentLerpTransform => currentLerpTransform;
        private Transform2 deltaTransform;
        public Transform2 DeltaTransform => deltaTransform;
        private float totalTime;

        public TransformationAnimation(string name, float speed = 1f, bool loop = false)
        {
            animationName = name;
            keyframes = new SortedSet<Keyframe>();
            isLooping = loop;
            isPlaying = loop;
            playSpeed = speed;
            currentLerpTransform = new();
        }

        public override void LoadContent()
        {
        }

        public TransformationAnimation AddKeyframe(float time, Vector2 position, float rotation, Vector2 scale)
        {
            float radians = MathHelper.ToRadians(rotation);
            keyframes.Add(new Keyframe(time, position, radians, scale));
            return this;
        }

        // public override void LoadContent()
        // {
        //     BazaarBountyGame game = BazaarBountyGame.GetGameInstance();
        //     animationTexture = game.Content.Load<Texture2D>(animationName);
        //     if (spriteWidth == 0 && spriteHeight == 0)
        //     {
        //         spriteWidth = animationTexture.Width;
        //         spriteHeight = animationTexture.Height;
        //     }
        // }

        public override void Play()
        {
            if (keyframes.Count == 0) return;
            if (isPlaying) return;
            isPlaying = true;
            isFinished = false;
            timeElapsed = 0;
            totalTime = keyframes.Last().Time;
            lastFrame = null;
            currentFrame = keyframes.First();
        }

        public override void Reset()
        {
            isPlaying = false;
            isFinished = false;
            timeElapsed = 0;
            lastFrame = null;
            currentFrame = keyframes.First();
        }

        public override void Update(GameTime gameTime)
        {
            if (!isPlaying || keyframes.Count == 0) return;

            timeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeElapsed > totalTime / playSpeed)
            {
                if (isLooping)
                {
                    timeElapsed = 0;
                }
                else
                {
                    isPlaying = false;
                    isFinished = true;
                    OnAnimationFinished?.Invoke();
                    currentLerpTransform = new();
                    return;
                }
            }

            // Check if we need to update the current keyframes
            if (timeElapsed > currentFrame.Time / playSpeed)
            {
                // Find the next keyframes
                lastFrame = currentFrame;
                currentFrame = keyframes.First(kf => kf.Time > timeElapsed);
            }

            // Interpolate between the current and next keyframes
            float startTime = lastFrame?.Time ?? 0;
            float lerpFactor = (timeElapsed - startTime) / (currentFrame.Time - startTime);

            float duration = currentFrame.Time - startTime;
            Transform2 lastTransform = lastFrame?.transform ?? new();
            Transform2 newTransform = Transform2.Lerp(lastTransform, currentFrame.transform, lerpFactor);
            deltaTransform = newTransform - currentLerpTransform;
            currentLerpTransform = newTransform;
        }
        /*
        public override void Draw(SpriteBatch spriteBatch, Vector2 basePosition, float baseAngle, Vector2 origin,
                                  SpriteEffects spriteEffect=SpriteEffects.None, float layer=0f, Color color=default)
        {
            color = color == default ? Color.White : color;
            Vector2 position = basePosition + currentLerpTransform.Position;
            float angle = baseAngle + currentLerpTransform.Rotation;
            Vector2 scale = spriteScale * currentLerpTransform.Scale;

            // Use the interpolated values to draw the sprite
            spriteBatch.Draw(animationTexture, position, null, color, angle, origin, scale, spriteEffect, layer);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 position, float angle,
                                  SpriteEffects spriteEffect=SpriteEffects.None, Color color=default)
        {
            Vector2 center = new Vector2((float)spriteWidth / 2, (float)spriteHeight / 2);
            float layer = 0f;
            Draw(spriteBatch, position, angle, center, spriteEffect, layer, color);
        }
        */
    }
}