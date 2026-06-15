using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RouteOptimizer;

public class AppDbContext : DbContext
{
    public DbSet<TransportTypeEntity> TransportTypes => Set<TransportTypeEntity>();
    public DbSet<OperatorEntity> Operators => Set<OperatorEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<StopEntity> Stops => Set<StopEntity>();
    public DbSet<RouteStopEntity> RouteStops => Set<RouteStopEntity>();
    public DbSet<SegmentEntity> Segments => Set<SegmentEntity>();
    public DbSet<PassengerFlowEntity> PassengerFlows => Set<PassengerFlowEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<RouteSearchResult> RouteSearchResults => Set<RouteSearchResult>();
    public DbSet<RouteLoadRow> RouteLoadRows => Set<RouteLoadRow>();
    public DbSet<DuplicateRouteRow> DuplicateRouteRows => Set<DuplicateRouteRow>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = DbConfig.LoadConnectionString();
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransportTypeEntity>(e =>
        {
            e.ToTable("TransportTypes");
            e.HasKey(x => x.TransportTypeId);
            e.Property(x => x.TransportTypeId).HasColumnName("transport_type_id");
            e.Property(x => x.TypeName).HasColumnName("type_name").HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<OperatorEntity>(e =>
        {
            e.ToTable("Operators");
            e.HasKey(x => x.OperatorId);
            e.Property(x => x.OperatorId).HasColumnName("operator_id");
            e.Property(x => x.OperatorName).HasColumnName("operator_name").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<RoleEntity>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<RouteEntity>(e =>
        {
            e.ToTable("Routes");
            e.HasKey(x => x.RouteId);
            e.Property(x => x.RouteId).HasColumnName("route_id");
            e.Property(x => x.RouteNumber).HasColumnName("route_number").HasMaxLength(10).IsRequired();
            e.Property(x => x.TransportTypeId).HasColumnName("transport_type_id");
            e.Property(x => x.OperatorId).HasColumnName("operator_id");
            e.Property(x => x.IntervalMin).HasColumnName("interval_min");
            e.Property(x => x.StartTime).HasColumnName("start_time").HasColumnType("time");
            e.Property(x => x.EndTime).HasColumnName("end_time").HasColumnType("time");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.HasOne(x => x.TransportType).WithMany(x => x.Routes).HasForeignKey(x => x.TransportTypeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Operator).WithMany(x => x.Routes).HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StopEntity>(e =>
        {
            e.ToTable("Stops");
            e.HasKey(x => x.StopId);
            e.Property(x => x.StopId).HasColumnName("stop_id");
            e.Property(x => x.StopName).HasColumnName("stop_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(10, 6);
            e.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(10, 6);
            e.Property(x => x.StopType).HasColumnName("stop_type").HasMaxLength(50).IsRequired();
            e.Property(x => x.HasPavilion).HasColumnName("has_pavilion");
            e.Property(x => x.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<RouteStopEntity>(e =>
        {
            e.ToTable("RouteStops");
            e.HasKey(x => x.RouteStopId);
            e.Property(x => x.RouteStopId).HasColumnName("route_stop_id");
            e.Property(x => x.RouteId).HasColumnName("route_id");
            e.Property(x => x.StopId).HasColumnName("stop_id");
            e.Property(x => x.StopOrder).HasColumnName("stop_order");
            e.HasOne(x => x.Route).WithMany(x => x.RouteStops).HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Stop).WithMany(x => x.RouteStops).HasForeignKey(x => x.StopId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.RouteId, x.StopOrder }).IsUnique();
            e.HasIndex(x => new { x.RouteId, x.StopId }).IsUnique();
        });

        modelBuilder.Entity<SegmentEntity>(e =>
        {
            e.ToTable("Segments");
            e.HasKey(x => x.SegmentId);
            e.Property(x => x.SegmentId).HasColumnName("segment_id");
            e.Property(x => x.RouteId).HasColumnName("route_id");
            e.Property(x => x.FromRouteStopId).HasColumnName("from_route_stop_id");
            e.Property(x => x.ToRouteStopId).HasColumnName("to_route_stop_id");
            e.Property(x => x.TravelTimeMinutes).HasColumnName("travel_time_minutes");
            e.Property(x => x.DistanceKm).HasColumnName("distance_km").HasPrecision(5, 2);
            e.HasOne(x => x.Route).WithMany(x => x.Segments).HasForeignKey(x => x.RouteId);
            e.HasOne(x => x.FromRouteStop).WithMany().HasForeignKey(x => new { x.RouteId, x.FromRouteStopId }).HasPrincipalKey(x => new { x.RouteId, x.RouteStopId });
            e.HasOne(x => x.ToRouteStop).WithMany().HasForeignKey(x => new { x.RouteId, x.ToRouteStopId }).HasPrincipalKey(x => new { x.RouteId, x.RouteStopId });
        });

        modelBuilder.Entity<PassengerFlowEntity>(e =>
        {
            e.ToTable("PassengerFlow");
            e.HasKey(x => x.PassengerFlowId);
            e.Property(x => x.PassengerFlowId).HasColumnName("passenger_flow_id");
            e.Property(x => x.SegmentId).HasColumnName("segment_id");
            e.Property(x => x.TimePeriod).HasColumnName("time_period").HasMaxLength(20).IsRequired();
            e.Property(x => x.PassengerCount).HasColumnName("passenger_count");
            e.HasOne(x => x.Segment).WithMany(x => x.PassengerFlows).HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(100);
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RouteSearchResult>(e =>
        {
            e.HasNoKey().ToView(null);
            e.Property(x => x.RouteNumber).HasColumnName("route_number");
            e.Property(x => x.TypeName).HasColumnName("type_name");
            e.Property(x => x.StartOrder).HasColumnName("start_order");
            e.Property(x => x.EndOrder).HasColumnName("end_order");
            e.Property(x => x.TransfersCount).HasColumnName("transfers_count");
        });

        modelBuilder.Entity<RouteLoadRow>(e =>
        {
            e.HasNoKey().ToView(null);
            e.Property(x => x.RouteNumber).HasColumnName("RouteNumber");
            e.Property(x => x.TotalPassengers).HasColumnName("TotalPassengers");
        });

        modelBuilder.Entity<DuplicateRouteRow>().HasNoKey().ToView(null);
    }
}

public static class DbConfig
{
    public static string LoadConnectionString()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"appsettings.json not found: {path}");
        }

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(Path.Combine(baseDir, "appsettings.json"), optional: false, reloadOnChange: true)
            .Build();

        var cs = configuration.GetConnectionString("RouteOptimumDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Connection string 'RouteOptimumDb' is missing.");
        }
        return cs;
    }
}
