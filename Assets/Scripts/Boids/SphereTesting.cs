using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereTesting : MonoBehaviour
{
    public int numPoints = 300;
    public float radius = 0.01f;
    public int highlightedSphere = 0;

    /// <summary>
    /// Callback to draw gizmos that are pickable and always drawn.
    /// </summary>
    void OnDrawGizmos()
    {
        Vector3[] directions = new PointsOnSphere(numPoints).directions;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 point = directions[i];
            if (i == highlightedSphere)
            {
                Gizmos.DrawSphere(point, radius * 5);
            }
            else
            {
                Gizmos.DrawSphere(point, radius);
            }
        }
    }
}
