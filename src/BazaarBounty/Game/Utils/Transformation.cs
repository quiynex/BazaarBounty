using System;
using Microsoft.Xna.Framework;

namespace BazaarBounty
{
    public struct Transform2
    {
        Vector2 position = Vector2.Zero;
        Vector2 scale = Vector2.One;
        float rotation = 0;
        
        public Transform2()
        {
            position = Vector2.Zero;
            scale = Vector2.One;
            rotation = 0;
        }

        public Transform2(Vector2 position, Vector2 scale, float rotation)
        {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public Vector2 Position => position;
        public Vector2 Scale => scale;
        public float Rotation => rotation;

        public static Transform2 Lerp(Transform2 a, Transform2 b, float t)
        {
            return new Transform2(
                Vector2.Lerp(a.Position, b.Position, t),
                Vector2.Lerp(a.Scale, b.Scale, t),
                MathHelper.Lerp(a.Rotation, b.Rotation, t)
            );
        }


        public static Transform2 operator +(Transform2 a, Transform2 b)
        {
            return new Transform2(
                a.Position + b.Position,
                a.Scale * b.Scale,
                a.Rotation + b.Rotation
            );
        }

        public static Transform2 operator -(Transform2 a, Transform2 b)
        {
            return new Transform2(
                a.Position - b.Position,
                a.Scale / b.Scale,
                a.Rotation - b.Rotation
            );
        }

        public static Transform2 operator *(Transform2 a, float b)
        {
            return new Transform2(
                a.Position * b,
                a.Scale * b,
                a.Rotation * b
            );
        }
        
        public static bool operator ==(Transform2 a, Transform2 b)
        {
            return a.Position == b.Position && a.Scale == b.Scale && Math.Abs(a.Rotation - b.Rotation) < Double.Epsilon;
        }

        public static bool operator !=(Transform2 a, Transform2 b)
        {
            return !(a == b);
        }

        // Untested, just to suppress warnings.
        public override bool Equals(object obj)
        {
            if (obj is Transform2)
            {
                Transform2 other = (Transform2)obj;
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(position, scale, rotation);
        }
    }
}