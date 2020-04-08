using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class that randomizes the rotation of all grandchildren of an object.
/// Technically the name is slightly incorrect, as it also modifies location slightly.
/// </summary>
public class RandomizeChildRotation : MonoBehaviour
{
    // The maximum distance to move each child
    public float maxDistancePerAxis = 1f;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// Thus, the randomization is done before anything is shown on screen
    /// </summary>
    void Start()
    {
        // For each child
        foreach (Transform debrisGroup in transform)
        {
            // For each grandchild
            foreach (Transform debrisItem in debrisGroup)
            {
                if (debrisItem.tag == "Debris")
                {
                    // Rotate this object to a random x, y, and z angle, but convert first
                    // to a quaternion, since that's how things are stored internally.
                    debrisItem.rotation = Quaternion.Euler(
                        Random.Range(0f, 360f),
                        Random.Range(0f, 360f),
                        Random.Range(0f, 360f)
                    );
                    // Then, move this item randomly along each axis up to `maxDistancePeraxis`
                    debrisItem.position = debrisItem.position
                                          + new Vector3(
                                            Random.Range(-maxDistancePerAxis, maxDistancePerAxis),
                                            Random.Range(-maxDistancePerAxis, maxDistancePerAxis),
                                            Random.Range(-maxDistancePerAxis, maxDistancePerAxis)
                                          );
                }
            }
        }
    }
}
