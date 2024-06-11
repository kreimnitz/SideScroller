using System;
using System.Collections.Generic;
using Godot;

public class ClientGameStateFactory : IClientGameStateFactory<ClientGameState>
{
    public ClientGameState Create()
    {
        return new ClientGameState();
    }
}

public interface IBoxManager
{

}

public class ClientGameState : IClientGameState
{
    private const int InitialXLeft = 135;
    private const int InitialXRight = 1410;
    private const float InitialY = 619.95f;
    private MovementManager _movementManager = new MovementManager();
    public Fighter3 Fighter0 { get; private set; }
    public Fighter3 Fighter1 { get; private set; }
    public Pistol Pistol { get; private set; }
    public Bullet Bullet => _movementManager.GetBullet();
    public ClientGameState()
    {
        Fighter0 = new Fighter3(new Vector2(InitialXLeft, InitialY), true);
        Fighter1 = new Fighter3(new Vector2(InitialXRight, InitialY), false);
        Pistol = new Pistol(new Vector2(650, 0), true);
        _movementManager.SetPistol(Pistol);
        _movementManager.AddFighter(Fighter0);
        _movementManager.AddFighter(Fighter1);
    }

    private void Reset()
    {
        Fighter0.Reset();
        Fighter1.Reset();
        Pistol.Reset();
        _movementManager = new MovementManager();
        _movementManager.SetPistol(Pistol);
        _movementManager.AddFighter(Fighter0);
        _movementManager.AddFighter(Fighter1);
    }

    public void ApplyInput(GameInput[] input, double deltaS, bool isPredict)
    {
        if (input.Length == 1)
        {
            var first = input[0];
            input = new GameInput[2];
            input[0] = first;
            input[1] = new();
        }
        if (input[0].PressedKeys.Contains(Key.Z))
        {
            Reset();
        }
        var deltaMs = (float)(deltaS * 1000);
        _movementManager.AdvanceState(input, deltaMs);
    }

    public void CopyFrom(IClientGameState otherInterface)
    {
        if (otherInterface is ClientGameState other)
        {
            Fighter0.CopyFrom(other.Fighter0);
            Fighter1.CopyFrom(other.Fighter1);
            Pistol.CopyFrom(other.Pistol);
            _movementManager.CopyFrom(other._movementManager);
            CopyBullet(other);
        }
    }

    private void CopyBullet(ClientGameState other)
    {
        if (Bullet == null && other.Bullet != null)
        {
            var bullet = new Bullet(other.Bullet.Position, other.Bullet.FacingRight);
            _movementManager.AddBullet(bullet);
        }
        else if (Bullet != null && other.Bullet != null)
        {
            Bullet.CopyFrom(other.Bullet);
        }
        else if (Bullet != null && other.Bullet == null)
        {
            _movementManager.RemoveBullet();
        }
    }
}