using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using Godot;

public class MovementManager
{
    private int _nextId = 0;
    private const double MinGap = 0.05;
    private const int MaxCollisionLoops = 100;
    private Dictionary<int, Box> _idToBox = new();
    private List<Box> _staticBoxes = new();
    private List<Fighter> _fighters = new();
    private IEnumerable<Box> _solidBoxes => _staticBoxes.Concat(_fighters);
    private Bullet _bullet = null;
    private Pistol _pistol;

    public MovementManager()
    {
    }

    public void CopyFrom(MovementManager other)
    {
        _nextId = other._nextId;
    }

    public Box GetBox(int id)
    {
        return _idToBox[id];
    }

    public void AddStatic(Box box)
    {
        _staticBoxes.Add(box);
        RegisterBox(box);
    }

    public void AddFighter(Fighter fighter)
    {
        _fighters.Add(fighter);
        RegisterBox(fighter);
    }

    public void AddBullet(Bullet bullet)
    {
        _bullet = bullet;
        RegisterBox(bullet);
    }

    public void RemoveBullet()
    {
        _idToBox.Remove(_bullet.ManagerId);
        _bullet = null;
    }

    public Bullet GetBullet()
    {
        return _bullet;
    }

    public Pistol GetPistol()
    {
        return _pistol;
    }

    public void SetPistol(Pistol pistol)
    {
        _pistol = pistol;
        RegisterBox(pistol);
    }

    private void RegisterBox(Box box)
    {
        box.SetMovementManager(this, _nextId++);
        _idToBox.Add(box.ManagerId, box);
    }

    public void AdvanceState(GameInput[] input, double deltaS)
    {
        CheckAttacks();
        ApplyInput(input, deltaS);
        UpdateVelocities(deltaS);
        UpdateFighterPositions(deltaS);
        UpdatePistolAndBulletPositions(deltaS);
        CheckPistol();
    }

    private void UpdatePistolAndBulletPositions(double deltaS)
    {
        _pistol.UpdatePosition(deltaS);
        _bullet?.UpdatePosition(deltaS);
    }

    public void CheckPistol()
    {
        if (_pistol == null || _pistol.IsWielded)
        {
            return;
        }
        List<(Fighter fighter, Fix64 overlap)> overlapInfos = new();
        foreach (var fighter in _fighters)
        {
            var intersection = fighter.GetHitbox().Intersection(_pistol.GetHitbox());
            if (intersection.Area != Fix64.Zero)
            {
                overlapInfos.Add((fighter, intersection.Area));
            }
        }
        foreach (var (fighter, overlap) in overlapInfos.OrderBy(i => i.overlap))
        {
            if (fighter.WieldPistol())
            {
                _pistol.IsWielded = true;
                return;
            }
        }
    }

    private void ApplyInput(GameInput[] input, double deltaS)
    {
        for (int i = 0; i < input.Length; i++)
        {
            _fighters[i].ApplyInput(input[i], deltaS);
        }
    }

    private void UpdateVelocities(double deltaS)
    {
        foreach (var box in _fighters)
        {
            box.UpdateVelocity(deltaS);
        }
        _pistol.UpdateVelocity(deltaS);
    }

    private void UpdateFighterPositions(double deltaS)
    {
        var remainingTime = deltaS;
        var collisions = GetFirstCollisions(remainingTime);
        var loops = 0;
        while (collisions.Count > 0 && loops < MaxCollisionLoops)
        {
            loops++;
            AdvanceFighterPositions(collisions[0].TimeS);
            foreach (var c in collisions)
            {
                ResolveCollision(c);
            }
            remainingTime -= collisions[0].TimeS;
            collisions = GetFirstCollisions(remainingTime);
        }
        if (loops == MaxCollisionLoops)
        {
            foreach (var box in _fighters)
            {
                box.Reset();
            }
        }
        else
        {
            AdvanceFighterPositions(remainingTime);
        }
    }

    private void CheckAttacks()
    {
        foreach (var (fighter1, fighter2) in GetFighterPairs())
        {
            Fighter.CheckPunches(fighter1, fighter2);
        }
        if (_bullet is null)
        {
            return;
        }
        foreach (var fighter in _fighters)
        {
            if (fighter.GetHitbox().Intersects(_bullet.GetHitbox()))
            {
                fighter.Kill();
                RemoveBullet();
                break;
            }
        }
    }

    private void ResolveCollision(CollisionInfo info)
    {
        EnsureGap(info.Fighter, info.Other, info.FighterSide, info.OtherSide);
        if (info.FighterSide == Side.Left || info.FighterSide == Side.Right)
        {
            if (info.Other.AnchoredX)
            {
                info.Fighter.Velocity = new(Fix64.Zero, info.Fighter.Velocity.Y);
                info.Fighter.AddAnchor(info.Other, info.FighterSide);
            }
            else if (info.Fighter.AnchoredX)
            {
                info.Other.Velocity = new(Fix64.Zero, info.Other.Velocity.Y);
                info.Other.AddAnchor(info.Fighter, info.OtherSide);
            }
            else
            {
                var aveVX = (info.Fighter.Velocity.X + info.Other.Velocity.X) * 0.5;
                info.Fighter.Velocity = new(aveVX, info.Fighter.Velocity.Y);
                info.Other.Velocity = new(aveVX, info.Other.Velocity.Y);
            }
        }
        if (info.FighterSide == Side.Top || info.FighterSide == Side.Bottom)
        {
            if (info.Other.AnchoredY)
            {
                info.Fighter.Velocity = new(info.Fighter.Velocity.X, Fix64.Zero);
                info.Fighter.AddAnchor(info.Other, info.FighterSide);
            }
            else if (info.Fighter.AnchoredY)
            {
                info.Other.Velocity = new(info.Other.Velocity.X, Fix64.Zero);
                info.Other.AddAnchor(info.Fighter, info.OtherSide);
            }
            else
            {
                var aveVY = (info.Fighter.Velocity.Y + info.Other.Velocity.Y) * 0.5;
                info.Fighter.Velocity = new(info.Fighter.Velocity.X, aveVY);
                info.Other.Velocity = new(info.Other.Velocity.X, aveVY);
            }
        }
        info.Fighter.OnCollision(info.FighterSide, info.Other);
        info.Other.OnCollision(info.OtherSide, info.Fighter);
    }

    private static void EnsureGap(Box a, Box b, Side aSide, Side bSide)
    {
        var nudge = MinGap;
        if (!a.IsSideAnchored(bSide))
        {
            NudgeAway(a, aSide, nudge);
        }
        if (!b.IsSideAnchored(aSide))
        {
            NudgeAway(b, bSide, nudge);
        }
    }

    private static void NudgeAway(Box a, Side s, double distance)
    {
        var newP = a.Position;
        switch (s)
        {
            case Side.Left:
                newP.X += distance;
                break;
            case Side.Right:
                newP.X -= distance;
                break;
            case Side.Top:
                newP.Y += distance;
                break;
            case Side.Bottom:
                newP.Y -= distance;
                break;
        }
        a.Position = newP;
    }

    private void AdvanceFighterPositions(double deltaS)
    {
        foreach (var box in _fighters)
        {
            box.UpdatePosition(deltaS);
        }
    }

    private List<CollisionInfo> GetFirstCollisions(double deltaS)
    {
        double lowestTime = double.PositiveInfinity;
        List<CollisionInfo> firstCollisions = new();
        foreach (var (fighter, box) in GetFighterToBoxPairs())
        {
            var collision = CheckCollision(fighter, box, deltaS);
            if (collision == CollisionInfo.NoCollision)
            {
                continue;
            }
            if (collision.TimeS == lowestTime)
            {
                firstCollisions.Add(collision);
            }
            else if (collision.TimeS < lowestTime)
            {
                lowestTime = collision.TimeS;
                firstCollisions.Clear();
                firstCollisions.Add(collision);
            }
            
        }
        return firstCollisions;
    }

    private IEnumerable<(Fighter, Box)> GetFighterToBoxPairs()
    {
        HashSet<Box> alreadyChecked = new();
        foreach (var fighter in _fighters)
        {
            alreadyChecked.Add(fighter);
            foreach (var box in _solidBoxes.Except(alreadyChecked))
            {
                yield return (fighter, box);
            }
        }
    }

    private IEnumerable<(Fighter, Fighter)> GetFighterPairs()
    {
        HashSet<Fighter> alreadyChecked = new();
        foreach (var fighter1 in _fighters)
        {
            alreadyChecked.Add(fighter1);
            foreach (var fighter2 in _fighters.Except(alreadyChecked))
            {
                yield return (fighter1, fighter2);
            }
        }
    }

    public CollisionInfo CheckCollision(Fighter a, Box b, double deltaS)
    {
        var vDif = b.Velocity - a.Velocity;
        if (vDif.X == Fix64.Zero && vDif.Y == Fix64.Zero)
        {
            return CollisionInfo.NoCollision;
        }
        var bMax = b.Position + b.Size;
        var bMin = b.Position;
        var aMax = a.Position + a.Size;
        var aMin = a.Position;
        if (vDif.X == Fix64.Zero && (aMin.X > bMax.X || bMin.X > aMax.X))
        {
            return CollisionInfo.NoCollision;
        }
        if (vDif.Y == Fix64.Zero && (aMin.Y > bMax.Y || bMin.Y > aMax.Y))
        {
            return CollisionInfo.NoCollision;
        }
        if (a.GetHitbox().Intersects(b.GetHitbox(), true))
        {
            Side aSide, bSide;
            if (aMax.X == bMin.X || aMin.X == bMax.X)
            {
                aSide = aMin.X < bMin.X ? Side.Right : Side.Left;
                bSide = aMin.X < bMin.X ? Side.Left : Side.Right;
            }
            else
            {
                aSide = aMin.Y < bMin.Y ? Side.Bottom : Side.Top;
                bSide = aMin.Y < bMin.Y ? Side.Top : Side.Bottom;
            }
            return new CollisionInfo()
            {
                Fighter = a,
                Other = b,
                TimeS = 0,
                FighterSide = aSide,
                OtherSide = bSide
            };
        }

        var firstOverlapTime = new FVector2(0, 0);
        var lastOverlapTime = new FVector2(double.PositiveInfinity, double.PositiveInfinity);

        for (int i = 0; i < 2; i++)
        {
            if (vDif[i] < 0.0f)
            {
                if (bMax[i] < aMin[i])
                {
                    return CollisionInfo.NoCollision; // moving apart
                }
                if (aMax[i] < bMin[i])
                {
                    firstOverlapTime[i] = (aMax[i] - bMin[i]) / vDif[i];
                }
                if (bMax[i] > aMin[i])
                {
                    lastOverlapTime[i] = (aMin[i] - bMax[i]) / vDif[i];
                }
            }
            else if (vDif[i] > 0.0f)
            {
                if (bMin[i] > aMax[i])
                {
                    return CollisionInfo.NoCollision; // moving apart
                }
                if (bMax[i] < aMin[i])
                {
                    firstOverlapTime[i] = (aMin[i] - bMax[i]) / vDif[i];
                }
                if (aMax[i] > bMin[i])
                {
                    lastOverlapTime[i] = (aMax[i] - bMin[i]) / vDif[i];
                }
            }
        }

        if (firstOverlapTime[0] > deltaS || firstOverlapTime[1] > deltaS)
        {
            return CollisionInfo.NoCollision;
        }

        if (Fix64.Max(firstOverlapTime.X, firstOverlapTime.Y) > Fix64.Min(lastOverlapTime.X, lastOverlapTime.Y))
        {
            return CollisionInfo.NoCollision;
        }

        var info = new CollisionInfo
        {
            Fighter = a,
            Other = b
        };
        if (firstOverlapTime.X < firstOverlapTime.Y)
        {
            info.TimeS = (double)firstOverlapTime.Y;
            info.FighterSide = vDif.Y > 0.0f ? Side.Top: Side.Bottom;
            info.OtherSide = vDif.Y > 0.0f ? Side.Bottom : Side.Top;
        }
        else
        {
            info.TimeS = (double)firstOverlapTime.X;
            info.FighterSide = vDif.X > 0.0f ? Side.Left : Side.Right;
            info.OtherSide = vDif.X > 0.0f ? Side.Right : Side.Left;
        }
        return info;
    }
}

public class CollisionInfo
{
    public static CollisionInfo NoCollision { get; } = new();
    public double TimeS { get; set; }
    public Fighter Fighter { get; set; }
    public Box Other { get; set; }
    public Side FighterSide { get; set; }
    public Side OtherSide { get; set; }
}