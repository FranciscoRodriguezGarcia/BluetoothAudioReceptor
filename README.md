# Bluetooth Audio Receptor

MVP para Windows 11 que permite recibir audio de dispositivos Bluetooth compatibles con **A2DP Source** y reproducirlo mediante la **salida de audio predeterminada de Windows**, utilizando la API nativa `Windows.Media.Audio.AudioPlaybackConnection`.

## Estado

Primera implementación de la app de escritorio **WPF + .NET 10** en `feature/a2dp-sink-mvp`. Código cargado para prueba local: **todavía no fue compilado ni validado con una consola real**.

## Requisitos

- Windows 11 con Bluetooth funcionando y controladores que soporten A2DP Sink.
- .NET 10 SDK para compilar desde código.
- Consola/dispositivo Bluetooth que pueda enviar audio por A2DP y esté emparejado con Windows.
- Parlantes o interfaz de audio configurados como **salida predeterminada** en Windows.

## Ejecutar

```powershell
git clone https://github.com/FranciscoRodriguezGarcia/BluetoothAudioReceptor.git
cd BluetoothAudioReceptor
git checkout feature/a2dp-sink-mvp
dotnet run --project .\BluetoothAudioReceptor\BluetoothAudioReceptor.csproj
```

Primero emparejá la fuente desde **Configuración > Bluetooth y dispositivos** de Windows. Abrí la aplicación, tocá **Buscar dispositivos**, seleccioná la fuente y elegí **Conectar audio**. La recepción utiliza el dispositivo de reproducción predeterminado, sin cambiar esa configuración.

Si la consola no aparece al buscar, el emparejamiento A2DP Source ↔ A2DP Sink puede no ser posible en esa combinación de hardware, firmware y drivers. La búsqueda enumera endpoints compatibles; no es un escáner Bluetooth genérico.

## Limitaciones

- **Switch 1 / Switch 2:** la capacidad de enviar audio A2DP no garantiza que puedan emparejarse como fuente con una PC Windows; hay que probarlo en hardware real.
- **PS3:** no envía el audio completo de los juegos por Bluetooth A2DP estándar, por lo que esta app no puede capturarlo así.
- **Latencia:** el perfil Bluetooth A2DP puede introducir retraso audible.
- La app no modifica drivers ni la salida predeterminada, y no implementa todavía selección de endpoint, control de volumen propio, inicio automático, instalador ni reconexión.
- Si `OpenAsync` falla, la UI muestra el resultado devuelto por Windows; no implica que el adaptador Bluetooth esté averiado.

## Referencia oficial

- [Microsoft Learn: Enable audio playback from remote Bluetooth-connected devices](https://learn.microsoft.com/en-us/windows/apps/develop/media-playback/enable-remote-audio-playback)
