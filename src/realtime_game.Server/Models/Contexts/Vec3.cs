public struct Vec3
{
    public float x, y, z;

    public Vec3(float x, float y, float z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vec3 operator *(Vec3 a, float f) => new Vec3(a.x * f, a.y * f, a.z * f);
    public static Vec3 operator /(Vec3 a, float f) => new Vec3(a.x / f, a.y / f, a.z / f);

    public float SqrMagnitude => x * x + y * y + z * z;
    public Vec3 Normalized
    {
        get
        {
            float mag = MathF.Sqrt(SqrMagnitude);
            if (mag > 0f) return this / mag;
            return new Vec3(0f, 0f, 0f);
        }
    }
}
