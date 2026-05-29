using System;
using UnityEngine;
using TMPro;

// Estructura contenedora para los datos biofísicos y de sensores
[System.Serializable]
public struct BLEData
{
    public float timestamp;
    public int heartRate;
    public Vector3 acceleration;
    public Vector3 gyroscope;
    public float pressure;
    public int steps;
    public float light;
    public float temperature;
    public float battery;
    public Quaternion rotation;
}

// Script para conectar con el dispositivo por BLE
public class BLEConnector : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    
    [Header("Sensor Panels")]
    public TextMeshProUGUI hrText;
    public TextMeshProUGUI motionText;
    public TextMeshProUGUI envText;
    public TextMeshProUGUI stepsText;

    // Evento público estático para que se suscriban otros componentes
    public static event Action<BLEData> OnDataReceived;

    private AndroidJavaObject bluetoothAdapter;
    private AndroidJavaObject bluetoothGatt; // objeto para la conexion
    private string connectedMac;

    // metodo llamado por el boton de conectar
    public void ConnectToDevice(string macAddress)
    {
        if (!string.IsNullOrEmpty(connectedMac))
        {
            Log("Ya hay una conexión activa. Desconectando primero...");
            DisconnectDevice();
        }

        try
        {
            Log($"Conectando a {macAddress}...");

            using (AndroidJavaClass btClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
            {
                bluetoothAdapter = btClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
                
                // obtenemos el dispositivo nativo
                AndroidJavaObject device = bluetoothAdapter.Call<AndroidJavaObject>("getRemoteDevice", macAddress);

                // contexto de unity en android
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // creamos callback y conectamos al gatt
                    // Usamos TRANSPORT_LE (2) para forzar conexion de bajo consumo, evita error 133 en Quest
                    int TRANSPORT_LE = 2;
                    AndroidJavaObject gattCallback = new AndroidJavaObject("com.biofeedback.ble.UnityBLEGattCallback", this.gameObject.name);
                    bluetoothGatt = device.Call<AndroidJavaObject>("connectGatt", context, false, gattCallback, TRANSPORT_LE);
                    connectedMac = macAddress;
                }
            }
        }
        catch (Exception e)
        {
            Log($"Error al conectar: {e.Message}");
        }
    }

    public void DisconnectDevice()
    {
        if (bluetoothGatt == null) return;

        try
        {
            bluetoothGatt.Call("disconnect");
            bluetoothGatt = null;
            connectedMac = null;
        }
        catch (Exception e)
        {
            Log($"Error al desconectar: {e.Message}");
        }
    }

    public void SendFrequencyCommand(int intervalMs)
    {
        if (bluetoothGatt == null)
        {
            Log("No hay conexión activa para enviar comandos.");
            return;
        }

        try
        {
            Log($"Enviando comando de frecuencia: {intervalMs}ms...");
            using (AndroidJavaClass uuidClass = new AndroidJavaClass("java.util.UUID"))
            using (AndroidJavaObject serviceUuid = uuidClass.CallStatic<AndroidJavaObject>("fromString", "12345678-1234-5678-1234-56789abcdef0"))
            using (AndroidJavaObject charUuid = uuidClass.CallStatic<AndroidJavaObject>("fromString", "12345678-1234-5678-1234-56789abcdef1"))
            {
                using (AndroidJavaObject service = bluetoothGatt.Call<AndroidJavaObject>("getService", serviceUuid))
                {
                    if (service != null)
                    {
                        using (AndroidJavaObject characteristic = service.Call<AndroidJavaObject>("getCharacteristic", charUuid))
                        {
                            if (characteristic != null)
                            {
                                string command = $"FREQ:{intervalMs}";
                                byte[] commandBytes = System.Text.Encoding.UTF8.GetBytes(command);

                                characteristic.Call<bool>("setValue", commandBytes);
                                bool success = bluetoothGatt.Call<bool>("writeCharacteristic", characteristic);
                                Log($"Comando enviado '{command}'. Éxito: {success}");
                            }
                            else
                            {
                                Log("Característica de configuración no encontrada.");
                            }
                        }
                    }
                    else
                    {
                        Log("Servicio de configuración no encontrado.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log($"Error al enviar comando: {e.Message}");
        }
    }

    // Callbacks llamados desde Java

    public void OnGattConnected(string mac)
    {
        Log($"Conectado a {mac}. Descubriendo servicios...");
    }

    public void OnGattDisconnected(string mac)
    {
        Log($"Desconectado de {mac}.");
        bluetoothGatt = null;
        connectedMac = null;
    }

    public void OnGattServicesDiscovered(string serviceUUIDs)
    {
        string[] uuids = serviceUUIDs.Split(';');
        Log($"Servicios descubiertos ({uuids.Length - 1}):");
        foreach (string uuid in uuids)
        {
            if (!string.IsNullOrEmpty(uuid)) Log("  · " + uuid);
        }
    }

    public void OnCharacteristicData(string payload)
    {
        string[] parts = payload.Split('|');
        if (parts.Length == 2)
        {
            string uuid = parts[0];
            string dataStr = parts[1];

            // 2A37 es el UUID estandar para pulsaciones (Hexadecimal array)
            if (uuid.StartsWith("00002a37", StringComparison.OrdinalIgnoreCase))
            {
                int bpm = ParseHeartRate(dataStr);
                if (hrText != null)
                {
                    hrText.text = $"<color=#ff4444><size=120>❤ {bpm}</size></color>\n<size=30>BPM</size>";
                }
                Debug.Log($"[BLEConnector] ❤ Pulso estandar: {bpm} BPM");

                // Disparar evento
                BLEData data = new BLEData();
                data.timestamp = Time.time;
                data.heartRate = bpm;
                OnDataReceived?.Invoke(data);
            }
            // 12345678...abcdef1 es el UUID de nuestra app BioWatch (Texto UTF-8 separado por comas)
            else if (uuid.Equals("12345678-1234-5678-1234-56789abcdef1", StringComparison.OrdinalIgnoreCase))
            {
                string[] sensorVals = dataStr.Split(',');
                if (sensorVals.Length >= 16)
                {
                    string pulso = sensorVals[0];
                    string accX = sensorVals[1]; string accY = sensorVals[2]; string accZ = sensorVals[3];
                    string gyrX = sensorVals[4]; string gyrY = sensorVals[5]; string gyrZ = sensorVals[6];
                    string presion = sensorVals[7];
                    string pasos = sensorVals[8];
                    string luz = sensorVals[9];
                    string temp = sensorVals[10];
                    string battery = sensorVals[11];
                    
                    // Datos de rotacion (Quaternions) por si quieres rotar objetos en el futuro
                    float qx = float.Parse(sensorVals[12], System.Globalization.CultureInfo.InvariantCulture);
                    float qy = float.Parse(sensorVals[13], System.Globalization.CultureInfo.InvariantCulture);
                    float qz = float.Parse(sensorVals[14], System.Globalization.CultureInfo.InvariantCulture);
                    float qw = float.Parse(sensorVals[15], System.Globalization.CultureInfo.InvariantCulture);

                    if (hrText != null) hrText.text = $"<color=#ff4444><size=120>❤ {pulso}</size></color>\n<size=30>BPM</size>";
                    
                    if (motionText != null) 
                        motionText.text = $"<size=40><color=#aaddff>ACCEL:</color> {accX}, {accY}, {accZ}\n" +
                                         $"<color=#ffaadd>GYRO:</color> {gyrX}, {gyrY}, {gyrZ}</size>";
                    
                    if (envText != null) 
                        envText.text = $"<size=35><color=#ddffaa>TEMPERATURA:</color> {temp} °C\n" +
                                         $"<color=#ddffaa>LUZ:</color> {luz} lx | <color=#aaffff>BAT:</color> {battery}%\n" +
                                         $"<color=#aaaaaa>PRESION:</color> {presion} hPa</size>";
                    
                    if (stepsText != null) 
                        stepsText.text = $"<size=50><color=#ffffaa>👣 {pasos} PASOS</color></size>";

                    // Disparar evento
                    BLEData data = new BLEData();
                    data.timestamp = Time.time;
                    int.TryParse(pulso, out data.heartRate);
                    
                    float ax, ay, az;
                    float.TryParse(accX, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ax);
                    float.TryParse(accY, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ay);
                    float.TryParse(accZ, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out az);
                    data.acceleration = new Vector3(ax, ay, az);

                    float gx, gy, gz;
                    float.TryParse(gyrX, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gx);
                    float.TryParse(gyrY, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gy);
                    float.TryParse(gyrZ, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gz);
                    data.gyroscope = new Vector3(gx, gy, gz);

                    float.TryParse(presion, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.pressure);
                    int.TryParse(pasos, out data.steps);
                    float.TryParse(luz, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.light);
                    float.TryParse(temp, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.temperature);
                    float.TryParse(battery, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.battery);
                    
                    data.rotation = new Quaternion(qx, qy, qz, qw);

                    OnDataReceived?.Invoke(data);
                }
                else if (sensorVals.Length >= 11)
                {
                    // Fallback para version anterior si fuera necesario
                    string pulso = sensorVals[0];
                    if (hrText != null) hrText.text = $"<color=#ff4444><size=120>❤ {pulso}</size></color>\n<size=30>BPM (Legacy)</size>";

                    BLEData data = new BLEData();
                    data.timestamp = Time.time;
                    int.TryParse(pulso, out data.heartRate);

                    if (sensorVals.Length >= 4)
                    {
                        float ax, ay, az;
                        float.TryParse(sensorVals[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ax);
                        float.TryParse(sensorVals[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ay);
                        float.TryParse(sensorVals[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out az);
                        data.acceleration = new Vector3(ax, ay, az);
                    }
                    if (sensorVals.Length >= 7)
                    {
                        float gx, gy, gz;
                        float.TryParse(sensorVals[4], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gx);
                        float.TryParse(sensorVals[5], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gy);
                        float.TryParse(sensorVals[6], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gz);
                        data.gyroscope = new Vector3(gx, gy, gz);
                    }
                    if (sensorVals.Length >= 8) float.TryParse(sensorVals[7], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.pressure);
                    if (sensorVals.Length >= 9) int.TryParse(sensorVals[8], out data.steps);
                    if (sensorVals.Length >= 10) float.TryParse(sensorVals[9], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.light);
                    if (sensorVals.Length >= 11) float.TryParse(sensorVals[10], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out data.temperature);

                    OnDataReceived?.Invoke(data);
                }
            }
            else
            {
                Log($"Dato recibido (Desconocido): {dataStr}");
            }
        }
    }

    // procesar los datos de frecuencia cardiaca
    private int ParseHeartRate(string hexData)
    {
        string[] hexBytes = hexData.Split(' ');
        if (hexBytes.Length < 2) return 0;

        try
        {
            byte flags = Convert.ToByte(hexBytes[0].Replace("0x", ""), 16);
            
            // segun el estandar, el primer bit indica si se usa 1 o 2 bytes para los bpm
            bool isUint16 = (flags & 0x01) != 0;

            if (isUint16 && hexBytes.Length >= 3)
            {
                byte b1 = Convert.ToByte(hexBytes[1].Replace("0x", ""), 16);
                byte b2 = Convert.ToByte(hexBytes[2].Replace("0x", ""), 16);
                return b1 + (b2 << 8); // se aplica little endian
            }
            else
            {
                return Convert.ToByte(hexBytes[1].Replace("0x", ""), 16);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BLEConnector] Error parseando datos HR: {e.Message}");
            return 0;
        }
    }

    public void OnGattError(string error)
    {
        Log($"Error GATT: {error}");
    }

    private void Log(string message)
    {
        Debug.Log("[BLEConnector] " + message);
        if (statusText != null) statusText.text += message + "\n";
    }

    void OnDestroy()
    {
        DisconnectDevice();
    }
}
