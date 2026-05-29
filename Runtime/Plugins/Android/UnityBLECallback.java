package com.biofeedback.ble;

import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanResult;
import java.util.List;

// clase envoltorio para heredar la clase abstracta de ScanCallback y conectar con jni
// aqui recibimos los datos y los enviamos a c#
public class UnityBLECallback extends ScanCallback {
    
    private String gameObjectName;
    
    public UnityBLECallback(String gameObject) {
        this.gameObjectName = gameObject;
    }

    @Override
    public void onScanResult(int callbackType, ScanResult result) {
        super.onScanResult(callbackType, result);
        
        if (result != null && result.getDevice() != null) {
            String address = result.getDevice().getAddress();
            String name = result.getDevice().getName();
            
            // si el dispositivo no trae nombre intentar sacarlo del payload del anuncio
            if (name == null || name.isEmpty()) {
                if (result.getScanRecord() != null) {
                    name = result.getScanRecord().getDeviceName();
                }
            }
            
            int rssi = result.getRssi();
            if (name == null || name.isEmpty()) name = "Desconocido";
            
            // pasar un string largo con el nombre, mac y rssi para procesarlo en unity
            String payload = name + "|" + address + "|" + rssi;
            
            // enviamos los datos usando los mensajes nativos de unity
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnDeviceDiscoveredFromJava", payload);
        }
    }

    @Override
    public void onBatchScanResults(List<ScanResult> results) {
        super.onBatchScanResults(results);
    }

    @Override
    public void onScanFailed(int errorCode) {
        super.onScanFailed(errorCode);
        com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, "OnScanFailedFromJava", String.valueOf(errorCode));
    }
}
