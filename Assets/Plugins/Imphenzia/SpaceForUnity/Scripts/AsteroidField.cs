using UnityEngine;
using System.Collections.Generic;

namespace Imphenzia.SpaceForUnity
{
    public class AsteroidField : MonoBehaviour
    {
        // TODO: This is a really hacky and not so great way of making sure that there
        //       aren't too many broken satellites. Ideally this will be updated.
        //       It probably also doesn't quite work as intended.
        private float brokenSatelliteChance = 0.1f;
        // Poly Count (quality) of the asteroids in the field
        public Asteroid.ChildrenContainer polyCount = Asteroid.ChildrenContainer.STANDALONE;
        // Poly Count (quality) of the asteroid colliders (LOW = fast, HIGH = slow)
        public Asteroid.ChildrenContainer polyCountCollider = Asteroid.ChildrenContainer.STANDALONE;

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
                // TODO: Related to the todo at the top, this assumes that there
                //       is exactly one broken satellite, and that it is always
                //       at the end of the array.
                int arrayChoice;
                if (Random.Range(0f, 1f) < brokenSatelliteChance)
                {
                    arrayChoice = prefabAsteroids.Length - 1;
                }
                else
                {
                    arrayChoice = Random.Range(0, prefabAsteroids.Length - 1);

                }
                // Select a random asteroid from the prefab array
                GameObject newAsteroidPrefab = prefabAsteroids[arrayChoice];

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

                // Set scale of asteroid within min/max scale * scaleMultiplier
                float newScale = Random.Range(minAsteroidScale, maxAsteroidScale) * scaleMultiplier;
                newAsteroid.transform.localScale = new Vector3(newScale, newScale, newScale);

                // Set a random orientation of the asteroid
                newAsteroid.transform.eulerAngles = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));


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

        /// <summary>
        /// Sets up the `asteroids`, or in this case, debris.
        /// </summary>
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

        // Previously, an internal function to allow weighted random selection
        // of materials. However, materials were removed and thus this currently
        // doesn't do anything. It might still be useful for creating debris
        // in a weighted manner.
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
