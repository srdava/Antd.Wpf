using System;
using System.Windows;
using System.Windows.Controls;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Internal;

internal sealed class InputScrollViewer : ScrollViewer
{
    static InputScrollViewer()
    {
        MinHeightProperty.OverrideMetadata(typeof(InputScrollViewer), new FrameworkPropertyMetadata(0d, null,
            (sender, value) => CoerceLineBound(sender, value, true)));
        MaxHeightProperty.OverrideMetadata(typeof(InputScrollViewer), new FrameworkPropertyMetadata(double.PositiveInfinity, null,
            (sender, value) => CoerceLineBound(sender, value, false)));
    }

    internal void UpdateLineBounds()
    {
        CoerceValue(MinHeightProperty);
        CoerceValue(MaxHeightProperty);
    }

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);
        if (e.ViewportHeightChange != 0)
        {
            UpdateLineBounds();
        }
    }

    private static object CoerceLineBound(DependencyObject sender, object value, bool minimum)
    {
        var viewer = (InputScrollViewer)sender;
        if (viewer.TemplatedParent is not AntInput input || !input.AcceptsReturn)
        {
            return value;
        }

        // TextBox uses font metrics for row limits; our multiline editor uses the explicit theme line pitch.
        double lineHeight = TextBlock.GetLineHeight(input);
        if (double.IsNaN(lineHeight))
        {
            return value;
        }

        if (input.ReadLocalValue(HeightProperty) != DependencyProperty.UnsetValue ||
            input.ReadLocalValue(MinHeightProperty) != DependencyProperty.UnsetValue ||
            input.ReadLocalValue(MaxHeightProperty) != DependencyProperty.UnsetValue)
        {
            return minimum ? 0d : double.PositiveInfinity;
        }

        int lines = minimum ? input.MinLines : input.MaxLines;
        double chrome = Math.Max(0, viewer.ActualHeight - viewer.ViewportHeight);
        return lines == int.MaxValue ? double.PositiveInfinity : lineHeight * lines + chrome;
    }
}
