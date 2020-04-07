using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubberband : MonoBehaviour
{
    public float rubberbandStrength = 0.01f;
    public float epsilonToIgnore = 5f;
    private Vector3 startPosition;
    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector3.SqrMagnitude(transform.position - startPosition) > epsilonToIgnore * epsilonToIgnore)
        {
            transform.position = Vector3.Lerp(transform.position, startPosition, rubberbandStrength);
        }

    }
}
