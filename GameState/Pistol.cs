using Godot;

public interface IPistol
{
    Vector2 Position { get; }
    public bool IsWielded { get; set; }
    bool FacingRight { get; }
}

public class Pistol : Box, IPistol
{
    public const int PistolWidth = 33;
    private const int PistolHeight = 18;
    private const int ShotYOffset = 0;
    private const float Gravity = 0.001f;
    public bool IsLoaded { get; set; }
    public bool FacingRight { get; set; }
    public bool IsWielded { get; set; }
    
    public Pistol(Vector2 position, bool facingRight)
        : base(position, new(PistolWidth, PistolHeight), false)
    {
        FacingRight = facingRight;
        Acceleration = new(0, Gravity);
        IsLoaded = true;
    }

    public void Shoot()
    {
        if (!IsLoaded)
        {
            return;
        }
        IsLoaded = false;
        var bulletX = FacingRight ? Position.X + PistolWidth : Position.X - Bullet.BulletWidth;
        var bulletY = Position.Y + ShotYOffset;
        MovementManager.AddBullet(new Bullet(new(bulletX, bulletY), FacingRight));
    }

    public override void Reset()
    {
        base.Reset();
        Acceleration = new(0, Gravity);
        IsLoaded = true;
        IsWielded = false;
        FacingRight = true;
    }

    public void CopyFrom(Pistol other)
    {
        base.CopyFrom(other);
        IsLoaded = other.IsLoaded;
        IsWielded = other.IsWielded;
        FacingRight = other.FacingRight;
    }

    public override void UpdatePosition(float deltaMs)
    {
        if (IsWielded)
        {
            return;
        }
        base.UpdatePosition(deltaMs);
        if (Position.Y > 681.95f)
        {
            Position = new (Position.X, 681.95f);
            Velocity = new ();
        }
    }
}

public class Bullet : Box
{
    public const int BulletWidth = 20;
    private const int BulletHeight = 13;
    private const float BulletVelocity = 3.0f;
    private const int PositionXMax = 2100;
    private const int PositionXMin = -500;

    public bool FacingRight { get; set; }
    
    public Bullet(Vector2 position, bool facingRight)
        : base(position, new(BulletWidth, BulletHeight), false)
    {
        FacingRight = facingRight;
        var velocityX = FacingRight ? BulletVelocity : -BulletVelocity;
        Velocity = new Vector2(velocityX, 0);
    }

    public override void UpdatePosition(float deltaMs)
    {
        base.UpdatePosition(deltaMs);
        if (Position.X > PositionXMax || Position.X < PositionXMin)
        {
            MovementManager.RemoveBullet();
        }
    }
}