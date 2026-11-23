using System.Windows;
using System.Windows.Input;

namespace StardewSurfacePad.Controls;

/// <summary>
/// Helpers to distinguish true mouse/pen events from touch-promoted mouse events in WPF.
/// </summary>
internal static class TouchInputHelper
{
    public static bool IsPureMouseOrPen(MouseEventArgs e) => e.StylusDevice is null;

    public static void DisableSystemTouchFeedback(DependencyObject element)
    {
        Stylus.SetIsPressAndHoldEnabled(element, false);
        Stylus.SetIsFlicksEnabled(element, false);
        Stylus.SetIsTapFeedbackEnabled(element, false);
        Stylus.SetIsTouchFeedbackEnabled(element, false);
    }
}
