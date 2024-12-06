var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.DiceAndRoll_Adventure_Server>("diceandroll-adventure-server");

var client = builder.AddProject<Projects.DiceAndRoll_Adventure_ConsoleApp>("diceandroll-adventure-consoleapp")
    .WithReference(server)
    .WaitFor(server);

builder.Build().Run();
