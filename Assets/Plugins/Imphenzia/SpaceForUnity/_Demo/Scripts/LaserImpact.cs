
using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class LaserImpact : MonoBehaviour
    {
        // Cache light to improve performance
        private Light _cacheLight;

        void Awake()
        {
            // If the child light exists...
            if (gameObject.GetComponentInChildren<Light>() != null)
            {
                // Cache the light component to improve performance
                _cacheLight = gameObject.GetComponentInChildren<Light>();
                // Find the child light and set intensity to 1.0
                _cacheLight.intensity = 1.0f;
                // Move the transform 5 units out so it's not spawn at impact point of the collider/mesh it just hit
                // which will light up the object better.
                _cacheLight.transform.Translate(Vector3.up * 5, Space.Self);
            }
            else
            {
                Debug.LogWarning("Missing required child light. Impact light effect won't be visible");
            }

            // Destroy after a second
            Destroy(gameObject, 1.0f);

        }

        void Update()
        {
            // If the light exists...
            if (_cacheLight != null)
            {
                // Set the intensity depending on the number of particles visible
                _cacheLight.intensity = (float)(transform.GetComponent<ParticleSystem>().particleCount / 50.0f);
            }
        }
    }
}