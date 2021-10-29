using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCollisionHandler : MonoBehaviour, IKinematicMotorCollisionHandler
{
    public void OnKinematicCollision(KinematicBody.MoveCollision collision)
    {
        Debug.Log("Collided with " + collision.otherCollider.name);
    }

    public void OnKinematicTrigger(KinematicBody.MoveCollision trigger)
    {
        Debug.Log("Triggered with " + trigger.otherCollider.name);
    }
}
