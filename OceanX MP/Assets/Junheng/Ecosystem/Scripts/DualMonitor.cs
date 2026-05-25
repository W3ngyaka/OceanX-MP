using UnityEngine;

public class DualMonitor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
