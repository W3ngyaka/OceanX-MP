using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Drop this on any GameObject in each scene.
// Set Role to Host on the trifold machine, Client on the tablet.
// Set HostAddress on the Client to the trifold's local IP (e.g. 192.168.1.x).
public class NetworkBootstrap : MonoBehaviour
{
    // Auto = decide from the platform (Windows/desktop -> Host, Android/mobile -> Client).
    // Host / Client = force that role regardless of platform (useful for testing in the Editor).
    public enum DeviceRole { Auto, Host, Client }

    [SerializeField] private DeviceRole _role       = DeviceRole.Auto;
    [SerializeField] private string     _hostAddress = "127.0.0.1";
    [SerializeField] private ushort     _port        = 7777;

    [Tooltip("Only needed on the Host — the EcosystemNetworkManager prefab to spawn.")]
    [SerializeField] private GameObject _ecosystemNetworkManagerPrefab;

    [Tooltip("Optional — LAN auto-discovery so the tablet finds the host without typing an IP. " +
             "Leave empty to auto-add one at runtime.")]
    [SerializeField] private LanDiscovery _discovery;

    // Gets the LanDiscovery component, adding one to this GameObject if none is assigned/present.
    public LanDiscovery GetDiscovery()
    {
        if (_discovery == null) _discovery = GetComponent<LanDiscovery>();
        if (_discovery == null) _discovery = gameObject.AddComponent<LanDiscovery>();
        return _discovery;
    }

    public ushort Port => _port;

    // Resolves the actual role to use at runtime.
    // Android tablet -> Client; Windows trifold machine (or Editor) -> Host.
    private DeviceRole ResolveRole()
    {
        if (_role != DeviceRole.Auto)
            return _role;

        return Application.isMobilePlatform ? DeviceRole.Client : DeviceRole.Host;
    }

    private void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkBootstrap] UnityTransport component not found on NetworkManager.");
            return;
        }

        DeviceRole role = ResolveRole();
        Debug.Log($"[NetworkBootstrap] Platform: {Application.platform}. Resolved role: {role}.");

        if (role == DeviceRole.Host)
        {
            // Host binds to all interfaces so any device on the network can connect
            transport.SetConnectionData("0.0.0.0", _port);
            Debug.Log($"[NetworkBootstrap] Starting as Host on port {_port}.");
            NetworkManager.Singleton.OnClientConnectedCallback += id => Debug.Log($"[NetworkBootstrap] Client connected! ID: {id}");

            if (NetworkManager.Singleton.StartHost())
            {
                SpawnEcosystemNetworkManager();
                // Start broadcasting our presence so tablets can auto-discover us.
                GetDiscovery().StartAdvertising(_port);
            }
            else
            {
                Debug.LogError($"[NetworkBootstrap] Failed to start Host (port {_port} likely already in use). " +
                               "Close any other running instance or free the port, then try again.");
            }
        }
        // Client waits for ConnectionScreenUI to call ConnectAsClient()
    }

    // Called by ConnectionScreenUI on the tablet
    public void ConnectAsClient(string ip)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip.Trim(), _port);
        Debug.Log($"[NetworkBootstrap] Starting as Client — connecting to {ip}:{_port}.");
        NetworkManager.Singleton.StartClient();
    }

    private void SpawnEcosystemNetworkManager()
    {
        if (_ecosystemNetworkManagerPrefab == null)
        {
            Debug.LogError("[NetworkBootstrap] EcosystemNetworkManager prefab not assigned.");
            return;
        }

        Debug.Log("[NetworkBootstrap] Spawning EcosystemNetworkManager.");
        GameObject go = Instantiate(_ecosystemNetworkManagerPrefab);
        go.GetComponent<NetworkObject>().Spawn();
    }
}
