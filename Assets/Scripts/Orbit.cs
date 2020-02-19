using UnityEngine;
using System.Collections;

public class Orbit : MonoBehaviour
{

    // Orbit speed of moon around its parent planet
    public float orbitSpeed = 0.0f;

    // Private Variables
    private Transform cacheTransform;

    void Start()
    {
        // Cache transforms to increase performance
        cacheTransform = transform;
    }

    void Update()
    {
        // Orbit around the planet at orbitSpeed
        if (cacheTransform != null)
        {
            cacheTransform.Rotate(Vector3.up * orbitSpeed * Time.deltaTime);
        }
    }
}