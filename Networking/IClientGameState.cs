public interface IClientGameState
{
    public void ApplyInput(GameInput[] input, double deltaS, bool isPredict);

    public void CopyFrom(IClientGameState other);
}

public interface IClientGameStateFactory<T> where T : IClientGameState
{
    public T Create();
}