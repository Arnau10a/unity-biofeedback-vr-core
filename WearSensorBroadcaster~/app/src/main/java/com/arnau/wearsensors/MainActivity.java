package com.arnau.wearsensors;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;

public class MainActivity extends Activity {
    private boolean isBroadcasting = false;
    private Button btnStart;
    private TextView tvStatus;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main); // UI Negra

        btnStart = findViewById(R.id.btnStart);
        tvStatus = findViewById(R.id.tvStatus);

        // Solicitamos los permisos críticos nada más abrir
        requestPermissions(new String[]{
                Manifest.permission.BODY_SENSORS,
                Manifest.permission.ACTIVITY_RECOGNITION,
                Manifest.permission.BLUETOOTH_ADVERTISE,
                Manifest.permission.BLUETOOTH_CONNECT,
                Manifest.permission.BLUETOOTH_SCAN,
                Manifest.permission.POST_NOTIFICATIONS
        }, 100);

        btnStart.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (!isBroadcasting) {
                    Intent serviceIntent = new Intent(MainActivity.this, BleGattServerService.class);
                    startForegroundService(serviceIntent);
                    
                    btnStart.setText("DETENER");
                    btnStart.setBackgroundColor(0xFFFF0000); // Rojo
                    tvStatus.setText("Transmitiendo a 1Hz...\n(Pantalla segura)");
                    isBroadcasting = true;
                } else {
                    stopService(new Intent(MainActivity.this, BleGattServerService.class));
                    
                    btnStart.setText("INICIAR");
                    btnStart.setBackgroundColor(0xFF4CAF50); // Verde
                    tvStatus.setText("Listo para transmitir");
                    isBroadcasting = false;
                }
            }
        });
    }
}
