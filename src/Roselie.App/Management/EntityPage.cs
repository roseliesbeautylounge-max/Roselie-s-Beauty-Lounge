using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App.Management;

internal sealed record TableColumn<T>(string Header, Func<T, object?> Value, string? Format = null, double Width = 1, Func<T, FrameworkElement>? Content = null);
internal sealed record EntityAction<T>(string Label, Action<T> Run);

internal sealed class EntityPageModel<T> : NotifyModel where T : Entity, new()
{
    private readonly BusinessService _service;
    private readonly UserAccount _user;
    private readonly IReadOnlyList<TableColumn<T>> _columns;
    private readonly Func<IEnumerable<T>, IEnumerable<T>>? _scope;
    private readonly Func<T, IReadOnlyList<FieldSpec>> _fields;
    private readonly Action<T>? _initialize;
    private readonly Action<T>? _validate;
    private readonly Action<T>? _save;
    private readonly Func<T, bool>? _editor;
    private readonly string _singular;
    private string _search = "";
    private string _filter = "All records";
    private T? _selected;
    private string _summary = "";
    public ObservableCollection<T> Items { get; } = [];
    public ICollectionView View { get; }
    public T? Selected { get => _selected; set { if (Set(ref _selected, value)) CommandManager.InvalidateRequerySuggested(); } }
    public string Search { get => _search; set { if (Set(ref _search, value)) RefreshFilter(); } }
    public string Filter { get => _filter; set { if (Set(ref _filter, value)) RefreshFilter(); } }
    public string Summary { get => _summary; private set => Set(ref _summary, value); }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand RefreshCommand { get; }
    public event Action? Reloading;
    public event Action? Filtered;
    public event Action? Reloaded;

    public EntityPageModel(BusinessService service, UserAccount user, string singular, IReadOnlyList<TableColumn<T>> columns, Func<T, IReadOnlyList<FieldSpec>> fields, Func<IEnumerable<T>, IEnumerable<T>>? scope = null, Action<T>? initialize = null, Action<T>? validate = null, Action<T>? save = null, Func<T, bool>? editor = null)
    {
        _service = service; _user = user; _singular = singular; _columns = columns; _fields = fields; _scope = scope; _initialize = initialize; _validate = validate; _save = save; _editor = editor;
        View = CollectionViewSource.GetDefaultView(Items); View.Filter = Matches;
        AddCommand = new UiCommand(Add);
        EditCommand = new UiCommand(Edit, () => Selected != null);
        DisableCommand = new UiCommand(ToggleActive, () => Selected != null && typeof(T).GetProperty("IsActive") != null);
        RefreshCommand = new UiCommand(() => ManagementUi.Try(Reload));
        Reload();
    }
    internal bool Matches(object item)
    {
        if (item is not T entity) return false;
        var active = typeof(T).GetProperty("IsActive")?.GetValue(entity) as bool?;
        if (Filter == "Active" && active != true || Filter == "Inactive" && active != false) return false;
        if (Filter == "Low stock" && entity is Product p && p.StockQuantity > p.ReorderLevel) return false;
        if (Filter == "Expiring in 30 days" && entity is Product exp && (exp.ExpirationDateUtc == null || exp.ExpirationDateUtc > DateTime.UtcNow.AddDays(30))) return false;
        if (entity is Appointment appointment && Enum.TryParse<AppointmentStatus>(Filter.Replace(" ", ""), out var visitStatus) && appointment.Status != visitStatus) return false;
        if (entity is Expense expense && (Filter == "Operating expenses" && expense.CommissionRecordId != null || Filter == "Commission payouts" && expense.CommissionRecordId == null)) return false;
        if (entity is CommissionRecord commission && (Filter == "Accrued" && (commission.IsReversed || commission.PaidAtUtc != null) || Filter == "Paid" && (commission.IsReversed || commission.PaidAtUtc == null) || Filter == "Reversed" && !commission.IsReversed)) return false;
        if (entity is CustomerPackage package)
        {
            var expired = package.ExpiresAtUtc < DateTime.UtcNow;
            var available = !package.IsCancelled && !expired && package.RemainingSessions > 0;
            if (Filter == "Available sessions" && !available || Filter == "Expiring soon" && (!available || package.ExpiresAtUtc == null || package.ExpiresAtUtc > DateTime.UtcNow.AddDays(30)) || Filter == "Completed" && (package.IsCancelled || expired || package.RemainingSessions > 0) || Filter == "Expired" && (package.IsCancelled || !expired) || Filter == "Cancelled" && !package.IsCancelled) return false;
        }
        return string.IsNullOrWhiteSpace(Search) || _columns.Any(c => (c.Value(entity) is IFormattable formatted && c.Format != null ? formatted.ToString(c.Format, CultureInfo.InvariantCulture) : c.Value(entity) is Enum enumValue ? EnumLabelConverter.Label(enumValue) : c.Value(entity)?.ToString() ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
    }
    private void RefreshFilter() { View.Refresh(); if (Selected != null && !View.Contains(Selected)) Selected = null; Summary = $"{View.Cast<T>().Count():N0} matching records · {Items.Count:N0} total"; Filtered?.Invoke(); }
    public void Reload()
    {
        Reloading?.Invoke();
        var selectedId = Selected?.Id;
        var list = _service.List<T>(_user).AsEnumerable(); if (_scope != null) list = _scope(list);
        Items.Clear(); foreach (var item in list) Items.Add(item);
        Selected = selectedId == null ? null : Items.FirstOrDefault(i => i.Id == selectedId);
        RefreshFilter(); Reloaded?.Invoke();
    }
    private void Save(T entity) { if (_save != null) _save(entity); else _service.Save(entity, _user); }
    private void Add() => ManagementUi.Try(() =>
    {
        var entity = new T(); _initialize?.Invoke(entity);
        if (_editor != null ? _editor(entity) : EntityForm.Edit(entity, typeof(T) == typeof(SalonService) ? "Add New Service" : $"Add {_singular}", _fields(entity), Save, _validate)) Reload();
    });
    private void Edit() => ManagementUi.Try(() =>
    {
        if (Selected == null) return;
        var entity = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Selected))!;
        if (_editor != null ? _editor(entity) : EntityForm.Edit(entity, typeof(T) == typeof(SalonService) ? "Edit Service" : $"Edit {_singular}", _fields(entity), Save, _validate)) Reload();
    });
    private void ToggleActive() => ManagementUi.Try(() =>
    {
        if (Selected == null) return;
        var property = typeof(T).GetProperty("IsActive")!;
        var active = (bool)property.GetValue(Selected)!;
        if (!ManagementUi.Confirm($"{(active ? "Disable" : "Activate")} this {_singular.ToLowerInvariant()}? Existing transactions and history remain available.")) return;
        var entity = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Selected))!;
        property.SetValue(entity, !active); Save(entity); Reload(); Ui.Toast(active ? "Record disabled." : "Record activated.");
    });
}

internal sealed class EntityPage<T> : UserControl where T : Entity, new()
{
    public EntityPageModel<T> Model { get; }
    public WrapPanel Toolbar { get; }
    public StackPanel Top { get; }
    public StackPanel SummaryRegion { get; }
    private readonly WrapPanel _cards = new();
    private readonly Dictionary<Guid, Border> _cardElements = [];
    private readonly DataGrid _grid;
    private readonly ScrollViewer _cardsScroll;
    private readonly StackPanel _viewOptions = new() { Orientation = Orientation.Horizontal };
    private readonly Border _recordSurface;
    private readonly ScrollViewer _headerScroll;
    private readonly StackPanel _footer;
    private readonly ContentControl _emptyOverlay;
    private bool _recordsVisible = true;
    private Func<T, FrameworkElement>? _cardFactory;
    private bool _showCards;
    private bool _renderPending;
    public EntityPage(BusinessService service, UserAccount user, string title, string description, string singular, IReadOnlyList<TableColumn<T>> columns, Func<T, IReadOnlyList<FieldSpec>> fields, IReadOnlyList<EntityAction<T>>? actions = null, Func<IEnumerable<T>, IEnumerable<T>>? scope = null, Action<T>? initialize = null, Action<T>? validate = null, bool canAdd = true, bool canEdit = true, bool canDisable = true, Action<T>? save = null, Func<T, bool>? editor = null)
    {
        Model = new EntityPageModel<T>(service, user, singular, columns, fields, scope, initialize, validate, save, editor); DataContext = Model;
        var root = new DockPanel(); Top = new StackPanel();
        var heading = new Grid { Margin = new Thickness(0, 0, 0, 18) }; heading.ColumnDefinitions.Add(new ColumnDefinition()); heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var intro = new StackPanel(); intro.Children.Add(ManagementUi.Text(title, 28, ManagementUi.Ink, true));
        var subtitle = ManagementUi.Text(description, 13, ManagementUi.Muted); subtitle.Margin = new Thickness(0, 6, 24, 0); subtitle.MaxWidth = 780; intro.Children.Add(subtitle); heading.Children.Add(intro);
        var primary = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        primary.Children.Add(Ui.IconButton("\uE72C", "Refresh records", () => Model.RefreshCommand.Execute(null)));
        if (canAdd) primary.Children.Add(ManagementUi.Button(typeof(T) == typeof(SalonService) ? "+ Add New Service" : $"+ Add {singular}", Model.AddCommand, true));
        Grid.SetColumn(primary, 1); heading.Children.Add(primary); Top.Children.Add(heading);
        SummaryRegion = new StackPanel(); Top.Children.Add(SummaryRegion);
        Toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) }; Top.Children.Add(Toolbar);
        var filters = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        filters.ColumnDefinitions.Add(new ColumnDefinition()); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var searchWrapper = new Grid { Margin = new Thickness(0, 0, 12, 0) };
        var search = new TextBox { Padding = new Thickness(38, 9, 38, 9), MinHeight = 40, ToolTip = $"Search {title.ToLowerInvariant()}" };
        search.SetBinding(TextBox.TextProperty, ManagementUi.Bind(nameof(Model.Search)));
        searchWrapper.Children.Add(search);
        var searchIcon = Ui.Icon("\uE721", 16); searchIcon.Foreground = ManagementUi.Muted; searchIcon.HorizontalAlignment = HorizontalAlignment.Left; searchIcon.Margin = new Thickness(12, 0, 0, 0); searchIcon.IsHitTestVisible = false; searchWrapper.Children.Add(searchIcon);
        var placeholder = ManagementUi.Text($"Search {title.ToLowerInvariant()}…", 13, ManagementUi.Muted); placeholder.Margin = new Thickness(38, 0, 38, 0); placeholder.VerticalAlignment = VerticalAlignment.Center; placeholder.IsHitTestVisible = false; searchWrapper.Children.Add(placeholder);
        var clearBtn = Ui.IconButton("\uE711", "Clear search", () => { Model.Search = ""; search.Focus(); }); clearBtn.HorizontalAlignment = HorizontalAlignment.Right; clearBtn.Margin = new Thickness(2); clearBtn.MinHeight = 32; clearBtn.Width = 32;
        searchWrapper.Children.Add(clearBtn);
        void SearchState() { placeholder.Visibility = search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; clearBtn.Visibility = search.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible; }
        search.TextChanged += (_, _) => SearchState(); SearchState();
        filters.Children.Add(searchWrapper);
        var filterValues = new List<string> { "All records" }; if (typeof(T).GetProperty("IsActive") != null) filterValues.AddRange(["Active", "Inactive"]); if (typeof(T) == typeof(Product)) filterValues.AddRange(["Low stock", "Expiring in 30 days"]);
        if (typeof(T) == typeof(Appointment)) filterValues.AddRange(["Pending", "Confirmed", "In progress", "Completed", "Cancelled"]);
        if (typeof(T) == typeof(Expense)) filterValues.AddRange(["Operating expenses", "Commission payouts"]);
        if (typeof(T) == typeof(CustomerPackage)) filterValues.AddRange(["Available sessions", "Expiring soon", "Completed", "Expired", "Cancelled"]);
        if (typeof(T) == typeof(CommissionRecord)) filterValues.AddRange(["Accrued", "Paid", "Reversed"]);
        var filter = new ComboBox { ItemsSource = filterValues, MinWidth = 165, MinHeight = 40, ToolTip = "Filter displayed records" };
        if (filterValues.Count == 1) filter.Visibility = Visibility.Collapsed;
        filter.SetBinding(ComboBox.SelectedItemProperty, ManagementUi.Bind(nameof(Model.Filter))); Grid.SetColumn(filter, 1); filters.Children.Add(filter); Top.Children.Add(filters);
        _viewOptions.Margin = new Thickness(10, 0, 0, 0); Grid.SetColumn(_viewOptions, 2); filters.Children.Add(_viewOptions);
        var footer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        _footer = footer;
        var footerHeading = new Grid { Margin = new Thickness(0, 0, 0, 8) }; footerHeading.ColumnDefinitions.Add(new ColumnDefinition()); footerHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var count = ManagementUi.Text("", 11, ManagementUi.Muted); count.SetBinding(TextBlock.TextProperty, new Binding(nameof(Model.Summary))); footerHeading.Children.Add(count);
        var selection = ManagementUi.Text("Select a record to see available actions", 11, ManagementUi.Muted); selection.TextWrapping = TextWrapping.NoWrap; selection.TextTrimming = TextTrimming.CharacterEllipsis; selection.MaxWidth = 440; Grid.SetColumn(selection, 1); footerHeading.Children.Add(selection); footer.Children.Add(footerHeading);
        var contextActions = new WrapPanel();
        if (canEdit) contextActions.Children.Add(ManagementUi.Button(typeof(T) == typeof(SalonService) ? "Edit Service" : "Edit selected", Model.EditCommand));
        if (canDisable && typeof(T).GetProperty("IsActive") != null) contextActions.Children.Add(ManagementUi.Button("Activate / disable", Model.DisableCommand));
        if (actions is { Count: > 0 }) foreach (var action in actions)
        {
            var button = ManagementUi.Button(action.Label, new UiCommand(() => ManagementUi.Try(() => { action.Run(Model.Selected!); Model.Reload(); }), () => Model.Selected != null));
            if ((action.Label.StartsWith("Remove") || action.Label.StartsWith("Delete") || action.Label == "Cancel") && Application.Current.TryFindResource("DangerButton") is Style danger) button.Style = danger;
            contextActions.Children.Add(button);
        }
        footer.Children.Add(contextActions); DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var headerScroll = new ScrollViewer { Content = Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, MaxHeight = 420 };
        _headerScroll = headerScroll;
        SizeChanged += (_, _) => { UpdateHeaderHeight(); QueueCards(); };
        DockPanel.SetDock(headerScroll, Dock.Top); root.Children.Add(headerScroll);
        _grid = ManagementUi.Table(Model.View);
        _grid.SetBinding(DataGrid.SelectedItemProperty, ManagementUi.Bind(nameof(Model.Selected)));
        foreach (var column in columns)
        {
            DataGridColumn rendered;
            if (column.Content != null || column.Header is "Status" or "Alert" or "Pricing" or "Confirmed" or "Role")
            {
                var visual = new FrameworkElementFactory(typeof(ContentControl)); visual.SetValue(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch); visual.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center); visual.SetBinding(ContentControl.ContentProperty, new Binding { Converter = new CellVisualConverter<T>(column) });
                rendered = new DataGridTemplateColumn { Header = column.Header, CellTemplate = new DataTemplate { VisualTree = visual } };
            }
            else
            {
                var style = new Style(typeof(TextBlock)); style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)); style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis)); style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(4, 0, 4, 0))); style.Setters.Add(new Setter(TextBlock.ToolTipProperty, new Binding { Converter = new RowValueConverter<T>(column.Value), StringFormat = column.Format }));
                if (column.Header.Contains("price", StringComparison.OrdinalIgnoreCase) || column.Header.Contains("cost", StringComparison.OrdinalIgnoreCase) || column.Header is "Amount" or "Paid" or "Rate" or "Stock value") { style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)); style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold)); }
                rendered = new DataGridTextColumn { Header = column.Header, Binding = new Binding { Converter = new RowValueConverter<T>(column.Value), StringFormat = column.Format }, ElementStyle = style };
            }
            rendered.Width = new DataGridLength(column.Width, DataGridLengthUnitType.Star); rendered.MinWidth = column.Header == "Confirmed" ? 145 : column.Header is "Status" or "Alert" or "Role" or "Pricing" ? 120 : 80; rendered.CanUserSort = true; _grid.Columns.Add(rendered);
        }
        _grid.Sorting += (_, e) =>
        {
            e.Handled = true; var index = _grid.Columns.IndexOf(e.Column); if (index < 0 || Model.View is not ListCollectionView list) return;
            var direction = e.Column.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            foreach (var column in _grid.Columns) column.SortDirection = null; e.Column.SortDirection = direction;
            list.CustomSort = new RowComparer<T>(columns[index].Value, direction); QueueCards();
        };
        _grid.MouseDoubleClick += (_, e) => { if (e.OriginalSource is DependencyObject source && FindRow(source) != null && canEdit && Model.EditCommand.CanExecute(null)) Model.EditCommand.Execute(null); };
        var tablePanel = new Grid(); tablePanel.Children.Add(_grid);
        _cardsScroll = new ScrollViewer { Content = _cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Visibility = Visibility.Collapsed }; tablePanel.Children.Add(_cardsScroll);
        var emptyTitle = typeof(T) == typeof(Appointment) ? "A little room in your schedule" : typeof(T) == typeof(CustomerPackage) ? "No prepaid sessions to show" : "Your directory starts here";
        var empty = new ContentControl();
        _emptyOverlay = empty; empty.Margin = new Thickness(0, 42, 0, 0);
        void RefreshEmpty()
        {
            var existing = Model.Items.Count > 0;
            empty.Content = ManagementUi.Empty(existing ? "No records match this view" : emptyTitle, existing ? "Try a different search or filter to find the record you need." : canAdd ? $"Add your first {singular} to begin, or choose another date or filter." : typeof(T) == typeof(CustomerPackage) ? "Paid package purchases will appear here with remaining sessions and visit history." : "Recorded activity will appear here when it is available.", compact: tablePanel.ActualHeight > 0 && tablePanel.ActualHeight < 280);
        }
        RefreshEmpty(); Model.Reloaded += RefreshEmpty;
        empty.HorizontalAlignment = HorizontalAlignment.Center; empty.VerticalAlignment = VerticalAlignment.Center;
        empty.SetBinding(VisibilityProperty, new Binding("View.IsEmpty") { Converter = new BooleanToVisibilityConverter() }); tablePanel.Children.Add(empty);
        tablePanel.SizeChanged += (_, _) => RefreshEmpty();
        _recordSurface = ManagementUi.Card(tablePanel, new Thickness(12)); root.Children.Add(_recordSurface);
        Model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Model.Selected)) { selection.Text = Model.Selected == null ? "Select a record to see available actions" : $"Selected · {columns[0].Value(Model.Selected)}"; selection.ToolTip = selection.Text; UpdateCardSelection(); } if (e.PropertyName == nameof(Model.Summary)) RefreshEmpty(); };
        Model.View.CollectionChanged += (_, _) => QueueCards(); Model.Reloaded += QueueCards;
        Content = root; Loaded += (_, _) => Ui.Enter(root);
    }

    public void SetCardView(Func<T, FrameworkElement> factory, bool defaultCards = false)
    {
        _cardFactory = factory;
        void SetMode(bool cards) { _showCards = cards; _grid.Visibility = cards ? Visibility.Collapsed : Visibility.Visible; _cardsScroll.Visibility = cards ? Visibility.Visible : Visibility.Collapsed; _emptyOverlay.Margin = cards ? new Thickness(0) : new Thickness(0, 42, 0, 0); _viewOptions.Children.Clear(); _viewOptions.Children.Add(Ui.Pill("Table", () => SetMode(false), !cards)); _viewOptions.Children.Add(Ui.Pill("Cards", () => SetMode(true), cards)); QueueCards(); }
        SetMode(defaultCards);
    }

    public void SetRecordAreaVisible(bool visible)
    {
        _recordsVisible = visible; _recordSurface.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _viewOptions.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) Model.Selected = null;
        UpdateHeaderHeight();
    }

    private void UpdateHeaderHeight() => _headerScroll.MaxHeight = Math.Max(170, ActualHeight - (_recordsVisible ? Math.Max(220, _footer.ActualHeight + 180) : _footer.ActualHeight + 8));

    private void QueueCards()
    {
        if (!_showCards || _cardFactory == null || _renderPending) return; _renderPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _renderPending = false; if (!_showCards || _cardFactory == null) return; _cards.Children.Clear(); _cardElements.Clear();
            var columns = Math.Max(1, (int)Math.Floor(Math.Max(260, ActualWidth - 28) / 310)); var width = Math.Max(240, (ActualWidth - 36) / columns - 12);
            foreach (var item in Model.View.Cast<T>())
            {
                var content = _cardFactory(item); var card = Ui.InteractiveCard(content); card.Width = width; card.Margin = new Thickness(4, 4, 8, 8); card.Padding = new Thickness(18); card.Focusable = true; card.Cursor = Cursors.Hand;
                card.MouseLeftButtonUp += (_, _) => Model.Selected = item; card.KeyDown += (_, e) => { if (e.Key is Key.Enter or Key.Space) { Model.Selected = item; e.Handled = true; } }; _cards.Children.Add(card); _cardElements[item.Id] = card;
            }
            UpdateCardSelection();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UpdateCardSelection()
    {
        foreach (var pair in _cardElements)
        {
            if (pair.Key == Model.Selected?.Id) { pair.Value.BorderBrush = ManagementUi.Rose; pair.Value.BorderThickness = new Thickness(2); }
            else { pair.Value.ClearValue(Border.BorderBrushProperty); pair.Value.ClearValue(Border.BorderThicknessProperty); }
        }
    }

    private static DataGridRow? FindRow(DependencyObject? element)
    {
        while (element is Visual or System.Windows.Media.Media3D.Visual3D) { if (element is DataGridRow row) return row; element = VisualTreeHelper.GetParent(element); if (element == null) return null; }
        return null;
    }
}

internal sealed class RowComparer<T>(Func<T, object?> value, ListSortDirection direction) : System.Collections.IComparer
{
    public int Compare(object? x, object? y)
    {
        var a = x is T first ? value(first) : null; var b = y is T second ? value(second) : null;
        static object? Normalize(object? input)
        {
            if (input is string text && (text.StartsWith('₱') || text.EndsWith('%')) && decimal.TryParse(text.Trim('₱', '%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return amount;
            return input;
        }
        a = Normalize(a); b = Normalize(b);
        var result = a == null ? b == null ? 0 : -1 : b == null ? 1 : a is IComparable comparable && a.GetType() == b.GetType() ? comparable.CompareTo(b) : StringComparer.CurrentCultureIgnoreCase.Compare(a.ToString(), b.ToString());
        return direction == ListSortDirection.Ascending ? result : -result;
    }
}

internal sealed class CellVisualConverter<T>(TableColumn<T> column) : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not T row) return null; if (column.Content != null) return column.Content(row);
        var text = column.Value(row)?.ToString() ?? "—"; if (text == "InProgress") text = "In progress"; if (text == "StartsAt") text = "Starts at";
        var badge = Ui.StatusPill(text); if (badge.Child is TextBlock label) { label.TextWrapping = TextWrapping.NoWrap; label.TextTrimming = TextTrimming.CharacterEllipsis; } badge.ToolTip = text; return badge;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

internal sealed class RowValueConverter<T>(Func<T, object?> value) : IValueConverter
{
    public object? Convert(object row, Type targetType, object parameter, CultureInfo culture)
    {
        if (row is not T typed) return null;
        var val = value(typed);
        if (val is bool b) return b ? "● Active" : "○ Inactive";
        if (val is AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Confirmed => "✓ Confirmed",
                AppointmentStatus.Completed => "✓ Completed",
                AppointmentStatus.InProgress => "▶ In progress",
                AppointmentStatus.Pending => "⏳ Pending",
                AppointmentStatus.Cancelled => "✕ Cancelled",
                _ => status.ToString()
            };
        }
        if (val is string s)
        {
            if (s.Equals("Review required", StringComparison.OrdinalIgnoreCase)) return "⚠ Review required";
            if (s.Equals("Active", StringComparison.OrdinalIgnoreCase)) return "● Active";
            if (s.Equals("Inactive", StringComparison.OrdinalIgnoreCase)) return "○ Inactive";
        }
        return val;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
