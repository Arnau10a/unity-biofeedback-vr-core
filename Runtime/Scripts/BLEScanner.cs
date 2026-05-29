using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class BLEScanner : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI uiConsole;

    [Tooltip("Arrastra aquí el componente BLEDeviceListUI para que los dispositivos generen botones clicables")]
    public BLEDeviceListUI deviceListUI;

    // guardar MACs para no repetir dispositivos escaneados
    private HashSet<string> discoveredDevices = new HashSet<string>();

    private AndroidJavaObject bluetoothAdapter;
    private AndroidJavaObject bluetoothLeScanner;
    
    // clase de callback de java
    private AndroidJavaObject javaCallbackWrapper; 
    private bool isScanning = false;

    void Start()
    {
        LogSafe("BLEScanner listo.");
    }

    public void StartScan()
    {
        if (isScanning) return;

        try
        {
            LogSafe("Iniciando escáner...");
            using (AndroidJavaClass btClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
            {
                bluetoothAdapter = btClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");

                if (bluetoothAdapter == null || !bluetoothAdapter.Call<bool>("isEnabled"))
                {
                    LogSafe("Error: Bluetooth apagado o no soportado.");
                    return;
                }

                bluetoothLeScanner = bluetoothAdapter.Call<AndroidJavaObject>("getBluetoothLeScanner");

                // iniciamos callback de java con nuestro gameobject
                javaCallbackWrapper = new AndroidJavaObject("com.biofeedback.ble.UnityBLECallback", this.gameObject.name);
                
                // iniciar el escaneo
                bluetoothLeScanner.Call("startScan", javaCallbackWrapper);
                isScanning = true;
                
                LogSafe("Buscando BLE...");
            }
        }
        catch (Exception e)
        {
            LogSafe($"Fallo crítico: {e.Message}");
        }
    }

    public void StopScan()
    {
        if (!isScanning || bluetoothLeScanner == null || javaCallbackWrapper == null) return;

        try
        {
            bluetoothLeScanner.Call("stopScan", javaCallbackWrapper);
            isScanning = false;
            LogSafe("Escaneo parado.");
        }
        catch (Exception e)
        {
            LogSafe($"Error al parar: {e.Message}");
        }
    }

    public void LogSafe(string message)
    {
        if (uiConsole != null) uiConsole.text += message + "\n";
        Debug.Log("[BLEScanner] " + message);
    }
    
    // callback llamado por java mediante SendMessage para no bloquear la UI
    public void OnDeviceDiscoveredFromJava(string payload)
    {
        // el payload viene con formato Nombre|MAC|RSSI
        string[] parts = payload.Split('|');
        if (parts.Length == 3)
        {
            string name = parts[0];
            string mac = parts[1];
            string rssi = parts[2];

            if (!discoveredDevices.Contains(mac))
            {
                discoveredDevices.Add(mac);

                // actualizar interfaz si existe o mandar a log en su defecto
                if (deviceListUI != null)
                    deviceListUI.AddOrUpdateDevice(name, mac, rssi);
                else
                    LogSafe($"> {name} | {mac} | {rssi} dB");
            }
        }
    }

    public void OnScanFailedFromJava(string errorCode)
    {
        LogSafe($"Error nativo: {errorCode}");
    }

    void OnDestroy()
    {
        StopScan(); // detener escaneo al destruir el script para ahorrar bateria
    }
}
