using ScreenDriver.Device;
using ScreenDriver.Widgets;

namespace ScreenDriver.Themes;

public abstract record Theme(ScreenLayoutMode LayoutMode, Widget[] Widgets);
