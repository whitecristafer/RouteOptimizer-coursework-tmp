using System.Windows;

namespace RouteOptimizer;

public partial class RouteEditorWindow : Window
{
    private readonly RouteEntity _model;
    public bool IsSaved { get; private set; }

    public RouteEditorWindow(RouteEntity model, IEnumerable<TransportTypeEntity> transportTypes, IEnumerable<OperatorEntity> operators)
    {
        InitializeComponent();
        _model = model;
        TransportTypeBox.ItemsSource = transportTypes.ToList();
        OperatorBox.ItemsSource = operators.ToList();

        if (_model.RouteId != 0)
        {
            RouteNumberBox.Text = _model.RouteNumber;
            TransportTypeBox.SelectedValue = _model.TransportTypeId;
            OperatorBox.SelectedValue = _model.OperatorId;
            IntervalBox.Text = _model.IntervalMin.ToString();
            StartTimeBox.Text = _model.StartTime.ToString(@"hh\:mm");
            EndTimeBox.Text = _model.EndTime.ToString(@"hh\:mm");
            ActiveBox.IsChecked = _model.IsActive;
        }
        else
        {
            ActiveBox.IsChecked = true;
            StartTimeBox.Text = "06:00";
            EndTimeBox.Text = "22:00";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RouteNumberBox.Text))
        {
            MessageBox.Show("Введите номер маршрута.");
            return;
        }

        if (TransportTypeBox.SelectedValue is not int transportTypeId || OperatorBox.SelectedValue is not int operatorId)
        {
            MessageBox.Show("Выберите тип транспорта и оператора.");
            return;
        }

        if (!int.TryParse(IntervalBox.Text, out var interval) || interval < 0)
        {
            MessageBox.Show("Некорректный интервал.");
            return;
        }

        if (!TimeSpan.TryParse(StartTimeBox.Text, out var startTime) ||
            !TimeSpan.TryParse(EndTimeBox.Text, out var endTime))
        {
            MessageBox.Show("Некорректное время. Формат HH:mm.");
            return;
        }

        _model.RouteNumber = RouteNumberBox.Text.Trim();
        _model.TransportTypeId = transportTypeId;
        _model.OperatorId = operatorId;
        _model.IntervalMin = interval;
        _model.StartTime = startTime;
        _model.EndTime = endTime;
        _model.IsActive = ActiveBox.IsChecked == true;
        IsSaved = true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
