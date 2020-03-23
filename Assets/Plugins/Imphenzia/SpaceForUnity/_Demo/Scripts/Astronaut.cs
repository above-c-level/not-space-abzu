using System;
using System.Drawing;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;



namespace Imphenzia.SpaceForUnity
{
    public class Astronaut : MonoBehaviour
    {
        [Tooltip("Thruster array containing reference to thrusters prefabs attached to the ship for propulsion")]
        public Thruster[] thrusters;

        [Tooltip("Specify the roll rate (multiplier for rolling the ship when steering left/right)")]
        public float rollRate = 100.0f;

        [Tooltip("Specify the yaw rate (multiplier for rudder/steering the ship when steering left/right)")]
        public float yawRate = 30.0f;

        [Tooltip("Specify the pitch rate (multiplier for pitch when steering up/down)")]
        public float pitchRate = 100.0f;

        [Tooltip("Vector3 array for mount points relative to ship where weapons will fire from")]
        public Vector3[] weaponMountPoints;

        [Tooltip("Reference to Laser Shot prefab, i.e. the laser bullet prefab to be instanitated")]
        public Transform laserShotPrefab;

        [Tooltip("Sound effect audio clip to be played when firing weapon")]
        public AudioClip soundEffectFire;

        [Tooltip("Audio source to play weapon")]
        public AudioSource audioSourceLaser;

        [Tooltip("Array of particle systems to play during warp speed")]
        public ParticleSystem[] warpFlames;

        [Tooltip("Particle system with speed lines around the ship")]
        public ParticleSystem warpUltra;

        [Tooltip("Audio source for spaceship warp speed sound effect")]
        public AudioSource audioSourceWarpUltra;
        [Tooltip("The number of collected star pieces")]
        public int collectedStarPieces = 0;
        [Tooltip("A temporary canvas for displaying a win screen")]
        public Canvas canvas;
        [Tooltip("How quickly an object should move towards its goal")]
        public float moveTowardsSpeed = 1;
        [Tooltip("How quickly the objects should go around the orbit")]
        public float orbitSpeedMultiplier = 1;
        [Tooltip("How close the object should be to its orbit point before it's close enough")]
        public float closeEnough = 0.05f;
        // TODO: restructure code. It might not be worth having all these variables
        public float apoapsis = 5;
        public float periapsis = 5;
        public float inclinationAngleDegrees = 20;
        public float inclinationNodeDegrees = 0;
        public float objectMass = 1;
        private Vector3 startPosition;

        // Private variables
        private Rigidbody cacheRigidbody;
        private TravelWarp travelWarp;
        private float orgWarpSpeed;
        private float orgWarpStrength;

        private List<objectInOrbit> orbitingObjects;
        class objectInOrbit
        {
            public Transform objectTransform;
            public Vector3 originPosition;
            public Vector3 targetPosition;
            public float angle;
            public IEnumerator coroutineEnumerator;
        }

        void Start()
        {
            // Initialize `orbitGoals`
            orbitingObjects = new List<objectInOrbit>();
            startPosition = transform.position;

            // Ensure that the thrusters in the array have been linked properly
            foreach (Thruster thruster in thrusters)
            {
                if (thruster == null)
                {
                    Debug.LogError("Thruster array not properly configured. Attach thrusters to the game object and link them to the Thrusters array.");
                }
            }
            // Cache reference to rigidbody to improve performance
            cacheRigidbody = GetComponent<Rigidbody>();
            if (cacheRigidbody == null)
            {
                Debug.LogError("Spaceship has no rigidbody - the thruster scripts will fail. Add rigidbody component to the spaceship.");
            }

            // If there is a SU_TravelWarp component on this ship, grab the reference to it
            if (gameObject.GetComponent<TravelWarp>() != null)
            {
                travelWarp = gameObject.GetComponent<TravelWarp>();
            }

            // Remember the original parameters to return to when exiting ultra warp (demo)
            if (travelWarp)
            {
                orgWarpSpeed = travelWarp.visualTextureSpeed;
                orgWarpStrength = travelWarp.visualWarpEffectMagnitude;
            }
        }

        void Update()
        {
            // Start all thrusters when pressing Fire 1
            if (Input.GetButtonDown("Fire1"))
            {
                foreach (Thruster thruster in thrusters)
                    thruster.StartThruster();

            }
            // Stop all thrusters when releasing Fire 1
            if (Input.GetButtonUp("Fire1"))
            {
                foreach (Thruster thruster in thrusters)
                    thruster.StopThruster();
            }

            if (Input.GetButtonDown("Fire2"))
            {
                // Iterate through each weapon mount point Vector3 in array
                foreach (Vector3 wmp in weaponMountPoints)
                {
                    // Calculate where the position is in world space for the mount point
                    Vector3 pos = transform.position + transform.right * wmp.x + transform.up * wmp.y + transform.forward * wmp.z;
                    // Instantiate the laser prefab at position with the spaceships rotation
                    Transform laserShot = (Transform)Instantiate(laserShotPrefab, pos, transform.rotation);
                    // Specify which transform it was that fired this round so we can ignore it for collision/hit
                    laserShot.GetComponent<LaserShot>().firedBy = transform;

                }
                // Play sound effect when firing
                if (soundEffectFire != null)
                {
                    audioSourceLaser.PlayOneShot(soundEffectFire);
                }
            }
            MoveSatellites();


            // // If space key is held down...
            // if (Input.GetKey(KeyCode.Space))
            // {
            //     // Play the particle systems in the warpFlames array - these are for visuals only, not proper thrusters
            //     foreach (ParticleSystem ps in warpFlames)
            //         ps.Play();
            //     // Ensure that the normal thrusters are on
            //     foreach (Thruster thruster in thrusters)
            //         thruster.StartThruster();

            //     // Set the Warp property of the SU_TravelWarp script to true so it knows we want to warp
            //     if (travelWarp != null)
            //         travelWarp.Warp = true;

            //     // Demo of modifying properties of TravelWarp - this example changes speed and strength of effect to simulate overdrive / ultra fast warping
            //     if (Input.GetKeyDown(KeyCode.RightShift))
            //     {
            //         if (!warpUltra.isPlaying)
            //             warpUltra.Play();

            //         if (travelWarp != null)
            //         {
            //             travelWarp.SetBrightness(2f);
            //             travelWarp.SetSpeed(4f);
            //             travelWarp.SetStrength(1f);
            //             travelWarp.SetUltraSpeedAddon(60f);
            //             audioSourceWarpUltra.Play();

            //         }
            //     }
            //     if (Input.GetKeyUp(KeyCode.RightShift))
            //     {
            //         if (travelWarp != null)
            //         {
            //             travelWarp.SetBrightness(1f);
            //             travelWarp.SetSpeed(orgWarpSpeed);
            //             travelWarp.SetStrength(orgWarpStrength);
            //             travelWarp.SetUltraSpeedAddon(0f);
            //         }
            //         if (warpUltra.isPlaying)
            //             warpUltra.Stop();
            //     }


            // }
            // else
            // {
            //     // If key is not held down...
            //     // Stop the particle systems in the array warpFlames
            //     foreach (ParticleSystem _ps in warpFlames)
            //         _ps.Stop();
            //     // If Fire1 is not pressed down, also stop the thrusters of the spaceship
            //     if (!Input.GetButton("Fire1"))
            //         foreach (Thruster _thruster in thrusters)
            //             _thruster.StopThruster();
            //     // Set the Warp property of SU_TravelWarp to false so we don't warp anymore
            //     if (travelWarp != null)
            //     {
            //         travelWarp.Warp = false;
            //         travelWarp.SetBrightness(1f);
            //         travelWarp.SetSpeed(0.5f);
            //         travelWarp.SetStrength(0.2f);
            //         travelWarp.SetUltraSpeedAddon(0);
            //     }
            //     if (warpUltra.isPlaying) warpUltra.Stop();
            // }

        }


        void FixedUpdate()
        {
            // In the physics update
            // Add relative rotational roll torque when steering left/right
            if (Input.GetKey(KeyCode.Q))
            {
                cacheRigidbody.AddRelativeTorque(new Vector3(0, 0, rollRate * cacheRigidbody.mass));
            }
            if (Input.GetKey(KeyCode.E))
            {
                cacheRigidbody.AddRelativeTorque(new Vector3(0, 0, -rollRate * cacheRigidbody.mass));
            }


            // Add rudder yaw torque when steering left/right
            cacheRigidbody.AddRelativeTorque(new Vector3(0, Input.GetAxis("Horizontal") * yawRate * cacheRigidbody.mass, 0));
            // Add pitch torque when steering up/down
            cacheRigidbody.AddRelativeTorque(new Vector3(Input.GetAxis("Vertical") * pitchRate * cacheRigidbody.mass, 0, 0));

        }

        /// <summary>
        /// OnTriggerEnter is called when the Collider other enters the trigger.
        /// </summary>
        /// <param name="other">The other Collider involved in this collision.</param>
        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "StarPiece")
            {
                // other.GetComponent<ObjectOrbit>().enabled = true;
                Transform starpiece = other.GetComponent<Transform>();

                // starpiece.parent = this.transform;
                AddObjectToOrbit(starpiece);
                other.GetComponent<Collider>().enabled = false;
                StartCoroutine(FadeIntensity(other.transform.GetChild(1).GetComponent<Light>()));
                collectedStarPieces += 1;

            }
            if (collectedStarPieces >= 5)
            {
                print("you're winner !");
                canvas.enabled = true;
            }
        }

        /// <summary>
        /// Fades the intensity of a light source down to 0.05 when started as
        /// coroutine
        /// </summary>
        /// <param name="lightSource">The light source to fade</param>
        IEnumerator FadeIntensity(Light lightSource)
        {
            // While the intensity is greater than 0.05
            while (lightSource.intensity > 0.05)
            {
                // Decrease the intensity by 0.001
                lightSource.intensity -= 0.001f;
                // But yield to other processes in the meantime
                yield return null;
            }
        }

        /// <summary>
        /// Adds an object to the orbit list, so that it can move around the player
        /// </summary>
        /// <param name="itemTransform"></param>
        void AddObjectToOrbit(Transform itemTransform)
        {
            // Make a new temporary object in orbit
            objectInOrbit temp = new objectInOrbit();
            // Store its transform
            temp.objectTransform = itemTransform;
            // Add it to the end of the list
            orbitingObjects.Add(temp);
            // Then go back through and recalculate the orbit information,
            // but for all of the objects
            RecalculateObjects();
        }

        /// <summary>
        /// Calculates the orbit information for all objects in `orbitingObjects`
        /// </summary>
        void RecalculateObjects()
        {
            // The angle of the orbit to calculate every other position from
            float rootAngle;
            // If there are no objects, don't calculate anything, just return
            if (orbitingObjects.Count == 0)
            {
                return;
            }
            // Otherwise, the root angle should be the angle of the first object
            else
            {
                rootAngle = orbitingObjects[0].angle;
            }

            // For each object in the orbit
            for (int i = 0; i < orbitingObjects.Count; i++)
            {
                // Get the object
                objectInOrbit currentObject = orbitingObjects[i];
                // Brief explanation:
                // If you take the index of an object, multiple by 2pi radians,
                // then divide by the number of items, you get a circle which
                // has equally spaced items, in radians.
                currentObject.angle = (float)((i * 2 * Math.PI / (orbitingObjects.Count))
                                                + rootAngle);
                // Whenever value goes above 2pi, this brings it back down to
                // the range of [0, 2pi]
                currentObject.angle %= (float)(2 * Math.PI);
                // Next, calculate the target position. This is done on the fly,
                // but it could be switched to the precalculated version fairly
                // easily.
                currentObject.originPosition = currentObject.objectTransform.position - transform.position;
                currentObject.targetPosition = CalculateOrbitAtPoint(currentObject.angle).position;
            }
        }

        /// <summary>
        /// Helper method to move all of the satellites at once.
        /// </summary>
        void MoveSatellites()
        {
            // TODO: Find a more efficient way of calculating directions of satellites.
            // Currently this calculates the intended direction on every frame,
            // which isn't exactly fast.
            RecalculateObjects();
            for (int i = 0; i < orbitingObjects.Count; i++)
            {
                objectInOrbit thisObject = orbitingObjects[i];
                if (Vector3.Distance(thisObject.originPosition,
                                     thisObject.targetPosition) > closeEnough)
                {
                    if (thisObject.coroutineEnumerator != null)
                    {
                        StopCoroutine(thisObject.coroutineEnumerator);
                    }
                    thisObject.coroutineEnumerator = Move(thisObject);
                    StartCoroutine(thisObject.coroutineEnumerator);
                }
                else
                {
                    MoveSingleSatellite(thisObject);
                }
            }
        }
        void MoveSingleSatellite(objectInOrbit satellite)
        {
            orbitPoint point = CalculateOrbitAtPoint(satellite.angle);
            satellite.objectTransform.position = point.position + transform.position;
            satellite.originPosition = point.position;
            // satellite.targetPosition = new Vector3(0,0,0);
            satellite.targetPosition = point.position;
            satellite.angle += point.velocity;
            if (satellite.angle > 720)
            {
                satellite.angle %= 720;
            }
        }

        IEnumerator Move(objectInOrbit objectToMove)
        {
            float percentage = Vector3.Angle(objectToMove.targetPosition - objectToMove.originPosition,
                                             cacheRigidbody.velocity);
            percentage /= 180f;
            float moveSpeedAddition = percentage * cacheRigidbody.velocity.magnitude;
            print(percentage + "\t" + moveSpeedAddition);

            while (Vector3.Distance(objectToMove.originPosition,
                                    objectToMove.targetPosition) > closeEnough)
            {
                objectToMove.objectTransform.position = Vector3.MoveTowards(
                    objectToMove.originPosition,
                    objectToMove.targetPosition,
                    (moveTowardsSpeed + moveSpeedAddition)
                        * Time.deltaTime
                ) + startPosition;
                yield return null;
            }
        }
        // TODO: Restructure the code so that these are referenceable.
        // Currently, this code is almost exactly just a duplicate of
        // what is found in ObjectOrbit.cs
        public struct orbitPoint
        {
            public Vector3 position;
            public float velocity;
        }

        /// <summary>
        /// Calculates the velocity and position an object should have at a given angle
        /// </summary>
        /// <param name="angle">The angle to calculate the orbit parameters for</param>
        /// <returns>The position and velocity in an orbitPoint struct</returns>
        public orbitPoint CalculateOrbitAtPoint(float angle)
        {
            // This is here only to make the speeds (almost) match between
            // precalculated and calculated on-the-fly orbits
            float internalSpeedMultiplier = (float)(orbitSpeedMultiplier * 2 * Math.PI / 720);
            float posY = 0;
            float G = 6.67408f;

            // This entire section is just lots of math. It's a combination of
            // trigonometry and the Keplerian equations. I'm not entirely convinced
            // that it's actually worth explaining here (since it's relatively
            // complicated) so I'm not going to.
            float semiMajorAxis = (apoapsis + periapsis) / 2;
            float semiMinorAxis = Mathf.Sqrt(apoapsis * periapsis);
            float eccentricity = Mathf.Sqrt(1 - (semiMinorAxis * semiMinorAxis)
                                        / (semiMajorAxis * semiMajorAxis));

            float movOffset = ((apoapsis + periapsis) / 2) - periapsis;
            float posX = semiMajorAxis * Mathf.Cos(angle) - movOffset;
            float posZ = semiMinorAxis * Mathf.Sin(angle);

            float r = Mathf.Sqrt(posX * posX + posZ * posZ);

            float nPosX = posX * Mathf.Cos(inclinationAngleDegrees * Mathf.Deg2Rad)
                          - posY * Mathf.Sin(inclinationAngleDegrees * Mathf.Deg2Rad);
            float nPosY = posY * Mathf.Cos(inclinationAngleDegrees * Mathf.Deg2Rad)
                          + posX * Mathf.Sin(inclinationAngleDegrees * Mathf.Deg2Rad);

            float nPosX2 = nPosX * Mathf.Cos(inclinationNodeDegrees * Mathf.Deg2Rad)
                          - posZ * Mathf.Sin(inclinationNodeDegrees * Mathf.Deg2Rad);
            float nPosZ = posZ * Mathf.Cos(inclinationNodeDegrees * Mathf.Deg2Rad)
                          + nPosX * Mathf.Sin(inclinationNodeDegrees * Mathf.Deg2Rad);

            orbitPoint tempOrbitPoint;
            // Once (almost) all the math is done, store it in an orbitPoint
            tempOrbitPoint.position = new Vector3(nPosX2, nPosY, nPosZ);
            tempOrbitPoint.velocity = internalSpeedMultiplier * Mathf.Sqrt((G * objectMass)
                                      * ((2 / r) - (1 / semiMajorAxis)));
            // Then return it
            return tempOrbitPoint;
        }
    }
}