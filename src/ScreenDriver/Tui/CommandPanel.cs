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
                        Constants.ScreenOrientationCommand,
                        Constants.ScreenThemeCommand,
                        Constants.ScreenBrightnessCommand,
                        Constants.BackCommand));

            switch (action)
            {
                case Constants.ScreenOrientationCommand:
                    ShowOrientationMenu();
                    break;

                case Constants.ScreenThemeCommand:
                    await ShowThemeMenu();
                    break;

                case Constants.ScreenBrightnessCommand:
                    ShowBrightnessMenu();
                    break;

                case Constants.BackCommand:
                    return;
            }
        }
    }

    private void ShowOrientationMenu()
    {
        var orientations = Enum.GetValues<ScreenOrientation>();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<ScreenOrientation>()
                .Title("[blue]Select Orientation[/]")
                .AddChoices(orientations));

        controller.SetOrientation(choice);
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

    private void ShowBrightnessMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Select Screen Brightness[/]")
                .AddChoices(Constants.BrightnessOptions.Keys));

        controller.SetBrightness(Constants.BrightnessOptions[choice]);
        AnsiConsole.MarkupLine($"[green]Brightness set to {choice}.[/]");
    }
}
