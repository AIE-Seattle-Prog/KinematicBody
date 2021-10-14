using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerMaskExtensions
{
    public static bool Test(this LayerMask mask, int other)
    {
        return (mask | (1 << other)) != 0;
    }
}
