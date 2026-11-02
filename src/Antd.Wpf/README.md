# Antd.Wpf · Foundation preview

Native WPF controls inspired by Ant Design. The package contains assets for .NET Framework 4.8 and .NET 8 Windows; later compatible Windows .NET hosts select the .NET 8 asset.

This alpha implements a foundation slice, not the complete Ant Design component catalog:

- `AntThemeScope`: Light/Dark resources, inherited size/motion settings, nested scopes, system high-contrast resources.
- `AntButton`: variants, semantic colors, shapes, icons, loading delay and command suppression.
- `AntInput`: native TextBox editing/binding, placeholder, prefix/suffix, clear, counter, sizes and validation presentation.
- `AntSelect`: native ComboBox selection, a recycled virtual item panel, scoped Popup resources and owner cleanup.
- `AntSwitch`: native ToggleButton commands and accessibility, two-state binding, checked/unchecked content and loading interaction guard.
- `AntCheckBox` / `AntRadioButton`: native checked/group/command/accessibility semantics with source-aligned geometry, gaps and theme resources. Group wrapper components are pending.
- `AntDivider`: horizontal/vertical solid, dashed or dotted lines; titled/ordinary separators, Start/Center/End with RTL, plain text, sizes, optional DIP title inset and native content templates. Rails use a cached drawing pen with one title presenter.
- `AntFlexPanel`: horizontal/vertical layout with optional wrapping, numeric row/column gaps, Start/Center/End/SpaceBetween/SpaceAround/SpaceEvenly justification, cross-axis alignment/Stretch, native RTL and child constraints. Grow/Shrink/Basis/AlignSelf and baseline are pending.
- `AntSpace`: default 8 DIP spacing, axis gap overrides, horizontal wrapping, alignment and decorative separator templates. Direct children remain unwrapped; separators at wrap boundaries are hidden. Compact/Addon and baseline are pending.
- `AntRow` / `AntCol`: 24 tracks, spans/offsets, wrapping, half gutters, six justification modes and vertical alignment. Span zero preserves content/bindings while collapsed. WPF's base Span defaults to24; responsive breakpoints, Order/Push/Pull and Grow remain pending.
- `AntLayout` / `AntHeader` / `AntContent` / `AntFooter`: native docking and nesting, source-based region spacing; scrolling is explicit in content.
- `AntSider`: independent Light/Dark scope, manual collapse with preserved width/state bindings, 80/zero collapsed widths, left/right/RTL, keyboard edge trigger, reason events and UIA ExpandCollapse. Zero-width mode retains its edge trigger in both states. Responsive collapse and animation are pending.

Visual defaults are based on the official Ant Design 6.6.8 styles and resolved default/dark tokens. Use `AntThemeScope` for the complete color resource context. Numeric token alignment does not prove cross-browser pixel identity.

Third-party style and vector notices are included in `THIRD-PARTY-NOTICES.txt`.

```xml
<antd:AntThemeScope xmlns:antd="https://antd-wpf.dev/xaml" Mode="Light">
  <StackPanel>
    <antd:AntButton Content="Save" Variant="Solid" SemanticColor="Primary" Command="{Binding SaveCommand}" />
    <antd:AntInput Text="{Binding Name}" Placeholder="Name" AllowClear="True" />
    <antd:AntSelect ItemsSource="{Binding Options}" SelectedItem="{Binding SelectedOption}" />
    <antd:AntSwitch IsChecked="{Binding Enabled}" CheckedContent="On" UncheckedContent="Off" />
  </StackPanel>
</antd:AntThemeScope>
```

The XAML URI is an assembly namespace identifier and does not require a network connection. Default styles load from the assembly. For C# hosts that also import newer WPF's ThemeMode, use `using ThemeMode = Antd.Wpf.Theming.ThemeMode;`.

Known limits: multiple selection, search/remote loading, advanced tokens, host message/modal services and the remaining catalog are not implemented in this slice. Historical PF02/PF05 slow samples remain unexplained. Switch maps inherited Large size to its official default size; it uses immediate transitions and a static busy indicator. Group controls, blurred shadows and full motion states are pending. Full keyboard/IME/DPI/screen-reader and long-running leak validation is still required before a stable release.
