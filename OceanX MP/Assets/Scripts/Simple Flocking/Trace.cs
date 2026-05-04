using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class Trace : MonoBehaviour
{
  public Material normalState;
  public Material activeState;

  public WayPoint[] wayPoints;
  private WayPoint curWP = null;

  public void Start()
  {
    foreach( var wp in wayPoints )
      SetTrigger(wp, false);

    if( wayPoints.Length > 0 )
    {
      curWP = wayPoints[0];
      SetTrigger( curWP, true );
    }
  }

  void SetTrigger( WayPoint wp, bool value )
  {
    wp.GetComponent<Collider>().isTrigger = value;
    wp.GetComponent<Renderer>().material = value ? activeState : normalState;
  }

#if UNITY_EDITOR
  [UnityEditor.MenuItem ("GameEditor/Collect route from priority of waypoints")]
  static void DoSomething ()
  {
    Trace curTrace = FindFirstObjectByType<Trace>();

    var list = new List<WayPoint>();
    var rgx = new Regex(@"\d+$");

    foreach( var wp in FindObjectsByType<WayPoint>(FindObjectsSortMode.None) )
    {

      list.Add(wp);
      wp.name = rgx.Split(wp.name)[0] + wp.editorPriority.ToString("D2");
      wp.trace = curTrace;
    }

    list.Sort( (t1, t2) => t1.editorPriority - t2.editorPriority );
    curTrace.wayPoints = list.ToArray();
  }
#endif

  public Vector3 GetAtractionPoint()
  {
    return curWP.transform.position;
  }

  public void NextWayPoint()
  {
    SetTrigger( curWP, false );

    var nextIndex = Array.FindIndex( wayPoints, (v) => v == curWP ) + 1;

    if( nextIndex == wayPoints.Length )
      nextIndex = 0;

    curWP = wayPoints[nextIndex];
    SetTrigger( curWP, true );
  }
}
