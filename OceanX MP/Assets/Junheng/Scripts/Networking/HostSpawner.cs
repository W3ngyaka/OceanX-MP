using Unity.Netcode;
using UnityEngine;

// Place this in the TRIFOLD scene only.
// Spawns the EcosystemNetworkManager prefab once the host is up.
// The client receives it automatically via NGO — no setup needed on the tablet side.
public class HostSpawner : MonoBehaviour
{
    [Tooltip("The EcosystemNetworkManager prefab (must be registered in NetworkManager's Network Prefabs list).")]
    public GameObject EcosystemNetworkManagerPrefab;

    private void Awake()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("[HostSpawner] Server started — spawning EcosystemNetworkManager.");
        GameObject go = Instantiate(EcosystemNetworkManagerPrefab);
        go.GetComponent<NetworkObject>().Spawn();
    }
}
