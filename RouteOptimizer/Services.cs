using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace RouteOptimizer;

public class RouteService
{
    public async Task<List<RouteListItem>> GetRoutesAsync()
    {
        using var db = new AppDbContext();
        return await (from r in db.Routes.AsNoTracking()
                      join t in db.TransportTypes.AsNoTracking() on r.TransportTypeId equals t.TransportTypeId
                      join o in db.Operators.AsNoTracking() on r.OperatorId equals o.OperatorId
                      orderby r.RouteNumber
                      select new RouteListItem
                      {
                          RouteId = r.RouteId,
                          RouteNumber = r.RouteNumber,
                          TransportTypeName = t.TypeName,
                          OperatorName = o.OperatorName,
                          IntervalMin = r.IntervalMin,
                          StartTime = r.StartTime,
                          EndTime = r.EndTime,
                          IsActive = r.IsActive
                      }).ToListAsync();
    }

    public async Task<List<TransportTypeEntity>> GetTransportTypesAsync()
    {
        using var db = new AppDbContext();
        return await db.TransportTypes.AsNoTracking().OrderBy(x => x.TypeName).ToListAsync();
    }

    public async Task<List<OperatorEntity>> GetOperatorsAsync()
    {
        using var db = new AppDbContext();
        return await db.Operators.AsNoTracking().OrderBy(x => x.OperatorName).ToListAsync();
    }

    public async Task<List<RoutePickItem>> GetRoutePicksAsync()
    {
        using var db = new AppDbContext();
        return await db.Routes.AsNoTracking()
            .OrderBy(x => x.RouteNumber)
            .Select(x => new RoutePickItem { RouteId = x.RouteId, RouteNumber = x.RouteNumber })
            .ToListAsync();
    }

    public async Task<List<StopPickItem>> GetStopPicksAsync()
    {
        using var db = new AppDbContext();
        return await db.Stops.AsNoTracking()
            .OrderBy(x => x.StopName)
            .Select(x => new StopPickItem { StopId = x.StopId, StopName = x.StopName })
            .ToListAsync();
    }

    public async Task<List<RouteStopListItem>> GetRouteStopsAsync(int routeId)
    {
        using var db = new AppDbContext();
        return await (from rs in db.RouteStops.AsNoTracking()
                      join s in db.Stops.AsNoTracking() on rs.StopId equals s.StopId
                      where rs.RouteId == routeId
                      orderby rs.StopOrder
                      select new RouteStopListItem
                      {
                          RouteStopId = rs.RouteStopId,
                          StopId = s.StopId,
                          StopOrder = rs.StopOrder,
                          StopName = s.StopName
                      }).ToListAsync();
    }

    public async Task<List<SegmentListItem>> GetSegmentsAsync(int routeId)
    {
        using var db = new AppDbContext();
        return await (from seg in db.Segments.AsNoTracking()
                      join rs1 in db.RouteStops.AsNoTracking() on new { seg.RouteId, seg.FromRouteStopId } equals new { rs1.RouteId, FromRouteStopId = rs1.RouteStopId }
                      join rs2 in db.RouteStops.AsNoTracking() on new { seg.RouteId, seg.ToRouteStopId } equals new { rs2.RouteId, ToRouteStopId = rs2.RouteStopId }
                      join s1 in db.Stops.AsNoTracking() on rs1.StopId equals s1.StopId
                      join s2 in db.Stops.AsNoTracking() on rs2.StopId equals s2.StopId
                      where seg.RouteId == routeId
                      orderby s1.StopName, s2.StopName
                      select new SegmentListItem
                      {
                          SegmentId = seg.SegmentId,
                          Display = $"{s1.StopName} → {s2.StopName} ({seg.TravelTimeMinutes} мин, {seg.DistanceKm} км)"
                      }).ToListAsync();
    }

    public async Task<List<UserEntity>> GetActiveUsersWithRolesAsync()
    {
        using var db = new AppDbContext();
        return await db.Users.Include(x => x.Role).Where(x => x.IsActive).ToListAsync();
    }

    public async Task<RouteEntity?> GetRouteByIdAsync(int routeId)
    {
        using var db = new AppDbContext();
        return await db.Routes.FirstOrDefaultAsync(x => x.RouteId == routeId);
    }

    public async Task<List<StopEntity>> GetAllStopsAsync()
    {
        using var db = new AppDbContext();
        return await db.Stops.AsNoTracking().OrderBy(x => x.StopName).ToListAsync();
    }

    public async Task<List<RouteSearchResult>> SearchRoutesAsync(int startStopId, int endStopId)
    {
        using var db = new AppDbContext();
        return await db.RouteSearchResults
            .FromSqlRaw("EXEC sp_FindRoutesBetweenStops @StartStopID={0}, @EndStopID={1}", startStopId, endStopId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<RouteLoadRow>> GetRouteLoadsAsync()
    {
        using var db = new AppDbContext();
        var sql = """
            SELECT r.route_number AS RouteNumber,
                   ISNULL(SUM(pf.passenger_count), 0) AS TotalPassengers
            FROM Routes r
            LEFT JOIN Segments s ON s.route_id = r.route_id
            LEFT JOIN PassengerFlow pf ON pf.segment_id = s.segment_id
            GROUP BY r.route_number
            ORDER BY r.route_number
            """;
        return await db.RouteLoadRows.FromSqlRaw(sql).AsNoTracking().ToListAsync();
    }

    public async Task<List<DuplicateRouteRow>> GetDuplicateRouteRowsAsync()
    {
        using var db = new AppDbContext();
        var routes = await (from rs in db.RouteStops.AsNoTracking()
                            join st in db.Stops.AsNoTracking() on rs.StopId equals st.StopId
                            join r in db.Routes.AsNoTracking() on rs.RouteId equals r.RouteId
                            orderby rs.RouteId, rs.StopOrder
                            select new { rs.RouteId, r.RouteNumber, rs.StopOrder, st.StopName })
                            .ToListAsync();

        var routeGroups = routes.GroupBy(x => x.RouteId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StopOrder).Select(x => x.StopName).ToList());

        var routeNumbers = routes.GroupBy(x => x.RouteId)
            .ToDictionary(g => g.Key, g => g.First().RouteNumber);

        var pairs = new List<DuplicateRouteRow>();
        var keys = routeGroups.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            for (int j = i + 1; j < keys.Count; j++)
            {
                var a = routeGroups[keys[i]];
                var b = routeGroups[keys[j]];
                var commonPairs = new List<string>();
                for (int k = 0; k < Math.Min(a.Count, b.Count) - 1; k++)
                {
                    if (a[k] == b[k] && a[k + 1] == b[k + 1])
                    {
                        commonPairs.Add($"{a[k]} → {a[k + 1]}");
                    }
                }

                if (commonPairs.Count > 0)
                {
                    pairs.Add(new DuplicateRouteRow
                    {
                        RouteA = routeNumbers[keys[i]],
                        RouteB = routeNumbers[keys[j]],
                        SharedStops = string.Join("; ", commonPairs.Distinct())
                    });
                }
            }
        }

        return pairs.OrderBy(x => x.RouteA).ThenBy(x => x.RouteB).ToList();
    }

    public async Task SaveRouteAsync(RouteEntity route)
    {
        using var db = new AppDbContext();
        if (route.RouteId == 0)
        {
            db.Routes.Add(route);
        }
        else
        {
            db.Routes.Update(route);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteRouteAsync(int routeId)
    {
        using var db = new AppDbContext();
        var entity = await db.Routes.FindAsync(routeId);
        if (entity is null) return;
        db.Routes.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task AddRouteStopAsync(int routeId, int stopId)
    {
        using var db = new AppDbContext();
        var currentMax = await db.RouteStops.Where(x => x.RouteId == routeId).MaxAsync(x => (int?)x.StopOrder) ?? 0;
        db.RouteStops.Add(new RouteStopEntity { RouteId = routeId, StopId = stopId, StopOrder = currentMax + 1 });
        await db.SaveChangesAsync();
    }

    public async Task DeleteRouteStopAsync(int routeStopId)
    {
        using var db = new AppDbContext();
        var item = await db.RouteStops.FirstOrDefaultAsync(x => x.RouteStopId == routeStopId);
        if (item is null) return;
        var routeId = item.RouteId;
        db.RouteStops.Remove(item);
        await db.SaveChangesAsync();
        await ReorderRouteStopsAsync(routeId);
    }

    public async Task MoveRouteStopAsync(int routeStopId, int direction)
    {
        using var db = new AppDbContext();
        var current = await db.RouteStops.FirstOrDefaultAsync(x => x.RouteStopId == routeStopId);
        if (current is null) return;

        var targetOrder = current.StopOrder + direction;
        var target = await db.RouteStops.FirstOrDefaultAsync(x => x.RouteId == current.RouteId && x.StopOrder == targetOrder);
        if (target is null) return;

        var currentOrder = current.StopOrder;
        var targetOldOrder = target.StopOrder;
        await using var tx = await db.Database.BeginTransactionAsync();

        current.StopOrder = currentOrder + 1_000_000;
        await db.SaveChangesAsync();

        target.StopOrder = currentOrder;
        await db.SaveChangesAsync();

        current.StopOrder = targetOldOrder;
        await db.SaveChangesAsync();

        await tx.CommitAsync();
    }

    public async Task ReorderRouteStopsAsync(int routeId)
    {
        using var db = new AppDbContext();
        var sql = """
            ;WITH ordered AS (
                SELECT route_stop_id,
                       ROW_NUMBER() OVER (ORDER BY stop_order) AS new_order
                FROM RouteStops
                WHERE route_id = {{0}}
            )
            UPDATE rs
            SET stop_order = o.new_order
            FROM RouteStops rs
            JOIN ordered o ON o.route_stop_id = rs.route_stop_id;
            """;
        await db.Database.ExecuteSqlRawAsync(sql, routeId);
    }

    public async Task<List<PassengerFlowEntity>> GetPassengerFlowsAsync(int _routeId, int segmentId)
    {
        using var db = new AppDbContext();
        return await db.PassengerFlows
            .Where(x => x.SegmentId == segmentId)
            .OrderBy(x => x.TimePeriod)
            .ToListAsync();
    }

    public async Task SavePassengerFlowAsync(int segmentId, string timePeriod, int count)
    {
        using var db = new AppDbContext();
        var existing = await db.PassengerFlows.FirstOrDefaultAsync(x => x.SegmentId == segmentId && x.TimePeriod == timePeriod);
        if (existing is null)
        {
            db.PassengerFlows.Add(new PassengerFlowEntity { SegmentId = segmentId, TimePeriod = timePeriod, PassengerCount = count });
        }
        else
        {
            existing.PassengerCount = count;
        }
        await db.SaveChangesAsync();
    }

    public async Task ToggleRouteActiveAsync(int routeId, bool active)
    {
        using var db = new AppDbContext();
        var route = await db.Routes.FindAsync(routeId);
        if (route is null) return;
        route.IsActive = active;
        await db.SaveChangesAsync();
    }

    public async Task ToggleStopActiveAsync(int stopId, bool active)
    {
        using var db = new AppDbContext();
        var stop = await db.Stops.FindAsync(stopId);
        if (stop is null) return;
        stop.IsActive = active;
        await db.SaveChangesAsync();
    }
}
