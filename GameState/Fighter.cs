using System;
using System.Collections.Generic;
using Godot;

public class Fighter : Box, IFighter
{
    public const int FighterWidth = 55;
    private const int FighterHeight = 80;
    private const float FighterVelocityXMax = 0.8f;
    private const float CoastAcceleration = 0.0011f;
    private const float RunAcceleration = 0.0015f;
    private const float SkidAcceleration = 0.003f;
    private const float JumpVelocity = -1.2f;
    private const float Gravity = 0.003f;
    private const float SlideDrag = Gravity / 2;
    private const float JumpCooldownMs = 300;
    private const float AttackCooldownMs = 500;
    private const float AttackActionDurationMs = 300;
    private const float PunchHitboxDurationMs = 50;
    private const float StunDurationMs = 500;
    private const float StunCooldownMs = 1000;
    private const float WieldCooldownMs = 1000;
    private const int PistolOffsetX = 3;
    private const int PistolOffsetY = 30;
    private Rect2 _punchHitbox = new(8, 34, 96, 40);
    private bool _stopping;
    private float _timeSinceLastJumpMs;
    private float _timeSinceLastStunMs = 5000;
    private float _timeSinceLastAttackMs = 5000;
    private float _timeSinceLastWieldMs = 5000;
    public FighterMovement Movement { get; protected set; } = FighterMovement.Idle;
    public FighterAction Action { get; protected set; } = FighterAction.None;
    public bool FacingRight { get; set; }
    public bool HasPistol { get; set; }

    public Fighter(Vector2 position, bool facingRight)
        : base(position, new Vector2(FighterWidth, FighterHeight), false)
    {
        FacingRight = facingRight;
        Acceleration = new(0, Gravity);
        VelocityCap = new(FighterVelocityXMax, float.PositiveInfinity);
    }

    public void CopyFrom(Fighter other)
    {
        base.CopyFrom(other);
        _stopping = other._stopping;
        _timeSinceLastJumpMs = other._timeSinceLastJumpMs;
        _timeSinceLastStunMs = other._timeSinceLastStunMs;
        _timeSinceLastAttackMs = other._timeSinceLastAttackMs;
        _timeSinceLastWieldMs = other._timeSinceLastWieldMs;
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

    public void ApplyInput(GameInput input, float deltaMs)
    {
        SetAction(input, deltaMs);
        SetAccelerationAndState(input, deltaMs);
        if (Movement == FighterMovement.Wallslide && Action == FighterAction.Punch)
        {
            Action = FighterAction.None;
        }
    }

    public override void UpdateVelocity(float deltaMs)
    {
        base.UpdateVelocity(deltaMs);
        if (_stopping)
        {
            Velocity = new(0, Velocity.Y);
            _stopping = false;
        }
        if (Action != FighterAction.Dying)
        {
            if (Velocity.X > 0)
            {
                FacingRight = true;
            }
            if (Velocity.X < 0)
            {
                FacingRight = false;
            }
        }
    }

    public override void UpdatePosition(float deltaMs)
    {
        base.UpdatePosition(deltaMs);
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

    public Rect2 GetAttackHitbox()
    {
        if (Action != FighterAction.Punch || _timeSinceLastAttackMs > PunchHitboxDurationMs)
        {
            return default;
        }
        var punchOffsetX = FacingRight ? _punchHitbox.Position.X : -_punchHitbox.Position.X - _punchHitbox.Size.X;
        return new(Position.X + punchOffsetX, Position.Y + _punchHitbox.Position.Y, _punchHitbox.Size);
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

    private void SetAccelerationAndState(GameInput input, float deltaMs)
    {
        var keys = new HashSet<Key>(input.PressedKeys);
        if (Action == FighterAction.Stunned || Action == FighterAction.Dying)
        {
            keys.Clear();
        }
        _timeSinceLastJumpMs += deltaMs;
        _stopping = false;
        float accelX;
        float accelY = Gravity;
        FighterMovement groundState = FighterMovement.Idle;
        if (keys.Contains(Key.Left) && !keys.Contains(Key.Right))
        {
            RemoveAnchor(Side.Right);
            if (Movement.IsAerialState())
            {
                accelX = Velocity.X > 0 ? -RunAcceleration : -RunAcceleration;
            }
            else
            {
                accelX = Velocity.X > 0 ? -SkidAcceleration : -RunAcceleration;
                groundState = Velocity.X > 0 ? FighterMovement.Skidding : FighterMovement.Running;
            }
        }
        else if (keys.Contains(Key.Right) && !keys.Contains(Key.Left))
        {
            RemoveAnchor(Side.Left);
            if (Movement.IsAerialState())
            {
                accelX = Velocity.X < 0 ? RunAcceleration : RunAcceleration;
            }
            else
            {
                accelX = Velocity.X < 0 ? SkidAcceleration : RunAcceleration;
                groundState = Velocity.X < 0 ? FighterMovement.Skidding : FighterMovement.Running;
            }
        }
        else if (Velocity.X != 0)
        {
            if (Velocity.Abs().X < CoastAcceleration * deltaMs)
            {
                _stopping = true;
                groundState = FighterMovement.Idle;
                accelX = 0;
            }
            else
            {
                accelX = Velocity.X > 0 ? -CoastAcceleration : CoastAcceleration;
                groundState = FighterMovement.Running;
            }
        }
        else
        {
            accelX = 0;
            groundState = FighterMovement.Idle;
        }

        var shouldJump = _timeSinceLastJumpMs > JumpCooldownMs && keys.Contains(Key.Space);
        if (shouldJump && Movement == FighterMovement.Wallslide)
        {
            _timeSinceLastJumpMs = 0;
            Movement = FighterMovement.WallslideJump;
            var xV = GetAnchor(Side.Left) != null ? FighterVelocityXMax : -FighterVelocityXMax;
            accelX = GetAnchor(Side.Left) != null ? RunAcceleration : -RunAcceleration;
            Velocity = new(xV, JumpVelocity);
            RemoveAnchors();
        }
        else if (shouldJump && GetAnchor(Side.Bottom) != null)
        {
            _timeSinceLastJumpMs = 0;
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
            var jumpV = (float)Math.Clamp(Velocity.Y + JumpVelocity, JumpVelocity * 1.5, -JumpVelocity * 1.5);
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
                var slideDrag = Velocity.Y > 0 ? SlideDrag : -SlideDrag;
                accelY -= slideDrag;
            }
            else
            {
                Movement = FighterMovement.WallslideJump;
            }
        }
        Acceleration = new(accelX, accelY);
    }

    private void SetAction(GameInput input, float deltaMs)
    {
        _timeSinceLastAttackMs += deltaMs;
        _timeSinceLastStunMs += deltaMs;
        _timeSinceLastWieldMs += deltaMs;
        if (Action == FighterAction.Dying)
        {
            return;
        }

        if (Action == FighterAction.Stunned)
        {
            if (_timeSinceLastStunMs > StunDurationMs)
            {
                Action = FighterAction.None;
            }
            else
            {
                return;
            }
        }
        if (Action == FighterAction.Punch && _timeSinceLastAttackMs > AttackActionDurationMs)
        {
            Action = FighterAction.None;
        }
        else if (_timeSinceLastAttackMs > AttackCooldownMs && input.PressedKeys.Contains(Key.X))
        {
            _timeSinceLastAttackMs = 0;
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
        if (_timeSinceLastStunMs > StunCooldownMs && Action != FighterAction.Dying)
        {
            Action = FighterAction.Stunned;
            _timeSinceLastStunMs = 0;
            if (HasPistol)
            {
                MovementManager.GetPistol().IsWielded = false;
                HasPistol = false;
            }
        }
    }

    public bool WieldPistol()
    {
        if (_timeSinceLastWieldMs > WieldCooldownMs)
        {
            HasPistol = true;
            _timeSinceLastWieldMs = 0;
            return true;
        }
        return false;
    }

    public static void CheckPunches(Fighter fighter1, Fighter fighter2)
    {
        var attackBox1 = fighter1.GetAttackHitbox();
        var attackBox2 = fighter2.GetAttackHitbox();
        var hit1 = attackBox1 != default && attackBox1.Intersection(fighter2.GetHitbox()) != default;
        var hit2 = attackBox2 != default && attackBox2.Intersection(fighter1.GetHitbox()) != default;
        if (hit1 && hit2)
        {
            if (fighter1._timeSinceLastAttackMs == fighter2._timeSinceLastAttackMs)
            {
                fighter1.Stun();
                fighter2.Stun();
            }
            else if (fighter1._timeSinceLastAttackMs > fighter2._timeSinceLastAttackMs)
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