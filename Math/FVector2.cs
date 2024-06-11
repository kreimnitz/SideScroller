using System;

namespace FixMath.NET;

public struct FVector2 : IEquatable<FVector2>
{
    public Fix64 X;
    public Fix64 Y;

    public FVector2(double x, double y)
    {
        X = (Fix64)x;
        Y = (Fix64)y;
    }

    public FVector2(Fix64 x, Fix64 y)
    {
        X = x;
        Y = y;
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is FVector2 other)
        {
            return Equals(other);
        }

        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public readonly bool Equals(FVector2 other)
    {
        if (X == other.X)
        {
            return Y == other.Y;
        }
        return false;
    }

    public static FVector2 operator +(FVector2 left, FVector2 right)
    {
        left.X += right.X;
        left.Y += right.Y;
        return left;
    }

    public static FVector2 operator -(FVector2 left, FVector2 right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        return left;
    }

    public static FVector2 operator *(FVector2 vec, Fix64 scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    public static FVector2 operator *(Fix64 scale, FVector2 vec)
    {
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    public static FVector2 operator *(FVector2 vec, double scale)
    {
        var fScale = (Fix64)scale;
        vec.X *= fScale;
        vec.Y *= fScale;
        return vec;
    }

    public static FVector2 operator *(double scale, FVector2 vec)
    {
        var fScale = (Fix64)scale;
        vec.X *= fScale;
        vec.Y *= fScale;
        return vec;
    }

    public static bool operator ==(FVector2 left, FVector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FVector2 left, FVector2 right)
    {
        return !left.Equals(right);
    }

    public Fix64 this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                _ => throw new ArgumentOutOfRangeException("index"),
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("index");
            }
        }
    }
}