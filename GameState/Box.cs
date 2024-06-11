using System.Collections.Generic;
using System.Linq;
using FixMath.NET;

public class Box
{
    private Dictionary<Side, int> _anchors = new();
    private FRect2 _lastHitbox;
    protected FVector2 InitialPosition { get; set; }
    protected MovementManager MovementManager { get; private set; }
    public int ManagerId { get; set; } = -1;
    public FVector2 VelocityCap { get; set; }
    public FVector2 Size { get; protected set; }
    public bool Anchored { get; set; }
    public FVector2 Position { get; set; }
    public FVector2 Velocity { get; set; }
    public FVector2 Acceleration { get; set; }
    public bool AnchoredX => Anchored || _anchors.ContainsKey(Side.Left) || _anchors.ContainsKey(Side.Right);
    public bool AnchoredY => Anchored || _anchors.ContainsKey(Side.Top) || _anchors.ContainsKey(Side.Bottom);

    public Box(FVector2 position, FVector2 size, bool anchored)
    {
        _lastHitbox = new FRect2(position, size);
        Position = position;
        InitialPosition = position;
        Size = size;
        Anchored = anchored;
        VelocityCap = new(Fix64.MaxValue, Fix64.MaxValue);
    }

    public void SetMovementManager(MovementManager movementManager, int id)
    {
        MovementManager = movementManager;
        ManagerId = id;
    }

    public void CopyFrom(Box other)
    {
        Position = other.Position;
        Velocity = other.Velocity;
        Acceleration = other.Acceleration;
        _lastHitbox = other._lastHitbox;
        InitialPosition = other.InitialPosition;
        _anchors = other._anchors.ToDictionary(e => e.Key, e => e.Value);
        VelocityCap = other.VelocityCap;
        Size = other.Size;
        Anchored = other.Anchored;
    }

    public FRect2 GetHitbox()
    {
        if (Position != _lastHitbox.Position)
        {
            _lastHitbox.Position = Position;
        }
        return _lastHitbox;
    }

    public virtual void Reset()
    {
        Position = InitialPosition;
        Velocity = new();
        Acceleration = new();
        _anchors.Clear();
    }

    public virtual void UpdateVelocity(double deltaS)
    {
        if (Anchored)
        {
            return;
        }

        UpdateAnchors();
        var tempV = Velocity + deltaS * Acceleration;
        if (AnchoredX)
        {
            var side = _anchors.ContainsKey(Side.Left) ? Side.Left : Side.Right;
            tempV.X = GetAnchor(side).Velocity.X;
        }
        if (AnchoredY)
        {
            var side = _anchors.ContainsKey(Side.Top) ? Side.Top : Side.Bottom;
            tempV.Y = GetAnchor(side).Velocity.Y;
        }
        tempV.X = Fix64.Clamp(tempV.X, -VelocityCap.X, VelocityCap.X);
        tempV.Y = Fix64.Clamp(tempV.Y, -VelocityCap.Y, VelocityCap.Y);
        Velocity = tempV;
    }

    public bool IsSideAnchored(Side side)
    {
        return Anchored || _anchors.ContainsKey(side);
    }

    protected virtual void UpdateAnchors()
    {
        if (Anchored)
        {
            return;
        }
        foreach (var key in _anchors.Keys.ToList())
        {
            var axis = 0;
            if (key == Side.Top || key == Side.Bottom)
            {
                axis = 1;
            }
            var otherAxis = (axis + 1) % 2;
            var anchor = GetAnchor(key);
            if (anchor.Velocity[axis] != Fix64.Zero || !Overlaps(anchor, otherAxis))
            {
                _anchors.Remove(key);
            }
        }
    }

    public virtual void UpdatePosition(double deltaS)
    {
        if (Anchored)
        {
            return;
        }
        Position += deltaS * Velocity;
    }

    public virtual void OnCollision(Side side, Box other)
    {
    }

    public void AddAnchor(Box anchor, Side side)
    {
        if (_anchors.ContainsKey(side))
        {
            _anchors[side] = anchor.ManagerId;
        }
        else
        {
            _anchors.Add(side, anchor.ManagerId);
        }
    }

    public void RemoveAnchor(Side side)
    {
        _anchors.Remove(side);
    }

    public void RemoveAnchors()
    {
        _anchors.Clear();
    }

    public Box GetAnchor(Side side)
    {
        if (!_anchors.ContainsKey(side))
        {
            return null;
        }
        return MovementManager.GetBox(_anchors[side]);
    }

    protected Box GetXAnchor()
    {
        if (_anchors.ContainsKey(Side.Left))
        {
            return GetAnchor(Side.Left);
        }
        if (_anchors.ContainsKey(Side.Right))
        {
            return GetAnchor(Side.Right);
        }
        return null;
    }

    public bool Overlaps(Box other, int i)
    {
        var max = Position[i] + Size[i];
        var min = Position[i];
        var otherMax = other.Position[i] + other.Size[i];
        var otherMin = other.Position[i];
        var disjoint = max < otherMin || otherMax < min;
        return !disjoint;
    }
}

public enum Side
{
    Left,
    Right,
    Top,
    Bottom
}