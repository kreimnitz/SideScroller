namespace FixMath.NET;

public partial struct Fix64
{
    public static Fix64 Max(Fix64 x, Fix64 y)
    {
        return x > y ? x : y;
    }

    public static Fix64 Min(Fix64 x, Fix64 y)
    {
        return x < y ? x : y;
    }

    public static Fix64 Clamp(Fix64 x, Fix64 min, Fix64 max)
    {
        if (x > max)
        {
            return max;
        }
        if (x < min)
        {
            return min;
        }
        return x;
    }

    public static explicit operator Fix64(int value)
    {
        return (Fix64)(double)value;
    }

    public static Fix64 operator +(Fix64 left, int right)
    {
        return left + (Fix64)right;
    }

    public static Fix64 operator +(int left, Fix64 right)
    {
        return (Fix64)left + right;
    }

    public static Fix64 operator -(Fix64 left, int right)
    {
        return left - (Fix64)right;
    }

    public static Fix64 operator -(int left, Fix64 right)
    {
        return (Fix64)left - right;
    }

    public static Fix64 operator *(Fix64 left, int right)
    {
        return left * (Fix64)right;
    }

    public static Fix64 operator *(int left, Fix64 right)
    {
        return (Fix64)left * right;
    }

    public static Fix64 operator +(Fix64 left, double right)
    {
        return left + (Fix64)right;
    }

    public static Fix64 operator +(double left, Fix64 right)
    {
        return (Fix64)left + right;
    }

    public static Fix64 operator -(Fix64 left, double right)
    {
        return left - (Fix64)right;
    }

    public static Fix64 operator -(double left, Fix64 right)
    {
        return (Fix64)left - right;
    }

    public static Fix64 operator *(Fix64 left, double right)
    {
        return left * (Fix64)right;
    }

    public static Fix64 operator *(double left, Fix64 right)
    {
        return (Fix64)left * right;
    }

    public static bool operator <(Fix64 left, double right)
    {
        return left < (Fix64)right;
    }

    public static bool operator <(double left, Fix64 right)
    {
        return (Fix64)left < right;
    }

    public static bool operator >(Fix64 left, double right)
    {
        return left > (Fix64)right;
    }

    public static bool operator >(double left, Fix64 right)
    {
        return (Fix64)left > right;
    }
}