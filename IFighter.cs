using Godot;

public interface IFighter
{
    Vector2 Position { get; }
    FighterMovement Movement { get; }
    FighterAction Action { get; }
    bool FacingRight { get; }
    bool HasPistol { get; }
}

public enum FighterMovement
{
    Idle,
    Running,
    Jumping,
    Landing,
    Skidding,
    Wallslide,
    WallslideJump,
}

public enum FighterAction
{
    None,
    Punch,
    Dying,
    Stunned,
}

public static class FighterStateExtensions
{
    public static bool IsAerialState(this FighterMovement state)
    {
        return state == FighterMovement.Jumping
            || state == FighterMovement.Wallslide
            || state == FighterMovement.WallslideJump;
    }
}