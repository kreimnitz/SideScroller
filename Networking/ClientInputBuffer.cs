using System.Collections.Generic;

public class ClientInputBuffer
{
    private int _nextInputId = 1;
    private readonly Queue<LocalInputSnapshot> _inputQueue = new();
    public IEnumerable<LocalInputSnapshot> Snapshots => _inputQueue;
    public int AddInput(GameInput gameInput, double deltaS)
    {
        var snapshot = new LocalInputSnapshot(gameInput, deltaS, _nextInputId++);
        _inputQueue.Enqueue(snapshot);
        return snapshot.Id;
    }

    public void TrimIdAndBefore(int id)
    {
        while (_inputQueue.Count > 0 && _inputQueue.Peek().Id <= id)
        {
            _inputQueue.Dequeue();
        }
    }
}

public class LocalInputSnapshot
{
    public int Id { get; set; }
    public double DeltaS { get; set; }
    public GameInput GameInput { get; set; }

    public LocalInputSnapshot(GameInput gameInput, double deltaS, int id)
    {
        Id = id;
        GameInput = gameInput;
        DeltaS = deltaS;
    }
}