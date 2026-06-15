using Microsoft.EntityFrameworkCore;

namespace RouteOptimizer;

public class TransportTypeEntity
{
    public int TransportTypeId { get; set; }
    public string TypeName { get; set; } = "";
    public ICollection<RouteEntity> Routes { get; set; } = new List<RouteEntity>();
}

public class OperatorEntity
{
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = "";
    public ICollection<RouteEntity> Routes { get; set; } = new List<RouteEntity>();
}

public class RoleEntity
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
}

public class RouteEntity
{
    public int RouteId { get; set; }
    public string RouteNumber { get; set; } = "";
    public int TransportTypeId { get; set; }
    public int OperatorId { get; set; }
    public int IntervalMin { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }

    public TransportTypeEntity? TransportType { get; set; }
    public OperatorEntity? Operator { get; set; }
    public ICollection<RouteStopEntity> RouteStops { get; set; } = new List<RouteStopEntity>();
    public ICollection<SegmentEntity> Segments { get; set; } = new List<SegmentEntity>();
}

public class StopEntity
{
    public int StopId { get; set; }
    public string StopName { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string StopType { get; set; } = "";
    public bool HasPavilion { get; set; }
    public bool IsActive { get; set; }
    public ICollection<RouteStopEntity> RouteStops { get; set; } = new List<RouteStopEntity>();
}

public class RouteStopEntity
{
    public int RouteStopId { get; set; }
    public int RouteId { get; set; }
    public int StopId { get; set; }
    public int StopOrder { get; set; }

    public RouteEntity? Route { get; set; }
    public StopEntity? Stop { get; set; }
}

public class SegmentEntity
{
    public int SegmentId { get; set; }
    public int RouteId { get; set; }
    public int FromRouteStopId { get; set; }
    public int ToRouteStopId { get; set; }
    public int TravelTimeMinutes { get; set; }
    public decimal DistanceKm { get; set; }

    public RouteEntity? Route { get; set; }
    public RouteStopEntity? FromRouteStop { get; set; }
    public RouteStopEntity? ToRouteStop { get; set; }
    public ICollection<PassengerFlowEntity> PassengerFlows { get; set; } = new List<PassengerFlowEntity>();
}

public class PassengerFlowEntity
{
    public int PassengerFlowId { get; set; }
    public int SegmentId { get; set; }
    public string TimePeriod { get; set; } = "";
    public int PassengerCount { get; set; }
    public SegmentEntity? Segment { get; set; }
}

public class UserEntity
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Email { get; set; }
    public string FullName { get; set; } = "";
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public RoleEntity? Role { get; set; }
}

[Keyless]
public class RouteSearchResult
{
    public string RouteNumber { get; set; } = "";
    public string TypeName { get; set; } = "";
    public int StartOrder { get; set; }
    public int EndOrder { get; set; }
    public int TransfersCount { get; set; }
}

[Keyless]
public class RouteLoadRow
{
    public string RouteNumber { get; set; } = "";
    public int TotalPassengers { get; set; }
}

[Keyless]
public class DuplicateRouteRow
{
    public string RouteA { get; set; } = "";
    public string RouteB { get; set; } = "";
    public string SharedStops { get; set; } = "";
}

public class RouteListItem
{
    public int RouteId { get; set; }
    public string RouteNumber { get; set; } = "";
    public string TransportTypeName { get; set; } = "";
    public string OperatorName { get; set; } = "";
    public int IntervalMin { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class RouteStopListItem
{
    public int RouteStopId { get; set; }
    public int StopId { get; set; }
    public int StopOrder { get; set; }
    public string StopName { get; set; } = "";
}

public class SegmentListItem
{
    public int SegmentId { get; set; }
    public string Display { get; set; } = "";
}

public class StopPickItem
{
    public int StopId { get; set; }
    public string StopName { get; set; } = "";
    public override string ToString() => StopName;
}

public class RoutePickItem
{
    public int RouteId { get; set; }
    public string RouteNumber { get; set; } = "";
    public override string ToString() => RouteNumber;
}
