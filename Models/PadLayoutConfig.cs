namespace StardewSurfacePad.Models;

public sealed class ElementPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class PadLayoutConfig
{
    public double GlobalOpacity { get; set; } = 0.88;
    public double ControlScale { get; set; } = 1.0;
    public bool BlockTouchMouse { get; set; } = true;

    public ElementPosition LeftShoulders { get; set; } = new();
    public ElementPosition RightShoulders { get; set; } = new();
    public ElementPosition SystemBar { get; set; } = new();
    public ElementPosition LeftStick { get; set; } = new();
    public ElementPosition DPad { get; set; } = new();
    public ElementPosition FaceButtons { get; set; } = new();
    public ElementPosition RightStick { get; set; } = new();

    /// <summary>
    /// Creates a Surface-ergonomic Switch Pro asymmetric default layout scaled to the screen dimensions.
    /// </summary>
    public static PadLayoutConfig CreateDefault(double screenWidth, double screenHeight)
    {
        double marginX = Math.Max(18, screenWidth * 0.018);
        double topMargin = Math.Max(10, screenHeight * 0.012);

        return new PadLayoutConfig
        {
            GlobalOpacity = 0.88,
            ControlScale = 1.0,
            LeftShoulders = new ElementPosition { X = 12, Y = 8 },
            RightShoulders = new ElementPosition { X = Math.Max(0, screenWidth - 278), Y = 8 },
            SystemBar = new ElementPosition { X = Math.Max(0, (screenWidth - 210) / 2.0), Y = topMargin },
            LeftStick = new ElementPosition { X = marginX + 18, Y = Math.Max(110, screenHeight * 0.35) },
            DPad = new ElementPosition { X = marginX + 115, Y = Math.Max(305, screenHeight - 210) },
            FaceButtons = new ElementPosition { X = Math.Max(0, screenWidth - marginX - 245), Y = Math.Max(110, screenHeight * 0.32) },
            RightStick = new ElementPosition { X = Math.Max(0, screenWidth - marginX - 255), Y = Math.Max(330, screenHeight - 196) }
        };
    }
}
