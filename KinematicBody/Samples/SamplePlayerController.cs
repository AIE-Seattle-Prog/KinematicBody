using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamplePlayerController : MonoBehaviour
{
    public KinematicBody playerBody;
    public Transform cameraArm;
    public Transform cameraTransform;

    public Vector3 cameraOffset;

    public float maxPitch = 180.0f;

    private float pitch;
    private float yaw;

    void Update()
    {
        yaw += Input.GetAxisRaw("Mouse X");
        Quaternion rot = Quaternion.AngleAxis(yaw, Vector3.up);

        Vector3 finalOffset = rot * cameraOffset;

        cameraTransform.position = playerBody.FootPosition + finalOffset;

        cameraTransform.LookAt(playerBody.FootPosition);
    }
}
