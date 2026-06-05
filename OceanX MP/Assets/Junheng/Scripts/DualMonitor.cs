using UnityEngine;

// Activates Display 2 (iPad via Spacedesk) on startup.
// Attach to any GameObject in the scene.
// Point a Camera and/or Canvas to Target Display: Display 2 to control what shows there.
public class DualMonitor : MonoBehaviour
{
    private void Start()
    {
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            Debug.Log("[DualMonitor] Display 2 activated.");
        }
        else
        {
            Debug.LogWarning("[DualMonitor] Display 2 not detected. Make sure Spacedesk is connected before pressing Play.");
        }
    }
}
