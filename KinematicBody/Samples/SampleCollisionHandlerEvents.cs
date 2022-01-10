using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SampleCollisionHandlerEvents : MonoBehaviour
{
    public UnityEvent<KinematicBody.MoveCollision> OnKinematicCollisionFired;
    public UnityEvent<KinematicBody.MoveCollision> OnKinematicTriggerFired;
    
    private void OnKinematicCollision(KinematicBody.MoveCollision collision)
    {
        OnKinematicCollisionFired.Invoke(collision);
    }

    private void OnKinematicTrigger(KinematicBody.MoveCollision trigger)
    {
        OnKinematicTriggerFired.Invoke(trigger);
    }
}
