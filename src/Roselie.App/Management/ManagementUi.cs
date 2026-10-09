using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Roselie.App.Management;

internal abstract class NotifyModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Changed(property); return true;
    }
}

internal sealed class UiCommand(Action action, Func<bool>? enabled = null) : ICommand
{
    public bool CanExecute(object? parameter) => enabled?.Invoke() ?? true;
    public void Execute(object? parameter) => action();
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

internal sealed record Choice(object? Value, string Label)
{
    public override string ToString() => Label;
}

internal sealed record FieldSpec(string Property, string Label, bool Required = false, string? Help = null, IReadOnlyList<Choice>? Choices = null, decimal? Min = null, decimal? Max = null, bool MultiLine = false, bool Secret = false);

internal sealed class FormField : NotifyModel
{
    public FieldSpec Spec { get; }
    public Type ValueType { get; }
    private object? _value;
    private string _error = "";
    public object? Value { get => _value; set { if (Set(ref _value, value)) Error = ""; } }
    public string Error { get => _error; set => Set(ref _error, value); }
    public FormField(FieldSpec spec, Type valueType, object? value)
    {
        Spec = spec;
        ValueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (spec.Choices != null) _value = spec.Choices.FirstOrDefault(c => Equals(c.Value, value));
        else if (ValueType == typeof(DateTime)) _value = value;
        else if (ValueType == typeof(DateOnly)) _value = value is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : null;
        else if (ValueType == typeof(bool)) _value = value ?? false;
        else _value = value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
    }

    public object? ConvertValue(Type targetType)
    {
        var value = Value is Choice choice ? choice.Value : Value;
        if (value is string text && string.IsNullOrWhiteSpace(text))
        {
            if (Spec.Required) throw new InvalidOperationException($"{Spec.Label} is required.");
            if (targetType == typeof(string)) return "";
            if (Nullable.GetUnderlyingType(targetType) != null) return null;
            if (ValueType == typeof(decimal) || ValueType == typeof(int)) value = "0";
        }
        if (value == null)
        {
            if (Spec.Required) throw new InvalidOperationException($"{Spec.Label} is required.");
            if (Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType) return null;
        }
        object? converted;
        try
        {
            if (value == null) converted = Activator.CreateInstance(ValueType);
            else if (ValueType.IsInstanceOfType(value)) converted = value;
            else if (ValueType.IsEnum) converted = Enum.Parse(ValueType, value.ToString()!);
            else if (ValueType == typeof(Guid)) converted = Guid.Parse(value.ToString()!);
            else if (ValueType == typeof(DateOnly) && value is DateTime date) converted = DateOnly.FromDateTime(date);
            else converted = Convert.ChangeType(value, ValueType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        { throw new InvalidOperationException($"Enter a valid {Spec.Label.ToLowerInvariant()}."); }
        if (converted is decimal amount && (amount < (Spec.Min ?? decimal.MinValue) || amount > (Spec.Max ?? decimal.MaxValue)))
            throw new InvalidOperationException($"{Spec.Label} must be between {Spec.Min ?? 0} and {Spec.Max?.ToString() ?? "the allowed limit"}.");
        if (converted is int number && (number < (Spec.Min ?? int.MinValue) || number > (Spec.Max ?? int.MaxValue)))
            throw new InvalidOperationException($"{Spec.Label} is outside its allowed range.");
        return converted;
    }
}

internal static class ManagementUi
{
    internal static readonly Brush Rose = new SolidColorBrush(Color.FromRgb(200, 121, 125));
    internal static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(52, 64, 82));
    internal static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(119, 114, 123));
    internal static readonly Brush Line = new SolidColorBrush(Color.FromRgb(233, 218, 219));
    internal static readonly Brush Blush = new SolidColorBrush(Color.FromRgb(243, 231, 231));

    public static TextBlock Text(string text, double size = 14, Brush? color = null, bool bold = false) => new()
    { Text = text, FontSize = size, Foreground = color ?? Ink, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };

    public static Button Button(string text, ICommand command, bool primary = false)
    {
        var b = Ui.Button(text, () => command.Execute(null), primary); b.Command = command; b.Padding = new Thickness(15, 9, 15, 9); b.MinHeight = 36; b.Margin = new Thickness(0, 0, 8, 8);
        return b;
    }

    public static Border Card(UIElement child, Thickness? padding = null)
    {
        var card = Ui.Card(child); card.Padding = padding ?? new Thickness(20); return card;
    }

    public static Border Metric(string label, string value, string detail, string icon = "◇", string accent = "#A85E67")
    {
        var content = new Grid(); content.ColumnDefinitions.Add(new ColumnDefinition()); content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var text = new StackPanel(); text.Children.Add(Text(label.ToUpperInvariant(), 10, Muted, true));
        var amount = Text(value, 24, Ink, true); amount.Margin = new Thickness(0, 6, 0, 5); text.Children.Add(amount); text.Children.Add(Text(detail, 11, Muted)); content.Children.Add(text);
        var symbol = Ui.Badge(icon, "#FAF0F0", accent, 20); symbol.VerticalAlignment = VerticalAlignment.Top; Grid.SetColumn(symbol, 1); content.Children.Add(symbol);
        var card = Card(content, new Thickness(16)); card.Margin = new Thickness(0, 0, 10, 12); return card;
    }

    public static FrameworkElement MetricRow(params Border[] cards)
    {
        var row = new System.Windows.Controls.Primitives.UniformGrid { Columns = cards.Length };
        foreach (var card in cards) row.Children.Add(card); return row;
    }

    public static FrameworkElement Profile(string name, string subtitle)
    {
        var initials = string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[..1])).ToUpperInvariant();
        var panel = new Grid(); panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); panel.ColumnDefinitions.Add(new ColumnDefinition());
        var avatar = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(16), Background = Ui.Brush("#F3E7E7"), Margin = new Thickness(0, 0, 10, 0), Child = Text(initials.Length == 0 ? "◇" : initials, 11, Ui.Brush("#A85E67"), true) };
        ((TextBlock)avatar.Child).HorizontalAlignment = HorizontalAlignment.Center; ((TextBlock)avatar.Child).VerticalAlignment = VerticalAlignment.Center; panel.Children.Add(avatar);
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; var fullName = Text(name, 13, Ink, true); fullName.TextTrimming = TextTrimming.CharacterEllipsis; fullName.TextWrapping = TextWrapping.NoWrap; copy.Children.Add(fullName);
        if (!string.IsNullOrWhiteSpace(subtitle)) { var detail = Text(subtitle, 11, Muted); detail.TextTrimming = TextTrimming.CharacterEllipsis; detail.TextWrapping = TextWrapping.NoWrap; copy.Children.Add(detail); } Grid.SetColumn(copy, 1); panel.Children.Add(copy); return panel;
    }

    public static FrameworkElement ProductThumbnail(string path)
    {
        var frame = new Border { Width = 54, Height = 54, CornerRadius = new CornerRadius(12), Background = Ui.Brush("#FAF0F0"), Margin = new Thickness(0, 0, 12, 0) };
        var icon = Ui.Icon("\uE719", 24); icon.Foreground = Ui.Brush("#A85E67"); frame.Child = icon;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return frame;
        try
        {
            using var stream = File.OpenRead(path);
            var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.DecodePixelWidth = 90; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
            frame.Child = new Image { Source = bitmap, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or FormatException or InvalidOperationException or System.Runtime.InteropServices.COMException) { }
        return frame;
    }

    public static FrameworkElement Empty(string title, string description, string icon = "◇", bool compact = false)
    {
        var panel = new StackPanel { MaxWidth = 400, Margin = new Thickness(compact ? 12 : 28), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var emblem = new Border { Width = 54, Height = 54, CornerRadius = new CornerRadius(18), Background = Ui.Brush("#FAF0F0"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 14), Child = Text(icon, 23, Rose) };
        ((TextBlock)emblem.Child).HorizontalAlignment = HorizontalAlignment.Center; ((TextBlock)emblem.Child).VerticalAlignment = VerticalAlignment.Center;
        if (!compact) panel.Children.Add(emblem); var heading = Text(title, compact ? 16 : 18, Ink, true); heading.TextAlignment = TextAlignment.Center; panel.Children.Add(heading);
        var body = Text(description, compact ? 12 : 13, Muted); body.TextAlignment = TextAlignment.Center; body.Margin = new Thickness(0, 8, 0, 0); panel.Children.Add(body); return panel;
    }

    public static Binding Bind(string path) => new(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

    public static FrameworkElement Field(FormField field)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 14), DataContext = field };
        if (field.ValueType != typeof(bool)) panel.Children.Add(Text(field.Spec.Label + (field.Spec.Required ? " *" : ""), 13, Ink, true));
        FrameworkElement editor;
        if (field.Spec.Secret)
        {
            var password = new PasswordBox { Padding = new Thickness(9, 7, 9, 7), MinHeight = 35, Margin = new Thickness(0, 6, 0, 0) };
            password.PasswordChanged += (_, _) => field.Value = password.Password; editor = password;
        }
        else if (field.Spec.Choices != null)
        {
            var combo = new ComboBox { ItemsSource = field.Spec.Choices, MinHeight = 35, Margin = new Thickness(0, 6, 0, 0) };
            combo.SetBinding(ComboBox.SelectedItemProperty, Bind(nameof(FormField.Value))); editor = combo;
            if (field.Spec.Choices.Count == 0)
            {
                combo.IsEnabled = false;
                var guidance = field.Spec.Property switch { "CustomerId" => "Add an active customer in Customers before creating this record.", "ServiceId" => "Add an active service in Service Catalog before creating this record.", "CategoryId" => "Create a category from the catalog's Categories action first.", "ProductId" => "Add an active product in Products before configuring this requirement.", "EmployeeId" => "Add an active employee profile first. Employees with an existing login should be edited or reactivated.", _ => "Create an eligible record in the corresponding directory first." };
                var note = Text(guidance, 12, Muted); note.Margin = new Thickness(0, 6, 0, 0); panel.Children.Add(note);
            }
        }
        else if (field.ValueType == typeof(bool))
        {
            var check = new CheckBox { Content = field.Spec.Label, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Ink, MinHeight = 32, Margin = new Thickness(0, 6, 0, 4) };
            check.SetBinding(CheckBox.IsCheckedProperty, Bind(nameof(FormField.Value))); editor = check;
        }
        else if (field.ValueType == typeof(DateTime) || field.ValueType == typeof(DateOnly))
        {
            var date = new DatePicker { MinHeight = 35, Margin = new Thickness(0, 6, 0, 0) };
            date.SetBinding(DatePicker.SelectedDateProperty, Bind(nameof(FormField.Value))); editor = date;
        }
        else if (field.ValueType.IsEnum)
        {
            var combo = new ComboBox { ItemsSource = Enum.GetValues(field.ValueType), MinHeight = 35, Margin = new Thickness(0, 6, 0, 0) };
            var label = new FrameworkElementFactory(typeof(TextBlock)); label.SetBinding(TextBlock.TextProperty, new Binding { Converter = new EnumLabelConverter() }); combo.ItemTemplate = new DataTemplate { VisualTree = label };
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(FormField.Value)) { Converter = new EnumTextConverter(field.ValueType), Mode = BindingMode.TwoWay }); editor = combo;
        }
        else
        {
            var input = new TextBox { Padding = new Thickness(9, 7, 9, 7), Margin = new Thickness(0, 6, 0, 0), MinHeight = 35, AcceptsReturn = field.Spec.MultiLine, TextWrapping = field.Spec.MultiLine ? TextWrapping.Wrap : TextWrapping.NoWrap };
            if (field.Spec.MultiLine) input.MinHeight = 70;
            if (field.ValueType == typeof(decimal) || field.ValueType == typeof(int)) { input.HorizontalContentAlignment = HorizontalAlignment.Right; if (Application.Current.TryFindResource("NumericInput") is Style numeric) input.Style = numeric; }
            input.SetBinding(TextBox.TextProperty, Bind(nameof(FormField.Value))); editor = input;
        }
        panel.Children.Add(editor);
        System.Windows.Automation.AutomationProperties.SetName(editor, field.Spec.Label + (field.Spec.Required ? ", required" : ""));
        var validation = Text("", 11, Ui.Brush("#C84D58")); validation.Margin = new Thickness(0, 5, 0, 0); validation.Visibility = Visibility.Collapsed; panel.Children.Add(validation);
        field.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName != nameof(FormField.Error)) return;
            validation.Text = field.Error; validation.Visibility = field.Error.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (editor is Control control) { if (field.Error.Length > 0) { control.BorderBrush = Ui.Brush("#C84D58"); control.BorderThickness = new Thickness(2); control.Focus(); control.BringIntoView(); } else { control.ClearValue(Control.BorderBrushProperty); control.ClearValue(Control.BorderThicknessProperty); } }
        };
        if (field.Spec.Help != null) { var help = Text(field.Spec.Help, 12, Muted); help.Margin = new Thickness(0, 5, 0, 0); panel.Children.Add(help); }
        return panel;
    }

    public static void Error(Exception error) => Ui.Guard(() => throw error);
    public static void Try(Action action) { try { action(); } catch (Exception ex) { Error(ex); } }
    public static bool Confirm(string text) => Ui.Confirm("Confirm action", text, destructive: text.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Cancel", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Disable", StringComparison.OrdinalIgnoreCase));
    public static string Money(decimal value) => value.ToString("₱#,##0.00", CultureInfo.InvariantCulture);

    public static void ShowDetails(string title, UIElement details)
    {
        var window = Window(title, 850, 650);
        window.Content = details is UserControl
            ? new Border { Child = details, Padding = new Thickness(24) }
            : new ScrollViewer { Content = details, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(24) };
        window.ShowDialog();
    }

    public static Window Window(string title, double width = 660, double height = 740)
    {
        // Shared dialog behavior supplies dimming, reduced-motion-aware transitions, bounds, and Escape.
        var window = Ui.Dialog(title, new Grid(), width, height);
        window.Loaded += (_, _) => { if (window.Content is FrameworkElement content) { Ui.Enter(content); content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)); } };
        return window;
    }

    public static DataGrid Table(IEnumerable rows, params (string Header, string Path, string? Format)[] columns)
    {
        var grid = new DataGrid { ItemsSource = rows, IsReadOnly = true, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false, SelectionMode = DataGridSelectionMode.Single, SelectionUnit = DataGridSelectionUnit.FullRow, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, HeadersVisibility = DataGridHeadersVisibility.Column, Background = Brushes.White, BorderThickness = new Thickness(0), MinHeight = 140, EnableRowVirtualization = true, EnableColumnVirtualization = true };
        foreach (var c in columns)
        {
            var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)); style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis)); style.Setters.Add(new Setter(TextBlock.ToolTipProperty, new Binding(c.Path) { StringFormat = c.Format }));
            if (c.Header.Contains("cost", StringComparison.OrdinalIgnoreCase) || c.Header.Contains("total", StringComparison.OrdinalIgnoreCase) || c.Header is "Amount" or "Balance" or "Change") style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            grid.Columns.Add(new DataGridTextColumn { Header = c.Header, Binding = new Binding(c.Path) { StringFormat = c.Format }, ElementStyle = style, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 85 });
        }
        return grid;
    }
}

internal sealed class EnumTextConverter(Type type) : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string s && Enum.TryParse(type, s, out var result) ? result : value;
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString();
}

internal sealed class EnumLabelConverter : IValueConverter
{
    internal static string Label(object? value) => Regex.Replace(value?.ToString() ?? "", "(?<=[a-z])(?=[A-Z])", " ");
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Label(value);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

internal static class EntityForm
{
    public static bool Edit<T>(T entity, string title, IReadOnlyList<FieldSpec> specs, Action<T> save, Action<T>? validate = null) where T : class
    {
        var fields = specs.Select(spec =>
        {
            var property = typeof(T).GetProperty(spec.Property) ?? throw new InvalidOperationException($"Unknown field {typeof(T).Name}.{spec.Property}");
            return new FormField(spec, property.PropertyType, property.GetValue(entity));
        }).ToList();
        var window = ManagementUi.Window(title);
        window.MinHeight = 380; window.SizeToContent = SizeToContent.Height;
        window.MaxHeight = Math.Min(window.MaxHeight, window.Owner is { ActualHeight: > 0 } owner ? Math.Max(400, owner.ActualHeight - 32) : 740);
        var root = new DockPanel { Margin = new Thickness(24) };
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var footerSurface = new Border { Child = footer, BorderBrush = ManagementUi.Line, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 16, 0, 0), Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(footerSurface, Dock.Bottom);
        var errorText = ManagementUi.Text("", 13, Brushes.Firebrick);
        var errorSurface = new Border { Child = errorText, Background = Ui.Brush("#FFF0F2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 12, 0, 0), Visibility = Visibility.Collapsed };
        var saving = false; Button? saveButton = null;
        var saveCommand = new UiCommand(() =>
        {
            saving = true; CommandManager.InvalidateRequerySuggested(); if (saveButton != null) saveButton.Content = "Saving…";
            try
            {
                // Convert every field before applying any changes, so invalid input cannot partially update an entity.
                foreach (var field in fields) field.Error = "";
                var converted = new List<(FormField Field, object? Value)>();
                foreach (var field in fields)
                {
                    try { converted.Add((field, field.ConvertValue(typeof(T).GetProperty(field.Spec.Property)!.PropertyType))); }
                    catch (Exception ex) { field.Error = ex.GetBaseException().Message; throw; }
                }
                foreach (var item in converted) typeof(T).GetProperty(item.Field.Spec.Property)!.SetValue(entity, item.Value);
                validate?.Invoke(entity); save(entity); window.DialogResult = true; Ui.Toast("Saved successfully.");
            }
            catch (Exception ex) { errorText.Text = ex.GetBaseException().Message; errorSurface.Visibility = Visibility.Visible; }
            finally { saving = false; CommandManager.InvalidateRequerySuggested(); if (saveButton != null) saveButton.Content = "Save changes"; }
        }, () => !saving);
        var cancel = ManagementUi.Button("Cancel", new UiCommand(() => window.DialogResult = false)); cancel.IsCancel = true; cancel.Margin = new Thickness(0, 0, 12, 0);
        footer.Children.Add(cancel); saveButton = ManagementUi.Button("Save changes", saveCommand, true); saveButton.IsDefault = true; saveButton.Margin = new Thickness(0); footer.Children.Add(saveButton);
        root.Children.Add(footerSurface);
        var top = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        top.Children.Add(Ui.Badge("SALON MANAGEMENT", "#FAF0F0", "#A85E67", 10));
        var subtitle = ManagementUi.Text("Update the details below. Fields marked * are required.", 12, ManagementUi.Muted); subtitle.Margin = new Thickness(0, 8, 0, 0); top.Children.Add(subtitle);
        top.Children.Add(errorSurface); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        var form = new StackPanel();
        foreach (var group in fields.GroupBy(field => specs.Count <= 7 ? "Details" : Section(field.Spec)))
        {
            var section = new StackPanel(); var label = ManagementUi.Text(group.Key, 15, ManagementUi.Ink, true); label.Margin = new Thickness(0, 0, 0, 16); section.Children.Add(label);
            var layout = new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition()); layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); layout.ColumnDefinitions.Add(new ColumnDefinition());
            var row = 0; var column = 0;
            foreach (var field in group)
            {
                var wide = field.Spec.MultiLine || field.Spec.Secret || field.Spec.Help != null;
                if (wide && column != 0) { row++; column = 0; }
                while (layout.RowDefinitions.Count <= row) layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var control = ManagementUi.Field(field); Grid.SetRow(control, row); Grid.SetColumn(control, column == 0 ? 0 : 2);
                if (wide) { Grid.SetColumnSpan(control, 3); row++; column = 0; } else if (column == 0) column = 1; else { row++; column = 0; }
                layout.Children.Add(control); field.PropertyChanged += (_, _) => errorSurface.Visibility = Visibility.Collapsed;
            }
            section.Children.Add(layout); form.Children.Add(ManagementUi.Card(section, new Thickness(18)));
        }
        root.Children.Add(new ScrollViewer { Content = form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        window.Content = root;
        window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(() => form.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)), System.Windows.Threading.DispatcherPriority.Input);
        return window.ShowDialog() == true;
    }

    private static string Section(FieldSpec spec) => spec.Property switch
    {
        "Username" or "Password" or "ConfirmPassword" or "Role" or "MaxDiscountPercent" or "AllowCustomPrices" => "Account access",
        "IsActive" or "OwnerConfirmed" or "CostInformationComplete" or "IsRetail" => "Availability & confirmation",
        "BasePrice" or "MinimumPrice" or "MaximumPrice" or "PricingType" or "PackageAvailable" or "PackagePrice" or "PurchaseCost" or "SellingPrice" or "Amount" or "Price" or "UnitCost" or "CommissionType" or "CommissionRate" or "Type" or "Rate" => "Pricing & amounts",
        "Date" or "StartTime" or "DurationMinutes" or "OccurredAtUtc" or "ExpirationDateUtc" or "ValidityDays" or "Status" or "OverlapOverride" => "Schedule & status",
        _ => "Details"
    };
}
