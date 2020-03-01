using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class Moon : MonoBehaviour
    {

        // Orbit speed of moon around its parent planet
        public float orbitSpeed = 0.0f;
        // Rotational speed of moon around its own acis
        public float rotationSpeed = 0.0f;

        // Private Variables
        private Transform cacheTransform;
        private Transform cacheMeshTransform;

        void Start()
        {
            // Cache transforms to increase performance
            cacheTransform = transform;
            cacheMeshTransform = transform.Find("MoonObject");
        }

        void Update()
        {
            // Orbit around the planet at orbitSpeed
            if (cacheTransform != null)
            {
                cacheTransform.Rotate(Vector3.up * orbitSpeed * Time.deltaTime);
            }

            // Rotate around own axis
            if (cacheMeshTransform != null)
            {
                cacheMeshTransform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            }
        }
    }
}