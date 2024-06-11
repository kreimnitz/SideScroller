using System.Collections.Generic;
using System.Linq;

public class SnapshotHistory<T> where T : ISequentialSnapshot
{
    private int _highestPoppedId;
    private int _highestAddedId;
    public HashSet<int> MissingSnapshots { get; set; } = new();
    public Dictionary<int, T> Snapshots { get; set; } = new();

    public List<T> PopValidSnapshots()
    {
        var validSnapshots = GetValidSnapshots();
        foreach (var snapshot in validSnapshots)
        {
            Snapshots.Remove(snapshot.Id);
            if (_highestPoppedId < snapshot.Id)
            {
                _highestPoppedId = snapshot.Id;
            }
        }
        return validSnapshots;
    }

    public bool HasValidSnapshots()
    {
        return GetValidSnapshots().Count > 0;
    }

    private List<T> GetValidSnapshots()
    {
        var validSnapshots = new List<T>();
        if (Snapshots.Count == 0)
        {
            return validSnapshots;
        }

        if (MissingSnapshots.Count == 0)
        {
            foreach (var key in Snapshots.Keys.OrderBy(x => x))
            {
                validSnapshots.Add(Snapshots[key]);
            }
            return validSnapshots;
        }
        int minMissingId = MissingSnapshots.Min() - 1;
        var minSnapshotId = Snapshots.Keys.Min();
        if (minMissingId < minSnapshotId)
        {
            return validSnapshots;
        }

        for (int i = minSnapshotId; i < minMissingId; i++)
        {
            validSnapshots.Add(Snapshots[i]);
        }
        return validSnapshots;
    }

    public void AddSnapshot(T snapshot)
    {
        if (Snapshots.ContainsKey(snapshot.Id) || _highestAddedId >= snapshot.Id)
        {
            return;
        }
        Snapshots.Add(snapshot.Id, snapshot);
        MissingSnapshots.Remove(snapshot.Id);
        if (_highestAddedId < snapshot.Id)
        {
            AddMissingSnapshotsBetweenIds(_highestAddedId, snapshot.Id);
            _highestAddedId = snapshot.Id;
        }
    }

    private void AddMissingSnapshotsBetweenIds(int lowId, int highId)
    {
        for (int i = lowId + 1; i < highId; i++)
        {
            MissingSnapshots.Add(i);
        }
    }
}