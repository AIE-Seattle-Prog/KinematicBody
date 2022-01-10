using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug character script that can send mock inputs to the motor. Provided for reference purposes.
/// </summary>
public class DebugPlayerCharacter : MonoBehaviour
{
    // The motor we're controlling
    public KinematicPlayerMotor motor;

    public bool useInputOverride = false;
    public Vector3 inputOverride;

    private void Update()
    {
        // send inputs to motor
        if (!useInputOverride)
        {
            motor.MoveInput(new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical")));
        }
        else
        {
            motor.MoveInput(inputOverride);
        }

        if (Input.GetButtonDown("Jump"))
        {
            motor.JumpInput();
        }
    }
}
