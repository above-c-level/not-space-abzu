using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class Asteroid : MonoBehaviour
    {
        // Originally to present choice of high, medium, or low quality mesh.
        // However, now it's for determining whether a piece of debris has
        // children, so that those can spawn properly.
        public enum ChildrenContainer { STANDALONE, PARENT };
        // Variable to set whether the debris is standalone or a parent
        public ChildrenContainer polyCount = ChildrenContainer.STANDALONE;
        // Originally a variable to set the poly count for the collider.
        // I'm not sure it has any use anymore, but it doesn't exactly hurt to
        // have for now, so it hasn't been removed.
        public ChildrenContainer polyCountCollider = ChildrenContainer.STANDALONE;

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

            // Cache transforms to increase performance
            cachedTransform = transform;
            // We don't need to do any mesh calculation if this is a parent
            if (polyCount == ChildrenContainer.PARENT)
            {
                return;
            }
            meshLowPoly = GetComponent<MeshFilter>();

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
                // If fading is not used, use the SU_Asteroid shader instead - it does not contain the vertex transformation required for scaling.
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
        public void SetPolyCount(ChildrenContainer newPolyCount) { SetPolyCount(newPolyCount, false); }
        public void SetPolyCount(ChildrenContainer newPolyCount, bool collider)
        {
            // If this is not the collider...
            if (!collider)
            {
                // This is the actual asteroid mesh.. so specify which poly count we want
                polyCount = newPolyCount;
            }
            else
            {
                // This is the collider mesh we set this time
                polyCountCollider = newPolyCount;
            }
        }

        /**********************************************************************
        NOTE: originally the methods below were used in setting velocity and
        rotation for objects and also the children. However, in testing it
        didn't seem to work, and I never had the time to figure out why.
        Since the game itself seemed to work alright without these actually
        doing anything, I just left them as-is.
        ***********************************************************************/
        public void SetRandomVelocity(float minSpeed,
                                      float maxSpeed,
                                      float scaleMultiplier = 0.0001f)
        {
            // If this is a parent node, then it contains children that must be
            // iterated over
            // if (polyCount == PolyCount.LOW)
            // {
            // }
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
            // If this is a parent node, then it contains children that must be
            // iterated over
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
