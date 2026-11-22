using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace BluetoothAudioReceptor;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<DeviceInformation> _devices = new();
    private AudioPlaybackConnection? _connection;
    private string? _connectedDeviceId;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        DeviceList.ItemsSource = _devices;
        Loaded += async (_, _) => await RefreshAsync();
        Closed += (_, _) => ReleaseConnection();
    }

    private async Task RefreshAsync()
    {
        if (_busy) return;
        SetBusy(true);
        SetStatus("Buscando dispositivos A2DP emparejados...");
        try
        {
            string? selectedId = (DeviceList.SelectedItem as DeviceInformation)?.Id;
            var selector = AudioPlaybackConnection.GetDeviceSelector();
            var found = await DeviceInformation.FindAllAsync(selector);

            _devices.Clear();
            foreach (var device in found.OrderBy(x => x.Name))
                _devices.Add(device);

            DeviceList.SelectedItem = _devices.FirstOrDefault(x => x.Id == selectedId);
            SetStatus(_devices.Count == 0
                ? "Sin dispositivos compatibles. Emparejá la consola desde Configuración > Bluetooth y volvé a buscar."
                : $"Se encontraron {_devices.Count} dispositivos compatibles. Seleccioná uno.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error al buscar dispositivos: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ConnectAsync()
    {
        if (_busy || DeviceList.SelectedItem is not DeviceInformation device)
            return;

        SetBusy(true);
        try
        {
            if (_connectedDeviceId == device.Id && _connection is not null)
            {
                SetStatus("La conexión ya está habilitada.");
                return;
            }

            ReleaseConnection();
            var connection = AudioPlaybackConnection.TryCreateFromId(device.Id);
            if (connection is null)
            {
                SetStatus("Windows no pudo crear una conexión A2DP para ese dispositivo.");
                return;
            }

            _connection = connection;
            _connectedDeviceId = device.Id;
            connection.StateChanged += Connection_StateChanged;
            SetStatus($"Habilitando recepción de audio para {device.Name}...");
            await connection.StartAsync();

            SetStatus($"Abriendo conexión con {device.Name}...");
            var result = await connection.OpenAsync();
            if (result.Status == AudioPlaybackConnectionOpenResultStatus.Success)
                SetStatus($"Conectado: {device.Name}. El sonido debería salir por el dispositivo predeterminado de Windows.");
            else
            {
                SetStatus($"Windows no pudo abrir la conexión: {result.Status}. Verificá que la consola pueda enviar audio Bluetooth.");
                ReleaseConnection();
            }
        }
        catch (Exception ex)
        {
            ReleaseConnection();
            SetStatus($"Error al conectar: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Connection_StateChanged(AudioPlaybackConnection sender, object args)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _connection)) return;

            if (sender.State == AudioPlaybackConnectionState.Closed)
                SetStatus("Conexión Bluetooth cerrada.");
            else if (sender.State == AudioPlaybackConnectionState.Opened)
                SetStatus("Audio Bluetooth conectado. Reproduciendo en la salida predeterminada de Windows.");

            UpdateButtons();
        });
    }

    private void ReleaseConnection()
    {
        if (_connection is not null)
        {
            _connection.StateChanged -= Connection_StateChanged;
            _connection.Dispose();
            _connection = null;
        }
        _connectedDeviceId = null;
        UpdateButtons();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        RefreshButton.IsEnabled = !_busy;
        ConnectButton.IsEnabled = !_busy && DeviceList.SelectedItem is DeviceInformation;
        DisconnectButton.IsEnabled = !_busy && _connection is not null;
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void ConnectButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        ReleaseConnection();
        SetStatus("Recepción de audio desactivada.");
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudo abrir Configuración de Windows: {ex.Message}");
        }
    }
}
