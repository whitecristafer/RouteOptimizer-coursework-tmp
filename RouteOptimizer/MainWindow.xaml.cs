using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RouteOptimizer;

public partial class MainWindow : Window
{
    private readonly RouteService _service = new();
    private readonly ObservableCollection<RouteListItem> _routes = new();
    private readonly ObservableCollection<RoutePickItem> _routePicks = new();
    private readonly ObservableCollection<StopPickItem> _stopPicks = new();
    private readonly ObservableCollection<RouteStopListItem> _routeStops = new();
    private readonly ObservableCollection<SegmentListItem> _segments = new();
    private readonly ObservableCollection<RouteSearchResult> _searchResults = new();
    private readonly ObservableCollection<RouteLoadRow> _loads = new();
    private readonly ObservableCollection<DuplicateRouteRow> _duplicates = new();
    private readonly ObservableCollection<RouteListItem> _modelRoutes = new();
    private readonly ObservableCollection<StopEntity> _modelStops = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RoleText.Text = $"Роль: {Session.CurrentRoleName ?? "Unknown"}";
        UserText.Text = Session.CurrentUser is null ? "" : $"Пользователь: {Session.CurrentUser.FullName}";

        RoutesGrid.ItemsSource = _routes;
        RouteStopsGrid.ItemsSource = _routeStops;
        FlowGrid.ItemsSource = _currentFlows;
        FlowSegmentBox.ItemsSource = _segments;
        SearchGrid.ItemsSource = _searchResults;
        LoadsGrid.ItemsSource = _loads;
        DuplicatesGrid.ItemsSource = _duplicates;
        ModelRoutesGrid.ItemsSource = _modelRoutes;
        ModelStopsGrid.ItemsSource = _modelStops;

        try
        {
            await LoadCommonDataAsync();
            ApplyRoleVisibility();
            await ReloadRoutesAsync();
            await ReloadAnalyticsAsync();
            await ReloadModelingAsync();
            await ReloadRouteStopsAsync();
            await ReloadFlowSegmentsAsync();
            await ReloadSearchStopsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "RouteOptimizer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyRoleVisibility()
    {
        RoutesTab.Visibility = Session.IsEditor ? Visibility.Visible : Visibility.Collapsed;
        RouteStopsTab.Visibility = Session.IsEditor ? Visibility.Visible : Visibility.Collapsed;
        PassengerFlowTab.Visibility = Session.IsEditor ? Visibility.Visible : Visibility.Collapsed;
        SearchTab.Visibility = Visibility.Visible;
        AnalyticsTab.Visibility = Session.IsPlanner ? Visibility.Visible : Visibility.Collapsed;
        ModelingTab.Visibility = Session.IsPlanner ? Visibility.Visible : Visibility.Collapsed;

        if (Session.IsGuest)
        {
            AnalyticsTab.Visibility = Visibility.Collapsed;
            ModelingTab.Visibility = Visibility.Collapsed;
            RoutesTab.Visibility = Visibility.Collapsed;
            RouteStopsTab.Visibility = Visibility.Collapsed;
            PassengerFlowTab.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadCommonDataAsync()
    {
        var routePicks = await _service.GetRoutePicksAsync();
        var stopPicks = await _service.GetStopPicksAsync();
        _routePicks.Clear();
        foreach (var r in routePicks) _routePicks.Add(r);
        _stopPicks.Clear();
        foreach (var s in stopPicks) _stopPicks.Add(s);

        RouteStopsRouteBox.ItemsSource = _routePicks;
        FlowRouteBox.ItemsSource = _routePicks;
        StartStopBox.ItemsSource = _stopPicks;
        EndStopBox.ItemsSource = _stopPicks;
    }

    private async Task ReloadRoutesAsync()
    {
        _routes.Clear();
        foreach (var r in await _service.GetRoutesAsync()) _routes.Add(r);
        RoutesGrid.ItemsSource = _routes;
    }

    private async Task ReloadRouteStopsAsync()
    {
        if (RouteStopsRouteBox.SelectedValue is null && _routePicks.Count > 0)
        {
            RouteStopsRouteBox.SelectedIndex = 0;
        }

        if (RouteStopsRouteBox.SelectedValue is int selectedRouteId)
        {
            _routeStops.Clear();
            foreach (var item in await _service.GetRouteStopsAsync(selectedRouteId)) _routeStops.Add(item);
        }
    }

    private async Task ReloadFlowSegmentsAsync()
    {
        if (FlowRouteBox.SelectedValue is null)
        {
            if (_routePicks.Count > 0)
                FlowRouteBox.SelectedIndex = 0;
            else
                return;
        }

        if (FlowRouteBox.SelectedValue is int selectedRouteId)
        {
            _segments.Clear();
            foreach (var seg in await _service.GetSegmentsAsync(selectedRouteId)) _segments.Add(seg);
            FlowSegmentBox.ItemsSource = _segments;
            if (_segments.Count > 0)
                FlowSegmentBox.SelectedIndex = 0;

            await ReloadCurrentFlowAsync();
        }
    }

    private readonly ObservableCollection<PassengerFlowEntity> _currentFlows = new();

    private async Task ReloadCurrentFlowAsync()
    {
        _currentFlows.Clear();
        if (FlowRouteBox.SelectedValue is int routeId && FlowSegmentBox.SelectedValue is int segmentId)
        {
            foreach (var f in await _service.GetPassengerFlowsAsync(routeId, segmentId)) _currentFlows.Add(f);
        }
    }

    private Task ReloadSearchStopsAsync()
    {
        if (StartStopBox.SelectedIndex < 0 && _stopPicks.Count > 0) StartStopBox.SelectedIndex = 0;
        if (EndStopBox.SelectedIndex < 0 && _stopPicks.Count > 1) EndStopBox.SelectedIndex = 1;
        return Task.CompletedTask;
    }

    private async Task ReloadAnalyticsAsync()
    {
        _loads.Clear();
        foreach (var row in await _service.GetRouteLoadsAsync()) _loads.Add(row);

        _duplicates.Clear();
        foreach (var row in await _service.GetDuplicateRouteRowsAsync()) _duplicates.Add(row);
    }

    private async Task ReloadModelingAsync()
    {
        _modelRoutes.Clear();
        foreach (var r in await _service.GetRoutesAsync()) _modelRoutes.Add(r);

        _modelStops.Clear();
        using var db = new AppDbContext();
        var stops = await db.Stops.AsNoTracking().OrderBy(x => x.StopName).ToListAsync();
        foreach (var s in stops) _modelStops.Add(s);
    }

    private async void ReloadRoutes_Click(object sender, RoutedEventArgs e)
    {
        try { await ReloadRoutesAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void AddRoute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var model = new RouteEntity { IsActive = true, StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(22) };
            var editor = new RouteEditorWindow(model, await _service.GetTransportTypesAsync(), await _service.GetOperatorsAsync()) { Owner = this };
            if (editor.ShowDialog() == true && editor.IsSaved)
            {
                await _service.SaveRouteAsync(model);
                await ReloadRoutesAsync();
                await LoadCommonDataAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void EditRoute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RoutesGrid.SelectedItem is not RouteListItem item) return;
            var model = await _service.GetRouteByIdAsync(item.RouteId);
            if (model is null) return;
            var editor = new RouteEditorWindow(model, await _service.GetTransportTypesAsync(), await _service.GetOperatorsAsync()) { Owner = this };
            if (editor.ShowDialog() == true && editor.IsSaved)
            {
                await _service.SaveRouteAsync(model);
                await ReloadRoutesAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void DeleteRoute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RoutesGrid.SelectedItem is not RouteListItem item) return;
            if (MessageBox.Show($"Удалить маршрут {item.RouteNumber}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            await _service.DeleteRouteAsync(item.RouteId);
            await ReloadRoutesAsync();
            await LoadCommonDataAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void RouteStopsRouteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try { await ReloadRouteStopsAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void AddRouteStop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RouteStopsRouteBox.SelectedValue is not int routeId) return;
            using var db = new AppDbContext();
            var existing = await db.RouteStops.AsNoTracking().Where(x => x.RouteId == routeId).Select(x => x.StopId).ToListAsync();
            var available = _stopPicks.Where(x => !existing.Contains(x.StopId)).ToList();
            if (available.Count == 0)
            {
                MessageBox.Show("Нет доступных остановок.");
                return;
            }

            var picker = new StopPickerWindow(available) { Owner = this };
            if (picker.ShowDialog() == true && picker.SelectedStop is not null)
            {
                await _service.AddRouteStopAsync(routeId, picker.SelectedStop.StopId);
                await ReloadRouteStopsAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void DeleteRouteStop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RouteStopsGrid.SelectedItem is not RouteStopListItem item) return;
            await _service.DeleteRouteStopAsync(item.RouteStopId);
            await ReloadRouteStopsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void MoveStopUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RouteStopsGrid.SelectedItem is not RouteStopListItem item) return;
            await _service.MoveRouteStopAsync(item.RouteStopId, -1);
            await ReloadRouteStopsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void MoveStopDown_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RouteStopsGrid.SelectedItem is not RouteStopListItem item) return;
            await _service.MoveRouteStopAsync(item.RouteStopId, 1);
            await ReloadRouteStopsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void FlowRouteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            await ReloadFlowSegmentsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void FlowSegmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            await ReloadCurrentFlowAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void SaveFlow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (FlowSegmentBox.SelectedValue is not int segmentId)
            {
                MessageBox.Show("Выберите перегон.");
                return;
            }

            if (!int.TryParse(MorningBox.Text, out var morning) ||
                !int.TryParse(DayBox.Text, out var day) ||
                !int.TryParse(EveningBox.Text, out var evening))
            {
                MessageBox.Show("Пассажиропоток должен быть числом.");
                return;
            }

            await _service.SavePassengerFlowAsync(segmentId, "morning", morning);
            await _service.SavePassengerFlowAsync(segmentId, "day", day);
            await _service.SavePassengerFlowAsync(segmentId, "evening", evening);
            await ReloadCurrentFlowAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (StartStopBox.SelectedValue is not int startId || EndStopBox.SelectedValue is not int endId)
            {
                MessageBox.Show("Выберите обе остановки.");
                return;
            }

            _searchResults.Clear();
            foreach (var row in await _service.SearchRoutesAsync(startId, endId)) _searchResults.Add(row);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка поиска: {ex.Message}", "RouteOptimizer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ReloadAnalytics_Click(object sender, RoutedEventArgs e)
    {
        try { await ReloadAnalyticsAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void ReloadModeling_Click(object sender, RoutedEventArgs e)
    {
        try { await ReloadModelingAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void SaveModeling_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();

            foreach (var route in _modelRoutes)
            {
                var entity = await db.Routes.FirstOrDefaultAsync(x => x.RouteId == route.RouteId);
                if (entity is not null && entity.IsActive != route.IsActive)
                    entity.IsActive = route.IsActive;
            }

            foreach (var stop in _modelStops)
            {
                var entity = await db.Stops.FirstOrDefaultAsync(x => x.StopId == stop.StopId);
                if (entity is not null && entity.IsActive != stop.IsActive)
                    entity.IsActive = stop.IsActive;
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Сохранено.");
            await ReloadModelingAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Session.CurrentUser = null;
        Session.CurrentRoleName = null;
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
