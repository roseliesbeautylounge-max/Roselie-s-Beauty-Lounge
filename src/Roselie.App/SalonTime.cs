namespace Roselie.App;

/// <summary>Business dates always use Philippine time, independent of workstation timezone.</summary>
public static class SalonTime
{
 public static DateTime Now=>DateTime.SpecifyKind(DateTime.UtcNow.AddHours(8),DateTimeKind.Unspecified);
 public static DateTime Today=>Now.Date;
 public static DateTime FromUtc(DateTime utc)=>DateTime.SpecifyKind(utc.AddHours(8),DateTimeKind.Unspecified);
 public static DateTime? FromUtc(DateTime? utc)=>utc.HasValue?FromUtc(utc.Value):null;
 public static DateTime ToUtc(DateTime philippineTime)=>DateTime.SpecifyKind(philippineTime.AddHours(-8),DateTimeKind.Utc);
}
