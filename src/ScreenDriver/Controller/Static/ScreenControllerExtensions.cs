using ScreenDriver.Controller.Commands;

namespace ScreenDriver.Controller.Static;

/// <summary>
/// Convenience methods for submitting typed commands to a ScreenController.
/// </summary>
public static class ScreenControllerExtensions
{
    extension(ScreenController controller)
    {
        public void SetBrightness(byte level)
            => controller.EnqueueCommand(new SetBrightnessCommand(level));

        public void FillScreen(byte r, byte g, byte b)
            => controller.EnqueueCommand(new FillScreenCommand(r, g, b));

        public void TurnScreenOff() 
            => controller.EnqueueCommand(new TurnOffScreenCommand());
        
        public void TurnScreenOn()
            => controller.EnqueueCommand(new TurnOnScreenCommand());
    }
}