using DiceAndRoll.Adventure.Shared.Models;
using Spectre.Console;

namespace DiceAndRoll.Adventure.ConsoleApp;

internal static class ConsoleOutput
{
    //var x = new List<CharacterInfo>
    //        {
    //            new("hedge_knight", "Hedge Knight", "Some background herwerewrewr ewr ewr we rew rew reweewr ewr ewr ewr ewr ew",
    //            "Some description hereerewrewr ewre wrew rew rew rewr ewrew rew", 100, 2, 8, "melee", [ "leather_coat", "hardened_coat", "gunslingers_coat", "sherrifs_coat" ], [ "revolvers", "hunting_revolvers", "sherrifs_sidearm", "peacemaker" ], [ "ricochet", "bullseye", "disarm", "playing_dirty" ], "leather_coat", "revolvers"),
    //            new("hedge_knight", "Hedge BNEE", "Some background ewr ewre rewr ew rew rew rew rewr ewrew wre wrew rew here",
    //            "Some descriptionerwr ewrewr ewrew rewewr ewr ewrew ewrew here", 80, 4, 4, "ranged", [], [], [], "weak_armor", "weak_weapon"),
    //            new("hedge_knight", "Hedge rrere", "Some background hereewewr ewrwer rew rwe rewr ewr wer ewr we rew rew rew rew rew rew",
    //            "Some description here...", 90, 5, 10, "melee", [], [], [], "weak_armor", "weak_weapon")
    //        };

    //DisplayCharacters(x);

    internal static void DisplayInitiativeRow()
    {
        AnsiConsole.MarkupLine("[bold yellow]Initiative Row:[/]");

        var columns = new List<Text>(){
            new Text("Item 1", new Style(Color.Red, Color.Black)),
            new Text("Item 2", new Style(Color.Green, Color.Black)),
            new Text("Item 3", new Style(Color.Blue, Color.Black))
        };

        AnsiConsole.Write(new Columns(columns));
    }

    internal static Panel DisplayBeligerent(bool isPlayer)
    {
        var panel = new Panel("Hello World");
        panel.Header = new PanelHeader("Player");
        panel.Border = BoxBorder.Rounded;
        panel.Padding = new Padding(1, 1, 1, 1);
        panel.Expand();

        return panel;
    }

    internal static void DisplayCharacters(List<CharacterInfo> characters, bool extendedView = false)
    {
        var table = new Table();
        table.Title("[bold blue underline]List of characters[/]");
        table.Border = TableBorder.AsciiDoubleHead;

        table.AddColumns([new TableColumn("Title"), new TableColumn("Description"), new TableColumn("Type"),
        new TableColumn("Health"), new TableColumn("Attack"), new TableColumn("Defense")]);

        foreach (var character in characters)
        {
            table.AddRow(new Markup($"[olive bold]{character.Title}[/]"), new Markup(character.Description), new Markup($"[blue]{character.Type}[/]"),
                DisplayHealthBar(character.Health), DisplayAttackBar(character.Attack), DisplayDefenseBar(character.Defense)
                );
        }

        AnsiConsole.Write(table);

        if (extendedView)
        {
            var table2 = new Table();
            table2.Title("[bold blue](details)[/]");
            table2.Border = TableBorder.Ascii;

            table2.AddColumns([new TableColumn("Title"), new TableColumn("Story"),
                new TableColumn("Armors"), new TableColumn("Weapons"), new TableColumn("Specials"), new TableColumn("Starting Equipment")]);

            foreach (var character in characters)
            {
                table2.AddRow(new Markup($"[olive bold]{character.Title}[/]"), new Markup($"[grey]{character.Story}[/]"),
                    new Rows(character.Armors.Select(x => new Text(x))), new Rows(character.Weapons.Select(x => new Text(x))),
                    new Rows(character.Specials.Select(x => new Text(x))),
                    new Rows(new Markup($"[lime bold]{character.Weapon}[/]"), new Markup($"[lime]{character.Armor}[/]"))
                    );
            }

            AnsiConsole.Write(table2);
        }
    }

    private static BarChart DisplayHealthBar(double value)
    {
        return new BarChart()
            .AddItem("HP", value, Color.Red)
            .WithMaxValue(100);
    }

    private static BarChart DisplayAttackBar(double value)
    {
        return new BarChart()
            .AddItem("ATT", value, Color.Orange1)
            .WithMaxValue(10);
    }

    private static BarChart DisplayDefenseBar(double value)
    {
        return new BarChart()
            .AddItem("DEF", value, Color.Yellow)
            .WithMaxValue(5);
    }

    internal static void DisplayStats(bool displayAdvancedView = false)
    {
        var panel = new Panel("ttt")
        {
            Header = new PanelHeader("Player stats"),
            Border = BoxBorder.Rounded,
            Expand = true

        };
        AnsiConsole.Write(panel);

        if (!displayAdvancedView) return;

        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left"),
                new Layout("Right")
                    .SplitRows(
                        new Layout("Top"),
                        new Layout("Bottom")));

        // Update the left column
        layout["Left"].Update(
            new Panel(
                Align.Center(
                    new Markup("Hello [blue]World![/]"),
                    VerticalAlignment.Middle))
                .Expand());

        // Render the layout
        AnsiConsole.Write(layout);
    }
}
