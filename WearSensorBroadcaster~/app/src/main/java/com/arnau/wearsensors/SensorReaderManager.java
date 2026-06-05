package com.arnau.wearsensors;

import android.content.Context;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.BatteryManager;
import android.content.Intent;
import android.content.IntentFilter;
import java.util.List;

public class SensorReaderManager implements     SensorEventListener {
    private SensorManager sensorManager;
    private Context context;
    
    // Sensores soportados
    private Sensor heartRateSensor;
    private Sensor accelSensor;
    private Sensor gyroSensor;
    private Sensor pressureSensor;
    private Sensor stepSensor;
    private Sensor lightSensor;
    private Sensor tempSensor;
    private Sensor rotationSensor; // Nuevo: Para orientacion 3D suave
    
    // Variables de estado
    private int currentHeartRate = 0;
    private float accX = 0f, accY = 0f, accZ = 0f;
    private float gyrX = 0f, gyrY = 0f, gyrZ = 0f;
    private float pressure = 0f;
    private int steps = 0;
    private float light = 0f;
    private float temperature = 0f;
    private int batteryLevel = 0;
    private float qX = 0f, qY = 0f, qZ = 0f, qW = 0f; // Quaternions para Unity
    
    private boolean isRunning = false;

    public SensorReaderManager(Context context) {
        this.context = context;
        sensorManager = (SensorManager) context.getSystemService(Context.SENSOR_SERVICE);
        if (sensorManager != null) {
            heartRateSensor = sensorManager.getDefaultSensor(Sensor.TYPE_HEART_RATE);
            accelSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
            gyroSensor = sensorManager.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
            pressureSensor = sensorManager.getDefaultSensor(Sensor.TYPE_PRESSURE);
            stepSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);
            lightSensor = sensorManager.getDefaultSensor(Sensor.TYPE_LIGHT);
            rotationSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR);

            // Busqueda inteligente del sensor de temperatura (para evitar el 0.0)
            List<Sensor> sensorList = sensorManager.getSensorList(Sensor.TYPE_ALL);
            for (Sensor s : sensorList) {
                String name = s.getName().toLowerCase();
                if (name.contains("wrist") || name.contains("skin") || name.contains("body") || name.contains("temperature")) {
                    tempSensor = s;
                    android.util.Log.d("BioWatch", "Sensor de temperatura encontrado: " + s.getName());
                    break;
                }
            }
            if (tempSensor == null) {
                tempSensor = sensorManager.getDefaultSensor(Sensor.TYPE_AMBIENT_TEMPERATURE);
            }
        }
    }

    public void start() {
        if (!isRunning && sensorManager != null) {
            // Intentamos registrar todos. Si alguno es NULL (el fabricante no lo incluyó o bloqueó), simplemente no crasheara y enviara 0.
            register(heartRateSensor);
            register(accelSensor);
            register(gyroSensor);
            register(pressureSensor);
            register(stepSensor);
            register(lightSensor);
            register(tempSensor);
            register(rotationSensor);
            isRunning = true;
        }
    }
    
    private void register(Sensor sensor) {
        if (sensor != null) {
            sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_NORMAL);
        }
    }

    public void stop() {
        if (isRunning && sensorManager != null) {
            sensorManager.unregisterListener(this);
            isRunning = false;
        }
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        int type = event.sensor.getType();

        if (type == Sensor.TYPE_HEART_RATE) {
            currentHeartRate = (int) event.values[0];
        } else if (type == Sensor.TYPE_ACCELEROMETER) {
            accX = event.values[0];
            accY = event.values[1];
            accZ = event.values[2];
        } else if (type == Sensor.TYPE_GYROSCOPE) {
            gyrX = event.values[0];
            gyrY = event.values[1];
            gyrZ = event.values[2];
        } else if (type == Sensor.TYPE_PRESSURE) {
            pressure = event.values[0];
        } else if (type == Sensor.TYPE_STEP_COUNTER) {
            steps = (int) event.values[0];
        } else if (type == Sensor.TYPE_LIGHT) {
            light = event.values[0];
        } else if (event.sensor == tempSensor) {
            temperature = event.values[0];
        } else if (type == Sensor.TYPE_ROTATION_VECTOR) {
            qX = event.values[0];
            qY = event.values[1];
            qZ = event.values[2];
            qW = event.values[3];
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) { }

    private void updateBattery() {
        IntentFilter ifilter = new IntentFilter(Intent.ACTION_BATTERY_CHANGED);
        Intent batteryStatus = context.registerReceiver(null, ifilter);
        if (batteryStatus != null) {
            int level = batteryStatus.getIntExtra(BatteryManager.EXTRA_LEVEL, -1);
            int scale = batteryStatus.getIntExtra(BatteryManager.EXTRA_SCALE, -1);
            batteryLevel = (int) ((level / (float) scale) * 100);
        }
    }

    // Genera string ordenado: PULSO,AccX,AccY,AccZ,GyrX,GyrY,GyrZ,Presion,Pasos,Luz,Temp,Bat,QX,QY,QZ,QW
    public String getLatestDataString() {
        updateBattery();
        return String.format(java.util.Locale.US, "%d,%.1f,%.1f,%.1f,%.2f,%.2f,%.2f,%.1f,%d,%.0f,%.1f,%d,%.4f,%.4f,%.4f,%.4f",
                currentHeartRate, accX, accY, accZ, gyrX, gyrY, gyrZ, pressure, steps, light, temperature, batteryLevel, qX, qY, qZ, qW);
    }
}
