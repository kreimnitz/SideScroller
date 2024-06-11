using System;
using System.Collections.Generic;
using FixMath.NET;
using Godot;

public class Fighter : Box, IFighter
{
    public const int FighterWidth = 55;
    private const int FighterHeight = 80;
    private const double FighterVelocityXMax = 800;
    private const double CoastAcceleration = 1100;
    private const double RunAcceleration = 1500;
    private const double SkidAcceleration = 3000;
    private const double JumpVelocity = -1200;
    private const double Gravity = 3000;
    private const double SlideDrag = Gravity / 2;
    private const double JumpCooldownS = 0.3;
    private const double AttackCooldownS = 0.5;
    private const double AttackActionDurationS = 0.3;
    private const double PunchHitboxDurationS = 0.05;
    private const double StunDurationS = 0.5;
    private const double StunCooldownS = 1.0;
    private const double WieldCooldownS = 1.0;
    private const int PistolOffsetX = 3;
    private const int PistolOffsetY = 30;
    private FRect2 _punchHitbox = new(new FVector2(8, 34), new FVector2(96, 40));
    private bool _stopping;
    private double _timeSinceLastJumpS;
    private double _timeSinceLastStunS = 5;
    private double _timeSinceLastAttackS = 5;
    private double _timeSinceLastWieldS = 5;
    public FighterMovement Movement { get; protected set; } = FighterMovement.Idle;
    public FighterAction Action { get; protected set; } = FighterAction.None;
    public bool FacingRight { get; set; }
    public bool HasPistol { get; set; }

    public Fighter(FVector2 position, bool facingRight)
        : base(position, new FVector2(FighterWidth, FighterHeight), false)
    {
        FacingRight = facingRight;
        Acceleration = new(0, Gravity);
        VelocityCap = new((Fix64)FighterVelocityXMax, Fix64.MaxValue);
    }

    public void CopyFrom(Fighter other)
    {
        base.CopyFrom(other);
        _stopping = other._stopping;
        _timeSinceLastJumpS = other._timeSinceLastJumpS;
        _timeSinceLastStunS = other._timeSinceLastStunS;
        _timeSinceLastAttackS = other._timeSinceLastAttackS;
        _timeSinceLastWieldS = other._timeSinceLastWieldS;
        Movement = other.Movement;
        Action = other.Action;
        FacingRight = other.FacingRight;
        HasPistol = other.HasPistol;
    }

    public override void Reset()
    {
        base.Reset();
        Movement = FighterMovement.Idle;
        Action = FighterAction.None;
        Acceleration = new(0, Gravity);
        FacingRight = true;
        HasPistol = false;
    }

    public void ApplyInput(GameInput input, double deltaS)
    {
        SetAction(input, deltaS);
        SetAccelerationAndState(input, deltaS);
        if (Movement == FighterMovement.Wallslide && Action == FighterAction.Punch)
        {
            Action = FighterAction.None;
        }
    }

    public override void UpdateVelocity(double deltaS)
    {
        base.UpdateVelocity(deltaS);
        if (_stopping)
        {
            Velocity = new(Fix64.Zero, Velocity.Y);
            _stopping = false;
        }
        if (Action != FighterAction.Dying)
        {
            if (Velocity.X > Fix64.Zero)
            {
                FacingRight = true;
            }
            if (Velocity.X < Fix64.Zero)
            {
                FacingRight = false;
            }
        }
    }

    public override void UpdatePosition(double deltaS)
    {
        base.UpdatePosition(deltaS);
        if (HasPistol)
        {
            var pistolX = FacingRight ? Position.X + FighterWidth + PistolOffsetX
                : Position.X - PistolOffsetX - Pistol.PistolWidth;
            var pistolY = Position.Y + PistolOffsetY;
            var pistol = MovementManager.GetPistol();
            pistol.Position = new(pistolX, pistolY);
            pistol.FacingRight = FacingRight;
        }
    }

    public FRect2 GetAttackHitbox()
    {
        if (Action != FighterAction.Punch || _timeSinceLastAttackS > PunchHitboxDurationS)
        {
            return default;
        }
        var punchOffsetX = FacingRight ? _punchHitbox.Position.X : -_punchHitbox.Position.X - _punchHitbox.Size.X;
        var punchPos = new FVector2(Position.X + punchOffsetX, Position.Y + _punchHitbox.Position.Y);
        return new(punchPos, _punchHitbox.Size);
    }

    protected override void UpdateAnchors()
    {
        base.UpdateAnchors();
        if (GetAnchor(Side.Bottom) == null)
        {
            RemoveAnchor(Side.Top);
        }
    }

    public override void OnCollision(Side side, Box other)
    {
        if (side == Side.Bottom && Movement.IsAerialState())
        {
            Movement = FighterMovement.Landing;
            AddAnchor(other, Side.Bottom);
            // TODO: implement landing state duration
        }
        if ((side == Side.Left || side == Side.Right) && Movement.IsAerialState() && other.Anchored)
        {
            Movement = FighterMovement.Wallslide;
        }
    }

    private void SetAccelerationAndState(GameInput input, double deltaS)
    {
        var keys = new HashSet<Key>(input.PressedKeys);
        if (Action == FighterAction.Stunned || Action == FighterAction.Dying)
        {
            keys.Clear();
        }
        _timeSinceLastJumpS += deltaS;
        _stopping = false;
        double accelX;
        double accelY = Gravity;
        FighterMovement groundState = FighterMovement.Idle;
        if (keys.Contains(Key.Left) && !keys.Contains(Key.Right))
        {
            RemoveAnchor(Side.Right);
            if (Movement.IsAerialState())
            {
                accelX = Velocity.X > Fix64.Zero ? -RunAcceleration : -RunAcceleration;
            }
            else
            {
                accelX = Velocity.X > Fix64.Zero ? -SkidAcceleration : -RunAcceleration;
                groundState = Velocity.X > Fix64.Zero ? FighterMovement.Skidding : FighterMovement.Running;
            }
        }
        else if (keys.Contains(Key.Right) && !keys.Contains(Key.Left))
        {
            RemoveAnchor(Side.Left);
            if (Movement.IsAerialState())
            {
                accelX = Velocity.X < Fix64.Zero ? RunAcceleration : RunAcceleration;
            }
            else
            {
                accelX = Velocity.X < Fix64.Zero ? SkidAcceleration : RunAcceleration;
                groundState = Velocity.X < Fix64.Zero ? FighterMovement.Skidding : FighterMovement.Running;
            }
        }
        else if (Velocity.X != Fix64.Zero)
        {
            if (Fix64.Abs(Velocity.X) < CoastAcceleration * deltaS)
            {
                _stopping = true;
                groundState = FighterMovement.Idle;
                accelX = 0;
            }
            else
            {
                accelX = Velocity.X > Fix64.Zero ? -CoastAcceleration : CoastAcceleration;
                groundState = FighterMovement.Running;
            }
        }
        else
        {
            accelX = 0;
            groundState = FighterMovement.Idle;
        }

        var shouldJump = _timeSinceLastJumpS > JumpCooldownS && keys.Contains(Key.Space);
        if (shouldJump && Movement == FighterMovement.Wallslide)
        {
            _timeSinceLastJumpS = 0;
            Movement = FighterMovement.WallslideJump;
            var xV = GetAnchor(Side.Left) != null ? FighterVelocityXMax : -FighterVelocityXMax;
            accelX = GetAnchor(Side.Left) != null ? RunAcceleration : -RunAcceleration;
            Velocity = new(xV, JumpVelocity);
            RemoveAnchors();
        }
        else if (shouldJump && GetAnchor(Side.Bottom) != null)
        {
            _timeSinceLastJumpS = 0;
            var anchor = GetAnchor(Side.Bottom);
            if (anchor is Fighter jumpedOn)
            {
                jumpedOn.Stun();
                if (!jumpedOn.AnchoredY)
                {
                    jumpedOn.Velocity = new(jumpedOn.Velocity.X, jumpedOn.Velocity.Y - JumpVelocity);
                }
            }

            RemoveAnchor(Side.Bottom);
            var jumpV = Fix64.Clamp(Velocity.Y + JumpVelocity, (Fix64)(JumpVelocity * 1.5), (Fix64)(JumpVelocity * -1.5));
            Velocity = new(Velocity.X, jumpV);
            Movement = FighterMovement.Jumping;
        }

        if (!Movement.IsAerialState())
        {
            Movement = groundState;
        }
        else if (Movement == FighterMovement.Wallslide)
        {
            if (AnchoredX)
            {
                var slideDrag = Velocity.Y > Fix64.Zero ? SlideDrag : -SlideDrag;
                accelY -= slideDrag;
            }
            else
            {
                Movement = FighterMovement.WallslideJump;
            }
        }
        Acceleration = new(accelX, accelY);
    }

    private void SetAction(GameInput input, double deltaS)
    {
        _timeSinceLastAttackS += deltaS;
        _timeSinceLastStunS += deltaS;
        _timeSinceLastWieldS += deltaS;
        if (Action == FighterAction.Dying)
        {
            return;
        }

        if (Action == FighterAction.Stunned)
        {
            if (_timeSinceLastStunS > StunDurationS)
            {
                Action = FighterAction.None;
            }
            else
            {
                return;
            }
        }
        if (Action == FighterAction.Punch && _timeSinceLastAttackS > AttackActionDurationS)
        {
            Action = FighterAction.None;
        }
        else if (_timeSinceLastAttackS > AttackCooldownS && input.PressedKeys.Contains(Key.X))
        {
            _timeSinceLastAttackS = 0;
            if (!HasPistol)
            {
                Action = FighterAction.Punch;
            }
            else
            {
                MovementManager.GetPistol().Shoot();
                // TODO pistol shoot state
            }
        }
    }

    public void Stun()
    {
        if (_timeSinceLastStunS > StunCooldownS && Action != FighterAction.Dying)
        {
            Action = FighterAction.Stunned;
            _timeSinceLastStunS = 0;
            if (HasPistol)
            {
                MovementManager.GetPistol().IsWielded = false;
                HasPistol = false;
            }
        }
    }

    public bool WieldPistol()
    {
        if (_timeSinceLastWieldS > WieldCooldownS)
        {
            HasPistol = true;
            _timeSinceLastWieldS = 0;
            return true;
        }
        return false;
    }

    public static void CheckPunches(Fighter fighter1, Fighter fighter2)
    {
        var attackBox1 = fighter1.GetAttackHitbox();
        var attackBox2 = fighter2.GetAttackHitbox();
        var hit1 = attackBox1.Area != Fix64.Zero && attackBox1.Intersection(fighter2.GetHitbox()).Area != Fix64.Zero;
        var hit2 = attackBox2.Area != Fix64.Zero && attackBox2.Intersection(fighter1.GetHitbox()).Area != Fix64.Zero;
        if (hit1 && hit2)
        {
            if (fighter1._timeSinceLastAttackS == fighter2._timeSinceLastAttackS)
            {
                fighter1.Stun();
                fighter2.Stun();
            }
            else if (fighter1._timeSinceLastAttackS > fighter2._timeSinceLastAttackS)
            {
                fighter2.Stun();
            }
            else
            {
                fighter1.Stun();
            }
        }
        else if (hit1)
        {
            fighter2.Stun();
        }
        else if (hit2)
        {
            fighter1.Stun();
        }
    }

    public void Kill()
    {
        Action = FighterAction.Dying;
    }
}