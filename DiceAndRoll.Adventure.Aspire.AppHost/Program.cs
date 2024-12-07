var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.DiceAndRoll_Adventure_Server>("diceandroll-adventure-server");

//var client = builder.AddProject<Projects.DiceAndRoll_Adventure_ConsoleApp>("diceandroll-adventure-consoleapp")
//    .WithReference(server)
//    .WaitFor(server)
//    .ExcludeFromManifest();

var path = new Projects.DiceAndRoll_Adventure_ConsoleApp().ProjectPath;
var dir = Path.GetDirectoryName(new Projects.DiceAndRoll_Adventure_ConsoleApp().ProjectPath);
var projectName = Path.GetFileName(path);

builder.AddExecutable("diceandroll-adventure-client-A", "cmd.exe", dir, $"/c start dotnet run --project {projectName}")
    .WithReference(server)
    .WaitFor(server)
    .ExcludeFromManifest();

builder.AddExecutable("diceandroll-adventure-client-B", "cmd.exe", dir, $"/c start dotnet run --project {projectName}")
    .WithReference(server)
    .WaitFor(server)
    .ExcludeFromManifest();

builder.Build().Run();
