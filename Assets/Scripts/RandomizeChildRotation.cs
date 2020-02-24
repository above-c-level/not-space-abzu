using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeChildRotation : MonoBehaviour
{
    public float maxDistancePerAxis = 1f;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform debrisGroup in transform)
        {
            foreach (Transform debrisItem in debrisGroup)
            {
                debrisItem.rotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
                debrisItem.position = debrisItem.position
                                      + new Vector3(
                                        Random.Range(-maxDistancePerAxis, maxDistancePerAxis),
                                        Random.Range(-maxDistancePerAxis, maxDistancePerAxis),
                                        Random.Range(-maxDistancePerAxis, maxDistancePerAxis)
                                      );
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
