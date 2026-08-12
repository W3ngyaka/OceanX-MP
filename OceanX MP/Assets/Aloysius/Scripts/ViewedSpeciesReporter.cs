using UnityEngine;


public class ViewedSpeciesReporter : MonoBehaviour
{


    public static void Report(int speciesIndex)
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        net.SetViewedSpeciesRpc(speciesIndex);
    }

    public static void Clear() { Report(-1); }
}
