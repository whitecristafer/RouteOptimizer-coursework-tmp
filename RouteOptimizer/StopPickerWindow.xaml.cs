using System.Windows;

namespace RouteOptimizer;

public partial class StopPickerWindow : Window
{
    public StopPickItem? SelectedStop { get; private set; }

    public StopPickerWindow(IEnumerable<StopPickItem> items)
    {
        InitializeComponent();
        StopsList.ItemsSource = items.ToList();
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        SelectedStop = StopsList.SelectedItem as StopPickItem;
        if (SelectedStop is null)
        {
            MessageBox.Show("Выберите остановку.");
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
