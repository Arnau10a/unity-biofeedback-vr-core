using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android; 
using TMPro; 

public class BLEPermissionManager : MonoBehaviour
{
    [Header("UI para Debug")]
    public TextMeshProUGUI uiLogText;

    // lista de permisos necesarios para android 12+
    private readonly string[] requiredPermissions = new string[]
    {
        "android.permission.BLUETOOTH_SCAN",
        "android.permission.BLUETOOTH_CONNECT",
        Permission.FineLocation 
    };

    void Start()
    {
        if (uiLogText != null) uiLogText.text = "";
        
        LogMessage("Comprobando permisos BLE...");
        CheckAndRequestPermissions();
    }

    public void CheckAndRequestPermissions()
    {
        List<string> permissionsToRequest = new List<string>();

        // solicitar solo los permisos que nos faltan
        foreach (string permission in requiredPermissions)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                permissionsToRequest.Add(permission);
            }
        }

        if (permissionsToRequest.Count > 0)
        {
            LogMessage($"Faltan {permissionsToRequest.Count} permisos, solicitando...");
            
            // preparar las callbacks para la respuesta del usuario
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += OnPermissionGranted;
            callbacks.PermissionDenied += OnPermissionDenied;
            callbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedDontAskAgain;

            Permission.RequestUserPermissions(permissionsToRequest.ToArray(), callbacks);
        }
        else
        {
            LogMessage("Permisos ok. Listo para iniciar.");
            OnAllPermissionsReady();
        }
    }

    private void OnPermissionGranted(string permissionName)
    {
        LogMessage($"Permiso concedido: {permissionName}");
    }

    private void OnPermissionDenied(string permissionName)
    {
        LogMessage($"Permiso denegado: {permissionName}");
    }

    private void OnPermissionDeniedDontAskAgain(string permissionName)
    {
        LogMessage($"Denegado siempre: {permissionName}. Ir a Ajustes.");
    }

    private void OnAllPermissionsReady()
    {
        LogMessage("Hardware BLE listo.");
    }

    private void LogMessage(string message)
    {
        Debug.Log($"[BLEManager] {message}");
        if (uiLogText != null) uiLogText.text += message + "\n";
    }
}
