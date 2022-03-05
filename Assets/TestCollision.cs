using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCollision : MonoBehaviour
{
    public Collider testCollider;

    public float contactOffset = 0.02f;

    private void Start()
    {
        testCollider.contactOffset = contactOffset;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Enter: " + collision.gameObject.name, this);
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Exit: " + collision.gameObject.name, this);
    }
}
