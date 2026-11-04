using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class SelectClearTests
{
    [Theory]
    [InlineData("SelectedItem")]
    [InlineData("SelectedValue")]
    [InlineData("SelectedIndex")]
    public void Select_ClearPreservesSelectionBindingsAndDuplicateLabels(string property) => Sta.Run(() =>
    {
        var first = new Item("A", "Same label");
        var second = new Item("B", "Same label");
        object initial = property == "SelectedIndex" ? 1 : property == "SelectedValue" ? "B" : second;
        var source = new Border { Tag = initial };
        DependencyProperty selection = property == "SelectedIndex" ? Selector.SelectedIndexProperty :
            property == "SelectedValue" ? Selector.SelectedValueProperty : Selector.SelectedItemProperty;
        var select = new AntSelect { ItemsSource = new[] { first, second }, SelectedValuePath = "Key", DisplayMemberPath = "Label", AllowClear = true };
        select.SetBinding(selection, new Binding(nameof(FrameworkElement.Tag)) { Source = source, Mode = BindingMode.TwoWay });
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            Assert.Same(second, select.SelectedItem);
            int changes = 0;
            int removed = 0;
            select.SelectionChanged += (_, args) => { changes++; removed += args.RemovedItems.Count; };
            select.ClearSelection();
            Sta.Pump();
            Assert.Equal(-1, select.SelectedIndex);
            Assert.Null(select.SelectedItem);
            Assert.Null(select.SelectedValue);
            Assert.Equal(1, changes);
            Assert.Equal(1, removed);
            Assert.Equal(property == "SelectedIndex" ? -1 : null, source.Tag);
            Assert.True(BindingOperations.IsDataBound(select, selection));
            select.ClearSelection();
            Assert.Equal(1, changes);
            source.Tag = initial;
            Sta.Pump();
            Assert.Same(second, select.SelectedItem);
            Assert.Equal(2, select.Items.Count);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_ClearGuardsAndDeleteRespectNativeEditing() => Sta.Run(() =>
    {
        var select = new KeyboardSelect { ItemsSource = new[] { "A", "B" }, SelectedIndex = 1 };
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            Assert.True(select.Focus());
            Assert.Equal(Visibility.Collapsed, ClearButton(select).Visibility);
            Assert.Equal(0, VisualTreeHelper.GetChildrenCount(ClearButton(select)));
            Click(select);
            Assert.Equal(1, select.SelectedIndex);
            Assert.False(select.Delete());
            select.AllowClear = true;
            select.IsEnabled = false;
            select.ClearSelection();
            Click(select);
            Assert.Equal(1, select.SelectedIndex);
            select.IsEnabled = true;
            select.IsEditable = true;
            Sta.Pump();
            Assert.False(select.Delete());
            Assert.Equal(1, select.SelectedIndex);
            select.IsEditable = false;
            select.IsReadOnly = true;
            Assert.True(select.Focus());
            Assert.True(select.Delete());
            Assert.Equal(-1, select.SelectedIndex);
            Assert.False(select.Delete());
            Assert.Equal(Visibility.Collapsed, ClearButton(select).Visibility);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_ClearInvokeClosesPopupAndKeepsVirtualization() => Sta.Run(() =>
    {
        var select = new AntSelect { ItemsSource = Enumerable.Range(0, 10000).Select(i => "Option " + i).ToArray(), SelectedIndex = 1, AllowClear = true };
        var open = new ToggleButton { IsChecked = false };
        select.SetBinding(ComboBox.IsDropDownOpenProperty, new Binding(nameof(ToggleButton.IsChecked)) { Source = open, Mode = BindingMode.TwoWay });
        var scope = new AntThemeScope { Content = select };
        Window window = Sta.Show(scope);
        try
        {
            int opens = 0;
            select.DropDownOpened += (_, _) => opens++;
            Assert.True(select.Focus());
            select.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
            Sta.Pump();
            var popup = (Popup)select.Template.FindName("PART_Popup", select);
            var root = (FrameworkElement)popup.Child;
            int realized = Descendants(root).OfType<ComboBoxItem>().Count();
            Assert.InRange(realized, 1, 45);
            Assert.Contains(scope.Resources, root.Resources.MergedDictionaries);
            var button = ClearButton(select);
            var peer = new ButtonAutomationPeer(button);
            Assert.Equal("Clear selection", peer.GetName());
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            Sta.Pump();
            Assert.Equal(-1, select.SelectedIndex);
            Assert.False(select.IsDropDownOpen);
            Assert.False(popup.IsOpen);
            Assert.Equal(false, open.IsChecked);
            Assert.True(BindingOperations.IsDataBound(select, ComboBox.IsDropDownOpenProperty));
            Assert.Equal(1, opens);
            Assert.DoesNotContain(scope.Resources, root.Resources.MergedDictionaries);
            Assert.Equal(10000, select.Items.Count);
            Assert.True(select.IsKeyboardFocusWithin);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ControlSize.Small, 24, FlowDirection.LeftToRight)]
    [InlineData(ControlSize.Middle, 32, FlowDirection.RightToLeft)]
    [InlineData(ControlSize.Large, 40, FlowDirection.LeftToRight)]
    public void Select_ClearAffordanceKeepsSizeThemeAndEditor(ControlSize size, double height, FlowDirection direction) => Sta.Run(() =>
    {
        var select = new AntSelect { ItemsSource = new[] { "Alpha" }, SelectedIndex = 0, AllowClear = true, Size = size, FlowDirection = direction };
        var scope = new AntThemeScope { Content = select };
        Window window = Sta.Show(scope);
        try
        {
            Assert.True(select.Focus());
            Sta.Pump();
            Button button = ClearButton(select);
            Assert.Equal(Visibility.Visible, button.Visibility);
            Assert.Equal(Visibility.Hidden, ((FrameworkElement)select.Template.FindName("Arrow", select)).Visibility);
            Assert.InRange(select.ActualHeight, height - 0.8, height + 0.8);
            Assert.Equal(direction, button.FlowDirection);
            object editor = select.Template.FindName("PART_EditableTextBox", select);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Same(editor, select.Template.FindName("PART_EditableTextBox", select));
            Assert.Equal("Alpha", select.SelectedItem);
            Assert.Equal(select.FindResource("Antd.Brush.Tertiary"), button.Foreground);
            select.ClearSelection();
            Sta.Pump();
            Assert.Equal(Visibility.Collapsed, button.Visibility);
            Assert.Equal(Visibility.Visible, ((FrameworkElement)select.Template.FindName("Arrow", select)).Visibility);
            Assert.Equal(Visibility.Visible, ((FrameworkElement)select.Template.FindName("Placeholder", select)).Visibility);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_ClearEditableSelectionPreservesNativeTextBinding() => Sta.Run(() =>
    {
        var source = new TextBox { Text = "Beta" };
        var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta" }, SelectedIndex = 1, AllowClear = true, IsEditable = true };
        select.SetBinding(ComboBox.TextProperty, new Binding(nameof(TextBox.Text)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            select.ClearSelection();
            Sta.Pump();
            Assert.Equal(-1, select.SelectedIndex);
            Assert.Equal(string.Empty, select.Text);
            Assert.Equal(string.Empty, source.Text);
            Assert.True(BindingOperations.IsDataBound(select, ComboBox.TextProperty));
            select.SetCurrentValue(ComboBox.TextProperty, "query");
            select.ClearSelection();
            Assert.Equal("query", select.Text);
            Assert.Equal(-1, select.SelectedIndex);
            select.SelectedIndex = 0;
            select.AllowClear = false;
            select.ClearSelection();
            Assert.Equal(-1, select.SelectedIndex);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_ClearRetemplateDetachesOldButtonAndAllowsMissingPart() => Sta.Run(() =>
    {
        var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta" }, SelectedIndex = 1, AllowClear = true };
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            Button old = ClearButton(select);
            select.Template = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border /></ControlTemplate>");
            Sta.Pump();
            old.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, select.SelectedIndex);
            select.ClearSelection();
            Assert.Equal(-1, select.SelectedIndex);
        }
        finally { window.Close(); }
    });

    private static Button ClearButton(AntSelect select) => (Button)select.Template.FindName("PART_ClearButton", select);
    private static void Click(AntSelect select) => ClearButton(select).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static IEnumerable<DependencyObject> Descendants(DependencyObject element)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) { yield return descendant; }
        }
    }

    private sealed class Item(string key, string label)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
    }

    private sealed class KeyboardSelect : AntSelect
    {
        public bool Delete()
        {
            var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Delete) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            OnPreviewKeyDown(args);
            return args.Handled;
        }
    }
}
