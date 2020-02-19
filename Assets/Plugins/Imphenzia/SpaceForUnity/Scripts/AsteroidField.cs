/*  Asteroid Field C# Script (version: 1.6)
    SPACE for UNITY - Space Scene Construction Kit
    https://www.imphenzia.com/space-for-unity
    (c) 2019 Imphenzia AB

    DESCRIPTION:
    This script creates a localized asteroid field around itself. As the object moves
    the asteroids will optionally re-spawn out of range asteroids within range (but out of sight.)

    INSTRUCTIONS:
    Use the AsteroidField prefab and make it a child of an object you wish to spawn asteroids around (e.g. a space ship)
    Alternatively, drag this script onto the game object that should be the center of the asteroid field.

    PROPERTIES:
    range           (radius of asteroid field)
    rotationSpeed   (rotational speed of the asteroid)
    velocity      (drift/movement speed of the asteroid)

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
    1.05    - Removed compiler conditional code, only 5.x supported.
    1.03    - Added compiler conditional code for major versions 4.1, 4.2, 4.3
            - Changed transparent asteroid material to new shader SpaceUnity/AsteroidTransparent located
            in a Resources subfolder to ensure it is included during compile (before, transparent asteroids
            wouldn't render in 4.x since the shader was not included in the build)
    1.02    - Prefixed with SU_AsteroidField to avoid naming conflicts.
            Added documentation.
    1.01    - Initial Release.
*/

using UnityEngine;
using System.Collections.Generic;

namespace Imphenzia.SpaceForUnity
{
    public class AsteroidField : MonoBehaviour
    {
        // Poly Count (quality) of the asteroids in the field
        public Asteroid.PolyCount polyCount = Asteroid.PolyCount.LOW;
        // Poly Count (quality) of the asteroid colliders (LOW = fast, HIGH = slow)
        public Asteroid.PolyCount polyCountCollider = Asteroid.PolyCount.LOW;

        // Array of prefabs that the asteroid fields should consist of
        public GameObject[] prefabAsteroids;

        // Array of weights for how frequently the asteroids should be created
        public byte[] asteroidWeights;

        // Range of asteroid field sphere (when asteroids are beyond this range from the game object
        // they will respawn (relocate) to within range at distanceSpawn of range.
        public float range = 20000.0f;
        // Maximum number of asteroids in the sphere (configure to your needs for look and performance)
        public int maxAsteroids = 2000;
        // Respawn destroyed asteroids true/false
        public bool respawnDestroyedAsteroids = true;
        // Respawn if out of range (must be true for infinite/endless asteroid fields
        public bool respawnIfOutOfRange = true;
        // Distance percentile of range to relocate/spawn asteroids to
        public float distanceSpawn = 0.95f;
        // Minimum scale of asteroid
        public float minAsteroidScale = 0.1f;
        // Maximum scale of asteroid
        public float maxAsteroidScale = 1.0f;
        // Multiplier of scale
        public float scaleMultiplier = 1.0f;
        // Transform for origin of asteroid field, if null it will use itself
        public Transform asteroidFieldOriginTransform; // null = self
                                                       // Distance percentile of spawn distance to start fading asteroids
                                                       // Visibility = 1.0 at distanceFade*distanceSpawn*range, and 0.0 at distanceSpawn*range
                                                       // (e.g. if range is 20000 and distanceFade = 0.7 asteroids will fade/scale 14000 (visible) -> 20000 (invisible)
        public float distanceFade = 0.7f;
        // Use shader to fade asteroids in/out
        public bool fadeAsteroids = true;
        // Exponent for fading asteroid 1.0 = linear (use 0.125, 0.5, 1 (linear), 2, 4, 8... for different fade curves)
        public float fadeAsteroidsFalloffExponent = 1f;
        // Use vertex shader to scale asteroids in/out
        public bool scaleAsteroids = true;
        // Exponent for scaling asteroid 1.0 = linear (use 0.125, 0.5, 1 (linear), 2, 4, 8... for different scale curves)
        public float scaleAsteroidsFalloffExponent = 1f;

        // Is rigid body or not
        public bool isRigidbody = false;

        // NON-RIGIDBODY ASTEROIDS ---
        // Minimum rotational speed of asteroid
        public float minAsteroidRotationLimit = 0.0f;
        // Maximum rotational speed of asteroid
        public float maxAsteroidRotationLimit = 1.0f;
        // Rotation speed multiplier
        public float rotationSpeedMultiplier = 1.0f;
        // Minimum drift/movement speed of asteroid

        public float minAsteroidVelocityLimit = 0.0f;
        // Maximum velocity of asteroid (drift/movement speed)
        public float maxAsteroidVelocityLimit = 1.0f;
        // Velocity (drift/movement speed) multiplier
        public float velocityMultiplier = 1.0f;
        // ---------------------------

        // RIGIDBODY ASTEROIDS -------
        // Mass of asteroid (scaled between minAsteroidScale/maxAsteroidScale)
        public float mass = 1.0f;
        // Minimum angular velocity of asteroid (rotational speed)
        public float minAsteroidAngularVelocity = 0.0f;
        // Maximum angular velocity of asteroid (rotational speed)
        public float maxAsteroidAngularVelocity = 1.0f;
        // Angular velocity (rotational speed) multiplier
        public float angularVelocityMultiplier = 1.0f;
        // ----------------------------

        // Private variables
        private float minAsteroidRotationSpeed;
        private float maxAsteroidRotationSpeed;
        private float minAsteroidVelocity;
        private float maxAsteroidVelocity;
        private float distanceToSpawn;
        private Transform asteroidTransform;
        private List<Transform> asteroidsTransforms = new List<Transform>();

        void OnEnable()
        {
            DoSetup();
            // Cache reference to transform to increase performance
            asteroidTransform = transform;

            // Calculate the actual spawn
            distanceToSpawn = range * distanceSpawn;

            // Check if there are any asteroid objects that was spawned prior to this script being disabled
            // If there are asteroids in the list, activate the gameObject again.
            for (int i = 0; i < asteroidsTransforms.Count; i++)
            {
                asteroidsTransforms[i].gameObject.SetActive(true);
            }

            // Spawn new asteroids in the entire sphere (not just at spawn range, hence the "false" parameter)
            SpawnAsteroids(false);

            // Use transform as origin (center) of asteroid field. If null, use this transform.
            if (asteroidFieldOriginTransform == null) asteroidFieldOriginTransform = transform;

            // Set the parameters for each asteroid for fading/scaling
            for (int i = 0; i < asteroidsTransforms.Count; i++)
            {
                Asteroid a = asteroidsTransforms[i].GetComponent<Asteroid>();
                if (a != null)
                {
                    // When spawning asteroids, set the parameters for the shader based on parameters of this asteroid field
                    a.fadeAsteroids = fadeAsteroids;
                    a.fadeAsteroidsFalloffExponent = 1f; // fadeAsteroidsFalloffExponent;
                    a.distanceFade = distanceFade;
                    a.visibilityRange = range;
                }
            }
        }

        void OnDisable()
        {
            // Asteroid field game object has been disabled, disable all the asteroids as well
            for (int i = 0; i < asteroidsTransforms.Count; i++)
            {
                // If the transform of the asteroid exists (it won't be upon application exit for example)...
                if (asteroidsTransforms[i] != null)
                {
                    // deactivate the asteroid gameObject
                    asteroidsTransforms[i].gameObject.SetActive(false);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            // Draw a yellow wire gizmo sphere at the transform's position with the size of the asteroid field
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }

        void Update()
        {
            if (prefabAsteroids.Length == 0)
            {
                return;
            }

            // Iterate through asteroids and relocate them as parent object moves
            for (int i = asteroidsTransforms.Count - 1; i >= 0; i--)
            {
                // Cache the reference to the Transform of the asteroid in the list
                Transform asteroid = asteroidsTransforms[i];

                // If the asteroid in the list has a Transform...
                if (asteroid != null)
                {
                    // Calculate the distance of the asteroid to the center of the asteroid field
                    float distance = Vector3.Distance(asteroid.position, asteroidTransform.position);

                    // If the distance is greater than the range variable...
                    if (distance > range && respawnIfOutOfRange)
                    {
                        // Relocate ("respawn") the asteroid to a new position at spawning distance
                        asteroid.position = (Random.onUnitSphere * distanceToSpawn) + asteroidTransform.position;
                        // Give the asteroid a new scale within the min/max scale range
                        float newScale = Random.Range(minAsteroidScale, maxAsteroidScale) * scaleMultiplier;
                        asteroid.localScale = new Vector3(newScale, newScale, newScale);
                        // Give the asteroid a new random rotation
                        Vector3 newRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
                        asteroid.eulerAngles = newRotation;
                        // Recalculate the distance since it has been relocated
                        //distance = Vector3.Distance(asteroid.position, cacheTransform.position);
                    }
                }
                else
                {
                    // Asteroid transform must have been been destroyed for some reason (from another script?), remove it from the lists
                    asteroidsTransforms.RemoveAt(i);
                }

                // 	If respawning is enabled and asteroid count is lower than Max Asteroids...
                if (respawnDestroyedAsteroids && asteroidsTransforms.Count < maxAsteroids)
                {
                    // Spawn new asteroids (where true states that they are to be spawned at spawn distance rather than anywhere in range)
                    SpawnAsteroids(true);
                }
            }
        }


        /// <summary>
        /// Spawns the number of asteroids needed to reach maxAsteroids
        /// </summary>
        /// <param name='atSpawnDistance'>
        /// true = spawn on sphere at distanceSpawn * range (used for respawning asteroids)
        /// false = spawn in sphere within distanceSpawn * range (used for brand new asteroid fields)
        /// </param>
        void SpawnAsteroids(bool atSpawnDistance)
        {
            // Spawn new asteroids at a distance if count is below maxAsteroids (e.g. asteroids were destroyed outside of this script)
            while (asteroidsTransforms.Count < maxAsteroids)
            {
                // Select a random asteroid from the prefab array
                GameObject newAsteroidPrefab = prefabAsteroids[Random.Range(0, prefabAsteroids.Length)];

                Vector3 newPosition = Vector3.zero;
                if (atSpawnDistance)
                {
                    // Spawn asteroid at spawn distance (this is used for existing asteroid fields so it spawns out of visible range)
                    newPosition = asteroidTransform.position + Random.onUnitSphere * distanceToSpawn;
                }
                else
                {
                    // Spawn asteroid anywhere within range (this is used for new asteroid fields before it becomes visible)
                    newPosition = asteroidTransform.position + Random.insideUnitSphere * distanceToSpawn;
                }

                // Instantiate the new asteroid at a random location
                GameObject newAsteroid = Instantiate(newAsteroidPrefab, newPosition, asteroidTransform.rotation) as GameObject;
                Renderer renderer = newAsteroid.GetComponent<Renderer>();
                Asteroid asteroid = newAsteroid.GetComponent<Asteroid>();

                // Add the asteroid to a list used to keep track of them
                asteroidsTransforms.Add(newAsteroid.transform);

                // Add the asteroid to a list used to keep track of them
                asteroidsTransforms.Add(newAsteroid.transform);

                // If the asteroid has the Asteroid script attached to it...
                if (asteroid != null)
                {
                    // If the asteroid has a collider...
                    if (newAsteroid.GetComponent<Collider>() != null)
                    {
                        asteroid.SetPolyCount(polyCountCollider, true);
                    }
                }

                // Set scale of asteroid within min/max scale * scaleMultiplier
                float newScale = Random.Range(minAsteroidScale, maxAsteroidScale) * scaleMultiplier;
                newAsteroid.transform.localScale = new Vector3(newScale, newScale, newScale);

                // Set a random orientation of the asteroid
                newAsteroid.transform.eulerAngles = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));


                Rigidbody rigidbody = newAsteroid.GetComponent<Rigidbody>();
                if (isRigidbody)
                {
                    // RIGIDBODY ASTEROIDS
                    // If the asteroid prefab has a rigidbody...
                    if (rigidbody != null)
                    {
                        // Set the mass to mass specified in AsteroidField multiplied by scale
                        rigidbody.mass = mass * newScale;
                        // Set the velocity (speed) of the rigidbody to within the min/max velocity range multiplier by velocityMultiplier
                        rigidbody.velocity = newAsteroid.transform.forward * Random.Range(minAsteroidVelocity, maxAsteroidVelocity);
                        // Set the angular velocity (rotational speed) of the rigidbody to within the min/max velocity range multiplier by velocityMultiplier
                        rigidbody.angularVelocity = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)) * Random.Range(minAsteroidAngularVelocity, maxAsteroidAngularVelocity) * angularVelocityMultiplier;
                    }
                    else
                    {
                        Debug.LogWarning("AsteroidField is set to spawn rigidbody asteroids but one or more asteroid prefabs do not have rigidbody component attached.");


                    }
                }
                else
                {
                    // NON-RIGIDBODY ASTEROIDS

                    // If the asteroid prefab has a rigidbody...
                    if (rigidbody != null)
                    {
                        // Destroy the rigidbody since the asteroid field is spawning non-rigidbody asteroids
                        Destroy(rigidbody);
                    }
                    // If the asteroid has the Asteroid script attached to it...
                    if (asteroid != null)
                    {
                        // Set rotation and drift axis and speed
                        asteroid.SetRandomRotation(minAsteroidRotationSpeed,
                                                   maxAsteroidRotationSpeed);
                        asteroid.SetRandomVelocity(minAsteroidVelocity,
                                                   maxAsteroidVelocity);
                    }
                }
            }
        }

        void DoSetup()
        {
            if (prefabAsteroids.Length == 0)
            {
                return;
            }
            minAsteroidRotationSpeed = minAsteroidRotationLimit * rotationSpeedMultiplier;
            maxAsteroidRotationSpeed = maxAsteroidRotationLimit * rotationSpeedMultiplier;
            minAsteroidVelocity = minAsteroidVelocityLimit * velocityMultiplier;
            maxAsteroidVelocity = maxAsteroidVelocityLimit * velocityMultiplier;


        }

        // Internal function to allow weighted random selection of materials
        static T WeightedRandom<T>(SortedList<int, T> list)
        {
            int max = list.Keys[list.Keys.Count - 1];
            int random = Random.Range(0, max);
            foreach (int key in list.Keys)
            {
                if (random <= key)
                {
                    return list[key];
                }
            }
            return default(T);
        }
    }
}
