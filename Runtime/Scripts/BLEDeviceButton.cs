using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Boton de la interfaz para un dispositivo
public class BLEDeviceButton : MonoBehaviour
{
    [Header("Componentes del Prefab")]
    public TextMeshProUGUI deviceNameText;
    public TextMeshProUGUI macAddressText;
    public TextMeshProUGUI rssiText;
    public Button connectButton;

    private string macAddress;
    private BLEConnector bleConnector;

    // configurar el boton con los datos del dispositivo
    public void Initialize(string name, string mac, string rssi, BLEConnector connector)
    {
        this.macAddress = mac;
        this.bleConnector = connector;

        deviceNameText.text = name;
        macAddressText.text = mac;
        rssiText.text = rssi + " dBm";

        // asignamos el listener por codigo en vez de usar el inspector
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private void OnConnectClicked()
    {
        if (bleConnector != null)
        {
            bleConnector.ConnectToDevice(macAddress);
        }
    }

    void OnDestroy()
    {
        // limpiamos el listener al destruir para evitar problemas
        connectButton.onClick.RemoveListener(OnConnectClicked);
    }
}
