using System;

namespace Shared
{
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float DistanceSquared(Vec3 other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public float DistanceTo(Vec3 other)
        {
            return (float)Math.Sqrt(DistanceSquared(other));
        }
    }
}
