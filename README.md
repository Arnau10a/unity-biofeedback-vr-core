# Biofeedback VR Core (Unity Package)

Este paquete permite establecer la conexión mediante Bluetooth Low Energy (BLE) con smartwatches (compatible con la app **BioWatch** y pulsómetros estándar) en dispositivos Android/Quest, y capturar y registrar sus datos fisiológicos y de movimiento de forma configurable.

---

## 🚀 Instalación en Unity

Puedes instalar este paquete directamente desde GitHub utilizando el **Unity Package Manager (UPM)**:

1. En Unity, abre la ventana del gestor de paquetes en **Window** > **Package Manager**.
2. Haz clic en el botón **`+`** situado en la esquina superior izquierda.
3. Selecciona la opción **Add package from git URL...**
4. Pega la siguiente dirección y haz clic en **Add**:
   ```text
   https://github.com/Arnau10a/unity-biofeedback-vr-core.git
   ```

Unity importará y compilará el paquete automáticamente dentro de tu proyecto.

---

## 📋 Estructura de Datos (`BLEData`)

Cuando el dispositivo está conectado, los datos del smartwatch se propagan a través del evento C# `BLEConnector.OnDataReceived` utilizando la estructura `BLEData`:

```csharp
[System.Serializable]
public struct BLEData
{
    public float timestamp;        // Tiempo de Unity (Time.time) en el que se recibió
    public int heartRate;          // Pulsaciones por minuto (BPM)
    public Vector3 acceleration;   // Acelerómetro (ejes X, Y, Z)
    public Vector3 gyroscope;      // Giroscopio (ejes X, Y, Z)
    public float pressure;         // Presión barométrica (hPa)
    public int steps;              // Contador de pasos acumulado
    public float light;            // Luminosidad ambiental (lx)
    public float temperature;      // Temperatura interna (°C)
    public float battery;          // Porcentaje de batería (%)
    public Quaternion rotation;    // Cuaternión de rotación del dispositivo
}
```

---

## 💾 Configuración del Guardado de Datos (`BLEDataSaver`)

El script `BLEDataSaver` permite registrar en un archivo CSV los datos fisiológicos recibidos de manera selectiva y en intervalos de tiempo personalizados.

### Pasos para usarlo:
1. Agrega el componente `BLEDataSaver` a cualquier GameObject activo de tu escena.
2. Configura los parámetros en el **Inspector**:
   - **Save Interval**: Frecuencia de registro en segundos (ej. `1.0` para guardar una muestra cada segundo). Pon `0` o un número menor si quieres almacenar los datos en tiempo real cada vez que el reloj envíe un paquete.
   - **Sub Folder**: Nombre del subdirectorio (por defecto `BLEDataLogs`).
   - **Variables a guardar**: Casillas de verificación para activar/desactivar el registro de variables concretas (Heart Rate, Acceleration, etc.).
3. Los logs se almacenarán en formato CSV en la ruta `Application.persistentDataPath` del dispositivo (en Android y Quest suele ser `Android/data/com.TuCompañia.TuJuego/files/BLEDataLogs/`).
4. **El archivo CSV se genera de forma dinámica**, conteniendo únicamente las columnas que se han seleccionado en el Inspector.

---

## 🛠️ Integración por código

Si quieres recibir las lecturas en tus propios scripts para activar mecánicas de Biofeedback dentro de la VR:

```csharp
using UnityEngine;

public class MiControladorBiofeedback : MonoBehaviour
{
    void OnEnable()
    {
        // Suscribirse al evento para recibir los datos procesados
        BLEConnector.OnDataReceived += OnDataReceived;
    }

    void OnDisable()
    {
        // Desuscribirse del evento al desactivar el objeto
        BLEConnector.OnDataReceived -= OnDataReceived;
    }

    private void OnDataReceived(BLEData data)
    {
        Debug.Log($"Ritmo cardíaco actual: {data.heartRate} BPM");
        
        if (data.heartRate > 100)
        {
            // Ejecutar lógica del juego si el usuario se estresa
        }
    }
}
```
