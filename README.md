# Biofeedback VR Core (Unity Package)

Este paquete permite establecer la conexión mediante Bluetooth Low Energy (BLE) con smartwatches (compatible con la app **BioWatch** y pulsómetros estándar) en dispositivos Android/Quest, y capturar y registrar sus datos fisiológicos y de movimiento de forma configurable.

## Características

- **Conexión BLE nativa en Android/Quest**: Comunicación de baja latencia usando llamadas directas de JNI (Java Native Interface) a la API de Android.
- **Estructura unificada de datos**: Todos los datos se parsean en la estructura `BLEData` y se exponen mediante el evento C# `BLEConnector.OnDataReceived`.
- **Guardado configurable (`BLEDataSaver`)**: Permite elegir qué variables guardar y a qué intervalo (en segundos) en un archivo CSV dinámico.

---

## Estructura de Datos (`BLEData`)

El evento `BLEConnector.OnDataReceived` proporciona una estructura con los siguientes campos:

- `timestamp` (float): Tiempo de Unity (`Time.time`).
- `heartRate` (int): Pulsaciones por minuto (BPM).
- `acceleration` (Vector3): Datos del acelerómetro (X, Y, Z).
- `gyroscope` (Vector3): Datos del giroscopio (X, Y, Z).
- `pressure` (float): Presión barométrica (hPa).
- `steps` (int): Contador de pasos.
- `light` (float): Nivel de luz ambiental (lx).
- `temperature` (float): Temperatura del dispositivo (°C).
- `battery` (float): Porcentaje de batería del smartwatch (%).
- `rotation` (Quaternion): Cuaternión de rotación del reloj.

---

## Cómo Integrarlo en un Proyecto Nuevo

1. **Copiar la carpeta**: Copia la carpeta `com.arnau.biofeedbackvr` y pégala dentro del directorio `Packages/` de tu proyecto de Unity.
2. **Detección automática**: Unity detectará automáticamente el paquete y lo compilará bajo el namespace por defecto.
3. **Uso de Prefabs**: Puedes arrastrar el prefab de conexión rápida si necesitas una UI visual, o bien añadir los scripts directamente a tus propios GameObjects.

---

## Configuración del Guardado de Datos (`BLEDataSaver`)

1. Añade el script `BLEDataSaver` a cualquier GameObject activo de tu escena.
2. En el **Inspector de Unity**, configura los parámetros:
   - **Save Interval**: El intervalo de guardado en segundos (ej. `1.0` para guardar una muestra cada segundo). Si se establece en `0` o menor, guardará los datos cada vez que llegue un nuevo paquete desde el reloj.
   - **Sub Folder**: Carpeta destino dentro de `Application.persistentDataPath` (por defecto `BLEDataLogs`).
   - **Variables a guardar**: Marca o desmarca las casillas correspondientes (Heart Rate, Acceleration, etc.) para incluirlas o excluirlas del archivo CSV.
3. Al iniciar la aplicación y recibir datos, se creará un archivo CSV con el nombre formateado como `BLE_Session_YYYYMMDD_HHMMSS.csv` en la ruta correspondiente de tu dispositivo o PC (en Android, suele ser `Android/data/com.TuCompañia.TuJuego/files/BLEDataLogs/`). El archivo CSV contendrá únicamente las columnas de los campos seleccionados en el Inspector de forma dinámica.
