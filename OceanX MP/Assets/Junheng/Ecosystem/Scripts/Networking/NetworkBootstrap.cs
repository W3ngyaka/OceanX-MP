using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Drop this on any GameObject in each scene.
// Set Role to Host on the trifold machine, Client on the tablet.
// Set HostAddress on the Client to the trifold's local IP (e.g. 192.168.1.x).
public class NetworkBootstrap : MonoBehaviour
{
    public enum DeviceRole { Host, Client }

    [SerializeField] private DeviceRole _role       = DeviceRole.Host;
    [SerializeField] private string     _hostAddress = "127.0.0.1";
    [SerializeField] private ushort     _port        = 7777;

    [Tooltip("Only needed on the Host — the EcosystemNetworkManager prefab to spawn.")]
    [SerializeField] private GameObject _ecosystemNetworkManagerPrefab;

    private void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkBootstrap] UnityTransport component not found on NetworkManager.");
            return;
        }

        if (_role == DeviceRole.Host)
        {
            // Host binds to all interfaces so any device on the network can connect
            transport.SetConnectionData("0.0.0.0", _port);
            Debug.Log($"[NetworkBootstrap] Starting as Host on port {_port}.");
            NetworkManager.Singleton.OnClientConnectedCallback += id => Debug.Log($"[NetworkBootstrap] Client connected! ID: {id}");
            NetworkManager.Singleton.StartHost();
            SpawnEcosystemNetworkManager();
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
