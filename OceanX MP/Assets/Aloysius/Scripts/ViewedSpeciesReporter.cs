using UnityEngine;

// TABLET: reports which species the info modal is currently showing to the host, so the LARGE
// SCREEN can mirror a summarised card for bystanders. Call Report(index) when the modal opens
// and Clear() when it closes. Index must match the sim's species order.
public class ViewedSpeciesReporter : MonoBehaviour
{
    public static ViewedSpeciesReporter Instance { get; private set; }

    void Awake() { Instance = this; }

    // Report the species the tablet is now showing (-1 to clear).
    public static void Report(int speciesIndex)
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        net.SetViewedSpeciesRpc(speciesIndex);
    }

    public static void Clear() { Report(-1); }
}
