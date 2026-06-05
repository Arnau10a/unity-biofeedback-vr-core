using UnityEngine;
using TMPro;

public class BLEDataConsumerExample : MonoBehaviour
{
    [Header("UI Reference (Opcional)")]
    public TextMeshProUGUI displayText;

    void OnEnable()
    {
        // 1. Nos suscribimos al evento estático OnDataReceived del paquete
        BLEConnector.OnDataReceived += HandleNewBiofeedbackData;
        Debug.Log("[BLEDataConsumerExample] Suscrito correctamente al evento OnDataReceived.");
    }

    void OnDisable()
    {
        // 2. IMPORTANTE: Desuscribirse al desactivar el script para evitar fugas de memoria
        BLEConnector.OnDataReceived -= HandleNewBiofeedbackData;
        Debug.Log("[BLEDataConsumerExample] Desuscrito del evento OnDataReceived.");
    }

    // 3. Este método se ejecutará automáticamente cada vez que el smartwatch envíe nuevos datos
    private void HandleNewBiofeedbackData(BLEData data)
    {
        // Ejemplo de uso de las variables recibidas:
        int pulso = data.heartRate;
        Vector3 aceleracion = data.acceleration;
        float bateria = data.battery;

        string infoLog = $"[Biofeedback recibido] HR: {pulso} BPM | Bat: {bateria}% | Accel: {aceleracion}";
        Debug.Log(infoLog);

        if (displayText != null)
        {
            displayText.text = $"ÚLTIMA LECTURA:\n" +
                               $"HR: {pulso} BPM\n" +
                               $"🔋 Batería: {bateria}%\n" +
                               $"🏃 Pasos: {data.steps}\n" +
                               $"🌡️ Temp: {data.temperature:F1} °C\n" +
                               $"🕒 Time: {data.timestamp:F2}s";
        }

        // Aquí el otro desarrollador integrará su lógica de juego:
        // Ejemplo: Si las pulsaciones superan 100 BPM, podríamos desencadenar eventos de estrés en el entorno virtual
        if (pulso > 100)
        {
            // Activar efectos visuales de estrés, cambiar música, etc.
        }
    }
}
