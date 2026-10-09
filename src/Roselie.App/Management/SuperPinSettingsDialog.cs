using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App.Management;

internal static class SuperPinSettingsDialog
{
    internal static bool Show(AuthService auth, UserAccount user)
    {
        var existing = auth.IsSuperPinEnabled(user);
        var window = ManagementUi.Window(existing ? "Change Super PIN" : "Create Super PIN", 560, 660);
        window.MinHeight = 420; window.SizeToContent = SizeToContent.Height;
        window.MaxHeight = Math.Min(window.MaxHeight, window.Owner is { ActualHeight: > 0 } owner ? Math.Max(420, owner.ActualHeight - 32) : 740);
        var root = new DockPanel { Margin = new Thickness(24) };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var footer = new Border { Child = actions, BorderBrush = ManagementUi.Line, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 16, 0, 0), Margin = new Thickness(0, 16, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);

        var body = new StackPanel(); body.Children.Add(Ui.Badge("PROTECTED DELETIONS", "#FAF0F0", "#A85E67", 10));
        var explanation = ManagementUi.Text("Use a separate Super PIN to delete services, package offers, and expenses. Your sign-in PIN stays unchanged.", 13, ManagementUi.Muted);
        explanation.Margin = new Thickness(0, 14, 0, 20); body.Children.Add(explanation);
        PasswordBox Field(string label, string id, int maximum)
        {
            body.Children.Add(ManagementUi.Text(label + " *", 13, ManagementUi.Ink, true));
            var box = new PasswordBox { MinHeight = 42, MaxLength = maximum, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 7, 0, 16) };
            AutomationProperties.SetName(box, label + ", required"); AutomationProperties.SetAutomationId(box, id); body.Children.Add(box); return box;
        }
        var password = Field("Current administrator password", "SuperPin.CurrentPassword", 256);
        var pin = Field(existing ? "New Super PIN" : "Super PIN", "SuperPin.NewPin", 12);
        var confirm = Field("Confirm Super PIN", "SuperPin.ConfirmPin", 12);
        body.Children.Add(ManagementUi.Text("Choose 6 to 12 digits. The PIN is protected locally and is never shown in account lists.", 12, ManagementUi.Muted));
        var errorText = ManagementUi.Text("", 12, Ui.Brush("#C84D58"));
        var error = new Border { Child = errorText, Background = Ui.Brush("#FFF0F2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 16, 0, 0), Visibility = Visibility.Collapsed }; body.Children.Add(error);
        void ClearSecrets() { password.Clear(); pin.Clear(); confirm.Clear(); }
        var busy = false; Button? save = null;
        var label = existing ? "Update Super PIN" : "Create Super PIN";
        var command = new UiCommand(() =>
        {
            busy = true; CommandManager.InvalidateRequerySuggested(); if (save != null) save.Content = "Saving…";
            try
            {
                if (string.IsNullOrEmpty(password.Password)) throw new InvalidOperationException("Enter your current administrator password.");
                if (pin.Password.Length is < 6 or > 12 || !pin.Password.All(c => c is >= '0' and <= '9')) throw new InvalidOperationException("Use a Super PIN containing 6 to 12 digits.");
                if (pin.Password != confirm.Password) throw new InvalidOperationException("The Super PIN entries do not match.");
                auth.EnrollSuperPin(pin.Password, password.Password, user);
                ClearSecrets(); window.DialogResult = true; Ui.Toast(existing ? "Super PIN updated." : "Super PIN created. Protected deletions are ready.");
            }
            catch (Exception ex)
            {
                ClearSecrets(); errorText.Text = ex.GetBaseException().Message; error.Visibility = Visibility.Visible; password.Focus();
            }
            finally { busy = false; if (save != null) save.Content = label; CommandManager.InvalidateRequerySuggested(); }
        }, () => !busy && password.Password.Length > 0 && pin.Password.Length > 0 && confirm.Password.Length > 0);
        var cancel = ManagementUi.Button("Cancel", new UiCommand(() => window.DialogResult = false)); cancel.IsCancel = true; cancel.Margin = new Thickness(0, 0, 12, 0); actions.Children.Add(cancel);
        save = ManagementUi.Button(label, command, true); save.IsDefault = true; save.Margin = new Thickness(0); AutomationProperties.SetAutomationId(save, "SuperPin.Save"); actions.Children.Add(save);
        foreach (var box in new[] { password, pin, confirm }) box.PasswordChanged += (_, _) => { error.Visibility = Visibility.Collapsed; CommandManager.InvalidateRequerySuggested(); };
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); window.Content = root;
        window.Loaded += (_, _) => password.Focus(); window.Closed += (_, _) => ClearSecrets();
        return window.ShowDialog() == true;
    }
}
