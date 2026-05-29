using System.Collections.Generic;
using UnityEngine;
using TMPro;

// script para gestionar la lista visual
public class BLEDeviceListUI : MonoBehaviour
{
    [Header("Referencias del Canvas")]
    [Tooltip("El ScrollView Content donde se crean los botones de dispositivos")]
    public Transform listContainer;

    [Tooltip("Prefab de BLEDeviceButton que se instancia por cada dispositivo")]
    public GameObject deviceButtonPrefab;

    [Tooltip("Referencia al conector para pasársela a cada botón")]
    public BLEConnector bleConnector;

    // texto de log
    public TextMeshProUGUI uiConsole;

    // diccionario para guardar los botones activos
    private Dictionary<string, BLEDeviceButton> activeButtons = new Dictionary<string, BLEDeviceButton>();

    // añadir dispositivo a la interfaz al encontrarlo
    public void AddOrUpdateDevice(string name, string mac, string rssi)
    {
        if (activeButtons.ContainsKey(mac))
        {
            // si ya existe solo actualizar el rssi
            activeButtons[mac].rssiText.text = rssi + " dBm";
            return;
        }

        // instanciar el prefab del boton
        GameObject buttonGO = Instantiate(deviceButtonPrefab, listContainer);
        BLEDeviceButton button = buttonGO.GetComponent<BLEDeviceButton>();

        if (button != null)
        {
            button.Initialize(name, mac, rssi, bleConnector);
            activeButtons[mac] = button;
            Log($"Dispositivo añadido a la lista: {name}");
        }
    }

    // limpiar todos los botones de la lista
    public void ClearList()
    {
        foreach (var button in activeButtons.Values)
        {
            if (button != null) Destroy(button.gameObject);
        }
        activeButtons.Clear();
        Log("Lista de dispositivos reiniciada.");
    }

    private void Log(string message)
    {
        Debug.Log("[BLEDeviceListUI] " + message);
        if (uiConsole != null) uiConsole.text += message + "\n";
    }
}
