namespace DiceAndRoll.Adventure.Server.Configuration;

public static class TypeExtensions
{
    public static List<T> Shuffle<T>(this List<T> list)
    {
        List<T> shuffledList = new(list);
        Random rng = new();
        int n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }
        return shuffledList;
    }

    public static string GetKeyExtension<T>(this T grain) where T : IGrainWithGuidCompoundKey
    {
        grain.GetPrimaryKey(out string keyExtension);
        return keyExtension;
    }
}

public class CurrentList<T> : List<T>
{
    private int _currentIndex = -1;

    public T? Current
    {
        get
        {
            if (_currentIndex >= 0 && _currentIndex < Count)
            {
                return this[_currentIndex];
            }
            return default;
        }
    }

    public void Reset()
    {
        _currentIndex = (Count > 0) ? 0 : - 1;
    }

    public T? MoveNext()
    {
        if (_currentIndex < Count - 1)
        {
            _currentIndex++;
        }
        else
        {
            // Reset the index to the beginning of the list
            _currentIndex = 0;
        }

        return Current;
    }

    public new void Add(T item)
    {
        base.Add(item);
    }

    public new void Remove(T item)
    {
        base.Remove(item);
    }

    public bool IsLast => _currentIndex == Count - 1;
}