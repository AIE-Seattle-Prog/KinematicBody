using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sample character script demonstrating how to send inputs to a motor. Provided for reference purposes.
/// </summary>
public class SamplePlayerCharacter : MonoBehaviour
{
    // The motor we're controlling
    public KinematicPlayerMotor motor;

    private void Update()
    {
        // send move inputs to motor
        motor.MoveInput(new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical")));

        // send jump inputs to motor
        if (Input.GetButtonDown("Jump"))
        {
            motor.JumpInput();
        }
    }
}
