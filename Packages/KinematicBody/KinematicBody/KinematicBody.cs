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
    /// Motor driving this body
    /// </summary>
    public IKinematicMotor Motor
    {
        get => motor;
        set
        {
            if(value as UnityEngine.Component)
            {
                var uObject = value as UnityEngine.Component;
                if(uObject.TryGetComponent<IKinematicMotorCollisionHandler>(out var handler))
                {
                    selfHandler = handler;
                }
                else
                {
                    selfHandler = null;
                }
            }

            motor = value;
        }
    }
    [SerializeField]
    private IKinematicMotor motor;
    private IKinematicMotorCollisionHandler selfHandler;
    
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
    /// Minimum desired distance between nearby surfaces and the surface of this body
    /// </summary>
    public float contactOffset = 0.005f;
    /// <summary>
    /// Minimum amount of allowed penetration
    /// </summary>
    public float skinWidth = 0.01f;
    /// <summary>
    /// Size of the box body in local space inclusive of the contact offset
    /// </summary>
    public Vector3 LocalBodySizeWithSkin => col.size + Vector3.one * contactOffset;
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

    [Header("Body Settings")]
    public Vector3 GravityScale = new Vector3(0, 2, 0);
    // velocity of the final object inclusive of external forces, given in world-space
    public Vector3 EffectiveGravity
    {
        get
        {
            Vector3 g = Physics.gravity;
            g.Scale(GravityScale);
            return g;
        }
    }
    
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

        public Vector3 collisionDirection;
        public float collisionPenetration;
    }

    public readonly static int MAX_COLLISIONS = 32;
    private MoveCollision[] LastCollisions = new MoveCollision[MAX_COLLISIONS];
    private MoveCollision[] LastTriggers = new MoveCollision[MAX_COLLISIONS];
    public int CollisionCount { get; private set; }
    public int TriggerCount { get; private set; }

    public bool SendCollisionEvents = false;

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
        DeferredCollideAndSlide(ref bodyPosition, ref bodyRotation, ref bodyVelocity, other);
        
        // apply movement immediately
        rbody.MovePosition(bodyPosition);
        rbody.MoveRotation(bodyRotation);
        InternalVelocity = bodyVelocity;
    }

    public void DeferredCollideAndSlide(ref Vector3 bodyPosition, ref Quaternion bodyRotation, ref Vector3 bodyVelocity, Collider other)
    {
        // ignore self collision
        if(other == col) { return; }
            
        bool isOverlap = Physics.ComputePenetration(col,
            bodyPosition,
            bodyRotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out var mtv,
            out var pen);

        if (isOverlap && pen > skinWidth)
        {
            MoveCollision collision = new MoveCollision
            {
                bodyPosition = bodyPosition,
                bodyRotation = bodyRotation,
                bodyVelocity = bodyVelocity,
                otherCollider = other,
                collisionDirection = mtv,
                collisionPenetration = pen
            };

            // collisions
            if (!other.isTrigger)
            {
                // defer to motor to resolve hit
                Motor.OnMoveHit(ref bodyPosition, ref bodyRotation, ref bodyVelocity, other, mtv, pen);
                if (CollisionCount < MAX_COLLISIONS) { LastCollisions[CollisionCount++] = collision; }
            }
            // triggers
            else
            {
                if (TriggerCount < MAX_COLLISIONS) { LastTriggers[TriggerCount++] = collision; }
            }
        }
    }
    
    public Collider[] Overlap(Vector3 bodyPosition, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        return Overlap(bodyPosition, LocalBodySize / 2, layerMask, queryMode);
    }
    
    public Collider[] Overlap(Vector3 bodyPosition, Vector3 bodyHalfExtents, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        bodyPosition = GetCenterAtBodyPosition(bodyPosition);
        // TODO: this needs to exclude itself from the list of overlaps
        return Physics.OverlapBox(bodyPosition, bodyHalfExtents, rbody.rotation, layerMask, queryMode);
    }
    
    public RaycastHit[] Cast(Vector3 bodyPosition, Vector3 direction, float distance, int layerMask = ~0, QueryTriggerInteraction queryMode = QueryTriggerInteraction.UseGlobal)
    {
        bodyPosition = GetCenterAtBodyPosition(bodyPosition);
        var allHits = Physics.BoxCastAll(bodyPosition, LocalBodySizeWithSkin/2, direction, rbody.rotation, distance, layerMask, queryMode);

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
    
    //
    // Unity Messages
    //

    private void Start()
    {
        OnValidate();
    }

    private void FixedUpdate()
    {
        Vector3 startPosition = rbody.position;
        
        Motor.OnPreMove();

        CollisionCount = 0;
        TriggerCount = 0;

        InternalVelocity = Motor.UpdateVelocity(InternalVelocity);

        //
        // integrate external forces
        //
        
        // apply gravity (if enabled)
        if (rbody.useGravity)
        {
            InternalVelocity += EffectiveGravity * Time.deltaTime;
        }

        Vector3 projectedPos = rbody.position + (InternalVelocity * Time.deltaTime);
        Vector3 projectedVel = InternalVelocity;
        Quaternion projectedRot = rbody.rotation;

        //
        // depenetrate from overlapping objects
        //

        // scale check
        Debug.Assert(Mathf.Approximately(transform.lossyScale.sqrMagnitude, 3) == true, "Scaling is not supported on KinematicBody game objects.");

        Vector3 sizeOriginal = col.size;
        Vector3 sizeWithSkin = col.size + Vector3.one * contactOffset;

        var candidates = Overlap(projectedPos, sizeWithSkin / 2, -1, QueryTriggerInteraction.Collide);

        // HACK: since we can't pass a custom size to Physics.ComputePenetration (see below),
        //       we need to assign it directly to the collide prior to calling it and then
        //       revert the change afterwards
        col.size = sizeWithSkin;

        foreach (var candidate in candidates)
        {
            DeferredCollideAndSlide(ref projectedPos, ref projectedRot, ref projectedVel, candidate);
        }

        // HACK: restoring size (see above HACK)
        col.size = sizeOriginal;
        
        // callback: pre-processing move before applying 
        Motor.OnFinishMove(ref projectedPos, ref projectedRot, ref projectedVel);
        
        // apply move
        rbody.MovePosition(projectedPos);
        rbody.MoveRotation(projectedRot);
        InternalVelocity = projectedVel;

        Velocity = (projectedPos - startPosition) / Time.fixedDeltaTime;
        
        // callback for after move is complete
        Motor.OnPostMove();

        if(SendCollisionEvents)
        {
            // send collisions
            for(int i = 0; i < CollisionCount; ++i)
            {
                // are we scheduled to handle collisions?
                if (selfHandler != null) { selfHandler.OnKinematicCollision(LastCollisions[i]); }

                MoveCollision edit = LastCollisions[i];
                edit.otherCollider = BodyCollider;

                IKinematicMotorCollisionHandler handler = null;
                Rigidbody otherRb = LastCollisions[i].OtherRigidbody;
                if (otherRb != null)
                {
                    otherRb.TryGetComponent<IKinematicMotorCollisionHandler>(out handler);
                }
                else
                {
                    LastCollisions[i].otherCollider.TryGetComponent<IKinematicMotorCollisionHandler>(out handler);
                }

                // send the event if there's a handler
                if(handler != null)
                {
                    handler.OnKinematicCollision(edit);
                }
            }

            // send triggers
            for (int i = 0; i < TriggerCount; ++i)
            {
                // are we scheduled to handle collisions?
                if (selfHandler != null) { selfHandler.OnKinematicTrigger(LastTriggers[i]); }

                MoveCollision edit = LastTriggers[i];
                edit.otherCollider = BodyCollider;

                IKinematicMotorCollisionHandler handler = null;
                Rigidbody otherRb = LastCollisions[i].OtherRigidbody;
                if (otherRb != null)
                {
                    otherRb.TryGetComponent<IKinematicMotorCollisionHandler>(out handler);
                }
                else
                {
                    LastTriggers[i].otherCollider.TryGetComponent<IKinematicMotorCollisionHandler>(out handler);
                }

                // send the event if there's a handler
                if (handler != null)
                {
                    handler.OnKinematicTrigger(edit);
                }
            }
        }
    }

    private void OnValidate()
    {
        contactOffset = Mathf.Clamp(contactOffset, 0.001f, float.PositiveInfinity);
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

        // draw box with contact offset
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(col.center, col.size + Vector3.one * contactOffset);

        // draw box with skin width
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(col.center, col.size - Vector3.one * skinWidth);
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
    /// Called by KinematicBody when the body hits another collider during its move
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

public interface IKinematicMotorCollisionHandler
{
    void OnKinematicCollision(KinematicBody.MoveCollision collision);
    void OnKinematicTrigger(KinematicBody.MoveCollision trigger);
}