using DiceAndRoll.Adventure.Server;
using DiceAndRoll.Adventure.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var campaignFile = Path.Combine(path, "Data/campaign.json");

if (args.Length > 0)
{
    campaignFile = args[0];
}

if (!File.Exists(campaignFile))
{
    Console.WriteLine("Campaign file not found: {0}", campaignFile);
    return -1;
}

var builder = Host.CreateDefaultBuilder(args);

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("game");
});

builder.ConfigureServices(services => {
    services.AddSingleton<Game>();
    services.AddSingleton<ICharacterService, CharacterService>();
    });

var host = builder.Build();   

await host.StartAsync();

Console.WriteLine("Map file name is '{0}'.", campaignFile);
Console.WriteLine("Setting up Dice & Roll Adventure game, please wait ...");

// Initialize the game world
var adventure = host.Services.GetRequiredService<Game>();
await adventure.StartAsync(campaignFile);

Console.WriteLine("Game setup completed.");
Console.WriteLine("Now you can invite the players to play the game :)...");

Console.WriteLine("Press any key to exit.");
Console.ReadKey();

await host.StopAsync();

return 0;
