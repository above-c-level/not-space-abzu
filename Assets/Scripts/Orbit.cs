using UnityEngine;
using System.Collections;

/// <summary>
/// An incredibly simple circular orbit class that acts by rotating a transform
/// </summary>
public class Orbit : MonoBehaviour
{

    // Orbit speed of moon around its parent planet
    public float orbitSpeed = 0.0f;

    // Private Variables
    private Transform cacheTransform;

    void Start()
    {
        // Cache the transform of the orbiting object to increase performance
        cacheTransform = transform;
    }

    void Update()
    {
        // If the transform is cached
        if (cacheTransform != null)
        {
            // Then rotate the transform about the vertical axis (up)
            cacheTransform.Rotate(Vector3.up * orbitSpeed * Time.deltaTime);
        }
    }
}