using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Roselie.Core.Models;

namespace Roselie.App.Management;

// Kept separately from the catalog pages so the scheduling and account workflows remain easy to review.
public static partial class ManagementPageFactory
{
    private sealed partial class Builder
    {
        public FrameworkElement Appointments()
        {
            var selectedDate = Local(DateTime.UtcNow).Date;
            var mode = "Day";
            var customers = Read<Customer>().ToDictionary(c => c.Id, c => c.FullName);
            var services = Read<SalonService>().ToDictionary(s => s.Id, s => s.Name);
            var employees = Read<Employee>().ToDictionary(e => e.Id, e => e.FullName);
            (DateTime Start, DateTime End) Range()
            {
                var day = selectedDate;
                if (mode == "Week") { var offset = ((int)day.DayOfWeek + 6) % 7; day = day.AddDays(-offset); return (day, day.AddDays(7)); }
                if (mode == "Month") { day = new DateTime(day.Year, day.Month, 1); return (day, day.AddMonths(1)); }
                return (day, day.AddDays(1));
            }
            var page = new EntityPage<Appointment>(service, user, "Appointments", "Manage local salon visits, staff assignments and appointment status. Staff scheduling conflicts are checked before saving.", "appointment",
            [Col<Appointment>("Start", a => Local(a.StartsAtUtc), "ddd dd MMM HH:mm", 1.5), Col<Appointment>("End", a => Local(a.EndsAtUtc), "HH:mm"), Col<Appointment>("Customer", a => customers.GetValueOrDefault(a.CustomerId, "Archived customer"), width: 2), Col<Appointment>("Service", a => services.GetValueOrDefault(a.ServiceId, "Archived service"), width: 2), Col<Appointment>("Staff", a => a.EmployeeId == null ? "Unassigned" : employees.GetValueOrDefault(a.EmployeeId.Value, "Archived staff"), width: 1.5), Col<Appointment>("Status", a => a.Status), Col<Appointment>("Notes", a => a.Notes, width: 2)],
            _ => [], [new("Visit details", a => AppointmentDetails(a)), new("Confirm", a => SetStatus(a, AppointmentStatus.Confirmed)), new("Start visit", a => SetStatus(a, AppointmentStatus.InProgress)), new("Complete", a => SetStatus(a, AppointmentStatus.Completed)), new("Cancel", a => SetStatus(a, AppointmentStatus.Cancelled))],
            scope: rows => { var range = Range(); var start = Utc(range.Start); var end = Utc(range.End); return rows.Where(a => a.StartsAtUtc < end && a.EndsAtUtc > start && (user.Role != RoleKind.SalonStaff || a.EmployeeId == user.EmployeeId)).OrderBy(a => a.StartsAtUtc); },
            initialize: a => { a.StartsAtUtc = Utc(selectedDate.AddHours(9)); a.EndsAtUtc = a.StartsAtUtc.AddMinutes(30); a.RecordedByUserId = user.Id; if (user.Role == RoleKind.SalonStaff) a.EmployeeId = user.EmployeeId; }, canAdd: user.Role != RoleKind.SalonStaff, canEdit: user.Role != RoleKind.SalonStaff, canDisable: false, editor: EditAppointment);

            var filterPanel = new StackPanel();
            var controls = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            var date = new DatePicker { SelectedDate = selectedDate, Width = 150, Margin = new Thickness(0, 0, 10, 6), MinHeight = 36, ToolTip = "Choose the calendar date" };
            var modes = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0) };
            var rangeText = ManagementUi.Text("", 15, ManagementUi.Ink, true); rangeText.Margin = new Thickness(0, 0, 0, 10);
            var counts = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            var weekdays = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 5) };
            foreach (var label in new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" }) { var day = ManagementUi.Text(label, 10, ManagementUi.Muted, true); day.TextAlignment = TextAlignment.Center; weekdays.Children.Add(day); }
            var calendar = new UniformGrid { Columns = 7 };
            var calendarScroll = new ScrollViewer { Content = calendar, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            var periodHeading = new Grid(); periodHeading.ColumnDefinitions.Add(new ColumnDefinition()); periodHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); periodHeading.Children.Add(rangeText); Grid.SetColumn(counts, 1); periodHeading.Children.Add(counts);
            filterPanel.Children.Add(controls); filterPanel.Children.Add(periodHeading); filterPanel.Children.Add(weekdays); filterPanel.Children.Add(calendarScroll);
            var changingDate = false;
            var allVisits = Read<Appointment>().Where(a => user.Role != RoleKind.SalonStaff || a.EmployeeId == user.EmployeeId).ToList();
            void ModeControls()
            {
                modes.Children.Clear();
                foreach (var value in new[] { "Day", "Week", "Month" }) { var button = Ui.Pill(value, () => { mode = value; ModeControls(); page.Model.Reload(); }, mode == value); System.Windows.Automation.AutomationProperties.SetAutomationId(button, "Calendar.View." + value); modes.Children.Add(button); }
                page.SetRecordAreaVisible(mode != "Month");
            }
            void SelectDate(DateTime day, bool dayView = false)
            {
                if (dayView) { mode = "Day"; ModeControls(); }
                selectedDate = day.Date; changingDate = true; date.SelectedDate = selectedDate; changingDate = false; page.Model.Reload();
            }
            controls.Children.Add(Ui.IconButton("\uE76B", "Previous calendar period", () => SelectDate(mode == "Month" ? selectedDate.AddMonths(-1) : selectedDate.AddDays(mode == "Week" ? -7 : -1))));
            controls.Children.Add(date);
            controls.Children.Add(Ui.IconButton("\uE76C", "Next calendar period", () => SelectDate(mode == "Month" ? selectedDate.AddMonths(1) : selectedDate.AddDays(mode == "Week" ? 7 : 1))));
            controls.Children.Add(Ui.Button("Today", () => SelectDate(Local(DateTime.UtcNow).Date))); controls.Children.Add(modes); ModeControls();

            void RefreshCalendar()
            {
                var range = Range();
                rangeText.Text = mode == "Day" ? selectedDate.ToString("dddd, dd MMMM yyyy") : mode == "Month" ? selectedDate.ToString("MMMM yyyy") : $"{range.Start:dd MMM} – {range.End.AddDays(-1):dd MMM yyyy}";
                counts.Children.Clear();
                var periodVisits = page.Model.View.Cast<Appointment>().ToList();
                var hasFilters = !string.IsNullOrWhiteSpace(page.Model.Search) || page.Model.Filter != "All records";
                var total = Ui.Badge($"{periodVisits.Count} {(hasFilters ? "matches" : "visits")}", "#FAF0F0", "#A85E67", 11); total.Margin = new Thickness(0, 0, 8, 4); counts.Children.Add(total);
                foreach (var status in new[] { AppointmentStatus.Pending, AppointmentStatus.Confirmed, AppointmentStatus.InProgress, AppointmentStatus.Completed })
                {
                    var count = periodVisits.Count(a => a.Status == status); if (count == 0) continue;
                    var label = status == AppointmentStatus.InProgress ? "In progress" : status.ToString();
                    var badge = Ui.Badge($"{count} {label.ToLowerInvariant()}", status is AppointmentStatus.Pending or AppointmentStatus.InProgress ? "#FEF3C7" : "#EAF5F0", status is AppointmentStatus.Pending or AppointmentStatus.InProgress ? "#C88B35" : "#27845F", 10); badge.Margin = new Thickness(0, 0, 8, 4); counts.Children.Add(badge);
                }
                calendar.Children.Clear();
                var displayStart = mode == "Month" ? range.Start.AddDays(-(((int)range.Start.DayOfWeek + 6) % 7)) : mode == "Week" ? range.Start : selectedDate.AddDays(-(((int)selectedDate.DayOfWeek + 6) % 7));
                var displayDays = mode == "Month" ? 42 : 7;
                var visits = allVisits.Where(a => page.Model.Matches(a)).ToList();
                for (var n = 0; n < displayDays; n++)
                {
                    var day = displayStart.AddDays(n);
                    var dayStart = Utc(day); var dayEnd = Utc(day.AddDays(1));
                    var daily = visits.Where(a => a.StartsAtUtc < dayEnd && a.EndsAtUtc > dayStart && a.Status != AppointmentStatus.Cancelled).OrderBy(a => a.StartsAtUtc).ToList();
                    var count = daily.Count;
                    var isToday = day == Local(DateTime.UtcNow).Date;
                    var isSelected = day == selectedDate;
                    var body = new StackPanel();
                    var dayLabel = ManagementUi.Text(isToday ? $"{day:dd} · Today" : day.ToString("dd"), mode == "Month" ? 12 : 13, isSelected ? Ui.Brush("#A85E67") : ManagementUi.Ink, true); dayLabel.TextAlignment = TextAlignment.Center; body.Children.Add(dayLabel);
                    var visitLabel = ManagementUi.Text(hasFilters ? count > 0 ? $"{count} matches" : "No matches" : count > 0 ? $"{count} visit{(count == 1 ? "" : "s")}" : "Available", mode == "Month" ? 9 : 10, count > 0 ? Ui.Brush("#A85E67") : ManagementUi.Muted); visitLabel.TextAlignment = TextAlignment.Center; visitLabel.Margin = new Thickness(0, mode == "Month" ? 2 : 4, 0, 0); body.Children.Add(visitLabel);
                    if (mode == "Week" && daily.FirstOrDefault() is Appointment first) { var preview = ManagementUi.Text(Local(first.StartsAtUtc).ToString("HH:mm") + " · " + customers.GetValueOrDefault(first.CustomerId, "Client"), 10, ManagementUi.Muted); preview.TextTrimming = TextTrimming.CharacterEllipsis; preview.TextWrapping = TextWrapping.NoWrap; preview.Margin = new Thickness(0, 6, 0, 0); body.Children.Add(preview); }
                    var button = Ui.Button("", () => SelectDate(day, true)); button.Content = body; button.Padding = mode == "Month" ? new Thickness(5, 2, 5, 2) : new Thickness(5, 7, 5, 7); button.Margin = new Thickness(3); button.MinHeight = mode == "Month" ? 38 : mode == "Week" ? 78 : 66; button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    button.Background = isSelected ? Ui.Brush("#FAF0F0") : Ui.Brush("#FFFFFF"); button.BorderBrush = isSelected ? ManagementUi.Rose : ManagementUi.Line; button.BorderThickness = new Thickness(isSelected ? 2 : 1); button.Opacity = mode == "Month" && day.Month != selectedDate.Month ? .45 : 1;
                    button.ToolTip = daily.Count == 0 ? hasFilters ? "No appointments match this calendar filter." : "No visits booked. Choose this day to create an appointment." : string.Join("\n", daily.Select(a => $"{Local(a.StartsAtUtc):HH:mm} {customers.GetValueOrDefault(a.CustomerId, "Client")} · {services.GetValueOrDefault(a.ServiceId, "Service")} · {a.Status}"));
                    System.Windows.Automation.AutomationProperties.SetName(button, $"{day:dddd dd MMMM yyyy}, {count} visits"); calendar.Children.Add(button);
                }
            }
            page.Model.Filtered += RefreshCalendar;
            page.Model.Reloaded += () => { allVisits = Read<Appointment>().Where(a => user.Role != RoleKind.SalonStaff || a.EmployeeId == user.EmployeeId).ToList(); RefreshCalendar(); };
            date.SelectedDateChanged += (_, _) => { if (!changingDate && date.SelectedDate is DateTime day) SelectDate(day); };
            page.SummaryRegion.Children.Add(ManagementUi.Card(filterPanel, new Thickness(16)));
            page.SetCardView(a =>
            {
                var card = new StackPanel(); card.Children.Add(Ui.StatusPill(a.Status == AppointmentStatus.InProgress ? "In progress" : a.Status.ToString()));
                var visitDate = ManagementUi.Text(Local(a.StartsAtUtc).ToString("ddd, dd MMM yyyy"), 11, ManagementUi.Muted); visitDate.Margin = new Thickness(0, 12, 0, 0); card.Children.Add(visitDate);
                var time = ManagementUi.Text($"{Local(a.StartsAtUtc):HH:mm} – {Local(a.EndsAtUtc):HH:mm}", 23, Ui.Brush("#A85E67"), true); time.Margin = new Thickness(0, 14, 0, 10); card.Children.Add(time);
                card.Children.Add(ManagementUi.Profile(customers.GetValueOrDefault(a.CustomerId, "Archived customer"), services.GetValueOrDefault(a.ServiceId, "Archived service")));
                var staff = ManagementUi.Text(a.EmployeeId == null ? "Staff to assign" : employees.GetValueOrDefault(a.EmployeeId.Value, "Archived staff"), 12, ManagementUi.Muted); staff.Margin = new Thickness(0, 12, 0, 0); card.Children.Add(staff); return card;
            });
            RefreshCalendar(); return page;
        }

        private void AppointmentDetails(Appointment appointment)
        {
            var customer = Read<Customer>().FirstOrDefault(c => c.Id == appointment.CustomerId);
            var salonService = Read<SalonService>().FirstOrDefault(s => s.Id == appointment.ServiceId);
            var employee = appointment.EmployeeId == null ? null : Read<Employee>().FirstOrDefault(e => e.Id == appointment.EmployeeId.Value);
            var panel = new StackPanel(); panel.Children.Add(Ui.StatusPill(appointment.Status == AppointmentStatus.InProgress ? "In progress" : appointment.Status.ToString()));
            var name = ManagementUi.Text(customer?.FullName ?? "Archived customer", 27, ManagementUi.Ink, true); name.Margin = new Thickness(0, 12, 0, 6); panel.Children.Add(name);
            panel.Children.Add(ManagementUi.Text(salonService?.Name ?? "Archived service", 16, ManagementUi.Muted));
            var metrics = ManagementUi.MetricRow(ManagementUi.Metric("Visit date", Local(appointment.StartsAtUtc).ToString("dd MMM yyyy"), Local(appointment.StartsAtUtc).ToString("dddd"), "◇"), ManagementUi.Metric("Appointment time", $"{Local(appointment.StartsAtUtc):HH:mm} – {Local(appointment.EndsAtUtc):HH:mm}", $"{(appointment.EndsAtUtc - appointment.StartsAtUtc).TotalMinutes:0} minutes · Philippine time", "◷"));
            metrics.Margin = new Thickness(0, 22, 0, 0); panel.Children.Add(metrics);
            var details = new StackPanel(); details.Children.Add(ManagementUi.Profile(employee?.FullName ?? "Unassigned staff", employee?.Position ?? "Choose staff while editing the appointment"));
            var phone = ManagementUi.Text("Customer contact · " + (string.IsNullOrWhiteSpace(customer?.Phone) ? "No phone recorded" : customer.Phone), 13, ManagementUi.Muted); phone.Margin = new Thickness(0, 16, 0, 0); details.Children.Add(phone);
            var notes = ManagementUi.Text(string.IsNullOrWhiteSpace(appointment.Notes) ? "No visit notes recorded." : appointment.Notes, 14, ManagementUi.Ink); notes.Margin = new Thickness(0, 16, 0, 0); details.Children.Add(notes);
            if (appointment.OverlapOverride) { var overrideNote = Ui.Badge("Scheduling overlap authorized", "#FEF3C7", "#C88B35"); overrideNote.Margin = new Thickness(0, 16, 0, 0); details.Children.Add(overrideNote); }
            panel.Children.Add(ManagementUi.Card(details)); Show("Visit details", panel);
        }

        private bool EditAppointment(Appointment appointment)
        {
            var start = Local(appointment.StartsAtUtc);
            var draft = new AppointmentDraft { CustomerId = appointment.CustomerId, ServiceId = appointment.ServiceId, EmployeeId = appointment.EmployeeId, Date = start.Date, StartTime = start.ToString("HH:mm"), DurationMinutes = Math.Max(1, (int)(appointment.EndsAtUtc - appointment.StartsAtUtc).TotalMinutes), Status = appointment.Status, Notes = appointment.Notes, OverlapOverride = appointment.OverlapOverride };
            var serviceChoices = Choices<SalonService>(s => s.Name, filter: s => s.IsActive || s.Id == appointment.ServiceId).ToList();
            // A catalog deletion preserves this existing appointment's service relationship.
            if (appointment.ServiceId != Guid.Empty && !serviceChoices.Any(c => Equals(c.Value, appointment.ServiceId)))
                serviceChoices.Add(new Choice(appointment.ServiceId, "Deleted service · original appointment"));
            return EntityForm.Edit(draft, "Appointment details", Fields(new("CustomerId", "Customer", true, Choices: Choices<Customer>(c => c.FullName, filter: c => c.IsActive || c.Id == appointment.CustomerId)), new("ServiceId", "Service", true, Choices: serviceChoices), new("EmployeeId", "Assigned staff", Choices: Choices<Employee>(e => e.FullName, true, e => (e.IsActive || e.Id == appointment.EmployeeId) && (user.Role != RoleKind.SalonStaff || e.Id == user.EmployeeId))), new("Date", "Visit date", true), new("StartTime", "Start time (24-hour HH:mm)", true, Help: "Example: 14:30 for 2:30 PM. All appointment times use Philippine time."), new("DurationMinutes", "Duration (minutes)", true, Min: 1, Max: 1440), new("Status", "Status"), new("Notes", "Notes", MultiLine: true), new("OverlapOverride", "Explicitly allow a staff scheduling overlap", Help: "Administrator authorization is required. Add a reason in Notes.")),
            d =>
            {
                if (!TimeOnly.TryParseExact(d.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) throw new InvalidOperationException("Enter the start time in 24-hour HH:mm format.");
                if (d.OverlapOverride && (user.Role != RoleKind.Administrator || string.IsNullOrWhiteSpace(d.Notes))) throw new InvalidOperationException("A scheduling overlap requires an administrator and a reason in Notes.");
                appointment.CustomerId = d.CustomerId; appointment.ServiceId = d.ServiceId; appointment.EmployeeId = d.EmployeeId;
                appointment.StartsAtUtc = Utc(d.Date.Date.Add(time.ToTimeSpan())); appointment.EndsAtUtc = appointment.StartsAtUtc.AddMinutes(d.DurationMinutes);
                appointment.Status = d.Status; appointment.Notes = d.Notes; appointment.OverlapOverride = d.OverlapOverride; appointment.RecordedByUserId = user.Id; Save(appointment);
            });
        }

        private void SetStatus(Appointment appointment, AppointmentStatus status)
        {
            if (status == AppointmentStatus.Cancelled && !ManagementUi.Confirm("Cancel this appointment? Its history will remain available.")) return;
            var updated = Read<Appointment>().Single(a => a.Id == appointment.Id);
            updated.Status = status; Save(updated);
        }

        public FrameworkElement Employees()
        {
            var accounts = Read<UserAccount>().ToDictionary(a => a.Id);
            UserAccount? Account(Employee employee) => employee.UserAccountId is Guid id && accounts.TryGetValue(id, out var account) ? account : accounts.Values.FirstOrDefault(a => a.EmployeeId == employee.Id);
            var page = new EntityPage<Employee>(service, user, "Employees", "Your salon team, their access and their earnings. Service-specific commissions continue to override each employee's default rate.", "employee",
            [Col<Employee>("Full name", e => e.FullName, width: 2, content: e => ManagementUi.Profile(e.FullName, e.Position)), Col<Employee>("Contact", e => e.Contact), Col<Employee>("Commission", e => CommissionLabel(e)), Col<Employee>("Login", e => Account(e)?.Username ?? "No account"), Col<Employee>("Role", e => RoleLabel(Account(e)?.Role), content: e => RoleBadge(Account(e)?.Role)), Col<Employee>("Status", e => Status(e.IsActive))],
            _ => EmployeeFields(),
            [new("Employee profile", e => EmployeeProfile(e)), new("Login account", e => EmployeeAccount(e)), new("Service commissions", e => ServiceCommissions(e)), new("Commission history", e => CommissionHistory(e))],
            validate: ValidateEmployee);
            void Summary()
            {
                var employees = page.Model.Items; var ids = employees.Select(e => e.Id).ToHashSet();
                var commissions = Read<CommissionRecord>().Where(c => ids.Contains(c.EmployeeId) && !c.IsReversed && c.PaidAtUtc == null).ToList();
                page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Active team", employees.Count(e => e.IsActive).ToString(), "Employees available for visits", "♙"), ManagementUi.Metric("Active logins", accounts.Values.Count(a => a.IsActive && a.EmployeeId != null && ids.Contains(a.EmployeeId.Value)).ToString(), "Linked employee access", "◇"), ManagementUi.Metric("Unpaid commission", Peso(commissions.Sum(c => c.Amount)), "Accrued earnings awaiting payout", "◈")));
            }
            page.Model.Reloading += () => { accounts.Clear(); foreach (var account in Read<UserAccount>()) accounts[account.Id] = account; };
            page.Model.Reloaded += Summary; Summary();
            page.Toolbar.Children.Add(ManagementUi.Button("All login accounts", new UiCommand(AllAccounts)));
            page.SetCardView(e =>
            {
                var account = Account(e); var card = new StackPanel(); card.Children.Add(ManagementUi.Profile(e.FullName, e.Position)); var role = RoleBadge(account?.Role); role.Margin = new Thickness(0, 16, 0, 12); card.Children.Add(role);
                card.Children.Add(ManagementUi.Text(string.IsNullOrWhiteSpace(e.Contact) ? "No contact recorded" : e.Contact, 12, ManagementUi.Muted));
                var commission = ManagementUi.Text("Default commission · " + CommissionLabel(e), 13, ManagementUi.Ink, true); commission.Margin = new Thickness(0, 12, 0, 8); card.Children.Add(commission);
                card.Children.Add(ManagementUi.Text(account == null ? "Login access has not been created" : "Login · " + account.Username, 11, ManagementUi.Muted));
                var status = Ui.StatusPill(e.IsActive ? "Active" : "Inactive"); status.Margin = new Thickness(0, 14, 0, 0); card.Children.Add(status); return card;
            });
            return page;
        }

        private static string CommissionLabel(Employee employee) => employee.CommissionType == CommissionType.Percentage ? $"{employee.CommissionRate:0.##}%" : employee.CommissionType == CommissionType.Fixed ? Peso(employee.CommissionRate) + " per service" : "None";
        private static IReadOnlyList<FieldSpec> EmployeeFields() => Fields(new("FullName", "Full name", true), new("Position", "Position / title", true), new("Contact", "Contact number"), new("CommissionType", "Default commission type"), new("CommissionRate", "Commission amount or percentage", true, Min: 0, Help: "Fixed = pesos per service. Percentage = percentage of eligible net service revenue."), new("IsActive", "Active employee"));
        private static void ValidateEmployee(Employee employee) { if (employee.CommissionType == CommissionType.Percentage && employee.CommissionRate > 100) throw new InvalidOperationException("Percentage commission must be between 0 and 100."); }
        private static string RoleLabel(RoleKind? role) => role == RoleKind.Administrator ? "Administrator" : role == RoleKind.Cashier ? "Cashier" : role == RoleKind.SalonStaff ? "Salon staff" : "No login";
        private static FrameworkElement RoleBadge(RoleKind? role) => Ui.Badge(RoleLabel(role), role == RoleKind.Administrator ? "#F3E7E7" : role == RoleKind.Cashier ? "#EAF1FA" : "#F3F4F6", role == RoleKind.Administrator ? "#A85E67" : role == RoleKind.Cashier ? "#5277A5" : "#77727B");

        private void EmployeeProfile(Employee employee)
        {
            var account = Read<UserAccount>().FirstOrDefault(a => a.EmployeeId == employee.Id || a.Id == employee.UserAccountId);
            var rates = Read<EmployeeServiceCommission>().Count(c => c.EmployeeId == employee.Id);
            var commissions = Read<CommissionRecord>().Where(c => c.EmployeeId == employee.Id && !c.IsReversed).ToList();
            var panel = new StackPanel(); panel.Children.Add(ManagementUi.Profile(employee.FullName, employee.Position));
            var badges = new WrapPanel { Margin = new Thickness(0, 20, 0, 18) }; var role = RoleBadge(account?.Role); role.Margin = new Thickness(0, 0, 8, 0); badges.Children.Add(role); badges.Children.Add(Ui.StatusPill(employee.IsActive ? "Active" : "Inactive")); panel.Children.Add(badges);
            panel.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Default rate", CommissionLabel(employee), rates + " service-specific rate overrides", "◇"), ManagementUi.Metric("Accrued unpaid", Peso(commissions.Where(c => c.PaidAtUtc == null).Sum(c => c.Amount)), "Excludes reversed commissions", "◈"), ManagementUi.Metric("Paid earnings", Peso(commissions.Where(c => c.PaidAtUtc != null).Sum(c => c.Amount)), "Recorded commission payouts", "♧")));
            var access = new StackPanel(); access.Children.Add(ManagementUi.Text("Contact & access", 17, ManagementUi.Ink, true));
            var copy = account == null ? "No login has been linked. Create access with an assigned role from Login account." : $"Username · {account.Username}\nAccess · {RoleLabel(account.Role)} · {(account.IsActive ? "Active login" : "Disabled login")}\nDiscount authorization · {account.MaxDiscountPercent:0.##}%\nCustom service pricing · {(account.AllowCustomPrices ? "Allowed" : "Restricted")}\nLast login · {(account.LastLoginAtUtc == null ? "Never" : Local(account.LastLoginAtUtc.Value).ToString("dd MMM yyyy HH:mm"))}";
            var contact = ManagementUi.Text(string.IsNullOrWhiteSpace(employee.Contact) ? "No contact number recorded." : employee.Contact, 13, ManagementUi.Muted); contact.Margin = new Thickness(0, 12, 0, 12); access.Children.Add(contact); access.Children.Add(ManagementUi.Text(copy, 13, ManagementUi.Ink));
            panel.Children.Add(ManagementUi.Card(access)); var actions = new WrapPanel(); actions.Children.Add(Ui.Button("Login account", () => EmployeeAccount(employee))); actions.Children.Add(Ui.Button("Service commissions", () => ServiceCommissions(employee))); actions.Children.Add(Ui.Button("Commission history", () => CommissionHistory(employee))); panel.Children.Add(actions);
            Show("Employee profile", panel);
        }

        private void EmployeeAccount(Employee employee)
        {
            var account = Read<UserAccount>().FirstOrDefault(a => a.EmployeeId == employee.Id || a.Id == employee.UserAccountId);
            if (account != null) { EditAccount(account); return; }
            if (!employee.IsActive) throw new InvalidOperationException("Activate this employee profile before creating a login account.");
            if (auth == null) throw new InvalidOperationException("Account service is unavailable. Reopen this page from the application navigation.");
            var draft = new AccountDraft();
            EntityForm.Edit(draft, $"Create login · {employee.FullName}", Fields(new("Username", "Username", true), new("Password", "Password", true, Help: "Use at least 12 characters. This password is hashed and never stored as plain text.", Secret: true), new("ConfirmPassword", "Confirm password", true, Secret: true), new("Role", "Role"), new("MaxDiscountPercent", "Maximum authorized discount (%)", true, Min: 0, Max: 100), new("AllowCustomPrices", "Allow custom service prices")),
            d =>
            {
                if (d.Password != d.ConfirmPassword) throw new InvalidOperationException("The passwords do not match.");
                var created = auth.CreateUser(d.Username, d.Password, employee.Id, d.Role, d.MaxDiscountPercent, d.AllowCustomPrices, user);
                employee.UserAccountId = created.Id; Save(employee); d.Password = ""; d.ConfirmPassword = "";
            });
        }

        private bool EditAccount(UserAccount account) => EntityForm.Edit(account, $"Login permissions · {account.Username}", Fields(new("Username", "Username", true), new("Role", "Access role"), new("MaxDiscountPercent", "Maximum authorized discount (%)", true, Min: 0, Max: 100), new("AllowCustomPrices", "Allow custom service prices"), new("IsActive", "Active login account")), Save,
            changed => { if (changed.Id == user.Id && (!changed.IsActive || changed.Role != RoleKind.Administrator)) throw new InvalidOperationException("Use another administrator account to disable or change the role of your current login. This keeps your signed-in session available."); });

        public void Accounts() => AllAccounts();

        private void AllAccounts()
        {
            var employees = Read<Employee>().ToDictionary(e => e.Id);
            var page = new EntityPage<UserAccount>(service, user, "Login accounts", "Create employee access, manage roles and permissions, or reset existing credentials. Passwords and PIN hashes are never displayed.", "employee account",
            [Col<UserAccount>("Username", a => a.Username, width: 2), Col<UserAccount>("Employee", a => a.EmployeeId != null && employees.TryGetValue(a.EmployeeId.Value, out var employee) ? employee.FullName : "Owner / unlinked account", width: 2), Col<UserAccount>("Role", a => RoleLabel(a.Role), content: a => RoleBadge(a.Role)), Col<UserAccount>("Discount limit", a => $"{a.MaxDiscountPercent:0.##}%"), Col<UserAccount>("Custom prices", a => a.AllowCustomPrices ? "Allowed" : "Restricted"), Col<UserAccount>("Last login", a => a.LastLoginAtUtc is DateTime login ? Local(login) : null, content: a => ManagementUi.Text(a.LastLoginAtUtc is DateTime login ? Local(login).ToString("dd MMM yyyy HH:mm") : "Never", 12, ManagementUi.Muted)), Col<UserAccount>("Status", a => Status(a.IsActive))],
            _ => [], [new("Reset password", a => ResetPassword(a))],
            canDisable: false, editor: account => Read<UserAccount>().Any(a => a.Id == account.Id) ? EditAccount(account) : CreateEmployeeLogin());
            void Summary()
            {
                var rows = page.Model.Items; var eligible = employees.Values.Count(e => e.IsActive && !rows.Any(a => a.EmployeeId == e.Id || a.Id == e.UserAccountId));
                page.SummaryRegion.Children.Clear(); page.SummaryRegion.Children.Add(ManagementUi.MetricRow(ManagementUi.Metric("Active access", rows.Count(a => a.IsActive).ToString(), "Enabled local login accounts", "◇"), ManagementUi.Metric("Administrators", rows.Count(a => a.IsActive && a.Role == RoleKind.Administrator).ToString(), "Authorized salon owners", "♙"), ManagementUi.Metric("Ready to link", eligible.ToString(), "Active employees without a login", "✦")));
                if (eligible == 0) { var note = ManagementUi.Text("Create an active employee profile first, or edit/reset the account already linked to an employee. Disabled accounts keep their employee link.", 12, ManagementUi.Muted); note.Margin = new Thickness(0, 0, 0, 12); page.SummaryRegion.Children.Add(note); }
            }
            page.Model.Reloading += () => { employees.Clear(); foreach (var employee in Read<Employee>()) employees[employee.Id] = employee; };
            page.Model.Reloaded += Summary; Summary();
            page.Toolbar.Children.Add(ManagementUi.Button("+ Add employee profile", new UiCommand(() => { var employee = new Employee(); if (EntityForm.Edit(employee, "Add employee profile", EmployeeFields(), Save, ValidateEmployee)) page.Model.Reload(); })));
            Show("Login accounts", page);
        }

        public bool CreateEmployeeLogin()
        {
            if (auth == null) throw new InvalidOperationException("The account service is unavailable. Reopen this page from Settings.");
            var existing = Read<UserAccount>();
            var employees = Read<Employee>().Where(e => e.IsActive && !existing.Any(a => a.EmployeeId == e.Id || a.Id == e.UserAccountId)).OrderBy(e => e.FullName).ToList();
            var draft = new AccountDraft();
            return EntityForm.Edit(draft, "Add employee account", Fields(new("EmployeeId", "Active employee", true, Choices: employees.Select(e => new Choice(e.Id, e.FullName + " · " + e.Position)).ToList()), new("Username", "Username", true), new("Password", "Password", true, Help: "Use 12 to 256 characters. Passwords are hashed locally.", Secret: true), new("ConfirmPassword", "Confirm password", true, Secret: true), new("Role", "Access role"), new("MaxDiscountPercent", "Maximum authorized discount (%)", true, Min: 0, Max: 100), new("AllowCustomPrices", "Allow custom service prices")),
            d =>
            {
                if (d.Password != d.ConfirmPassword) throw new InvalidOperationException("The passwords do not match.");
                var employee = Read<Employee>().FirstOrDefault(e => e.Id == d.EmployeeId && e.IsActive) ?? throw new InvalidOperationException("Select an active employee profile.");
                if (Read<UserAccount>().Any(a => a.EmployeeId == employee.Id || a.Id == employee.UserAccountId)) throw new InvalidOperationException("This employee already has a login. Edit, reset or reactivate the existing account.");
                var created = auth.CreateUser(d.Username, d.Password, employee.Id, d.Role, d.MaxDiscountPercent, d.AllowCustomPrices, user);
                employee.UserAccountId = created.Id; Save(employee); d.Password = ""; d.ConfirmPassword = "";
            });
        }

        private void ResetPassword(UserAccount account)
        {
            if (account.Id == user.Id) throw new InvalidOperationException("Use Settings → Change my password to change your own password. That workflow confirms your current password and keeps your session available.");
            if (auth == null) throw new InvalidOperationException("The account service is unavailable.");
            var draft = new AccountDraft();
            EntityForm.Edit(draft, $"Reset password · {account.Username}", Fields(new("Password", "New password", true, Help: "Use at least 12 characters.", Secret: true), new("ConfirmPassword", "Confirm new password", true, Secret: true)),
            d => { if (d.Password != d.ConfirmPassword) throw new InvalidOperationException("The passwords do not match."); auth.ResetPassword(account.Id, d.Password, user); d.Password = ""; d.ConfirmPassword = ""; });
        }

        private void ServiceCommissions(Employee employee)
        {
            var names = Read<SalonService>().ToDictionary(s => s.Id, s => s.Name);
            Show("Service commissions", new EntityPage<EmployeeServiceCommission>(service, user, $"Rates · {employee.FullName}", "These rates replace the default commission for the selected service.", "service rate",
            [Col<EmployeeServiceCommission>("Service", c => names.GetValueOrDefault(c.ServiceId, "Archived service"), width: 3), Col<EmployeeServiceCommission>("Type", c => c.Type), Col<EmployeeServiceCommission>("Rate", c => c.Type == CommissionType.Percentage ? $"{c.Rate:0.##}%" : Peso(c.Rate))],
            _ => Fields(new("ServiceId", "Service", true, Choices: Choices<SalonService>(s => s.Name)), new("Type", "Commission type"), new("Rate", "Amount or percentage", true, Min: 0)),
            [new("Remove override", c => { if (ManagementUi.Confirm("Remove this service-specific rate and use the default commission?")) service.Remove<EmployeeServiceCommission>(c.Id, user); })],
            scope: rows => rows.Where(c => c.EmployeeId == employee.Id), initialize: c => c.EmployeeId = employee.Id,
            validate: c => { if (c.Type == CommissionType.Percentage && c.Rate > 100) throw new InvalidOperationException("Percentage commission cannot exceed 100."); }, canDisable: false));
        }

        private void CommissionHistory(Employee employee)
        {
            Show("Commission history", new EntityPage<CommissionRecord>(service, user, employee.FullName, "Accrued commissions already reduce profit. Recording a payout below preserves cash expense history without counting the commission twice.", "commission",
            [Col<CommissionRecord>("Date", c => Local(c.EarnedAtUtc), "dd MMM yyyy HH:mm", 2), Col<CommissionRecord>("Type", c => c.Type), Col<CommissionRecord>("Rate", c => c.Rate), Col<CommissionRecord>("Basis", c => Peso(c.BasisAmount)), Col<CommissionRecord>("Amount", c => Peso(c.Amount)), Col<CommissionRecord>("Status", c => c.IsReversed ? "Reversed" : c.PaidAtUtc == null ? "Accrued" : "Paid")],
            _ => [], [new("Record commission payout", c => PayCommission(c, employee))], scope: rows => rows.Where(c => c.EmployeeId == employee.Id).OrderByDescending(c => c.EarnedAtUtc), canAdd: false, canEdit: false, canDisable: false));
        }

        private void PayCommission(CommissionRecord commission, Employee employee)
        {
            if (commission.IsReversed || commission.PaidAtUtc != null) throw new InvalidOperationException("This commission is reversed or already paid.");
            var payout = new Expense { Category = "Commissions", Description = $"Commission payout · {employee.FullName}", Amount = commission.Amount, CommissionRecordId = commission.Id, RecordedByUserId = user.Id, OccurredAtUtc = Local(DateTime.UtcNow).Date };
            EntityForm.Edit(payout, $"Pay commission · {Peso(commission.Amount)}", Fields(new("Description", "Payout description", true), new("OccurredAtUtc", "Payment date", true), new("PaymentMethod", "Payment method"), new("ReferenceNumber", "Reference number")),
            e => { e.OccurredAtUtc = Utc(e.OccurredAtUtc.Date); Save(e); });
        }
    }

    private sealed class AppointmentDraft
    {
        public Guid CustomerId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid? EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = "09:00";
        public int DurationMinutes { get; set; } = 30;
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; } = "";
        public bool OverlapOverride { get; set; }
    }
    private sealed class AccountDraft
    {
        public Guid? EmployeeId { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public RoleKind Role { get; set; } = RoleKind.SalonStaff;
        public decimal MaxDiscountPercent { get; set; } = 20;
        public bool AllowCustomPrices { get; set; }
    }
}
