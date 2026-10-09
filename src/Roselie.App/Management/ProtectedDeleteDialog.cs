using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace Roselie.App.Management;

internal static class ProtectedDeleteDialog
{
    internal static bool Show(string title, string target, string description, Func<bool> configured, Action enroll, Action<string, string> commit)
    {
        var ready = configured();
        var window = ManagementUi.Window(title, 560, 620);
        window.MinHeight = 420; window.SizeToContent = SizeToContent.Height;
        window.MaxHeight = Math.Min(window.MaxHeight, window.Owner is { ActualHeight: > 0 } owner ? Math.Max(420, owner.ActualHeight - 32) : 720);
        var root = new DockPanel { Margin = new Thickness(24) };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var footer = new Border { Child = actions, BorderBrush = ManagementUi.Line, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 16, 0, 0), Margin = new Thickness(0, 16, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var body = new StackPanel(); body.Children.Add(Ui.Badge("ADMINISTRATOR APPROVAL", "#FFF0F2", "#C84D58", 10));
        var name = ManagementUi.Text(target, 21, ManagementUi.Ink, true); name.Margin = new Thickness(0, 16, 0, 10); body.Children.Add(name);
        body.Children.Add(ManagementUi.Text(description, 13, ManagementUi.Muted));
        var setup = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        setup.Children.Add(ManagementUi.Text("Create your separate Super PIN before deleting records.", 13, ManagementUi.Ink, true));
        var setupHelp = ManagementUi.Text("It authorizes protected deletions. Keep it separate from your sign-in PIN.", 12, ManagementUi.Muted); setupHelp.Margin = new Thickness(0, 7, 0, 12); setup.Children.Add(setupHelp);
        var setupButton = Ui.Button("Create Super PIN", () =>
        {
            enroll(); ready = configured(); setup.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
            CommandManager.InvalidateRequerySuggested();
        }, true);
        setup.Children.Add(setupButton); body.Children.Add(setup);
        var fields = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        fields.Children.Add(ManagementUi.Text("Reason for deletion *", 13, ManagementUi.Ink, true));
        var reason = new TextBox { MinHeight = 66, TextWrapping = TextWrapping.Wrap, MaxLength = 500, Margin = new Thickness(0, 7, 0, 16) }; AutomationProperties.SetName(reason, "Reason for deletion, required"); AutomationProperties.SetAutomationId(reason, "Deletion.Reason"); fields.Children.Add(reason);
        fields.Children.Add(ManagementUi.Text("Super PIN *", 13, ManagementUi.Ink, true));
        var pin = new PasswordBox { MinHeight = 42, MaxLength = 12, Margin = new Thickness(0, 7, 0, 7) }; AutomationProperties.SetName(pin, "Super PIN, required"); AutomationProperties.SetAutomationId(pin, "Deletion.SuperPin"); fields.Children.Add(pin);
        fields.Children.Add(ManagementUi.Text("Enter the separate 6–12 digit Super PIN to approve this deletion.", 12, ManagementUi.Muted));
        body.Children.Add(fields);
        var errorText = ManagementUi.Text("", 12, Ui.Brush("#C84D58"));
        var error = new Border { Child = errorText, Background = Ui.Brush("#FFF0F2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 16, 0, 0), Visibility = Visibility.Collapsed }; body.Children.Add(error);
        var busy = false;
        var cancel = ManagementUi.Button("Cancel", new UiCommand(() => window.DialogResult = false)); cancel.IsCancel = true; cancel.Margin = new Thickness(0, 0, 12, 0); actions.Children.Add(cancel);
        Button? delete = null;
        var command = new UiCommand(() =>
        {
            busy = true; CommandManager.InvalidateRequerySuggested(); if (delete != null) delete.Content = "Deleting…";
            try
            {
                if (!ready) throw new InvalidOperationException("Create a Super PIN before deleting this record.");
                if (string.IsNullOrWhiteSpace(reason.Text)) throw new InvalidOperationException("Enter a reason for deletion.");
                if (reason.Text.Any(char.IsControl)) throw new InvalidOperationException("Enter a deletion reason without line breaks or control characters.");
                if (pin.Password.Length is < 6 or > 12 || !pin.Password.All(c => c is >= '0' and <= '9')) throw new InvalidOperationException("Use a Super PIN containing 6 to 12 digits.");
                commit(pin.Password, reason.Text.Trim());
                pin.Clear(); window.DialogResult = true; Ui.Toast("Deletion saved. Historical records are preserved.");
            }
            catch (Exception ex)
            {
                errorText.Text = ex.GetBaseException().Message; error.Visibility = Visibility.Visible;
                pin.Clear(); pin.Focus();
            }
            finally { busy = false; if (delete != null) delete.Content = "Delete"; CommandManager.InvalidateRequerySuggested(); }
        }, () => ready && !busy && !string.IsNullOrWhiteSpace(reason.Text) && pin.Password.Length > 0);
        delete = ManagementUi.Button("Delete", command); if (Application.Current.TryFindResource("DangerButton") is Style danger) delete.Style = danger; delete.Margin = new Thickness(0); AutomationProperties.SetAutomationId(delete, "Deletion.Confirm"); actions.Children.Add(delete);
        reason.TextChanged += (_, _) => { error.Visibility = Visibility.Collapsed; CommandManager.InvalidateRequerySuggested(); };
        pin.PasswordChanged += (_, _) => CommandManager.InvalidateRequerySuggested();
        setup.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); window.Content = root;
        window.Loaded += (_, _) => { if (ready) reason.Focus(); else setupButton.Focus(); };
        window.Closed += (_, _) => pin.Clear();
        return window.ShowDialog() == true;
    }
}
