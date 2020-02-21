/*  SU_Asteroid C# Script (version: 1.6)
    SPACE for UNITY - Space Scene Construction Kit
    https://www.imphenzia.com/space-for-unity
    (c) 2019 Imphenzia AB

    DESCRIPTION:
    This script handles an asteroid in terms of rotation and movement.

    INSTRUCTIONS:
    Drag this script onto an asteroid (or use the existing Asteroid prefab) and configure properties.

    PROPERTIES:
        rotationSpeed	(rotational speed of the asteroid)
        driftSpeed		(drift/movement speed of the asteroid)
        visibilityRange (visibility range of asteroids - invisible beyond the range)
        distanceFade    (distance percentile at which the asteroid will start to fade/scale, 0.5 = half way between origin and visibilityRange)
        fadeAsteroids   (if the asteroids should fade/scale or not)
        fadeAsteroidsFalloffExponent (customizable exponent for fading 1.0 = linear (default) - use 0.25, 0.5, 2, 4, 8 for curved fading/scaling

    Version History
    1.6     - New Imphenzia.SpaceForUnity namespace to replace SU_ prefix.
            - Moved asset into Plugins/Imphenzia/SpaceForUnity for asset best practices.
    1.5     - Changed the way asteroids fade. Instead of using expensive alpha (transparency) fading scaling is used at the perimeter instead.
              The scaling is performed in a vertex shader so the GPU does the work for performance. This also looks better as the previous
              method of fading in asteroids when there was a light background, like a star or galaxy, looked odd.
              The asteroid shader requires a _AsteroidOrigin parameter to be set so the vertex shader knows where the view center is so it can fade
              at the perimeter. The shader origin is set globally by SU_AsteroidFadeOrigin.cs and the script is added to the main camera at runtime (non-persistent)
              by default. If you want a different object to be the center, e.g. a spaceship or another camera, manually add the SU_AsteroidFadeOrigin component/script
              to a desired object.
            - Performance of asteroids greatly increased by using GPU Vertex shader for fading/scaling and removing alpha transparency.
            - Asteroids have a material with a Render Queue of 1900, therefore they are included in the effect of SU_TravelWarp (which warps everything with a RenderQueue < 1990)

    1.02    - Prefixed with SU_Asteroid to avoid naming conflicts.
            - Added documentation.
    1.01    - Initial Release.
*/

using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class Asteroid : MonoBehaviour
    {
        // Originally to present choice of high, medium, or low quality mesh
        public enum PolyCount { LOW, PARENT };
        // Variable to set the poly count (quality) of the asteroid, default is Low quality
        public PolyCount polyCount = PolyCount.LOW;
        // Variable to set the poly count for the collider (MUCH faster to use the low poly version)
        public PolyCount polyCountCollider = PolyCount.LOW;

        // Reference to different quality meshes
        private MeshFilter meshLowPoly;

        // Rotation speed
        public float rotationSpeed = 0.0f;
        // Vector3 axis to rotate around
        public Vector3 rotationalAxis = Vector3.up;
        // Drift/movement speed
        public float driftSpeed = 0.0f;
        // Vector3 direction for drift/movement
        public Vector3 driftAxis = Vector3.up;
        // Visibility range is used to fade/scale in/out asteroids at a distance
        public float visibilityRange = 20000f;
        // Distance percentile of spawn distance to start fading/scaling asteroids
        // Visibility = 1.0 at distanceFade*distanceSpawn*visibilityRange, and 0.0 at distanceSpawn*visibilityRange
        // (e.g. if visibilityRange is 20000 and distanceFade = 0.7 asteroids will fade/scale 14000 (fullly visible) -> 20000 (invisible)
        public float distanceFade = 0.7f;
        // Use shader to fade asteroids in/out
        public bool fadeAsteroids = true;
        // Exponent for fading asteroid 1.0 = linear (use 0.125, 0.5, 1 (linear), 2, 4, 8... for different fade curves)
        public float fadeAsteroidsFalloffExponent = 1f;
        // Private variables
        private Transform cachedTransform;

        // Material of asteroid, needed to send parameters to shader for distance fade/scale effect
        private Material material;

        void Start()
        {
            cachedTransform = transform;
            if (polyCount == PolyCount.PARENT)
            {
                return;
            }
            meshLowPoly = GetComponent<MeshFilter>();
            // Cache transforms to increase performance

            // Set the mesh based on poly count (quality)
            SetPolyCount(polyCount);

            // Material of asteroid, needed to send parameters to shader for distance fade/scale effect
            material = GetComponent<Renderer>().material;

            // Set asteroid material shader fade/scale settings
            if (fadeAsteroids)
            {
                // Fading (or scaling) of asteroids is enabled, set the shader of the asteroid to "SU_AsteroidFade"
                material.shader = Shader.Find("SpaceUnity/SU_AsteroidFade");
                // Set the shader parameters falloff, inner, and outer radius. Asteroids will fade/scale in the region between inner and outer radius.
                material.SetFloat("_FadeFalloffExp", fadeAsteroidsFalloffExponent);
                material.SetFloat("_InnerRadius", visibilityRange * distanceFade);
                material.SetFloat("_OuterRadius", visibilityRange);

                // If there is no fade origin in the scene, add one to the main camera object during run time....
                if (FindObjectOfType<AsteroidFadeOrigin>() == null)
                    // Assign the component to a game object manually if you desire another origin for the asteroids to fade/in out relative to.
                    Camera.main.gameObject.AddComponent<AsteroidFadeOrigin>();
            }
            else
            {
                // If fading is not used, use the SU_Asteroid shader instead - it does not contain the vertex transormation requierd for scaling.
                material.shader = Shader.Find("SpaceUnity/SU_Asteroid");
            }

        }

        void Update()
        {
            if (cachedTransform != null)
            {
                // Rotate around own axis
                cachedTransform.Rotate(rotationalAxis * rotationSpeed * Time.deltaTime);
                // Move in world space according to drift speed
                cachedTransform.Translate(driftAxis * driftSpeed * Time.deltaTime, Space.World);
            }
        }

        // Set the mesh based on the poly count (quality)
        public void SetPolyCount(PolyCount newPolyCount) { SetPolyCount(newPolyCount, false); }
        public void SetPolyCount(PolyCount newPolyCount, bool collider)
        {
            // If this is not the collider...
            if (!collider)
            {
                // This is the actual asteroid mesh.. so specify which poly count we want
                polyCount = newPolyCount;
                // switch (newPolyCount)
                // {
                //     case PolyCount.LOW:
                //         // access the MeshFilter component and change the sharedMesh to the low poly version
                //         transform.GetComponent<MeshFilter>().sharedMesh = meshLowPoly.sharedMesh;
                //         break;
                // }
            }
            else
            {
                // This is the collider mesh we set this time
                polyCountCollider = newPolyCount;
                // switch (newPolyCount)
                // {
                //     case PolyCount.LOW:
                //         // access the MeshFilter component and change the sharedMesh to the low poly version
                //         transform.GetComponent<MeshCollider>().sharedMesh = meshLowPoly.sharedMesh;
                //         break;
                // }
            }
        }
        public void SetRandomVelocity(float minSpeed,
                                      float maxSpeed,
                                      float scaleMultiplier = 0.0001f)
        {
            // If this is a parent node, then it contains children that must be
            // iterated over
            // if (polyCount == PolyCount.LOW)
            // {

            //     // Iterate here, but how?
            //     // for (int i = 0; i < transform.childCount; i++)
            //     // {
            //     //     Transform child = transform.GetChild(i);
            //     //     Asteroid subcomponent = child.GetComponent<Asteroid>();
            //     //     subcomponent.RandomVelocity(minSpeed * scaleMultiplier,
            //     //                                 maxSpeed * scaleMultiplier);
            //     // }
            // }
            // // Otherwise, this is a standalone child
            // else
            // {
            //     RandomVelocity(minSpeed, maxSpeed);
            // }
        }

        public void SetRandomRotation(float minSpeed,
                                      float maxSpeed,
                                      float scaleMultiplier = 0.0001f)
        {
            // // RandomRotation(minSpeed, maxSpeed);
            // // If this is a parent node, then it contains children that must be
            // // iterated over
            // if (polyCount == PolyCount.LOW)
            // {
            //     RandomRotation(minSpeed, maxSpeed);
            // }

        }

        private void RandomVelocity(float minSpeed, float maxSpeed)
        {
            driftSpeed = Random.Range(minSpeed, maxSpeed);
            driftAxis = new Vector3(Random.Range(0.0f, 1.0f),
                                    Random.Range(0.0f, 1.0f),
                                    Random.Range(0.0f, 1.0f));
            driftAxis.Normalize();
        }

        private void RandomRotation(float minSpeed, float maxSpeed)
        {
            rotationSpeed = Random.Range(minSpeed, maxSpeed);
            rotationalAxis = new Vector3(Random.Range(0.0f, 1.0f),
                                         Random.Range(0.0f, 1.0f),
                                         Random.Range(0.0f, 1.0f));
            rotationalAxis.Normalize();
        }

    }

}
