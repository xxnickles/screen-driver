using ScreenDriver.Controller;
using ScreenDriver.Controller.Commands;
using ScreenDriver.Device;
using ScreenDriver.Themes;
using Spectre.Console;

namespace ScreenDriver.Tui;

public class CommandPanel(ScreenController controller, ThemeRegistry registry)
{
    /// <summary>
    /// Shows the interactive menu in a loop. Returns when the user picks "Back to log".
    /// </summary>
    public async Task ShowMenu()
    {
        while (true)
        {
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[blue]Commands[/]")
                    .AddChoices(
                        "Screen On",
                        "Screen Off",
                        "Orientation",
                        "Theme",
                        "Back to log"));

            switch (action)
            {
                case "Screen On":
                    controller.EnqueueCommand(new SetBrightnessCommand(0));
                    AnsiConsole.MarkupLine("[green]Screen turned on.[/]");
                    break;

                case "Screen Off":
                    controller.EnqueueCommand(new SetBrightnessCommand(255));
                    AnsiConsole.MarkupLine("[green]Screen turned off.[/]");
                    break;

                case "Orientation":
                    await ShowOrientationMenu();
                    break;

                case "Theme":
                    await ShowThemeMenu();
                    break;

                case "Back to log":
                    return;
            }
        }
    }

    private async Task ShowOrientationMenu()
    {
        var orientations = Enum.GetValues<ScreenOrientation>();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<ScreenOrientation>()
                .Title("[blue]Select Orientation[/]")
                .AddChoices(orientations));

        controller.EnqueueCommand(new SetOrientationCommand(choice));
        AnsiConsole.MarkupLine($"[green]Orientation set to {choice}.[/]");
    }

    private async Task ShowThemeMenu()
    {
        var themes = registry.GetAvailableThemes();
        var active = controller.ActiveTheme;

        var choices = themes.Select(t =>
            t == active ? $"* {t} (active)" : $"  {t}").ToArray();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Select Theme[/]")
                .AddChoices(choices));

        // Extract theme name from the choice string
        var name = choice.TrimStart('*', ' ').Replace(" (active)", "").Trim();

        if (name != active)
        {
            await controller.SwapTheme(name);
            AnsiConsole.MarkupLine($"[green]Theme switched to '{name}'.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[grey]Already on '{name}'.[/]");
        }
    }
}
