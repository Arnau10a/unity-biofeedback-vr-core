package com.biofeedback.ble;

import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattDescriptor;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothProfile;
import java.util.List;
import java.util.UUID;

// clase envoltorio para gatt porque es abstracta y c# no la asimila igual
public class UnityBLEGattCallback extends BluetoothGattCallback {

    private String gameObjectName;
    
    // uuid estandar para configurar descriptores de notificaciones
    private static final UUID CLIENT_CHARACTERISTIC_CONFIG = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb");

    public UnityBLEGattCallback(String gameObject) {
        this.gameObjectName = gameObject;
    }

    // cuando nos conectamos o desconectamos de algo
    @Override
    public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
        if (status != BluetoothGatt.GATT_SUCCESS) {
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnGattError", "Error GATT status: " + status + " en estado " + newState);
            if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                gatt.close();
            }
            return;
        }

        if (newState == BluetoothProfile.STATE_CONNECTED) {
            // avisamos al juego
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnGattConnected", gatt.getDevice().getAddress());
            
            // IMPORTANTE: Pedimos ensanchar el canal a 512 bytes para que quepan todos los sensores de golpe
            gatt.requestMtu(512);
            
            // pedimos buscar los servicios del dispositivo
            gatt.discoverServices();
        } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnGattDisconnected", gatt.getDevice().getAddress());
            gatt.close();
        }
    }

    // cuando ya nos cargan los servicios delegados
    @Override
    public void onServicesDiscovered(BluetoothGatt gatt, int status) {
        if (status != BluetoothGatt.GATT_SUCCESS) {
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnGattError", "onServicesDiscovered failed: " + status);
            return;
        }

        // agrupar uuids para pasarlos a unity facilmente
        List<BluetoothGattService> services = gatt.getServices();
        StringBuilder sb = new StringBuilder();
        for (BluetoothGattService service : services) {
            sb.append(service.getUuid().toString()).append(";");
        }
        com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnGattServicesDiscovered", sb.toString());

        // comprobamos si hay caracteristicas con notificaciones activables
        enableNotificationsOnFirstCompatibleCharacteristic(gatt, services);
    }

    // callback de cuando llegan datos de las notificaciones
    @Override
    public void onCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
        byte[] data = characteristic.getValue();
        if (data != null && data.length > 0) {
            String uuid = characteristic.getUuid().toString();
            String dataStr;
            
            // Si es nuestro reloj BioWatch, el dato viene en texto (UTF-8). Si es un pulsómetro estándar u otro, en Hex.
            if (uuid.equals("12345678-1234-5678-1234-56789abcdef1")) {
                dataStr = new String(data, java.nio.charset.StandardCharsets.UTF_8);
            } else {
                dataStr = bytesToHex(data);
            }
            
            // montar texto con el uuid y los datos preparados
            String payload = uuid + "|" + dataStr;
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnCharacteristicData", payload);
        }
    }

    // funcion para encontrar una caracteristica que podamos activar (priorizar BioWatch y luego pulso)
    private void enableNotificationsOnFirstCompatibleCharacteristic(BluetoothGatt gatt, List<BluetoothGattService> services) {
        // 1. Buscar primero nuestro reloj BioWatch personalizado
        UUID BIOWATCH_SERVICE_UUID = UUID.fromString("12345678-1234-5678-1234-56789abcdef0");
        UUID BIOWATCH_CHAR_UUID = UUID.fromString("12345678-1234-5678-1234-56789abcdef1");
        
        BluetoothGattService bioService = gatt.getService(BIOWATCH_SERVICE_UUID);
        if (bioService != null) {
            BluetoothGattCharacteristic bioCharacteristic = bioService.getCharacteristic(BIOWATCH_CHAR_UUID);
            if (bioCharacteristic != null && enableNotification(gatt, bioCharacteristic)) {
                return; // Exito con nuestro reloj!
            }
        }

        // 2. Buscar servicio de ritmo cardiaco usando el uuid estandar (Legacy fallback)
        UUID HEART_RATE_SERVICE_UUID = UUID.fromString("0000180d-0000-1000-8000-00805f9b34fb");
        UUID HEART_RATE_MEASUREMENT_CHAR_UUID = UUID.fromString("00002a37-0000-1000-8000-00805f9b34fb");

        BluetoothGattService hrService = gatt.getService(HEART_RATE_SERVICE_UUID);
        if (hrService != null) {
            BluetoothGattCharacteristic hrCharacteristic = hrService.getCharacteristic(HEART_RATE_MEASUREMENT_CHAR_UUID);
            if (hrCharacteristic != null && enableNotification(gatt, hrCharacteristic)) {
                return; // confirmacion de exito con pulsometro estandar
            }
        }

        // 3. Buscar otra cualquiera por si falla
        for (BluetoothGattService service : services) {
            for (BluetoothGattCharacteristic characteristic : service.getCharacteristics()) {
                if (enableNotification(gatt, characteristic)) {
                    return; 
                }
            }
        }
    }

    private boolean enableNotification(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) {
        int props = characteristic.getProperties();
        boolean canNotify = (props & BluetoothGattCharacteristic.PROPERTY_NOTIFY) != 0;
        boolean canIndicate = (props & BluetoothGattCharacteristic.PROPERTY_INDICATE) != 0;

        if (canNotify || canIndicate) {
            gatt.setCharacteristicNotification(characteristic, true);

            // modificar caracteristica para activar aviso en el dispositivo
            BluetoothGattDescriptor descriptor = characteristic.getDescriptor(CLIENT_CHARACTERISTIC_CONFIG);
            if (descriptor != null) {
                descriptor.setValue(canNotify
                        ? BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                        : BluetoothGattDescriptor.ENABLE_INDICATION_VALUE);
                gatt.writeDescriptor(descriptor);
            }
            return true;
        }
        return false;
    }

    // funcion de formatear bytes a texto hex
    private String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) {
            sb.append(String.format("0x%02X ", b));
        }
        return sb.toString().trim();
    }
}
