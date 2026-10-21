using System.Globalization;

namespace ParkingSystem;

public static class ParkingTime
{
    public static DateTimeOffset Wib(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7));
    public static DateOnly Today(TimeProvider provider) => DateOnly.FromDateTime(Wib(provider.GetUtcNow()).DateTime);
    public static (DateTime From, DateTime To) Bounds(DateOnly from, DateOnly to) =>
        (new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime,
         new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).UtcDateTime);
    public static string Money(decimal value) => value.ToString("C2", CultureInfo.GetCultureInfo("id-ID"));
}
