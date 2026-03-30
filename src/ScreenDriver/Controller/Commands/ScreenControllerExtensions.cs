using ScreenDriver.Device;

namespace ScreenDriver.Controller.Commands;

/// <summary>
/// Convenience methods for submitting typed commands to a ScreenController.
/// </summary>
public static class ScreenControllerExtensions
{
    extension(ScreenController controller)
    {
        public void SetOrientation(ScreenOrientation orientation)
            => controller.EnqueueCommand(new SetOrientationCommand(orientation));

        public void SetBrightness(byte level)
            => controller.EnqueueCommand(new SetBrightnessCommand(level));

        public void FillScreen(byte r, byte g, byte b)
            => controller.EnqueueCommand(new FillScreenCommand(r, g, b));
    }
}
