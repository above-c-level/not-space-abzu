using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarWind : MonoBehaviour
{
    [HideInInspector]
    public bool isActive = false;
    [HideInInspector]
    public Rigidbody astronautBody;
    [HideInInspector]
    public float windForce;

    /// <summary>
    /// Runs every physics update.
    /// </summary>
    void FixedUpdate()
    {
        // If the thruster is active...
        if (isActive)
        {
            // Add force without rotational torque
            astronautBody.AddForce(transform.forward * windForce);
        }
    }
}
