namespace RouteOptimizer;

public static class Session
{
    public static UserEntity? CurrentUser { get; set; }
    public static string? CurrentRoleName { get; set; }
    public static bool IsEditor => string.Equals(CurrentRoleName, "Editor", StringComparison.OrdinalIgnoreCase);
    public static bool IsPlanner => string.Equals(CurrentRoleName, "Planner", StringComparison.OrdinalIgnoreCase);
    public static bool IsGuest => string.Equals(CurrentRoleName, "Guest", StringComparison.OrdinalIgnoreCase);
}
