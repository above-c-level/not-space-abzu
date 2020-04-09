using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatControl : MonoBehaviour
{
    public float gravityStrength = 1000f;
    private Rigidbody cacheRigidbody;
    // Start is called before the first frame update
    void Start()
    {
        cacheRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        cacheRigidbody.AddForce(Vector3.down * gravityStrength);
    }
}
