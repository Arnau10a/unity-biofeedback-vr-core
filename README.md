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

## 📦 Uso Rápido: Prefab Todo-En-Uno

El paquete incluye un prefab pre-configurado llamado `Biofeedback_UI_System` que agrupa toda la funcionalidad (conexión, interfaz de usuario Full HD, persistencia CSV y ejemplo de consumo de datos).

Para usarlo:
1. Navega en tu proyecto de Unity a `Packages` > `Biofeedback VR Core` > `Prefabs`.
2. Arrastra el prefab **`Biofeedback_UI_System`** directamente a tu escena.
3. ¡Listo! Ya tienes la interfaz de escaneo, conexión, visualizador de sensores y guardado CSV listo para ejecutarse.

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

El script `BLEDataSaver` (incluido en el objeto `BLE_Scanner` del Prefab) permite registrar en un archivo CSV los datos fisiológicos recibidos de manera selectiva.

### Parámetros en el Inspector:
- **Save Interval**: Frecuencia de registro en segundos (ej. `1.0` para guardar una muestra cada segundo). Pon `0` o menos si quieres almacenar los datos en tiempo real por cada paquete.
- **Sub Folder**: Nombre del subdirectorio (por defecto `BLEDataLogs` en `Application.persistentDataPath`).
- **Variables a guardar**: Casillas para activar/desactivar el registro de variables concretas.

---

## ⚡ Control de Frecuencia del Smartwatch

Puedes ordenar al smartwatch que cambie la tasa de envío de los datos a nivel de sensor físico llamando a:
```csharp
// Cambia el intervalo a 500 ms (el reloj enviará datos cada medio segundo)
bleConnector.SendFrequencyCommand(500);
```

---

## 🛠️ Integración por código (Consumidor)

Si quieres recibir las lecturas en tus propios scripts (como se simula en el objeto `MyGameControllerExample` del Prefab):

```csharp
using UnityEngine;

public class MiControladorBiofeedback : MonoBehaviour
{
    void OnEnable()
    {
        BLEConnector.OnDataReceived += OnDataReceived;
    }

    void OnDisable()
    {
        BLEConnector.OnDataReceived -= OnDataReceived;
    }

    private void OnDataReceived(BLEData data)
    {
        Debug.Log($"Ritmo cardíaco recibido: {data.heartRate} BPM");
    }
}
```
