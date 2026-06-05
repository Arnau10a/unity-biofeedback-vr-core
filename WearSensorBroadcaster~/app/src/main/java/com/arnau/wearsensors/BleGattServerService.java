package com.arnau.wearsensors;

import android.annotation.SuppressLint;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGattCharacteristic;
import android.bluetooth.BluetoothGattServer;
import android.bluetooth.BluetoothGattServerCallback;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.le.AdvertiseCallback;
import android.bluetooth.le.AdvertiseData;
import android.bluetooth.le.AdvertiseSettings;
import android.bluetooth.le.BluetoothLeAdvertiser;
import android.content.Context;
import android.content.Intent;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.ParcelUuid;
import android.util.Log;

import java.nio.charset.StandardCharsets;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;

@SuppressLint("MissingPermission")
public class BleGattServerService extends Service {
    private static final String TAG = "BleGattServer";
    
    // UUIDs personalizados para tu app (Nuevos. En Unity tendrás que conectarte a estos)
    public static final UUID CUSTOM_SERVICE_UUID = UUID.fromString("12345678-1234-5678-1234-56789abcdef0");
    public static final UUID CUSTOM_CHARACTERISTIC_UUID = UUID.fromString("12345678-1234-5678-1234-56789abcdef1");

    private BluetoothManager bluetoothManager;
    private BluetoothAdapter bluetoothAdapter;
    private BluetoothLeAdvertiser bluetoothLeAdvertiser;
    private BluetoothGattServer gattServer;
    private BluetoothGattCharacteristic dataCharacteristic;
    
    private final Set<BluetoothDevice> registeredDevices = new HashSet<>();
    private SensorReaderManager sensorManager;
    private final Handler handler = new Handler(Looper.getMainLooper());
    private boolean isBroadcasting = false;
    private long broadcastIntervalMs = 1000; // Intervalo de envío en ms (por defecto 1000ms)

    private final Runnable updateRunnable = new Runnable() {
        @Override
        public void run() {
            if (isBroadcasting) {
                broadcastSensorData();
                handler.postDelayed(this, broadcastIntervalMs); // Enviar con intervalo dinámico
            }
        }
    };

    @Override
    public void onCreate() {
        super.onCreate();
        sensorManager = new SensorReaderManager(this);
        initBluetooth();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        startForegroundService();
        startServer();
        return START_STICKY; // Mantener vivo si la pantalla se apaga
    }

    private void startForegroundService() {
        String channelId = "BioBroadcasterChannel";
        NotificationChannel channel = new NotificationChannel(channelId, "BioBroadcaster", NotificationManager.IMPORTANCE_LOW);
        getSystemService(NotificationManager.class).createNotificationChannel(channel);

        Notification notification = new Notification.Builder(this, channelId)
                .setContentTitle("BioWatch")
                .setContentText("Enviando sensores de salud por BLE...")
                .setSmallIcon(android.R.drawable.ic_menu_compass)
                .build();
        startForeground(1, notification);
    }

    private void initBluetooth() {
        bluetoothManager = (BluetoothManager) getSystemService(Context.BLUETOOTH_SERVICE);
        bluetoothAdapter = bluetoothManager.getAdapter();
    }

    private void startServer() {
        if (bluetoothAdapter == null || !bluetoothAdapter.isEnabled()) {
            Log.e(TAG, "Bluetooth no activado");
            return;
        }

        gattServer = bluetoothManager.openGattServer(this, gattServerCallback);
        if (gattServer == null) {
            Log.e(TAG, "No se pudo crear GATT server");
            return;
        }

        BluetoothGattService service = new BluetoothGattService(CUSTOM_SERVICE_UUID, BluetoothGattService.SERVICE_TYPE_PRIMARY);
        // Propiedades: Read (para leerlo), Notify (para enviar automático) y Write (para recibir comandos)
        dataCharacteristic = new BluetoothGattCharacteristic(
                CUSTOM_CHARACTERISTIC_UUID,
                BluetoothGattCharacteristic.PROPERTY_READ | BluetoothGattCharacteristic.PROPERTY_NOTIFY | BluetoothGattCharacteristic.PROPERTY_WRITE,
                BluetoothGattCharacteristic.PERMISSION_READ | BluetoothGattCharacteristic.PERMISSION_WRITE
        );
        service.addCharacteristic(dataCharacteristic);
        gattServer.addService(service);

        startAdvertising();
        sensorManager.start();
        isBroadcasting = true;
        handler.post(updateRunnable);
    }

    private void startAdvertising() {
        bluetoothLeAdvertiser = bluetoothAdapter.getBluetoothLeAdvertiser();
        if (bluetoothLeAdvertiser == null) return;

        AdvertiseSettings settings = new AdvertiseSettings.Builder()
                .setAdvertiseMode(AdvertiseSettings.ADVERTISE_MODE_LOW_POWER) // Low Power ideal para batería
                .setConnectable(true)
                .setTimeout(0)
                .setTxPowerLevel(AdvertiseSettings.ADVERTISE_TX_POWER_MEDIUM)
                .build();

        AdvertiseData data = new AdvertiseData.Builder()
                .setIncludeDeviceName(true)
                .addServiceUuid(new ParcelUuid(CUSTOM_SERVICE_UUID))
                .build();

        // Intenta cambiar nombre
        try { 
            bluetoothAdapter.setName("BioWatch"); 
        } catch(SecurityException e) {
            Log.w(TAG, "No se pudo cambiar el nombre del adaptador", e);
        } 
        
        try {
            bluetoothLeAdvertiser.startAdvertising(settings, data, advertiseCallback);
        } catch (SecurityException e) {
            Log.e(TAG, "Faltan permisos de publicidad Bluetooth", e);
        }
    }

    private final AdvertiseCallback advertiseCallback = new AdvertiseCallback() {
        @Override
        public void onStartSuccess(AdvertiseSettings settingsInEffect) {
            Log.i(TAG, "Publicidad BLE iniciada con éxito");
        }

        @Override
        public void onStartFailure(int errorCode) {
            Log.e(TAG, "Error iniciando publicidad: " + errorCode);
        }
    };

    private final BluetoothGattServerCallback gattServerCallback = new BluetoothGattServerCallback() {
        @Override
        public void onConnectionStateChange(BluetoothDevice device, int status, int newState) {
            if (newState == android.bluetooth.BluetoothProfile.STATE_CONNECTED) {
                registeredDevices.add(device); // Quest 3 se conectó
            } else if (newState == android.bluetooth.BluetoothProfile.STATE_DISCONNECTED) {
                registeredDevices.remove(device); // Quest 3 se desconectó
            }
        }

        @Override
        public void onCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic) {
            if (CUSTOM_CHARACTERISTIC_UUID.equals(characteristic.getUuid())) {
                byte[] value = sensorManager.getLatestDataString().getBytes(StandardCharsets.UTF_8);
                try {
                    gattServer.sendResponse(device, requestId, android.bluetooth.BluetoothGatt.GATT_SUCCESS, offset, value);
                } catch(SecurityException e) {
                    Log.e(TAG, "Excepción de seguridad respondiendo a ReadRequest", e);
                }
            }
        }

        @Override
        public void onCharacteristicWriteRequest(BluetoothDevice device, int requestId, BluetoothGattCharacteristic characteristic, boolean preparedWrite, boolean responseNeeded, int offset, byte[] value) {
            if (CUSTOM_CHARACTERISTIC_UUID.equals(characteristic.getUuid())) {
                if (value != null && value.length > 0) {
                    String strValue = new String(value, StandardCharsets.UTF_8);
                    Log.d(TAG, "Escritura recibida en característica: " + strValue);
                    if (strValue.startsWith("FREQ:")) {
                        try {
                            String freqStr = strValue.substring(5).trim();
                            long newInterval = Long.parseLong(freqStr);
                            if (newInterval > 0) {
                                broadcastIntervalMs = newInterval;
                                Log.i(TAG, "Frecuencia de transmisión cambiada a: " + broadcastIntervalMs + " ms");
                            }
                        } catch (NumberFormatException e) {
                            Log.e(TAG, "Error al parsear frecuencia recibida: " + strValue, e);
                        }
                    }
                }
                if (responseNeeded) {
                    try {
                        gattServer.sendResponse(device, requestId, android.bluetooth.BluetoothGatt.GATT_SUCCESS, offset, value);
                    } catch (SecurityException e) {
                        Log.e(TAG, "Excepción de seguridad respondiendo a WriteRequest", e);
                    }
                }
            }
        }
    };

    private void broadcastSensorData() {
        if (registeredDevices.isEmpty()) return; // No enviar nada si nadie escucha (Ahorra bateria)

        String data = sensorManager.getLatestDataString();
        byte[] value = data.getBytes(StandardCharsets.UTF_8);
        dataCharacteristic.setValue(value);

        for (BluetoothDevice device : registeredDevices) {
            try {
                gattServer.notifyCharacteristicChanged(device, dataCharacteristic, false);
            } catch (SecurityException e) {
                Log.e(TAG, "Excepción de seguridad al enviar broadcast", e);
            }
        }
    }

    @Override
    public void onDestroy() {
        isBroadcasting = false;
        sensorManager.stop();
        if (bluetoothLeAdvertiser != null) {
            try {
                bluetoothLeAdvertiser.stopAdvertising(advertiseCallback);
            } catch (SecurityException e) {
                Log.e(TAG, "Excepción deteniendo publicidad", e);
            }
        }
        if (gattServer != null) {
            try { 
                gattServer.close(); 
            } catch(SecurityException e) {
                Log.e(TAG, "Excepción cerrando GATT server", e);
            }
        }
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) { return null; }
}
