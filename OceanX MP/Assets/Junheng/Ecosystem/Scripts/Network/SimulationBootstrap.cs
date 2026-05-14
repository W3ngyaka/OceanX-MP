using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

// Attach to any GameObject in your startup or main scene.
// Set Role to Trifold on the laptop build, Tablet on the Android APK.
//
// Laptop hotspot IP is always 192.168.137.1 when using Windows Mobile Hotspot.
// If using a router instead, run ipconfig on the laptop and paste the IPv4 address.
public class SimulationBootstrap : MonoBehaviour
{
    public enum DeviceRole { Trifold, Tablet }

    [Header("Device Role")]
    [Tooltip("Trifold = laptop host. Tablet = Android client.")]
    [SerializeField] private DeviceRole _role = DeviceRole.Trifold;

    [Header("Network")]
    [SerializeField] private string _laptopIP = "192.168.137.1"; // Windows hotspot default
    [SerializeField] private ushort _port     = 7777;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text _statusLabel;

    private void Start()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (_role == DeviceRole.Trifold)
        {
            transport.SetConnectionData("0.0.0.0", _port);
            NetworkManager.Singleton.StartHost();
            SetStatus("Trifold — hosting on port " + _port);
        }
        else
        {
            transport.SetConnectionData(_laptopIP, _port);
            NetworkManager.Singleton.StartClient();
            SetStatus($"Tablet — connecting to {_laptopIP}:{_port}…");

            NetworkManager.Singleton.OnClientConnectedCallback    += _ => SetStatus("Connected to Trifold ✓");
            NetworkManager.Singleton.OnClientDisconnectCallback   += _ => SetStatus("Disconnected — retrying…");
        }
    }

    private void SetStatus(string message)
    {
        Debug.Log("[SimulationBootstrap] " + message);
        if (_statusLabel != null)
            _statusLabel.text = message;
    }
}
