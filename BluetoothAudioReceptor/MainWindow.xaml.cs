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
    private DeviceWatcher? _watcher;
    private AudioPlaybackConnection? _connection;
    private string? _connectedDeviceId;
    private bool _enabled;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        DeviceList.ItemsSource = _devices;
        Loaded += (_, _) => StartWatcher();
        Closed += (_, _) =>
        {
            StopWatcher();
            ReleaseConnection();
        };
    }

    private void StartWatcher()
    {
        StopWatcher();
        _devices.Clear();
        EmptyText.Visibility = Visibility.Visible;
        SetStatus("Esperando endpoints A2DP emparejados. La detección Bluetooth se configura en Windows.");
        try
        {
            _watcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
            _watcher.Added += Watcher_Added;
            _watcher.Removed += Watcher_Removed;
            _watcher.Updated += Watcher_Updated;
            _watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
            _watcher.Start();
        }
        catch (Exception ex)
        {
            SetStatus($"Error iniciando detección: {ex.Message} (0x{ex.HResult:X8})");
        }
        UpdateButtons();
    }

    private void StopWatcher()
    {
        if (_watcher is null) return;
        _watcher.Added -= Watcher_Added;
        _watcher.Removed -= Watcher_Removed;
        _watcher.Updated -= Watcher_Updated;
        _watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
        if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
            _watcher.Stop();
        _watcher = null;
    }

    private void Watcher_Added(DeviceWatcher sender, DeviceInformation info)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _watcher)) return;
            if (_devices.All(d => d.Id != info.Id)) _devices.Add(info);
            EmptyText.Visibility = _devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateButtons();
        });
    }

    private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _watcher)) return;
            _devices.FirstOrDefault(d => d.Id == update.Id)?.Update(update);
        });
    }

    private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _watcher)) return;
            var device = _devices.FirstOrDefault(d => d.Id == update.Id);
            if (device is not null) _devices.Remove(device);
            if (_connectedDeviceId == update.Id)
            {
                ReleaseConnection();
                SetStatus("El endpoint seleccionado dejó de estar disponible.");
            }
            EmptyText.Visibility = _devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateButtons();
        });
    }

    private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _watcher)) return;
            if (_devices.Count == 0)
                SetStatus("Windows no muestra fuentes A2DP emparejadas. La Switch puede no reconocer esta PC como auricular.");
            else
                SetStatus($"Windows encontró {_devices.Count} endpoint(s) A2DP. Seleccioná una fuente y habilitá la recepción.");
        });
    }

    private async Task EnableAsync()
    {
        if (_busy || DeviceList.SelectedItem is not DeviceInformation device) return;
        SetBusy(true);
        try
        {
            if (_connectedDeviceId == device.Id && _enabled)
            {
                SetStatus("La recepción ya está habilitada para este endpoint.");
                return;
            }

            ReleaseConnection();
            var connection = AudioPlaybackConnection.TryCreateFromId(device.Id);
            if (connection is null)
            {
                SetStatus("Windows no expone una conexión A2DP Sink utilizable para el endpoint seleccionado.");
                return;
            }

            _connection = connection;
            _connectedDeviceId = device.Id;
            connection.StateChanged += Connection_StateChanged;
            SetStatus($"Habilitando receptor para {device.Name}...");
            await connection.StartAsync();
            _enabled = true;
            SetStatus($"Recepción habilitada para {device.Name}. Ahora intentá conectar desde la fuente. Si no inicia el audio, usá «Conectar audio».");
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudo habilitar: {ex.Message} (HRESULT 0x{ex.HResult:X8})");
            ReleaseConnection();
        }
        finally { SetBusy(false); }
    }

    private async Task ConnectAsync()
    {
        if (_busy || !_enabled || _connection is null) return;
        SetBusy(true);
        try
        {
            SetStatus("Solicitando apertura de audio A2DP...");
            var result = await _connection.OpenAsync();
            SetStatus(result.Status == AudioPlaybackConnectionOpenResultStatus.Success
                ? "Conexión de audio abierta. La reproducción usa la salida predeterminada de Windows."
                : $"Windows rechazó la conexión de audio: {result.Status}. Verificá emparejamiento, perfil A2DP y driver Bluetooth.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error al abrir audio: {ex.Message} (HRESULT 0x{ex.HResult:X8})");
        }
        finally { SetBusy(false); }
    }

    private void Connection_StateChanged(AudioPlaybackConnection sender, object args)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!ReferenceEquals(sender, _connection)) return;
            if (sender.State == AudioPlaybackConnectionState.Opened)
                SetStatus("Audio A2DP conectado; usando la salida predeterminada de Windows.");
            else if (sender.State == AudioPlaybackConnectionState.Closed)
                SetStatus("La conexión A2DP está cerrada.");
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
        _enabled = false;
        UpdateButtons();
    }

    private void SetBusy(bool busy) { _busy = busy; UpdateButtons(); }

    private void UpdateButtons()
    {
        if (DeviceList is null) return;
        RefreshButton.IsEnabled = !_busy;
        EnableButton.IsEnabled = !_busy && DeviceList.SelectedItem is DeviceInformation;
        ConnectButton.IsEnabled = !_busy && _enabled;
        DisconnectButton.IsEnabled = !_busy && _connection is not null;
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_busy) StartWatcher();
    }

    private async void EnableButton_Click(object sender, RoutedEventArgs e) => await EnableAsync();
    private async void ConnectButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        ReleaseConnection();
        SetStatus("Recepción deshabilitada; no se modificaron los dispositivos predeterminados de Windows.");
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
        => OpenSettings("ms-settings:bluetooth");

    private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("control.exe", "bthprops.cpl") { UseShellExecute = true }); }
        catch (Exception ex) { SetStatus($"No se pudo abrir el panel clásico: {ex.Message}"); }
    }

    private void OpenSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) { SetStatus($"No se pudo abrir Configuración: {ex.Message}"); }
    }
}
