using UnityEngine;
using System;

public class Fish : MonoBehaviour
{
  [Serializable]
  public class Settings
  {
    public float SpeedMultipliyer = 1.5f;
    public float ViewRadius = 0.8f;
    public float OptDistance = 0.15f;
    public float MinSpeed { get{ return 0.1f * SpeedMultipliyer; } }
    public float InclineFactor { get{ return 300.0f / SpeedMultipliyer; } }
    public float AligmentForcePart = 0.002f;
    public float TotalForceMultipliyer = 12;
    public float Inertness = 0.6f;
    public float VerticalPriority = 0.5f; // fish schools spread more evenly in 3D

    [System.Xml.Serialization.XmlIgnore]
    public Trace Trace { get; set; }
    public float AttractrionForce = 0.02f;
  }

  [Serializable]
  public class DebugSettings
  {
    public bool enableDrawing = false;

    public bool obstaclesAvoidanceDraw = false;
    public Color obstaclesAvoidanceColor = Color.red;

    public bool velocityDraw = false;
    public Color velocityColor = Color.grey;

    public bool positionForceDraw = false;
    public Color positionForceColor = Color.cyan;

    public bool alignmentForceDraw = false;
    public Color alignmentForceColor = Color.yellow;

    public bool cohesionForceDraw = false;
    public Color cohesionForceColor = Color.magenta;

    public bool collisionsAvoidanceForceDraw = false;
    public Color collisionsAvoidanceForceColor = Color.green;

    public bool attractionForceDraw = false;
    public Color attractionForceColor = Color.green;

    public bool totalForceDraw = false;
    public Color totalForceColor = Color.black;
  }

  public interface ITrigger
  {
    void OnTouch( Fish fish );
  }

  private Settings sts = null;
  public Settings SettingsRef {
    get { return sts; }
    set { sts = value; }
  }

  private DebugSettings dbgSts = null;
  public DebugSettings DebugSettingsRef {
    get { return dbgSts; }
    set { dbgSts = value; }
  }

  private Vector3 velocity = Vector3.zero;
  public Vector3 Velocity { get{ return velocity; } }

  void Start()
  {
    if( sts == null )
    {
      sts = FishSchool.GetSettings( gameObject );

      if( sts == null )
      {
        Debug.LogWarning( "Fish initialized with standalone settings copy" );
        sts = new Settings();
      }
    }

    if( dbgSts == null )
    {
      Debug.LogWarning( "Fish initialized with standalone debug settings copy" );
      dbgSts = new DebugSettings();
    }
  }

  void FixedUpdate()
  {
    // Craig Reynolds boids algorithm — http://www.red3d.com/cwr/boids/
    // Each fish is affected by three forces:
    //   cohesion, separation + collisionAvoidance, alignment

    var sepForce = new FishTools.SeparationForce(sts);
    var collAvoid = new FishTools.CollisionAvoidanceForce( sts, sepForce.Calc(sts.OptDistance) );

    var centeroid = Vector3.zero;
    var collisionAvoidance = Vector3.zero;
    var avgSpeed = Vector3.zero;
    var neighbourCount = 0;

    var direction = transform.rotation * Vector3.forward;
    var curPos = transform.position;

    foreach( var vis in Physics.OverlapSphere(curPos, sts.ViewRadius) )
    {
      var visPos = vis.transform.position;
      Fish fish;
      ITrigger trigger;

      if( (fish = vis.GetComponent<Fish>()) != null ) // schoolmates
      {
        Vector3 separationForce;

        if( !sepForce.Calc(curPos, visPos, out separationForce) )
          continue;

        collisionAvoidance += separationForce;
        ++neighbourCount;
        centeroid += visPos;
        avgSpeed += fish.velocity;
      }
      else if( (trigger = vis.GetComponent<ITrigger>()) != null )
      {
        if( GetComponent<Collider>().bounds.Intersects(vis.bounds) )
          trigger.OnTouch(this);
      }
      else // obstacles (reefs, rocks, etc.)
      {
        FishTools.CollisionAvoidanceForce.Force force;
        if( collAvoid.Calc(curPos, direction, vis, out force) )
        {
          collisionAvoidance += force.dir;

          if( dbgSts.enableDrawing && dbgSts.obstaclesAvoidanceDraw )
            Drawer.DrawRay( force.pos, force.dir, dbgSts.obstaclesAvoidanceColor );
        }
      }
    }

    if( neighbourCount > 0 )
    {
      centeroid = centeroid / neighbourCount - curPos;
      centeroid.y *= sts.VerticalPriority;
      avgSpeed = avgSpeed / neighbourCount - velocity;
    }

    var positionForce = (1.0f - sts.AligmentForcePart) * sts.SpeedMultipliyer * (centeroid + collisionAvoidance);
    var alignmentForce = sts.AligmentForcePart * avgSpeed / Time.deltaTime;
    var attractionForce = CalculateAttractionForce( sts, curPos, velocity );
    var totalForce = sts.TotalForceMultipliyer * ( positionForce + alignmentForce + attractionForce );

    var newVelocity = (1 - sts.Inertness) * (totalForce * Time.deltaTime) + sts.Inertness * velocity;

    velocity = CalcNewVelocity( sts.MinSpeed, velocity, newVelocity, direction );

    var rotation = CalcRotation( sts.InclineFactor, velocity, totalForce );

    if( MathTools.IsValid(rotation) )
      gameObject.transform.rotation = rotation;

    if( dbgSts.enableDrawing )
    {
      if( dbgSts.velocityDraw )
        Drawer.DrawRay( curPos, velocity, dbgSts.velocityColor );

      if( dbgSts.positionForceDraw )
        Drawer.DrawRay( curPos, positionForce, dbgSts.positionForceColor );

      if( dbgSts.alignmentForceDraw )
        Drawer.DrawRay( curPos, alignmentForce, dbgSts.alignmentForceColor );

      if( dbgSts.cohesionForceDraw )
        Drawer.DrawRay( curPos, centeroid, dbgSts.cohesionForceColor );

      if( dbgSts.collisionsAvoidanceForceDraw )
        Drawer.DrawRay( curPos, collisionAvoidance, dbgSts.collisionsAvoidanceForceColor );

      if( dbgSts.attractionForceDraw )
        Drawer.DrawRay( curPos, attractionForce, dbgSts.attractionForceColor );

      if( dbgSts.totalForceDraw )
        Drawer.DrawRay( curPos, totalForce, dbgSts.totalForceColor );
    }
  }

  void Update()
  {
    transform.position += velocity * Time.deltaTime;
  }

  // Pulls fish toward the current waypoint on the Trace path
  static Vector3 CalculateAttractionForce( Settings sts, Vector3 curPos, Vector3 curVelocity )
  {
    if( !sts.Trace )
      return Vector3.zero;

    var attrPos = sts.Trace.GetAtractionPoint();
    var direction = (attrPos - curPos).normalized;

    var factor = sts.AttractrionForce * sts.SpeedMultipliyer * MathTools.AngleToFactor( direction, curVelocity );

    return factor * direction;
  }

  static Vector3 CalcNewVelocity( float minSpeed, Vector3 curVel, Vector3 dsrVel, Vector3 defaultVelocity )
  {
    var curVelLen = curVel.magnitude;

    if( curVelLen > MathTools.epsilon )
      curVel /= curVelLen;
    else
    {
      curVel = defaultVelocity;
      curVelLen = 1;
    }

    var dsrVelLen = dsrVel.magnitude;
    var resultLen = minSpeed;

    if( dsrVelLen > MathTools.epsilon )
    {
      dsrVel /= dsrVelLen;

      var angleFactor = MathTools.AngleToFactor(dsrVel, curVel);
      var rotReqLength = 2 * curVelLen * angleFactor;
      var speedRest = dsrVelLen - rotReqLength;

      if( speedRest > 0 )
      {
        curVel = dsrVel;
        resultLen = speedRest;
      }
      else
      {
        curVel = Vector3.Slerp( curVel, dsrVel, dsrVelLen / rotReqLength );
      }

      if( resultLen < minSpeed )
        resultLen = minSpeed;
    }

    return curVel * resultLen;
  }

  // Fish roll slightly when turning, similar to banking
  static Quaternion CalcRotation( float inclineFactor, Vector3 velocity, Vector3 totalForce )
  {
    if( velocity.sqrMagnitude < MathTools.sqrEpsilon )
      return new Quaternion( float.NaN, float.NaN, float.NaN, float.NaN );

    var rightVec = MathTools.RightVectorXZProjected(velocity);
    var inclineDeg = MathTools.VecProjectedLength( totalForce, rightVec ) * -inclineFactor;
    return Quaternion.LookRotation( velocity ) * Quaternion.AngleAxis(Mathf.Clamp(inclineDeg, -45, 45), Vector3.forward);
  }
}
