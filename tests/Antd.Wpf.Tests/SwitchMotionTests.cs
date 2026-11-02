using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class SwitchMotionTests
{
    [Theory]
    [InlineData(ControlSize.Small, FlowDirection.LeftToRight, 12)]
    [InlineData(ControlSize.Middle, FlowDirection.LeftToRight, 22)]
    [InlineData(ControlSize.Small, FlowDirection.RightToLeft, 12)]
    [InlineData(ControlSize.Middle, FlowDirection.RightToLeft, 22)]
    public void Switch_MotionFinishesAtOfficialEndpointsInBothDirections(ControlSize size, FlowDirection direction, double travel) => Sta.Run(() =>
    {
        var control = new AntSwitch { Size = size, FlowDirection = direction, UseLayoutRounding = false };
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            Border thumb = Thumb(control);
            TranslateTransform translation = Translation(control);
            double before = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, 0), window).X;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
            control.IsChecked = true;
            Assert.True(translation.HasAnimatedProperties);
            Assert.Equal(travel, (double)translation.GetAnimationBaseValue(TranslateTransform.XProperty));
            Sta.WaitUntil(() => !translation.HasAnimatedProperties);
            Assert.Equal(travel, translation.X);
            double after = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, 0), window).X;
            Assert.Equal(direction == FlowDirection.RightToLeft ? -travel : travel, after - before, 5);
            control.IsChecked = false;
            Assert.True(translation.HasAnimatedProperties);
            Sta.WaitUntil(() => !translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_MotionCancellationCoversScopeVisibilityAndUnload() => Sta.Run(() =>
    {
        var control = new AntSwitch { UseLayoutRounding = false };
        var scope = new AntThemeScope { Content = control };
        Window window = Sta.Show(scope);
        try
        {
            TranslateTransform translation = Translation(control);
            control.IsChecked = true;
            Assert.True(translation.HasAnimatedProperties);
            scope.MotionEnabled = false;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(22, translation.X);
            scope.MotionEnabled = true;
            control.IsChecked = false;
            Assert.True(translation.HasAnimatedProperties);
            control.Visibility = Visibility.Hidden;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
            control.IsChecked = true;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(22, translation.X);
            control.Visibility = Visibility.Visible;
            Sta.Pump();
            Assert.False(translation.HasAnimatedProperties);
            control.IsChecked = false;
            Assert.True(translation.HasAnimatedProperties);
            scope.Content = null;
            Sta.Pump();
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
            scope.Content = control;
            Sta.Pump();
            Assert.False(Translation(control).HasAnimatedProperties);
            control.IsChecked = true;
            Assert.True(Translation(control).HasAnimatedProperties);
            Sta.WaitUntil(() => !Translation(control).HasAnimatedProperties);
            Assert.Equal(22, Translation(control).X);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_RapidMotionReversalPreservesBindingAndLatestState() => Sta.Run(() =>
    {
        var source = new CheckBox { IsChecked = false };
        var control = new AntSwitch { UseLayoutRounding = false };
        control.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = source, Mode = BindingMode.TwoWay });
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            TranslateTransform translation = Translation(control);
            for (int index = 0; index < 30; index++)
            {
                control.SetCurrentValue(ToggleButton.IsCheckedProperty, index % 2 == 0);
                Sta.Pump();
                double current = translation.X;
                control.SetCurrentValue(ToggleButton.IsCheckedProperty, control.IsChecked != true);
                Assert.InRange(translation.X, current - 0.01, current + 0.01);
            }
            source.IsChecked = true;
            Sta.Pump();
            Sta.WaitUntil(() => !translation.HasAnimatedProperties);
            Assert.True(control.IsChecked);
            Assert.Equal(22, translation.X);
            Assert.True(BindingOperations.IsDataBound(control, ToggleButton.IsCheckedProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_RetemplateReleasesOldMotionAndAllowsMissingMotionParts() => Sta.Run(() =>
    {
        var control = new AntSwitch { UseLayoutRounding = false };
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            ControlTemplate original = control.Template;
            TranslateTransform previous = Translation(control);
            control.IsChecked = true;
            Assert.True(previous.HasAnimatedProperties);
            control.Template = (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='44' Height='22' /></ControlTemplate>");
            Sta.Pump();
            Assert.False(previous.HasAnimatedProperties);
            control.IsChecked = false;
            Assert.False(control.IsChecked);
            control.IsChecked = true;
            control.Template = original;
            Sta.Pump();
            Assert.NotSame(previous, Translation(control));
            Assert.False(Translation(control).HasAnimatedProperties);
            Assert.Equal(22, Translation(control).X);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_MotionFramesDoNotRemeasureControl() => Sta.Run(() =>
    {
        var control = new MeasureSwitch { Width = 100, CheckedContent = "On", UncheckedContent = "Off", UseLayoutRounding = false };
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            control.IsChecked = true;
            Assert.True(Translation(control).HasAnimatedProperties);
            Sta.Pump();
            int measures = control.Measures;
            Sta.WaitUntil(() => !Translation(control).HasAnimatedProperties);
            Assert.True(measures > 0);
            Assert.Equal(measures, control.Measures);
            Assert.Equal(78, Translation(control).X);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_InitiallyCheckedResizeAndDisabledMotionKeepExactPosition() => Sta.Run(() =>
    {
        var control = new AntSwitch { IsChecked = true, Width = 100, UseLayoutRounding = false };
        var scope = new AntThemeScope { Content = control, MotionEnabled = false };
        Window window = Sta.Show(scope);
        try
        {
            TranslateTransform translation = Translation(control);
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(78, translation.X);
            scope.Size = ControlSize.Small;
            Sta.Pump();
            Assert.Equal(84, translation.X);
            control.Width = 120;
            Sta.Pump();
            Assert.Equal(104, translation.X);
            scope.MotionEnabled = true;
            control.IsChecked = false;
            Assert.True(translation.HasAnimatedProperties);
            control.IsLoading = true;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
            Assert.False(control.IsEnabled);
            control.IsChecked = true;
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(104, translation.X);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Switch_LabelResizeRetargetsActiveMotionInEitherDirection(FlowDirection direction) => Sta.Run(() =>
    {
        var control = new AntSwitch { UncheckedContent = "Off", CheckedContent = "Enabled", FlowDirection = direction, UseLayoutRounding = false };
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            double initialWidth = control.ActualWidth;
            control.IsChecked = true;
            Assert.True(Translation(control).HasAnimatedProperties);
            control.CheckedContent = "Enabled with a long confirmation label";
            Sta.Pump();
            Sta.WaitUntil(() => !Translation(control).HasAnimatedProperties);
            Assert.True(control.ActualWidth > initialWidth + 30);
            Assert.Equal(control.ActualWidth - Thumb(control).ActualWidth - 4, Translation(control).X, 5);
            control.Size = ControlSize.Small;
            Sta.Pump();
            Assert.False(Translation(control).HasAnimatedProperties);
            Assert.Equal(12, Thumb(control).ActualWidth);
            Assert.Equal(control.ActualWidth - 16, Translation(control).X, 5);
        }
        finally { window.Close(); }
    });

    private static Border Thumb(AntSwitch control) => (Border)control.Template.FindName("Thumb", control);
    private static TranslateTransform Translation(AntSwitch control) => Assert.IsType<TranslateTransform>(Thumb(control).RenderTransform);

    private sealed class MeasureSwitch : AntSwitch
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size constraint) { Measures++; return base.MeasureOverride(constraint); }
    }
}
