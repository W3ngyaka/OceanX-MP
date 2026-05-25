using System;
using UnityEngine;

public class WayPoint : MonoBehaviour, Fish.ITrigger
{
  public int editorPriority = 0;
  public Trace trace;

  public void OnTouch(Fish fish)
  {
    if( GetComponent<Collider>().isTrigger )
      trace.NextWayPoint();
  }
}
