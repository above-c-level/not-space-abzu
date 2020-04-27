using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidCrash : MonoBehaviour
{
    public float rotationStrength = 100;
    public float maxMovementForce = 25;
    public GameObject otherAsteroid;
    private Rigidbody cacheRigidbody;
    // Start is called before the first frame update
    void Start()
    {
        cacheRigidbody = GetComponent<Rigidbody>();
        Vector3 heading = otherAsteroid.transform.position - transform.position;

        cacheRigidbody.AddTorque(Random.onUnitSphere * rotationStrength * 10000000);
        cacheRigidbody.AddForce(heading * maxMovementForce * 10000);
    }
}