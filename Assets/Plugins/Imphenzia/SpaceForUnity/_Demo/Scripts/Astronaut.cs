using System;
using System.Drawing;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


//! follow script
// set up rocket on beginning of level / connect two levels - lukas / jesse
//// Figure out why particles randomly stop working with astronaut


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
    [Tooltip("An array of particle effects that show visual damage")]
    public Transform[] visualDamageParticles;
    [Tooltip("How hard the solar wind should push the astronaut")]
    public float solarWindPushForce = 25000f;

    [Tooltip("Audio clip for collecting a star piece that isn't the last one")]
    public AudioClip collectStarPiece;
    [Tooltip("Audio clip for collecting the last star piece")]
    public AudioClip collectFinalStarPiece;
    [Tooltip("Audio clip for when the player crashes into something they shouldn't crash into")]
    public AudioClip bonkSound;
    // TODO: restructure code. It might not be worth having all these variables
    public float apoapsis = 5;
    public float periapsis = 5;
    public float inclinationAngleDegrees = 20;
    public float inclinationNodeDegrees = 0;
    public float objectMass = 1;
    private Vector3 startPosition;

    // Private variables
    private int hitCount;
    private Rigidbody cacheRigidbody;
    private TravelWarp travelWarp;
    private float orgWarpSpeed;
    private float orgWarpStrength;
    private Vector3 rollAxis = new Vector3(0, 1, 0);
    private Vector3 pitchAxis = new Vector3(1, 0, 0);
    private Vector3 yawAxis = new Vector3(0, 0, 1);
    private bool inSolarWind = false;
    private SolarWind solarWindArea;
    private AudioSource astronautAudio;

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
        astronautAudio = this.GetComponent<AudioSource>();
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
            {
                thruster.StartThruster();
            }

        }
        // Stop all thrusters when releasing Fire 1
        if (Input.GetButtonUp("Fire1"))
        {
            foreach (Thruster thruster in thrusters)
            {
                thruster.StopThruster();
            }
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

        // In the physics update
        // Add relative rotational roll torque when steering left/right
        if (Input.GetKey(KeyCode.Q))
        {
            cacheRigidbody.AddRelativeTorque(rollRate * cacheRigidbody.mass * rollAxis);
        }
        if (Input.GetKey(KeyCode.E))
        {
            cacheRigidbody.AddRelativeTorque(-rollRate * cacheRigidbody.mass * rollAxis);
        }

        // Add rudder yaw torque when steering left/right
        cacheRigidbody.AddRelativeTorque(-Input.GetAxis("Horizontal") * yawRate * cacheRigidbody.mass * yawAxis);
        // Add pitch torque when steering up/down
        cacheRigidbody.AddRelativeTorque(Input.GetAxis("Vertical") * pitchRate * cacheRigidbody.mass * pitchAxis);
    }

    // void FixedUpdate()
    // {

    // }

    /// <summary>
    /// OnCollisionEnter is called when this collider/rigidbody has begun
    /// touching another rigidbody/collider.
    /// </summary>
    /// <param name="other">The Collision data associated with this collision.</param>
    void OnCollisionEnter(Collision other)
    {
        if (other.collider.tag == "Debris")
        {
            astronautAudio.PlayOneShot(bonkSound);
            hitCount++;
            if (hitCount > visualDamageParticles.Length)
            {
                return;
            }
            else
            {
                visualDamageParticles[hitCount - 1].gameObject.SetActive(true);
            }
        }
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
            collectedStarPieces++;
            if (collectedStarPieces >= 1)
            {
                astronautAudio.PlayOneShot(collectFinalStarPiece);
                Invoke("GoToWinScene", 3);
            }
            else
            {
                astronautAudio.PlayOneShot(collectStarPiece);
            }
        }

        if (other.tag == "Wind")
        {
            inSolarWind = true;
            solarWindArea = other.GetComponent<SolarWind>();
            solarWindArea.isActive = true;
            solarWindArea.windForce = solarWindPushForce;
            solarWindArea.astronautBody = cacheRigidbody;
        }
    }
    void GoToWinScene()
    {
        SceneManager.LoadScene("Win");
    }

    /// <summary>
    /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Wind")
        {
            inSolarWind = false;
            solarWindArea.isActive = false;
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
        float moveSpeedAddition = 0 * cacheRigidbody.velocity.magnitude;
        // print(percentage + "\t" + moveSpeedAddition);

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
