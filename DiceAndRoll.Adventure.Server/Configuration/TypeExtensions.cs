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
