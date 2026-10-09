using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Roselie.Core.Models;
using Roselie.Infrastructure;

namespace Roselie.App.Management;

public static partial class ManagementPageFactory
{
    // The isolated smoke harness captures each form and closes it without saving.
    internal static void SmokeDialogs(BusinessService service, UserAccount user, AuthService auth)
    {
        if (Ui.SmokeDialogObserver == null) throw new InvalidOperationException("Management dialog smoke requires the capture observer.");
        var builder = new Builder(service, user, auth);
        ((EntityPage<SalonService>)builder.Services()).Model.AddCommand.Execute(null);
        ((EntityPage<Product>)builder.Products()).Model.AddCommand.Execute(null);
        ((EntityPage<Customer>)builder.Customers()).Model.AddCommand.Execute(null);
        ((EntityPage<Appointment>)builder.Appointments()).Model.AddCommand.Execute(null);
        ((EntityPage<Employee>)builder.Employees()).Model.AddCommand.Execute(null);
        ((EntityPage<Expense>)builder.Expenses()).Model.AddCommand.Execute(null);
    }

    internal static void SelectCalendarView(FrameworkElement page, string view)
    {
        if (view is not ("Day" or "Week" or "Month")) throw new ArgumentException("Choose Day, Week or Month.", nameof(view));
        var button = Descendants(page).OfType<Button>().FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "Calendar.View." + view)
            ?? throw new InvalidOperationException("The appointment calendar is not displayed.");
        button.Command?.Execute(null);
    }

    internal static void SmokeAccountDialogs(BusinessService service, UserAccount user, AuthService auth)
    {
        if (Ui.SmokeDialogObserver == null) throw new InvalidOperationException("Account dialog smoke requires the capture observer.");
        ShowAccountManagement(service, user, auth);
        ShowEmployeeAccountCreation(service, user, auth);
    }

    internal static void SmokeSuperPinDialog(AuthService auth, UserAccount user)
    {
        if (Ui.SmokeDialogObserver == null) throw new InvalidOperationException("Super PIN dialog smoke requires the capture observer.");
        ShowSuperPinSettings(auth, user);
    }

    internal static void SmokeDeletionDialogs(BusinessService service, UserAccount user, AuthService auth)
    {
        if (Ui.SmokeDialogObserver == null) throw new InvalidOperationException("Protected deletion dialog smoke requires the capture observer.");
        var builder = new Builder(service, user, auth);
        var offer = service.List<PackageOffer>(user).FirstOrDefault() ?? throw new InvalidOperationException("Create an isolated package offer fixture before capturing its deletion dialog.");
        // A committed service deletion also removes its offers. Keep the separate offer fixture available.
        var salonService = service.List<SalonService>(user).FirstOrDefault(s => s.Id != offer.ServiceId) ?? throw new InvalidOperationException("Create an isolated service fixture that is separate from the package offer before capturing deletion dialogs.");
        var expense = service.List<Expense>(user).FirstOrDefault() ?? throw new InvalidOperationException("Create an isolated expense fixture before capturing its deletion dialog.");
        builder.DeleteService(salonService); builder.DeletePackageOffer(offer); builder.DeleteExpense(expense);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        yield return parent;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }
}
