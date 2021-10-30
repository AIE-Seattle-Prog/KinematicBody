using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCollisionHandler : MonoBehaviour
{
    private void OnKinematicCollision(KinematicBody.MoveCollision collision)
    {
        Debug.Log("Collided with " + collision.otherCollider.name);
    }

    private void OnKinematicTrigger(KinematicBody.MoveCollision trigger)
    {
        Debug.Log("Triggered with " + trigger.otherCollider.name);
    }
}
