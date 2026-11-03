# Antd.Wpf · Foundation preview

Native WPF controls inspired by Ant Design. The package contains assets for .NET Framework 4.8 and .NET 8 Windows; later compatible Windows .NET hosts select the .NET 8 asset.

This alpha implements a foundation slice, not the complete Ant Design component catalog:

- `AntThemeScope`: Light/Dark resources, inherited size/motion settings, nested scopes, system high-contrast resources.
- `AntButton`: variants, semantic colors, shapes, icons, loading delay and command suppression.
- `AntInput`: native TextBox editing/binding, placeholder, prefix/suffix, clear, UTF-16 counter with optional MaxLength display, sizes and validation presentation. Bound over-limit text is preserved; IsOverMaxLength exposes the derived error state and UI Automation reports it. Multiline input grows between MinLines/MaxLines using the theme line pitch and scrolls at the limit.
- `AntInput` search mode: IsSearchEnabled adds a joined primary text button. SearchCommand receives InputSearchEventArgs with Text, Enter/Button/Clear reason and CancellationToken; SearchRequested runs first and can cancel dispatch. IsLoading is host-owned and blocks repeat requests. Escape, CancelSearch, unload, command replacement and disabling search mode cancel the active token. The host must check cancellation/version before publishing asynchronous results. SearchButtonContent and SearchButtonStyle customize the button.
- `AntSelect`: native ComboBox selection, a recycled virtual item panel, scoped Popup resources and owner cleanup.
- `AntSwitch`: native ToggleButton commands and accessibility, two-state binding, checked/unchecked content, loading interaction guard and 200 ms thumb motion. MotionEnabled disables transitions; hiding, unloading and retemplating release clocks. Color/label transitions, pressed-handle stretch, shadow and busy rotation are pending.
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
    <antd:AntInput Text="{Binding Name}" Placeholder="Name" AllowClear="True" ShowCount="True" MaxLength="20" />
    <antd:AntInput IsSearchEnabled="True" SearchCommand="{Binding SearchCommand}" IsLoading="{Binding IsSearching}"
                   SearchButtonContent="Search" AllowClear="True" />
    <antd:AntInput AcceptsReturn="True" TextWrapping="Wrap" MinLines="2" MaxLines="4"
                   VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" VerticalContentAlignment="Top" />
    <antd:AntSelect ItemsSource="{Binding Options}" SelectedItem="{Binding SelectedOption}" />
    <antd:AntSwitch IsChecked="{Binding Enabled}" CheckedContent="On" UncheckedContent="Off" />
  </StackPanel>
</antd:AntThemeScope>
```

The XAML URI is an assembly namespace identifier and does not require a network connection. Default styles load from the assembly. For C# hosts that also import newer WPF's ThemeMode, use `using ThemeMode = Antd.Wpf.Theming.ThemeMode;`.

Known limits: Select multiple selection/remote search, Password/OTP, advanced tokens, host message/modal services and the remaining catalog are not implemented in this slice. Search currently implements the primary text-button form; the default neutral icon-only form and full compact-addon combinations are pending. Historical PF02/PF05 slow samples remain unexplained. Switch maps inherited Large size to its official default size and uses a static busy indicator. Group controls, blurred shadows and full motion states are pending. Multiline auto-height needs an auto-sized parent slot; a locally specified Height/MinHeight/MaxHeight takes precedence over row bounds, following native TextBox sizing policy. Full keyboard/IME/DPI/screen-reader and long-running leak validation is still required before a stable release.
