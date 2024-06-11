using System;

namespace FixMath.NET;

public struct FVector3
{
    public Fix64 X;
    public Fix64 Y;
    public Fix64 Z;

    public FVector3(Fix64 x, Fix64 y, Fix64 z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static FVector3 operator +(FVector3 left, FVector3 right)
    {
        left.X += right.X;
        left.Y += right.Y;
        left.Z += right.Z;
        return left;
    }

    public static FVector3 operator -(FVector3 left, FVector3 right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        left.Z -= right.Z;
        return left;
    }

    public static FVector3 operator *(FVector3 vec, Fix64 scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    public Fix64 this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
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
                case 2:
                    Z = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("index");
            }
        }
    }
}