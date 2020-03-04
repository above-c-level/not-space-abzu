using System.ComponentModel.DataAnnotations;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectOrbit : MonoBehaviour
{
    [Tooltip("How quickly the object should orbit")]
    public float speedMultiplier = 2;
    [Tooltip("The maximum distance from the focus")]
    [MinAttribute(0)] // Limits the value to be greater than or equal to 0
    public float apoapsis = 4; // Apoapsis
    [Tooltip("The minimum distance from the focus")]
    [MinAttribute(0)]
    public float periapsis = 1; // Periapsis
    [Tooltip("How heavily inclined (tilted) the orbit should be.")]
    [RangeAttribute(-180, 180)]

    public float inclinationNodeDegrees = 0;
    [Tooltip("The rotation of the inclination plane about the z axis")]
    [RangeAttribute(-90, 90)]
    public float inclinationAngleDegrees = 0;
    [Tooltip("How heavy the orbiting object is")]
    [MinAttribute(0)]
    public float mass = 1;
    [Tooltip("Where in the orbit the object should start")]
    public float startingPosition = 0;
    [Tooltip("The axis the object should rotate around")]
    public Vector3 rotationAxis = new Vector3(1, 2, 3);
    [Tooltip("How quickly the object should rotate")]
    [MinAttribute(0)]
    public float rotationSpeed = 1.75f;
    [Tooltip("A transform for where the focus of the orbit should lie")]
    public Transform focusPoint;
    [Tooltip("Whether the orbit should be precalculated")]
    public bool usePrecalculatedOrbit = true;
    // The current position in the orbit. Changes over the lifetime of the orbit.
    private float currentPosition;
    // The "length" and "width" of the ellipse, divided by two
    private float semiMajorAxis;
    private float semiMinorAxis;
    // How squished or circular the orbit is.
    private float eccentricity;
    // Position variables
    private float posX = 0;
    private float posZ = 0;
    private float posY = 0;
    // The "radius" from the focus
    private float r;
    // The angle of the orbit being calculated, will come up again later
    private float angle = 0; // Values between 0 and 2PI
    // Gravitational constant
    private float G = 6.67408f;
    // An array of orbit points. Struct is below.
    private orbitPoint[] orbit;
    // A struct for holding the position and velocity for a given
    // point in an orbit
    struct orbitPoint
    {
        public Vector3 position;
        public float velocity;
    }
    void Start()
    {
        // First, initialize the orbit array
        orbit = new orbitPoint[721];
        // Then, initialize `currentPosition`
        currentPosition = startingPosition;
        // Finally, generate the orbit
        if (usePrecalculatedOrbit)
        {
            generateOrbit();
        }
    }

    void FixedUpdate()
    {
        // Every fixed period of time, move the object around the orbit
        moveSatellite();
        // And also rotate it
        rotateSatellite();
    }


    /// <summary>
    /// Generates an orbit in half-degrees, and stores it in `orbit`
    /// </summary>
    void generateOrbit()
    {
        // For each half-degree
        for (int index = 0; index < 720; index++)
        {
            // Calculate the current angle (in radians)
            angle = index * (Mathf.PI * 2) / 720;
            // Then get the orbit information at that point, and store it
            orbit[index] = calculateOrbitAtPoint(angle);
        }
        // Lastly, reset angle back to 0
        angle = 0;
    }

    /// <summary>
    /// Calculates the velocity and position an object should have at a given angle
    /// </summary>
    /// <param name="angle">The angle to calculate the orbit parameters for</param>
    /// <returns>The position and velocity in an orbitPoint struct</returns>
    orbitPoint calculateOrbitAtPoint(float angle)
    {
        // This is here only to make the speeds (almost) match between
        // precalculated and calculated on-the-fly orbits
        float internalSpeedMultiplier = speedMultiplier;
        if (!usePrecalculatedOrbit)
        {
            // I don't know exactly where this comes from, but the precalculated
            // orbit runs much slower (in rotational terms) than one calculated
            // on the fly. This multiplier approximates the difference.
            internalSpeedMultiplier *= 0.008723f;
        }
        // This entire section is just lots of math. It's a combination of
        // trigonometry and the Keplerian equations. I'm not entirely convinced
        // that it's actually worth explaining here (since it's relatively
        // complicated) so I'm not going to.
        semiMajorAxis = (apoapsis + periapsis) / 2;
        semiMinorAxis = Mathf.Sqrt(apoapsis * periapsis);
        eccentricity = Mathf.Sqrt(1 - (semiMinorAxis * semiMinorAxis)
                                    / (semiMajorAxis * semiMajorAxis));

        float movOffset = ((apoapsis + periapsis) / 2) - periapsis;
        posX = semiMajorAxis * Mathf.Cos(angle) - movOffset;
        posZ = semiMinorAxis * Mathf.Sin(angle);

        r = Mathf.Sqrt(posX * posX + posZ * posZ);

        float nPosX = posX * Mathf.Cos(inclinationAngleDegrees * Mathf.Deg2Rad)
                      - posY * Mathf.Sin(inclinationAngleDegrees * Mathf.Deg2Rad);
        float nPosY = posY * Mathf.Cos(inclinationAngleDegrees * Mathf.Deg2Rad)
                      + posX * Mathf.Sin(inclinationAngleDegrees * Mathf.Deg2Rad);

        float nPosX2 = nPosX * Mathf.Cos(inclinationNodeDegrees * Mathf.Deg2Rad)
                      - posZ * Mathf.Sin(inclinationNodeDegrees * Mathf.Deg2Rad);
        float nPosZ = posZ * Mathf.Cos(inclinationNodeDegrees * Mathf.Deg2Rad)
                      + nPosX * Mathf.Sin(inclinationNodeDegrees * Mathf.Deg2Rad);

        orbitPoint tempOrbitPoint;
        // Once (almost) all the math is done, store it in an orbitPoint
        tempOrbitPoint.position = new Vector3(nPosX2, nPosY, nPosZ);
        tempOrbitPoint.velocity = internalSpeedMultiplier * Mathf.Sqrt((G * mass)
                                  * ((2 / r) - (1 / semiMajorAxis)));
        // Then return it
        return tempOrbitPoint;
    }

    /// <summary>
    /// A helper method to move the satellite around the orbit. Takes into
    /// account the `usePrecalculatedOrbit` variable.
    /// </summary>
    void moveSatellite()
    {
        // If we want to use the one that's precalculated
        if (usePrecalculatedOrbit)
        {
            // Then try to lerp between the precalculated points
            try
            {
                // Floor of point for a minimum to lerp between
                int index1 = Mathf.FloorToInt(this.currentPosition);
                // One above that for the max to lerp between
                int index2 = index1 + 1;

                // How far we are in between the two lerp points
                float lerpPoint = this.currentPosition - index1;
                // Lastly, linearly interpolate between the points
                Vector3 currentPosition = (1 - lerpPoint) * orbit[index1].position + lerpPoint * (orbit[index2].position);
                // Use the calculated value to offset the satellite
                transform.position = currentPosition + focusPoint.position;

                // Then do the same thing for the velocity
                float currentVel = (1 - lerpPoint) * orbit[index1].velocity + lerpPoint * (orbit[index2].velocity);
                this.currentPosition += currentVel;

                // If we've come full-circle, reset back to the beginning
                if (this.currentPosition > 720)
                {
                    this.currentPosition = 0;
                }
            }
            // Sometimes, very rarely, the above logic breaks. I honestly don't
            // know why it happens, but this tries to get past the break.
            catch
            {
                currentPosition += 1;
                print("broken");
            }
        }
        // In this case, we're calculating orbits on the fly.
        else
        {
            // The logic is almost exactly the same,
            // but without the linear interpolation
            orbitPoint point = calculateOrbitAtPoint(this.currentPosition);
            transform.position = point.position + focusPoint.position;
            this.currentPosition += point.velocity;
            // Lastly, once we've gone above 720,
            // divide by 720 and keep the remainder.
            // Example: We hit 721.45 due to velocity, and 721.45 % 720 is 1.45,
            // so that would be the new current position.
            if (this.currentPosition > 720)
            {
                this.currentPosition %= 720;
            }
        }

    }

    /// <summary>
    /// A helper method to rotate the satellite
    /// </summary>
    void rotateSatellite()
    {
        transform.rotation *= Quaternion.Euler(rotationAxis.normalized * rotationSpeed);
    }


}
