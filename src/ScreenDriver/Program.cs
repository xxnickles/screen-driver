using ScreenDriver;

// Proof of concept: connect to the screen and fill it with a solid color.
// This is the simplest possible test — if the screen turns red, we know
// the protocol, serial connection, and RGB565 encoding all work correctly.

using var screen = args.Length > 0
    ? new ScreenDevice(args[0])
    : ScreenDevice.AutoDetect();

Console.WriteLine($"Opening connection to screen...");
var sizeId = screen.Initialize();
Console.WriteLine($"Screen responded with size ID: 0x{sizeId:X2} ({sizeId})");

screen.SetBrightness(0); // Max brightness
screen.SetOrientation(0); // Portrait

Console.WriteLine("Filling screen with red...");
screen.FillScreen(255, 0, 0);
Console.WriteLine("Done! Screen should now be solid red.");

Console.WriteLine("Press Enter to fill with blue, or Ctrl+C to exit.");
Console.ReadLine();

screen.FillScreen(0, 0, 255);
Console.WriteLine("Screen should now be solid blue.");

Console.WriteLine("Press Enter to turn off screen and exit.");
Console.ReadLine();

screen.ScreenOff();
