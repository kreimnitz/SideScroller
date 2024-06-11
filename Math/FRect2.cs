using FixMath.NET;

public struct FRect2
{
    private FVector2 _position;
    public FVector2 Position
    {
        readonly get
        {
            return _position;
        }
        set
        {
            _position = value;
        }
    }

    private FVector2 _size;
    public FVector2 Size
    {
        readonly get
        {
            return _size;
        }
        set
        {
            _size = value;
        }
    }

    public readonly Fix64 Area => _size.X * _size.Y;

    public FRect2(FVector2 position, FVector2 size)
    {
        _position = position;
        _size = size;
    }

    public readonly FRect2 Intersection(FRect2 b)
    {
        FRect2 rect = b;
        if (!Intersects(rect))
        {
            return default;
        }

        rect._position.X = Fix64.Max(b._position.X, _position.X);
        rect._position.Y = Fix64.Max(b._position.Y, _position.Y);
        FVector2 vector = b._position + b._size;
        FVector2 vector2 = _position + _size;
        rect._size.X = Fix64.Min(vector.X, vector2.X) - rect._position.X;
        rect._size.Y = Fix64.Min(vector.Y, vector2.Y) - rect._position.Y;
        return rect;
    }

    public readonly bool Intersects(FRect2 b, bool includeBorders = false)
    {
        if (includeBorders)
        {
            if (_position.X > b._position.X + b._size.X)
            {
                return false;
            }

            if (_position.X + _size.X < b._position.X)
            {
                return false;
            }

            if (_position.Y > b._position.Y + b._size.Y)
            {
                return false;
            }

            if (_position.Y + _size.Y < b._position.Y)
            {
                return false;
            }
        }
        else
        {
            if (_position.X >= b._position.X + b._size.X)
            {
                return false;
            }

            if (_position.X + _size.X <= b._position.X)
            {
                return false;
            }

            if (_position.Y >= b._position.Y + b._size.Y)
            {
                return false;
            }

            if (_position.Y + _size.Y <= b._position.Y)
            {
                return false;
            }
        }

        return true;
    }
}