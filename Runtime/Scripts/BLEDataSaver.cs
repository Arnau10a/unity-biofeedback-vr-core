using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class BLEDataSaver : MonoBehaviour
{
    [Header("Configuración de Guardado")]
    [Tooltip("Intervalo en segundos para guardar datos. Si es 0 o menor, se guarda cada vez que se recibe un paquete.")]
    public float saveInterval = 1.0f;
    
    [Tooltip("Subcarpeta dentro de Application.persistentDataPath donde se guardarán los logs.")]
    public string subFolder = "BLEDataLogs";

    [Header("Variables a Guardar")]
    public bool saveHeartRate = true;
    public bool saveAcceleration = true;
    public bool saveGyroscope = true;
    public bool savePressure = true;
    public bool saveSteps = true;
    public bool saveLight = true;
    public bool saveTemperature = true;
    public bool saveBattery = true;
    public bool saveRotation = false;

    private string filePath;
    private bool isHeaderWritten = false;
    private List<string> selectedHeaders = new List<string>();
    
    private BLEData latestData;
    private bool hasNewData = false;
    private float lastSaveTime = 0f;

    void OnEnable()
    {
        BLEConnector.OnDataReceived += HandleDataReceived;
    }

    void OnDisable()
    {
        BLEConnector.OnDataReceived -= HandleDataReceived;
    }

    void Start()
    {
        // Preparar directorio y ruta del archivo
        string directoryPath = Path.Combine(Application.persistentDataPath, subFolder);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string fileName = $"BLE_Session_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        filePath = Path.Combine(directoryPath, fileName);
        Debug.Log($"[BLEDataSaver] Los datos se guardarán en: {filePath}");

        // Configurar las columnas seleccionadas
        InitializeHeaders();

        // Si tenemos un intervalo definido, iniciamos el temporizador
        if (saveInterval > 0f)
        {
            InvokeRepeating(nameof(SaveCachedData), saveInterval, saveInterval);
        }
    }

    private void InitializeHeaders()
    {
        selectedHeaders.Clear();
        selectedHeaders.Add("Timestamp"); // Siempre incluido como base de tiempo

        if (saveHeartRate) selectedHeaders.Add("HeartRate");
        
        if (saveAcceleration)
        {
            selectedHeaders.Add("AccX");
            selectedHeaders.Add("AccY");
            selectedHeaders.Add("AccZ");
        }
        
        if (saveGyroscope)
        {
            selectedHeaders.Add("GyrX");
            selectedHeaders.Add("GyrY");
            selectedHeaders.Add("GyrZ");
        }
        
        if (savePressure) selectedHeaders.Add("Pressure");
        if (saveSteps) selectedHeaders.Add("Steps");
        if (saveLight) selectedHeaders.Add("Light");
        if (saveTemperature) selectedHeaders.Add("Temperature");
        if (saveBattery) selectedHeaders.Add("Battery");
        
        if (saveRotation)
        {
            selectedHeaders.Add("RotX");
            selectedHeaders.Add("RotY");
            selectedHeaders.Add("RotZ");
            selectedHeaders.Add("RotW");
        }
    }

    private void HandleDataReceived(BLEData data)
    {
        latestData = data;
        hasNewData = true;

        // Si no hay intervalo definido (<= 0), guardamos instantáneamente
        if (saveInterval <= 0f)
        {
            WriteRow(latestData);
        }
    }

    private void SaveCachedData()
    {
        // Guardamos si tenemos datos nuevos (o repetimos el último si fuera necesario,
        // pero preferimos guardar sólo cuando hay datos frescos)
        if (hasNewData)
        {
            WriteRow(latestData);
            hasNewData = false;
        }
    }

    private void WriteRow(BLEData data)
    {
        try
        {
            using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                // Escribir cabecera si es la primera fila
                if (!isHeaderWritten)
                {
                    writer.WriteLine(string.Join(",", selectedHeaders));
                    isHeaderWritten = true;
                }

                // Construir fila de datos de forma dinámica
                List<string> rowValues = new List<string>();
                
                // Timestamp
                rowValues.Add(data.timestamp.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

                if (saveHeartRate)
                {
                    rowValues.Add(data.heartRate.ToString());
                }

                if (saveAcceleration)
                {
                    rowValues.Add(data.acceleration.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.acceleration.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.acceleration.z.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (saveGyroscope)
                {
                    rowValues.Add(data.gyroscope.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.gyroscope.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.gyroscope.z.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (savePressure)
                {
                    rowValues.Add(data.pressure.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (saveSteps)
                {
                    rowValues.Add(data.steps.ToString());
                }

                if (saveLight)
                {
                    rowValues.Add(data.light.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (saveTemperature)
                {
                    rowValues.Add(data.temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (saveBattery)
                {
                    rowValues.Add(data.battery.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (saveRotation)
                {
                    rowValues.Add(data.rotation.x.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.rotation.y.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.rotation.z.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                    rowValues.Add(data.rotation.w.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                }

                writer.WriteLine(string.Join(",", rowValues));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BLEDataSaver] Error escribiendo datos en CSV: {e.Message}");
        }
    }
}
