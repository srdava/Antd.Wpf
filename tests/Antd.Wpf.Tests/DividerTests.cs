using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class DividerTests
{
    [Fact]
    public void Divider_DefaultTemplateLoadsFromPackResource() => Sta.Run(() =>
    {
        var scope = new AntThemeScope();
        var dictionary = new ResourceDictionary { Source = new Uri("/Antd.Wpf;component/Themes/Controls/Divider.xaml", UriKind.Relative) };
        var style = (Style)dictionary[typeof(AntDivider)];
        var template = (ControlTemplate)style.Setters.OfType<Setter>().Single(setter => setter.Property == Control.TemplateProperty).Value;
        Assert.NotNull(template);
        var divider = new AntDivider { Content = "Diagnostic", Style = style };
        scope.Content = divider;
        Window window = Sta.Show(scope);
        try { Assert.NotNull(divider.Template); Assert.NotNull(Part(divider)); }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ControlSize.Small, false, 8)]
    [InlineData(ControlSize.Middle, false, 16)]
    [InlineData(ControlSize.Large, false, 24)]
    [InlineData(ControlSize.Small, true, 8)]
    [InlineData(ControlSize.Middle, true, 16)]
    [InlineData(ControlSize.Large, true, 16)]
    public void Divider_OfficialMarginsAndTitleTypography(ControlSize size, bool title, double margin) => Sta.Run(() =>
    {
        var divider = new AntDivider { Size = size, Content = title ? "Title" : null };
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            Assert.Equal(new Thickness(0, margin, 0, margin), divider.Margin);
            Assert.Equal(title ? 16 : 14, divider.FontSize);
            Assert.Equal(title ? FontWeights.Medium : FontWeights.Normal, divider.FontWeight);
            Assert.Equal(new Thickness(divider.FontSize, 0, divider.FontSize, 0), divider.Padding);
            Assert.Equal(divider.FontSize * 22 / 14, TextBlock.GetLineHeight(divider));
            Assert.Equal(title, divider.HasTitle);
            Assert.False(divider.Focusable);
            Assert.False(divider.IsTabStop);
            if (title)
            {
                divider.IsPlain = true;
                Sta.Pump();
                Assert.Equal(14, divider.FontSize);
                Assert.Equal(new Thickness(14, 0, 14, 0), divider.Padding);
                Assert.Equal(FontWeights.Normal, divider.FontWeight);
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_DefaultSpacingAndExplicitScopeSizeAreDistinct() => Sta.Run(() =>
    {
        var divider = new AntDivider();
        var scope = new AntThemeScope { Content = divider };
        Window window = Sta.Show(scope);
        try
        {
            Assert.Equal(ControlSize.Large, divider.Size);
            Assert.Equal(new Thickness(0, 24, 0, 24), divider.Margin);
            scope.Size = ControlSize.Small;
            Sta.Pump();
            Assert.Equal(ControlSize.Small, divider.Size);
            Assert.Equal(new Thickness(0, 8, 0, 8), divider.Margin);
            divider.Size = ControlSize.Large;
            Sta.Pump();
            Assert.Equal(new Thickness(0, 24, 0, 24), divider.Margin);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_FontTokensAndLocalPaddingSurviveThemeAndSizeChanges() => Sta.Run(() =>
    {
        var divider = new AntDivider { Content = "Custom typography" };
        var scope = new AntThemeScope { Content = divider };
        scope.Resources["Antd.FontSize"] = 18d;
        scope.Resources[new ComponentResourceKey(typeof(AntThemeScope), "Antd.FontSize.Large")] = 24d;
        Window window = Sta.Show(scope);
        try
        {
            Assert.Equal(24, divider.FontSize);
            Assert.Equal(new Thickness(24, 0, 24, 0), divider.Padding);
            divider.IsPlain = true;
            Sta.Pump();
            Assert.Equal(18, divider.FontSize);
            Assert.Equal(new Thickness(18, 0, 18, 0), divider.Padding);
            divider.Padding = new Thickness(2);
            scope.Mode = ThemeMode.Dark;
            scope.Size = ControlSize.Small;
            Sta.Pump();
            Assert.Equal(18, divider.FontSize);
            Assert.Equal(new Thickness(2), divider.Padding);
            Assert.Equal(new Thickness(0, 8, 0, 8), divider.Margin);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(Orientation.Horizontal, DividerVariant.Solid)]
    [InlineData(Orientation.Horizontal, DividerVariant.Dashed)]
    [InlineData(Orientation.Horizontal, DividerVariant.Dotted)]
    [InlineData(Orientation.Vertical, DividerVariant.Solid)]
    [InlineData(Orientation.Vertical, DividerVariant.Dashed)]
    [InlineData(Orientation.Vertical, DividerVariant.Dotted)]
    public void Divider_RendersEachLineVariantInBothOrientations(Orientation orientation, DividerVariant variant) => Sta.Run(() =>
    {
        var divider = new AntDivider
        {
            Orientation = orientation,
            Variant = variant,
            BorderBrush = Brushes.Black,
            Margin = new Thickness(0),
            UseLayoutRounding = false,
        };
        if (orientation == Orientation.Horizontal) { divider.Width = 120; }
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            FrameworkElement part = Part(divider);
            byte[] pixels = Raster(part, out int width, out int height);
            int x = orientation == Orientation.Horizontal ? 16 : width / 2;
            int y = orientation == Orientation.Horizontal ? height / 2 : 16;
            Assert.Equal(variant != DividerVariant.Dashed, pixels[(y * width + x) * 4 + 3] > 0);
            if (variant == DividerVariant.Dotted)
            {
                x = orientation == Orientation.Horizontal ? 4 : width / 2;
                y = orientation == Orientation.Horizontal ? height / 2 : 4;
                Assert.Equal(0, pixels[(y * width + x) * 4 + 3]);
            }

            Assert.Equal(1, VisualTreeHelper.GetChildrenCount(part));
            if (orientation == Orientation.Vertical)
            {
                Assert.InRange(divider.ActualHeight, 12, 13.5);
                Assert.Equal(new Thickness(0), divider.Margin);
                Assert.Equal(-0.84, ((TranslateTransform)divider.RenderTransform).Y, 8);
            }
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight, DividerTitlePlacement.Start)]
    [InlineData(FlowDirection.LeftToRight, DividerTitlePlacement.Center)]
    [InlineData(FlowDirection.LeftToRight, DividerTitlePlacement.End)]
    [InlineData(FlowDirection.RightToLeft, DividerTitlePlacement.Start)]
    [InlineData(FlowDirection.RightToLeft, DividerTitlePlacement.Center)]
    [InlineData(FlowDirection.RightToLeft, DividerTitlePlacement.End)]
    public void Divider_TitlePlacementMirrorsInRtl(FlowDirection direction, DividerTitlePlacement placement) => Sta.Run(() =>
    {
        var divider = new AntDivider { Content = "Title", Width = 400, Margin = new Thickness(0), FlowDirection = direction, TitlePlacement = placement };
        var scope = new AntThemeScope { Content = divider };
        Window window = Sta.Show(scope);
        try
        {
            ContentPresenter title = Title(divider);
            Rect titleBounds = title.TransformToAncestor(scope).TransformBounds(new Rect(title.RenderSize));
            Rect bounds = divider.TransformToAncestor(scope).TransformBounds(new Rect(divider.RenderSize));
            double center = titleBounds.X + titleBounds.Width / 2;
            double middle = bounds.X + bounds.Width / 2;
            if (placement == DividerTitlePlacement.Center) { Assert.InRange(center, middle - 1, middle + 1); }
            else { Assert.Equal((placement == DividerTitlePlacement.Start) == (direction == FlowDirection.LeftToRight), center < middle); }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_CustomInsetLongTitlesAndClearingRemainBounded() => Sta.Run(() =>
    {
        var divider = new AntDivider { Content = "Title", Width = 400, Height = 40, Margin = new Thickness(0), TitlePlacement = DividerTitlePlacement.Start, TitleInset = 30 };
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            ContentPresenter title = Title(divider);
            Assert.InRange(title.TranslatePoint(new Point(), divider).X, 29.19, 30.81);
            divider.TitlePlacement = DividerTitlePlacement.End;
            Sta.Pump();
            double right = title.TranslatePoint(new Point(title.ActualWidth, 0), divider).X;
            Assert.InRange(right, 369.19, 370.81);
            divider.Content = new string('长', 1000);
            divider.Width = 10;
            Sta.Pump();
            Assert.InRange(title.ActualWidth, 9.19, 10.81);
            divider.Width = 0;
            Sta.Pump();
            Assert.Equal(0, title.ActualWidth);
            divider.Width = 120;
            divider.Content = null;
            divider.BorderBrush = Brushes.Black;
            Sta.Pump();
            Assert.False(divider.HasTitle);
            Assert.Equal(Visibility.Collapsed, Title(divider).Visibility);
            byte[] pixels = Raster(Part(divider), out int width, out int height);
            Assert.True(pixels[((height / 2) * width + width / 2) * 4 + 3] > 0);
            divider.Content = string.Empty;
            Sta.Pump();
            Assert.False(divider.HasTitle);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_ContentTemplateFontScalingAndVerticalModePreserveContent() => Sta.Run(() =>
    {
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding("Label"));
        var template = new DataTemplate { VisualTree = text };
        var content = new { Label = "Templated title" };
        var divider = new AntDivider { Content = content, ContentTemplate = template, FontSize = 24 };
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            Assert.Equal("Templated title", ((TextBlock)VisualTreeHelper.GetChild(Title(divider), 0)).Text);
            Assert.Equal(new Thickness(24, 0, 24, 0), divider.Padding);
            divider.Orientation = Orientation.Vertical;
            Sta.Pump();
            Assert.False(divider.HasTitle);
            Assert.Equal(Visibility.Collapsed, Title(divider).Visibility);
            Assert.Same(content, divider.Content);
            Assert.Same(template, divider.ContentTemplate);
            Assert.Equal(new Thickness(8, 0, 8, 0), divider.Margin);
            Assert.InRange(divider.ActualHeight, 21, 22.5);
            divider.Orientation = Orientation.Horizontal;
            Sta.Pump();
            Assert.True(divider.HasTitle);
            Assert.Equal("Templated title", ((TextBlock)VisualTreeHelper.GetChild(Title(divider), 0)).Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_ThemeUpdatesPreserveBindingsAndMutableBrushOwnership() => Sta.Run(() =>
    {
        var source = new TextBlock { Text = "Bound title" };
        var divider = new AntDivider { Margin = new Thickness(3), Width = 160 };
        divider.SetBinding(ContentControl.ContentProperty, new Binding(nameof(TextBlock.Text)) { Source = source });
        var scope = new AntThemeScope { Content = divider };
        Window window = Sta.Show(scope);
        try
        {
            Assert.Equal(Color.FromRgb(5, 5, 5), ((SolidColorBrush)divider.BorderBrush).Color);
            Assert.Equal(0.06, divider.BorderBrush.Opacity);
            ContentPresenter title = Title(divider);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(Color.FromRgb(253, 253, 253), ((SolidColorBrush)divider.BorderBrush).Color);
            Assert.Equal(0.12, divider.BorderBrush.Opacity);
            Assert.Same(title, Title(divider));
            Assert.NotNull(BindingOperations.GetBindingExpression(divider, ContentControl.ContentProperty));
            Assert.Equal(new Thickness(3), divider.Margin);
            var brush = new SolidColorBrush(Colors.Red);
            divider.Content = null;
            divider.BorderBrush = brush;
            Sta.Pump();
            byte[] red = Raster(Part(divider), out _, out _);
            Assert.False(brush.IsFrozen);
            brush.Color = Colors.Blue;
            Sta.Pump();
            byte[] blue = Raster(Part(divider), out _, out _);
            Assert.False(red.SequenceEqual(blue));
            Assert.Same(brush, divider.BorderBrush);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_PaintOnlyChangesAvoidMeasureAndOldTemplatesDetach() => Sta.Run(() =>
    {
        var divider = new MeasuredDivider { Content = "Stable title" };
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            FrameworkElement part = Part(divider);
            divider.Measures = 0;
            for (int index = 0; index < 30; index++)
            {
                divider.BorderBrush = index % 2 == 0 ? Brushes.Red : Brushes.Blue;
                divider.Variant = (DividerVariant)(index % 3);
                Sta.Pump();
            }

            Assert.Equal(0, divider.Measures);
            Assert.Same(part, Part(divider));
            divider.Template = new ControlTemplate(typeof(AntDivider)) { VisualTree = new FrameworkElementFactory(typeof(Border)) };
            divider.ApplyTemplate();
            Sta.Pump();
            Assert.Null(VisualTreeHelper.GetParent(part));
            divider.TitlePlacement = DividerTitlePlacement.Start;
            divider.Content = "New template";
            Sta.Pump();
            Assert.Equal("New template", divider.Content);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_RejectsInvalidParametersAndReportsSeparatorSemantics() => Sta.Run(() =>
    {
        var divider = new AntDivider { Content = "Section" };
        Assert.Throws<ArgumentException>(() => divider.TitleInset = -1);
        Assert.Throws<ArgumentException>(() => divider.TitleInset = double.NaN);
        Assert.Throws<ArgumentException>(() => divider.TitleInset = double.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => divider.TitlePlacement = (DividerTitlePlacement)42);
        Assert.Throws<ArgumentException>(() => divider.Variant = (DividerVariant)42);
        Assert.Throws<ArgumentException>(() => divider.Orientation = (Orientation)42);
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(divider);
            Assert.Equal(AutomationControlType.Separator, peer.GetAutomationControlType());
            Assert.Equal("Section", peer.GetName());
            Assert.False(peer.IsKeyboardFocusable());
            divider.Orientation = Orientation.Vertical;
            Sta.Pump();
            Assert.Equal(string.Empty, peer.GetName());
            System.Windows.Automation.AutomationProperties.SetName(divider, "Named separator");
            Assert.Equal("Named separator", peer.GetName());
            divider.ClearValue(System.Windows.Automation.AutomationProperties.NameProperty);
            Assert.Equal(string.Empty, peer.GetName());
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Divider_TitleSizeChangesRepaintRailsWithoutReplacingTemplate() => Sta.Run(() =>
    {
        var content = new Border { Width = 20, Height = 10, Background = Brushes.Transparent };
        var divider = new AntDivider { Content = content, Padding = new Thickness(0), Width = 200, Height = 40, BorderBrush = Brushes.Black, Margin = new Thickness(0) };
        Window window = Sta.Show(new AntThemeScope { Content = divider });
        try
        {
            FrameworkElement part = Part(divider);
            byte[] before = Raster(part, out int width, out int height);
            Assert.True(before[((height / 2) * width + 60 * 4) * 4 + 3] > 0);
            content.Width = 160;
            Sta.Pump();
            byte[] after = Raster(part, out width, out height);
            Assert.Equal(0, after[((height / 2) * width + 60 * 4) * 4 + 3]);
            Assert.Same(part, Part(divider));
        }
        finally { window.Close(); }
    });

    private static FrameworkElement Part(AntDivider divider) => (FrameworkElement)divider.Template.FindName("PART_Layout", divider);
    private static ContentPresenter Title(AntDivider divider) => (ContentPresenter)divider.Template.FindName("PART_Title", divider);
    private static byte[] Raster(FrameworkElement part, out int width, out int height)
    {
        width = Math.Max(1, (int)Math.Ceiling(part.ActualWidth * 4));
        height = Math.Max(1, (int)Math.Ceiling(part.ActualHeight * 4));
        var image = new RenderTargetBitmap(width, height, 384, 384, PixelFormats.Pbgra32);
        image.Render(part);
        var pixels = new byte[width * height * 4];
        image.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }

    private sealed class MeasuredDivider : AntDivider
    {
        public int Measures { get; set; }
        protected override Size MeasureOverride(Size constraint)
        {
            Measures++;
            return base.MeasureOverride(constraint);
        }
    }
}
