using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A fully-kinematic body with callbacks for handling collision and movement
/// </summary>
[SelectionBase]
public class KinematicBody : MonoBehaviour
{
    /// <summary>
    /// motor driving this body
    /// </summary>
    public IKinematicMotor motor;
    
    [Header("Body Definition")]
#pragma warning disable 0649 // Assigned in Unity inspector
    [SerializeField]
    private BoxCollider col;
    public BoxCollider BodyCollider => col;
    [SerializeField]
    private Rigidbody rbody;
    public Rigidbody BodyRigidbody => rbody;
#pragma warning restore 0649 // Assigned in Unity inspector
    /// <summary>
    /// Size of the box body in local space
    /// </summary>
    public Vector3 LocalBodySize => col.size;
    /// <summary>
    /// Buffer zone around the actual collider of the body
    /// </summary>
    public float skinWidth = 0.01f;
    /// <summary>
    /// Additional buffer zone around the skin that generates contacts
    /// </summary>
    public float contactOffset = 0.01f;
    /// <summary>
    /// Size of the box body in local space inclusive of the skin
    /// </summary>
    public Vector3 LocalBodySizeWithSkin => col.size + Vector3.one * skinWidth;
    /// <summary>
    /// Size of the box body in local space inclusive of the contact offset
    /// </summary>
    public Vector3 LocalBodySizeWithContactOffset => LocalBodySizeWithSkin + Vector3.one * contactOffset;
    public Vector3 GetLocalOffsetToCenter()
    {
        return col.center;
    }
    public Vector3 GetCenterAtBodyPosition(Vector3 bodyPosition)
    {
        return bodyPosition + col.center;
    }
    /// <summary>
    /// Position of the feet (aka bottom) of the body
    /// </summary>
    public Vector3 FootPosition => transform.TransformPoint(col.center + Vector3.down * col.size.y/2.0f);
    /// <summary>
    /// Offset from the pivot of the body to the feet
    /// </summary>
    public Vector3 FootOffset => (FootPosition - transform.position);
    /// <summary>
    /// The Internal Velocity of the player represents their ideal velocity
    /// </summary>    
    public Vector3 InternalVelocity { get; set; }
    public Vector3 Velocity { get; private set; }

    public Vector3 VelocityXZ { get { Vector3 v = Velocity; v.y = 0.0f; return v; } }
    public Vector3 VelocityXY { get { Vector3 v = Velocity; v.z = 0.0f; return v; } }

    [System.Serializable]
    public struct MoveCollision
    {
        public Vector3 bodyPosition;
        public Quaternion bodyRotation;

        public Vector3 bodyVelocity;
        public Collider otherCollider;
        public Rigidbody OtherRigidbody => otherCollider.attachedRigidbody;
        public Component EffectiveOther
        {
            get
            {
                Rigidbody otherRbody = OtherRigidbody;
                if(otherRbody != null) { return otherRbody; }
                else { return otherCollider; }
            }
        }

        public Vector3 collisionDirection;
        public float collisionPenetration;
    }
    [System.Serializable]
    public struct PenetrationData
    {
        public Collider other;
        public Vector3 direction;
        public float penetrationDepth;
    }
    
    public readonly static int MAX_COLLISIONS = 32;
    private MoveCollision[] LastCollisions = new MoveCollision[MAX_COLLISIONS];
    private MoveCollision[] LastTriggers = new MoveCollision[MAX_COLLISIONS];
    public int CollisionCount { get; private set; }
    public int TriggerCount { get; private set; }

    [Header("Body Settings")]
    public bool SendCollisionMessages = false;

    public MoveCollision GetCollision(int index)
    {
        if(index >= CollisionCount || index < 0) { throw new IndexOutOfRangeException(); }
        return LastCollisions[index];
    }

    public MoveCollision GetTrigger(int index)
    {
        if (index >= CollisionCount || index < 0) { throw new IndexOutOfRangeException(); }
        return LastTriggers[index];
    }

    public void CollideAndSlide(Vector3 bodyPosition, Quaternion bodyRotation, Vector3 bodyVelocity, Collider other)
    {
        MoveCollideAndSlide(ref bodyPosition, ref bodyRotation, ref bodyVelocity, other);
        
        // apply movement immediately
        rbody.MovePosition(bodyPosition);
        rbody.MoveRotation(bodyRotation);
        InternalVelocity = bodyVelocity;
    }

    public bool ComputePenetration(Vector3 bodyPosition, Quaternion bodyRotation, Collider other, Vector3 otherPosition, Quaternion otherRotation, out Vector3 direction, out float pen)
    {
        direction = Vector3.zero;
        pen = 0.0f;

        // ignore self collision
        if (other == col) { return false; }

        return Physics.ComputePenetration(col,
            bodyPosition,
            bodyRotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out direction,
            out pen);
    }

    public bool MoveCollideAndSlide(ref Vector3 bodyPosition, ref Quaternion bodyRotation, ref Vector3 bodyVelocity, Collider other)
    {
        bool didOverlap = CollideAndSlideWithData(ref bodyPosition, ref bodyRotation, ref bodyVelocity, other, out var data);
        
        // collisions
        MoveCollision collision = new MoveCollision
        {
            bodyPosition = bodyPosition,
            bodyRotation = bodyRotation,
            bodyVelocity = bodyVelocity,
            otherCollider = other,
            collisionDirection = data.direction,
            collisionPenetration = data.penetrationDepth
        };
        
        if (!other.isTrigger)
        {
            if (CollisionCount < MAX_COLLISIONS) { LastCollisions[CollisionCount++] = collision; }
        }
        // triggers
        else
        {
            if (TriggerCount < MAX_COLLISIONS) { LastTriggers[TriggerCount++] = collision; }
        }

        return didOverlap;
    }
    
    public bool CollideAndSlideWithData(ref Vector3 bodyPosition, ref Quaternion bodyRotation, ref Vector3 bodyVelocity, Collider other, out PenetrationData data)
    {
        // ignore self collision
        if(other == col) { data = new PenetrationData(); return false; }
            
        bool isOverlap = ComputePenetration(
            bodyPosition,
            bodyRotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out var mtv,
            out var pen);

        data = new PenetrationData() { other = other, direction = mtv, penetrationDepth = pen };
        
        if (isOverlap)
        {
            if (!other.isTrigger)
            {
                // defer to motor to resolve hit
                motor.OnMoveHit(ref bodyPosition, ref bodyRotation, ref bodyVelocity, other, mtv, pen);
            }
        }
        
        return isOverlap;
    }
    
    public Collider[] Overlap(Vector3 bodyPosition, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        return Overlap(bodyPosition, LocalBodySize / 2, layerMask, queryMode);
    }
    
    public Collider[] Overlap(Vector3 bodyPosition, Vector3 bodyHalfExtents, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        bodyPosition = GetCenterAtBodyPosition(bodyPosition);
        return Physics.OverlapBox(bodyPosition, bodyHalfExtents, rbody.rotation, layerMask, queryMode);
    }
    
    public RaycastHit[] Cast(Vector3 bodyPosition, Vector3 direction, float distance, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        return Cast(bodyPosition, LocalBodySize / 2, direction, distance, layerMask, queryMode);
    }
    
    public RaycastHit[] Cast(Vector3 bodyPosition, Vector3 bodyHalfExtents, Vector3 direction, float distance, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        bodyPosition = GetCenterAtBodyPosition(bodyPosition);
        var allHits = Physics.BoxCastAll(bodyPosition, bodyHalfExtents, direction, rbody.rotation, distance, layerMask, queryMode);

        // TODO: this is terribly inefficient and generates garbage, please optimize this
        List<RaycastHit> filteredhits = new List<RaycastHit>(allHits);
        filteredhits.RemoveAll( x => x.collider == col);
        return filteredhits.ToArray();
    }

    public RaycastHit[] Trace(Vector3 startBodyPosition, Vector3 endBodyPosition, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 offset = endBodyPosition - startBodyPosition;
        float len = offset.magnitude;

        Vector3 dir = offset / len;
        return Cast(startBodyPosition, dir, len, layerMask, queryMode);
    }
    
    public RaycastHit[] Trace(Vector3 startBodyPosition, Vector3 endBodyPosition, Vector3 bodyHalfExtents, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 offset = endBodyPosition - startBodyPosition;
        float len = offset.magnitude;

        Vector3 dir = offset / len;
        return Cast(startBodyPosition, bodyHalfExtents, dir, len, layerMask, queryMode);
    }
    
    //
    // Unity Messages
    //

    private void Start()
    {
        OnValidate();
    }

    private void FixedUpdate()
    {
        // scale check
        Debug.Assert(Mathf.Approximately(transform.lossyScale.sqrMagnitude, 3) == true, "Scaling is not supported on KinematicBody game objects.");

        Vector3 startPosition = rbody.position;
        Vector3 startSize = col.size;

        //
        // begin kinematicbody lifecycle
        //

        motor.OnPreMove();

        // clear bookkeeping variables

        CollisionCount = 0;
        TriggerCount = 0;

        //
        // resolve pre-overlapping colliders if any
        //
        /*
        var preexistingContacts = Overlap(startPosition, LocalBodySizeWithContactOffset / 2, -1, QueryTriggerInteraction.Ignore);
        foreach(var contact in preexistingContacts)
        {

        }
        */

        //
        // update internal forces
        //

        InternalVelocity = motor.UpdateVelocity(InternalVelocity);
        
        Vector3 projectedPos = rbody.position + (InternalVelocity * Time.deltaTime);
        Vector3 projectedVel = InternalVelocity;
        Quaternion projectedRot = rbody.rotation;

        var traces = Trace(startPosition, projectedPos, LocalBodySizeWithContactOffset, -1, QueryTriggerInteraction.Ignore);
        bool isClean = true;
        foreach(var trace in traces)
        {
            if (trace.collider != BodyCollider)
            {
                isClean = false;
                break;
            }
        }

        if (isClean)
        {
            goto FinishMove;
        }
        
        //
        // gather contacts
        //

        var contacts = Overlap(projectedPos, LocalBodySizeWithContactOffset / 2, -1, QueryTriggerInteraction.Collide);

        // depenetrate from overlapping objects

        // HACK: since we can't pass a custom size to Physics.ComputePenetration (see below),
        //       we need to assign it directly to the collide prior to calling it and then
        //       revert the change afterwards
        col.size = LocalBodySizeWithSkin;

        foreach (var candidate in contacts)
        {
            // TODO: consider proper contact offsets
            MoveCollideAndSlide(ref projectedPos, ref projectedRot, ref projectedVel, candidate);
        }

        // HACK: restoring size (see above HACK)
        col.size = startSize;
        
        FinishMove:
        
        // callback: pre-processing move before applying 
        motor.OnFinishMove(ref projectedPos, ref projectedRot, ref projectedVel);
        
        // apply move
        rbody.MovePosition(projectedPos);
        rbody.MoveRotation(projectedRot);
        InternalVelocity = projectedVel;

        Velocity = (projectedPos - startPosition) / Time.fixedDeltaTime;
        
        PostMove:
        
        // callback for after move is complete
        motor.OnPostMove();

        // enumerate collision messages if applicable
        if(SendCollisionMessages)
        {
            // send collisions
            for(int i = 0; i < CollisionCount; ++i)
            {
                SendMessage("OnKinematicCollision", LastCollisions[i], SendMessageOptions.DontRequireReceiver);

                MoveCollision edit = LastCollisions[i];
                edit.otherCollider = BodyCollider;

                LastCollisions[i].otherCollider.SendMessage("OnKinematicCollision", edit, SendMessageOptions.DontRequireReceiver);
            }

            // send triggers
            for (int i = 0; i < TriggerCount; ++i)
            {
                SendMessage("OnKinematicTrigger", LastCollisions[i], SendMessageOptions.DontRequireReceiver);

                MoveCollision edit = LastCollisions[i];
                edit.otherCollider = BodyCollider;

                LastTriggers[i].otherCollider.SendMessage("OnKinematicTrigger", edit, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnValidate()
    {
        skinWidth = Mathf.Clamp(skinWidth, 0.001f, float.PositiveInfinity);

        if (rbody != null)
        {
            rbody.isKinematic = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // early exit for missing collider
        if(col == null) { return; }

        // don't support scaling at this time - recreate the TRS matrix assuming unit scale
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // draw box with skin width
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col.center, LocalBodySizeWithSkin);
        
        // draw box with skin width
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(col.center, LocalBodySizeWithContactOffset);
    }

    private void Reset()
    {
        if (col == null) { col = GetComponent<BoxCollider>(); }
        if (rbody == null) { rbody = GetComponent<Rigidbody>(); }

        OnValidate();
    }
}

public interface IKinematicMotor
{
    /// <summary>
    /// Called by KinematicBody when it initially updates its velocity
    /// </summary>
    /// <param name="oldVelocity">Existing velocity</param>
    /// <returns>Returns the new velocity to apply to the body</returns>
    Vector3 UpdateVelocity(Vector3 oldVelocity);

    /// <summary>
    /// Called by KinematicBody when the body needs to resolve an overlap
    /// </summary>
    /// <param name="curPosition">Position of the body at time of impact</param>
    /// <param name="curRotation">Rotation of the body at time of impact</param>
    /// <param name="curVelocity">Velocity of the body at time of impact</param>
    /// <param name="other">The collider that was struck</param>
    /// <param name="direction">Depenetration direction</param>
    /// <param name="pen">Penetration depth</param>
    void OnMoveHit(ref Vector3 curPosition, ref Quaternion curRotation, ref Vector3 curVelocity, Collider other, Vector3 direction, float pen);

    // TODO: Make these callbacks instead of part of the interface
    
    /// <summary>
    /// Called before the body has begun moving
    /// </summary>
    void OnPreMove();

    /// <summary>
    /// Called before the move is applied to the body.
    ///
    /// This provides an opportunity to perform any post-processing on the move
    /// before it is applied to the body.
    /// </summary>
    /// <param name="curPosition">Position that the body would move to</param>
    /// <param name="curPosition">Position that the body would rotate to</param>
    /// <param name="curVelocity">Velocity that the body would move with on next update</param>
    void OnFinishMove(ref Vector3 curPosition, ref Quaternion curRotation, ref Vector3 curVelocity);
    
    /// <summary>
    /// Called after the body has moved to its final position for this frame
    /// </summary>
    void OnPostMove();
}
