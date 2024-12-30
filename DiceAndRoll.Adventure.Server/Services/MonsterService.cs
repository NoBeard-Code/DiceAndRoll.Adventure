using DiceAndRoll.Adventure.Shared.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace DiceAndRoll.Adventure.Server.Services;

public sealed class MonsterService : IMonsterService
{
    private readonly List<MonsterInfo> _monsters = [];

    private async Task LoadMonstersAsync()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var filePath = Path.Combine(path, "Data/monsters.json");

        Console.WriteLine("Loading monsters from file: {0}", filePath);

        // Read the contents of the monsters file and deserialize it
        var jsonData = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, MonsterInfo>>(jsonData)!;

        foreach (var monster in data.Values)
        {
            _monsters.Add(monster);
        }
    }

    public async Task<List<MonsterInfo>> GetMonstersAsync()
    {
        if (_monsters.Count == 0)
        {
            await LoadMonstersAsync();
        }

        return _monsters;
    }

}
