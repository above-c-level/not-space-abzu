using UnityEngine;

namespace Imphenzia.SpaceForUnity
{
    public class AsteroidFadeOrigin : MonoBehaviour
    {
        // Used for shader int ID instead of string for performance since it's updated every frame
        private int shaderIDOrigin;

        void Start()
        {
            // Grab the integer of the shader property for better performance since we update every frame
            shaderIDOrigin = Shader.PropertyToID("_AsteroidOrigin");
        }

        void Update()
        {
            // Keep updating the shader(s) with the position of the main camera so it knows where to fade asteroids
            Shader.SetGlobalVector(shaderIDOrigin, transform.position);
        }
    }
}