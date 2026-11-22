# Bluetooth Audio Receptor

Aplicación Windows 11 / WPF / .NET 10 que intenta recibir audio A2DP de fuentes Bluetooth previamente emparejadas usando la API oficial `AudioPlaybackConnection`.

## Estado actual

Rama: `feature/a2dp-sink-mvp`. Se ajustó el flujo tras la primera prueba: enumeración en tiempo real con `DeviceWatcher`, habilitar recepción (`StartAsync`) y abrir audio (`OpenAsync`) como operaciones independientes, mensajes de errores/HRESULT y accesos a opciones Bluetooth de Windows.

**Importante:** abrir Configuración Bluetooth y activar descubrimiento no equivale a anunciar un dispositivo de audio A2DP Sink. La API utilizada no proporciona una función para transformar a Windows en auriculares Bluetooth universalmente detectables por la Switch. Esto puede requerir capacidades de perfil y del stack Bluetooth que Windows no exponga desde la app. No se garantiza que Nintendo Switch se pueda emparejar ni conectar.

## Ejecutar

```powershell
git clone https://github.com/FranciscoRodriguezGarcia/BluetoothAudioReceptor.git
cd BluetoothAudioReceptor
git checkout feature/a2dp-sink-mvp
dotnet run --project .\BluetoothAudioReceptor\BluetoothAudioReceptor.csproj
```

Requisitos: Windows 11, adaptador Bluetooth con soporte A2DP Sink, SDK .NET 10 para compilar y dispositivo remoto capaz de enviar audio A2DP.

## Prueba

1. En Windows: Configuración → Bluetooth y dispositivos. Intentar emparejar una fuente de audio Bluetooth.
2. En la app, observar los endpoints detectados automáticamente. **No es un escáner de consolas no emparejadas**.
3. Seleccionar un endpoint remoto genuino (un elemento llamado «Desktop» puede ser otra PC o un endpoint que no corresponda a la Switch).
4. «Habilitar recepción» y después «Conectar audio», o iniciar conexión desde la fuente cuando el sistema lo permita.
5. Copiar el mensaje de estado completo. Los errores CLR/COM muestran HRESULT cuando corresponda.

## Limitaciones

- No probado ni compilado en Windows en esta sesión.
- La PC puede mostrarse como equipo Bluetooth, pero no necesariamente como auriculares o parlante.
- Nintendo Switch requiere dispositivo receptor A2DP con SBC y descubrimiento compatible.
- PS3 no envía audio general de juegos por A2DP.
- No se cambian drivers, registro ni dispositivo de audio predeterminado.
- Los dispositivos sin emparejar no aparecen mediante `AudioPlaybackConnection.GetDeviceSelector()`.
- A2DP puede tener latencia de juego.

## Documentación

- [Microsoft: Enable audio playback from remote Bluetooth-connected devices](https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/enable-remote-audio-playback)
- [Nintendo: Bluetooth Audio](https://www.nintendo.com/my/support/switch/accessories/bluetooth_audio.html)
