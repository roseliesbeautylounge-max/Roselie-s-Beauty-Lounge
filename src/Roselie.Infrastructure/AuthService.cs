using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Roselie.Core.Models;

namespace Roselie.Infrastructure;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password)) { Salt = salt, DegreeOfParallelism = 2, MemorySize = 65536, Iterations = 3 };
        return $"argon2id$v19$m=65536,t=3,p=2${Convert.ToBase64String(salt)}${Convert.ToBase64String(argon.GetBytes(32))}";
    }
    public static bool Verify(string password, string encoded)
    {
        try
        {
            var p = encoded.Split('$');
            if (p.Length != 5 || p[0] != "argon2id" || p[2] != "m=65536,t=3,p=2") return false;
            using var argon = new Argon2id(Encoding.UTF8.GetBytes(password)) { Salt = Convert.FromBase64String(p[3]), DegreeOfParallelism = 2, MemorySize = 65536, Iterations = 3 };
            return CryptographicOperations.FixedTimeEquals(argon.GetBytes(32), Convert.FromBase64String(p[4]));
        }
        catch (FormatException) { return false; }
    }
}

public sealed class AuthService
{
    private readonly DatabaseService _database;
    private UserAccount? _session;
    private Guid _sessionUserId;
    private DateTime _lastActivity;
    public UserAccount? CurrentUser => _session;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(15);
    public int FailedAttemptLimit { get; private set; } = 5;
    public TimeSpan LockoutDuration { get; private set; } = TimeSpan.FromMinutes(15);
    public AuthService(DatabaseService database) { _database = database; ReloadSecuritySettings(); }
    internal void ReloadSecuritySettings()
    {
        lock(_database.Gate)
        {
            using var db=_database.OpenContext();var settings=db.Set<ApplicationSetting>().AsNoTracking().Where(s=>s.Key.StartsWith("Security.")).ToDictionary(s=>s.Key,s=>s.Value);
            if(int.TryParse(settings.GetValueOrDefault("Security.SessionTimeoutMinutes"),out var timeout) && timeout is >=1 and <=120) SessionTimeout=TimeSpan.FromMinutes(timeout);
            if(int.TryParse(settings.GetValueOrDefault("Security.LockoutAttempts"),out var attempts) && attempts is >=3 and <=10) FailedAttemptLimit=attempts;
            if(int.TryParse(settings.GetValueOrDefault("Security.LockoutMinutes"),out var duration) && duration is >=5 and <=1440) LockoutDuration=TimeSpan.FromMinutes(duration);
        }
    }
    public bool FirstRunRequired { get { lock (_database.Gate) { using var db = _database.OpenContext(); return !db.Set<UserAccount>().Any(); } } }
    public string CreateFirstAdmin(string username, string password)
    {
        ValidateCredentials(username, password);
        lock (_database.Gate)
        {
            using var db = _database.OpenContext();
            if (db.Set<UserAccount>().Any()) throw new InvalidOperationException("An administrator has already been created.");
            var code = NewRecoveryCode();
            var role = db.Set<Role>().Single(r => r.Kind == RoleKind.Administrator);
            var user = new UserAccount { Username = username.Trim(), NormalizedUsername = Normalize(username), PasswordHash = PasswordHasher.Hash(password), RecoveryCodeHash = PasswordHasher.Hash(code), Role = RoleKind.Administrator, RoleId = role.Id, MaxDiscountPercent = 100, AllowCustomPrices = true };
            db.Add(user); Audit(db, user.Id, "First administrator created", user.Id, "Save the recovery code separately from this computer."); db.SaveChanges(); return code;
        }
    }
    public UserAccount Login(string username, string password)
    {
        lock (_database.Gate)
        {
            using var db = _database.OpenContext();
            var normalized = Normalize(username);
            var user = db.Set<UserAccount>().SingleOrDefault(u => u.NormalizedUsername == normalized);
            if (user is null) { PasswordHasher.Verify(password, PasswordHasher.Hash("unknown-account-check")); Audit(db, null, "Login failed", null, "Unknown username."); db.SaveChanges(); throw new UnauthorizedAccessException("Username or password is incorrect."); }
            if (!user.IsActive) throw new UnauthorizedAccessException("This account is inactive. Contact the administrator.");
            EnsureNotLocked(user);
            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= FailedAttemptLimit) user.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                Audit(db, user.Id, "Login failed", user.Id, user.LockedUntilUtc.HasValue ? "Account locked after repeated failures." : "Incorrect password."); db.SaveChanges();
                throw new UnauthorizedAccessException("Username or password is incorrect.");
            }
            user.FailedLoginAttempts = 0; user.LockedUntilUtc = null; user.LastLoginAtUtc = DateTime.UtcNow;
            Audit(db, user.Id, "Login successful", user.Id, "Offline login."); db.SaveChanges();
            return BeginSession(user);
        }
    }
    public UserAccount LoginWithPin(string username, string pin)
    {
        lock (_database.Gate)
        {
            using var db = _database.OpenContext();
            var normalized = Normalize(username);
            var user = db.Set<UserAccount>().SingleOrDefault(u => u.NormalizedUsername == normalized);
            if (user is null)
            {
                PasswordHasher.Verify(pin, PasswordHasher.Hash("unknown-local-pin-check"));
                Audit(db, null, "PIN login failed", null, "Unknown username."); db.SaveChanges();
                throw new UnauthorizedAccessException("Username or PIN is incorrect, or local PIN sign-in is not enabled.");
            }
            if (!user.IsActive) throw new UnauthorizedAccessException("This account is inactive. Contact the administrator.");
            EnsureNotLocked(user);
            var hash = UnprotectPinHash(user);
            var valid = hash != null && IsValidPin(pin) && PasswordHasher.Verify(pin, hash);
            if (!valid)
            {
                RegisterFailure(db, user, "PIN login failed", "Incorrect or unavailable local PIN.");
                throw new UnauthorizedAccessException("Username or PIN is incorrect, or local PIN sign-in is not enabled.");
            }
            user.FailedLoginAttempts = 0; user.LockedUntilUtc = null; user.LastLoginAtUtc = DateTime.UtcNow;
            Audit(db, user.Id, "PIN login successful", user.Id, "Offline local PIN login."); db.SaveChanges();
            return BeginSession(user);
        }
    }
    public UserAccount LoginWithPin(string pin)
    {
        lock (_database.Gate)
        {
            using var db = _database.OpenContext();
            var locked = PinLoginLockout(db);
            if(locked > DateTime.UtcNow) throw new UnauthorizedAccessException("PIN sign-in is temporarily locked. Use your password or try again later.");
            var lookup = IsValidPin(pin) ? PinLookup(pin) : "";
            var prefix = lookup.Length > 0 ? lookup[..(lookup.LastIndexOf(':') + 1)] : "";
            var matches = new List<UserAccount>();
            if(lookup.Length > 0)
            {
                foreach(var account in db.Set<UserAccount>().Where(a => a.IsActive).ToList())
                {
                    if(string.IsNullOrEmpty(account.PinHash)) continue;
                    if(account.PinLookupHash != lookup && account.PinLookupHash?.StartsWith(prefix, StringComparison.Ordinal) == true) continue;
                    var hash = UnprotectPinHash(account);
                    if(hash != null && PasswordHasher.Verify(pin, hash)) matches.Add(account);
                }
            }
            if(matches.Count != 1)
            {
                RecordGlobalPinFailure(db, locked);
                Audit(db, null, "PIN login failed", null, matches.Count > 1 ? "Ambiguous local PIN; password sign-in required." : "Unrecognized or unavailable local PIN.");
                db.SaveChanges();
                throw new UnauthorizedAccessException("PIN is incorrect or unavailable, or matches more than one account. Use your password to sign in.");
            }
            var user = matches[0];EnsureNotLocked(user);
            user.PinLookupHash = lookup;user.FailedLoginAttempts = 0;user.LockedUntilUtc = null;user.LastLoginAtUtc = DateTime.UtcNow;
            ResetGlobalPinFailures(db);
            Audit(db, user.Id, "PIN login successful", user.Id, "Offline local PIN login without username.");db.SaveChanges();
            return BeginSession(user);
        }
    }
    public bool IsPinEnabled(UserAccount user)
    {
        lock (_database.Gate) { return UnprotectPinHash(Require(user)) != null; }
    }
    public void EnrollPin(string pin, string currentPassword, UserAccount user)
    {
        lock (_database.Gate)
        {
            Require(user);
            if (!IsValidPin(pin)) throw new ArgumentException("Use a PIN containing 6 to 12 digits.", nameof(pin));
            using var db = _database.OpenContext();
            var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
            ConfirmPassword(db, account, currentPassword, "PIN enrollment failed");
            var lookup = PinLookup(pin);var prefix = lookup[..(lookup.LastIndexOf(':') + 1)];
            foreach(var other in db.Set<UserAccount>().Where(a => a.Id != account.Id && a.IsActive).ToList())
            {
                if(other.PinLookupHash == lookup) throw new ArgumentException("This PIN is already enrolled for another active account. Choose a different PIN.", nameof(pin));
                if(other.PinLookupHash?.StartsWith(prefix, StringComparison.Ordinal) == true) continue;
                var otherHash = UnprotectPinHash(other);
                if(otherHash != null && PasswordHasher.Verify(pin, otherHash)) throw new ArgumentException("This PIN is already enrolled for another active account. Choose a different PIN.", nameof(pin));
            }
            var bytes = Encoding.UTF8.GetBytes(PasswordHasher.Hash(pin));
            try { account.PinHash = Convert.ToBase64String(ProtectedData.Protect(bytes, account.Id.ToByteArray(), DataProtectionScope.CurrentUser)); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            account.FailedLoginAttempts = 0; account.LockedUntilUtc = null;
            account.PinLookupHash = lookup;ResetGlobalPinFailures(db);
            Audit(db, account.Id, "Local PIN enabled", account.Id, "Password-confirmed enrollment for this Windows account and device."); db.SaveChanges();
            _session!.PinHash = null;_session.PinLookupHash = null;
        }
    }
    public void DisablePin(string currentPassword, UserAccount user)
    {
        lock (_database.Gate)
        {
            Require(user);
            using var db = _database.OpenContext();
            var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
            ConfirmPassword(db, account, currentPassword, "PIN removal failed");
            account.PinHash = null;account.PinLookupHash = null; account.FailedLoginAttempts = 0; account.LockedUntilUtc = null;ResetGlobalPinFailures(db);
            Audit(db, account.Id, "Local PIN disabled", account.Id, "Password-confirmed removal."); db.SaveChanges();
            _session!.PinHash = null;_session.PinLookupHash = null;
        }
    }
    private UserAccount BeginSession(UserAccount user)
    {
        user.PasswordHash = ""; user.RecoveryCodeHash = null; user.PinHash = null;user.PinLookupHash = null;user.SuperPinHash = null;
        _session = user; _sessionUserId = user.Id; _lastActivity = DateTime.UtcNow; return user;
    }
    private static bool IsValidPin(string? pin) => pin != null && pin.Length is >= 6 and <= 12 && pin.All(c => c is >= '0' and <= '9');
    private static string? UnprotectPinHash(UserAccount user)
        => UnprotectHash(user.PinHash, user.Id.ToByteArray());
    private static string? UnprotectHash(string? protectedHash, byte[] entropy)
    {
        if (string.IsNullOrWhiteSpace(protectedHash)) return null;
        byte[]? bytes = null;
        try
        {
            bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedHash), entropy, DataProtectionScope.CurrentUser);
            var encoded = Encoding.UTF8.GetString(bytes);var parts = encoded.Split('$');
            if (parts.Length != 5 || parts[0] != "argon2id" || parts[1] != "v19" || parts[2] != "m=65536,t=3,p=2" || Convert.FromBase64String(parts[3]).Length != 16 || Convert.FromBase64String(parts[4]).Length != 32) return null;
            return encoded;
        }
        catch (CryptographicException) { return null; }
        catch (FormatException) { return null; }
        finally { if(bytes != null) CryptographicOperations.ZeroMemory(bytes); }
    }
    private string PinLookup(string pin)
    {
        var path = Path.Combine(_database.DataDirectory, "pin-lookup.key");byte[]? key = null;
        try
        {
            if(File.Exists(path))
            {
                try {key = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);if(key.Length != 32){CryptographicOperations.ZeroMemory(key);key = null;}}
                catch(CryptographicException) {key = null;}
            }
            if(key == null)
            {
                key = RandomNumberGenerator.GetBytes(32);var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try {File.WriteAllBytes(temporary, ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser));File.Move(temporary, path, true);}
                finally {if(File.Exists(temporary))File.Delete(temporary);}
            }
            var fingerprint = Convert.ToHexString(SHA256.HashData(key).AsSpan(0, 8));
            var bytes = Encoding.UTF8.GetBytes(pin);
            try {return "v1:" + fingerprint + ":" + Convert.ToHexString(HMACSHA256.HashData(key, bytes));}
            finally {CryptographicOperations.ZeroMemory(bytes);}
        }
        finally {if(key != null)CryptographicOperations.ZeroMemory(key);}
    }
    private const string PinAttemptsKey = "Security.PinLoginFailedAttempts";
    private const string PinLockoutKey = "Security.PinLoginLockedUntilUtc";
    private static DateTime? PinLoginLockout(SalonDbContext db)
    {
        var value = db.Set<ApplicationSetting>().SingleOrDefault(s => s.Key == PinLockoutKey)?.Value;
        return DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? date : null;
    }
    private static void SecurityValue(SalonDbContext db, string key, string value)
    {
        var setting = db.Set<ApplicationSetting>().SingleOrDefault(s => s.Key == key);
        if(setting == null){setting = new ApplicationSetting {Key = key};db.Add(setting);}setting.Value = value;
    }
    private void RecordGlobalPinFailure(SalonDbContext db, DateTime? locked)
    {
        var value = db.Set<ApplicationSetting>().SingleOrDefault(s => s.Key == PinAttemptsKey)?.Value;
        var attempts = int.TryParse(value, out var parsed) ? Math.Clamp(parsed, 0, FailedAttemptLimit) : 0;
        if(locked.HasValue && locked <= DateTime.UtcNow) attempts = 0;
        attempts++;SecurityValue(db, PinAttemptsKey, attempts.ToString(CultureInfo.InvariantCulture));
        SecurityValue(db, PinLockoutKey, attempts >= FailedAttemptLimit ? DateTime.UtcNow.Add(LockoutDuration).ToString("O", CultureInfo.InvariantCulture) : "");
    }
    private static void ResetGlobalPinFailures(SalonDbContext db)
    {
        SecurityValue(db, PinAttemptsKey, "0");SecurityValue(db, PinLockoutKey, "");
    }
    private static byte[] SuperPinEntropy(Guid id) => id.ToByteArray().Concat(Encoding.UTF8.GetBytes("Roselie.SuperPin.v1")).ToArray();
    public bool IsSuperPinEnabled(UserAccount user)
    {
        lock(_database.Gate){var account = Require(user, true);return UnprotectHash(account.SuperPinHash, SuperPinEntropy(account.Id)) != null;}
    }
    public void EnrollSuperPin(string superPin, string currentPassword, UserAccount user)
    {
        lock(_database.Gate)
        {
            Require(user, true);if(!IsValidPin(superPin))throw new ArgumentException("Use a Super PIN containing 6 to 12 digits.", nameof(superPin));
            using var db = _database.OpenContext();var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
            ConfirmPassword(db, account, currentPassword, "Super PIN enrollment failed");
            var changed = account.SuperPinHash != null;var bytes = Encoding.UTF8.GetBytes(PasswordHasher.Hash(superPin));
            try {account.SuperPinHash = Convert.ToBase64String(ProtectedData.Protect(bytes, SuperPinEntropy(account.Id), DataProtectionScope.CurrentUser));}
            finally {CryptographicOperations.ZeroMemory(bytes);}
            account.SuperPinFailedAttempts = 0;account.SuperPinLockedUntilUtc = null;account.FailedLoginAttempts = 0;account.LockedUntilUtc = null;
            Audit(db, account.Id, changed ? "Super PIN changed" : "Super PIN enabled", account.Id, "Password-confirmed deletion credential enrollment for this Windows profile.");db.SaveChanges();_session!.SuperPinHash = null;
        }
    }
    internal string? VerifySuperPinForMutation(SalonDbContext db, UserAccount user, string superPin)
    {
        Require(user, true);var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
        if(!account.IsActive || account.Role != RoleKind.Administrator)throw new UnauthorizedAccessException("Administrator access is required.");
        if(account.SuperPinLockedUntilUtc > DateTime.UtcNow)return "Super PIN verification is temporarily locked. Try again later or reset it with your current password.";
        var hash = UnprotectHash(account.SuperPinHash, SuperPinEntropy(account.Id));
        if(hash == null || !IsValidPin(superPin) || !PasswordHasher.Verify(superPin, hash))
        {
            if(account.SuperPinLockedUntilUtc.HasValue && account.SuperPinLockedUntilUtc <= DateTime.UtcNow)account.SuperPinFailedAttempts = 0;
            account.SuperPinFailedAttempts++;if(account.SuperPinFailedAttempts >= FailedAttemptLimit)account.SuperPinLockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
            Audit(db, account.Id, "Super PIN verification failed", account.Id, "Incorrect or unavailable deletion credential.");
            return "Super PIN is incorrect or unavailable. Enroll a separate Super PIN in User security.";
        }
        account.SuperPinFailedAttempts = 0;account.SuperPinLockedUntilUtc = null;return null;
    }
    private void EnsureNotLocked(UserAccount user)
    {
        if(user.LockedUntilUtc > DateTime.UtcNow) throw new UnauthorizedAccessException($"Too many unsuccessful attempts. Try again after the {LockoutDuration.TotalMinutes:0}-minute lockout.");
    }
    private void RegisterFailure(SalonDbContext db, UserAccount user, string action, string details)
    {
        user.FailedLoginAttempts++;
        if(user.FailedLoginAttempts >= FailedAttemptLimit) user.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
        Audit(db, user.Id, action, user.Id, user.LockedUntilUtc.HasValue ? "Account locked after repeated failures." : details); db.SaveChanges();
    }
    private void ConfirmPassword(SalonDbContext db, UserAccount user, string password, string action)
    {
        EnsureNotLocked(user);
        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            RegisterFailure(db, user, action, "Incorrect password confirmation.");
            throw new UnauthorizedAccessException("The current password is incorrect.");
        }
    }
    public UserAccount Require(UserAccount user, bool administratorOnly = false)
    {
        lock (_database.Gate)
        {
            if (_session is null || !ReferenceEquals(user, _session) || user.Id != _sessionUserId) throw new UnauthorizedAccessException("Sign in before continuing.");
            if (DateTime.UtcNow - _lastActivity > SessionTimeout) { _session = null; throw new UnauthorizedAccessException("Your session expired. Sign in again."); }
            using var db = _database.OpenContext();
            var current = db.Set<UserAccount>().AsNoTracking().Single(u => u.Id == _sessionUserId);
            if (!current.IsActive) { _session = null; throw new UnauthorizedAccessException("This account is inactive."); }
            if (administratorOnly && current.Role != RoleKind.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
            _lastActivity = DateTime.UtcNow; return current;
        }
    }
    public void Touch(UserAccount user) => Require(user);
    internal void EndSessionAfterRestore() { _session = null; _sessionUserId = Guid.Empty; }
    public void Logout(UserAccount? user = null)
    {
        lock (_database.Gate)
        {
            if (_session != null) { using var db = _database.OpenContext(); Audit(db, _sessionUserId, "Logout", _sessionUserId, "Session ended."); db.SaveChanges(); }
            _session = null;
        }
    }
    public UserAccount CreateUser(string username, string password, Guid? employeeId, RoleKind role, decimal maxDiscountPercent, bool allowCustomPrices, UserAccount admin)
    {
        Require(admin, true); ValidateCredentials(username, password);
        if (maxDiscountPercent < 0 || maxDiscountPercent > 100 || decimal.Round(maxDiscountPercent, 2) != maxDiscountPercent) throw new ArgumentException("Discount limit must be from 0% to 100% with at most two decimals.");
        lock (_database.Gate)
        {
            using var db = _database.OpenContext(); var normalized = Normalize(username);
            if (db.Set<UserAccount>().Any(u => u.NormalizedUsername == normalized)) throw new ArgumentException("This username is already used.");
            if (employeeId.HasValue)
            {
                var employee = db.Set<Employee>().SingleOrDefault(e => e.Id == employeeId && e.IsActive);
                if(employee == null) throw new ArgumentException("Select an active employee.");
                if(db.Set<UserAccount>().Any(a => a.EmployeeId == employeeId) || (employee.UserAccountId.HasValue && db.Set<UserAccount>().Any(a => a.Id == employee.UserAccountId)))
                    throw new ArgumentException("This employee already has an account. Reactivate or reset the existing account instead.");
            }
            var roleRecord = db.Set<Role>().Single(r => r.Kind == role);
            var account = new UserAccount { Username = username.Trim(), NormalizedUsername = normalized, PasswordHash = PasswordHasher.Hash(password), Role = role, RoleId = roleRecord.Id, EmployeeId = employeeId, MaxDiscountPercent = maxDiscountPercent, AllowCustomPrices = allowCustomPrices };
            db.Add(account); Audit(db, admin.Id, "Account created", account.Id, account.Username); db.SaveChanges(); account.PasswordHash = ""; return account;
        }
    }
    public string RecoverAccount(string username, string recoveryCode, string newPassword)
    {
        ValidateCredentials(username, newPassword);
        lock (_database.Gate)
        {
            using var db = _database.OpenContext(); var normalized = Normalize(username); var user = db.Set<UserAccount>().SingleOrDefault(u => u.NormalizedUsername == normalized);
            if (user is null || !user.IsActive || user.Role != RoleKind.Administrator || string.IsNullOrWhiteSpace(user.RecoveryCodeHash)) throw new UnauthorizedAccessException("Recovery details are invalid.");
            if (user.LockedUntilUtc > DateTime.UtcNow) throw new UnauthorizedAccessException("Recovery is temporarily locked. Try again later.");
            if (!PasswordHasher.Verify(recoveryCode.Trim().ToUpperInvariant(), user.RecoveryCodeHash))
            {
                user.FailedLoginAttempts++; if (user.FailedLoginAttempts >= FailedAttemptLimit) user.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                Audit(db, user.Id, "Recovery failed", user.Id, "Invalid recovery code."); db.SaveChanges(); throw new UnauthorizedAccessException("Recovery details are invalid.");
            }
            var replacement = NewRecoveryCode(); user.PasswordHash = PasswordHasher.Hash(newPassword); user.RecoveryCodeHash = PasswordHasher.Hash(replacement); ClearLocalPins(user); user.FailedLoginAttempts = 0; user.LockedUntilUtc = null;
            Audit(db, user.Id, "Account recovered", user.Id, "Password reset, recovery code rotated and local PIN disabled."); db.SaveChanges(); _session = null; return replacement;
        }
    }
    public void ResetPassword(Guid accountId, string newPassword, UserAccount admin)
    {
        Require(admin, true); ValidateCredentials("valid", newPassword);
        lock (_database.Gate) { using var db = _database.OpenContext(); var account = db.Set<UserAccount>().Find(accountId) ?? throw new ArgumentException("Account not found."); account.PasswordHash = PasswordHasher.Hash(newPassword); ClearLocalPins(account); account.LockedUntilUtc = null; account.FailedLoginAttempts = 0; Audit(db,admin.Id,"Password reset",account.Id,"Administrator reset; local PINs disabled."); db.SaveChanges(); if (_session?.Id == accountId) _session = null; }
    }
    public void ChangeOwnPassword(string currentPassword, string newPassword, UserAccount user)
    {
        lock (_database.Gate)
        {
            Require(user, true);
            ValidateCredentials("valid", newPassword);
            using var db = _database.OpenContext();
            var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
            ConfirmPassword(db, account, currentPassword, "Password change failed");
            account.PasswordHash = PasswordHasher.Hash(newPassword);
            ClearLocalPins(account); account.FailedLoginAttempts = 0; account.LockedUntilUtc = null;
            Audit(db, account.Id, "Administrator password changed", account.Id, "Current password confirmed; local PIN disabled and session ended.");
            db.SaveChanges();
            _session = null; _sessionUserId = Guid.Empty;
        }
    }
    public void UpdateOwnProfile(string displayName, UserAccount user)
    {
        lock (_database.Gate)
        {
            Require(user, true);
            var name = displayName?.Trim() ?? "";
            if(name.Length is < 1 or > 100 || (displayName?.Any(char.IsControl) ?? false))
                throw new ArgumentException("Use a profile name containing 1 to 100 characters without control characters.", nameof(displayName));
            using var db = _database.OpenContext();
            var account = db.Set<UserAccount>().Single(a => a.Id == _sessionUserId);
            account.DisplayName = name;
            Audit(db, account.Id, "Administrator profile updated", account.Id, "Display name updated; sign-in username unchanged.");
            db.SaveChanges();
            _session!.DisplayName = name;_session.PasswordHash = "";_session.RecoveryCodeHash = null;_session.PinHash = null;_session.PinLookupHash = null;_session.SuperPinHash = null;
        }
    }
    private static void ClearLocalPins(UserAccount account)
    {
        account.PinHash = null;account.PinLookupHash = null;account.SuperPinHash = null;account.SuperPinFailedAttempts = 0;account.SuperPinLockedUntilUtc = null;
    }
    internal static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static void ValidateCredentials(string username, string password)
    {
        if (username.Trim().Length is < 3 or > 50 || username.Any(char.IsControl)) throw new ArgumentException("Username must contain 3 to 50 characters.");
        if (password.Length < 12 || password.Length > 256) throw new ArgumentException("Use a password with 12 to 256 characters.");
    }
    private static string NewRecoveryCode() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    internal static void Audit(SalonDbContext db, Guid? userId, string action, Guid? entityId, string details, string entityType = "UserAccount") => db.Add(new AuditLog { UserId = userId, Action = action, EntityId = entityId, EntityType = entityType, Details = details });
}


