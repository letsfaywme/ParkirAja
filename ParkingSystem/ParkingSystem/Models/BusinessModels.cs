namespace ParkingSystem;

public enum UserRole { Admin, Jukir }
public enum VehicleType { Motorcycle, Car }
public enum DepositStatus { Pending, Matched, NeedReview }
public sealed record UserSession(int Id, string Username, string Name, UserRole Role, int? ParkingSpotId);
public sealed record ParkingTransaction(int Id, int ParkingSpotId, string LocationName, int JukirId, string JukirName, string PlateNumber, VehicleType VehicleType, DateTimeOffset EntryTime, DateTimeOffset? ExitTime, decimal RateAtEntry, decimal? Fare, string Status, int? ExitJukirId, string? ExitJukirName);
public sealed record HistoryPage(IReadOnlyList<ParkingTransaction> Rows, int Total, int Page);
public sealed record ParkingSpot(int Id, string Name, string Address, decimal MotorcycleRate, decimal CarRate, int Capacity, bool IsActive, int Occupied);
public sealed record DashboardSummary(int Entries, int Exits, decimal Revenue, IReadOnlyList<ParkingTransaction> Latest, IReadOnlyList<ParkingSpot> Spots);
