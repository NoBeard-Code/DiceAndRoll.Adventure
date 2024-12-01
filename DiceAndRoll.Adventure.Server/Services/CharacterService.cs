using DiceAndRoll.Adventure.Shared.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace DiceAndRoll.Adventure.Server.Services;

public sealed class CharacterService : ICharacterService
{
    private readonly List<CharacterInfo> _characters = [];

    private async Task LoadCharactersAsync()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var filePath = Path.Combine(path, "Data/characters.json");

        Console.WriteLine("Loading characters from file: {0}", filePath);

        // Read the contents of the characters file and deserialize it
        var jsonData = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, CharacterInfo>>(jsonData)!;

        foreach (var character in data.Values)
        {
            _characters.Add(character);
        }
    }

    public async Task<List<CharacterInfo>> GetCharactersAsync()
    {
        if (_characters.Count == 0)
        {
            await LoadCharactersAsync();
        }

        return _characters;
    }
}
