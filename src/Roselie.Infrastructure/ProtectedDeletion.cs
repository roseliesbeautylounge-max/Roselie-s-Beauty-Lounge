using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;

namespace Roselie.Infrastructure;

public sealed partial class BusinessService
{
    public void DeleteService(Guid id, string superPin, UserAccount user, string reason = "Administrator authorized deletion")
        => ProtectedDelete<SalonService>(id, superPin, user, reason, "Service deleted", (db, service, _) =>
        {
            if(service.IsDeleted)throw new InvalidOperationException("This service has already been deleted.");
            service.IsDeleted = true;service.IsActive = false;service.PackageAvailable = false;
            foreach(var offer in db.Set<PackageOffer>().Where(p => p.ServiceId == service.Id)) {offer.IsDeleted = true;offer.IsActive = false;}
            return service.Name;
        });

    public void DeletePackageOffer(Guid id, string superPin, UserAccount user, string reason = "Administrator authorized deletion")
        => ProtectedDelete<PackageOffer>(id, superPin, user, reason, "Package offer deleted", (db, offer, _) =>
        {
            if(offer.IsDeleted)throw new InvalidOperationException("This package offer has already been deleted.");
            offer.IsDeleted = true;offer.IsActive = false;
            var service = db.Set<SalonService>().Find(offer.ServiceId);
            if(service != null && offer.Id == StandardPackageId(service))service.PackageAvailable = false;
            return offer.Name;
        });

    public void VoidExpense(Guid id, string superPin, UserAccount user, string reason = "Administrator authorized deletion")
        => ProtectedDelete<Expense>(id, superPin, user, reason, "Expense voided", (db, expense, account) =>
        {
            if(expense.IsVoided)throw new InvalidOperationException("This expense has already been voided.");
            expense.IsVoided = true;expense.VoidedAtUtc = DateTime.UtcNow;expense.VoidedByUserId = account.Id;expense.VoidReason = reason.Trim();
            if(expense.CommissionRecordId.HasValue && !db.Set<Expense>().Any(e => e.Id != expense.Id && e.CommissionRecordId == expense.CommissionRecordId && !e.IsVoided))
            {
                var commission = db.Set<CommissionRecord>().Find(expense.CommissionRecordId.Value);
                if(commission != null)commission.PaidAtUtc = null;
            }
            return $"{expense.Category}; {expense.Description}; Original amount PHP {expense.Amount:0.00}";
        });

    private void ProtectedDelete<T>(Guid id, string superPin, UserAccount user, string reason, string action, Func<SalonDbContext,T,UserAccount,string> mutate) where T : Entity
    {
        lock(_database.Gate)
        {
            var account = Require(user, true);
            if(string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500 || reason.Any(char.IsControl))throw new ArgumentException("Enter a deletion reason containing 1 to 500 characters without control characters.", nameof(reason));
            using var db = _database.OpenContext();using var transaction = db.Database.BeginTransaction();
            var error = _auth.VerifySuperPinForMutation(db, user, superPin);
            if(error != null)
            {
                db.SaveChanges();transaction.Commit();
                throw new UnauthorizedAccessException(error);
            }
            var entity = db.Set<T>().Find(id) ?? throw new ArgumentException("The record was not found.", nameof(id));
            var details = mutate(db, entity, account);
            AuthService.Audit(db, account.Id, action, entity.Id, details + "; Reason: " + reason.Trim(), typeof(T).Name);
            db.SaveChanges();transaction.Commit();
        }
    }
}
