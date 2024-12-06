var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DiceAndRoll_Adventure_Server>("diceandroll-adventure-server");

builder.Build().Run();
