using System;
using System.Drawing;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    [Tooltip("How quickly an object should move towards its goal")]
    public float moveTowardsSpeed = 100;
    [Tooltip("How close the object should be to its target before it's close enough")]
    public float closeEnough = 10f;
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
    private List<Transform> collectedStarPieces = new List<Transform>();
    private List<Vector3> breadcrumbs = new List<Vector3>();
    void Start()
    {
        astronautAudio = this.GetComponent<AudioSource>();
        startPosition = transform.position;
        breadcrumbs.Add(transform.position);

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

        float leadingDisplacement = (transform.position - breadcrumbs[0]).magnitude;
        if (leadingDisplacement >= closeEnough)
        {
            breadcrumbs.RemoveAt(breadcrumbs.Count - 1);
            breadcrumbs.Insert(0, transform.position);
            leadingDisplacement %= closeEnough;
        }
        if (leadingDisplacement != 0 && breadcrumbs.Count > 1)
        {
            Vector3 pos = Vector3.Lerp(breadcrumbs[1], breadcrumbs[0], leadingDisplacement / closeEnough);
            collectedStarPieces[0].position = pos;
            collectedStarPieces[0].rotation = Quaternion.Slerp(Quaternion.LookRotation(breadcrumbs[0] - breadcrumbs[1]),
                                                               Quaternion.LookRotation(transform.position - breadcrumbs[0]),
                                                               leadingDisplacement / closeEnough);
            for (int i = 1; i < collectedStarPieces.Count; i++)
            {
                pos = Vector3.Lerp(breadcrumbs[i + 1], breadcrumbs[i], leadingDisplacement / closeEnough);
                collectedStarPieces[i].position = pos;
                collectedStarPieces[i].rotation = Quaternion.Slerp(Quaternion.LookRotation(breadcrumbs[i] - breadcrumbs[i + 1]),
                                                                   Quaternion.LookRotation(breadcrumbs[i - 1] - breadcrumbs[i]),
                                                                   leadingDisplacement / closeEnough);
            }
        }

    }

    void FixedUpdate()
    {
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
            other.GetComponent<Collider>().enabled = false;
            StartCoroutine(FadeIntensity(other.transform.GetChild(1).GetComponent<Light>()));
            collectedStarPieces.Add(other.transform);
            breadcrumbs.Add(other.transform.position);
            if (collectedStarPieces.Count >= 5)
            {
                astronautAudio.PlayOneShot(collectFinalStarPiece);
                if (SceneManager.GetActiveScene().name == "Level1layout")
                {
                    Invoke("GoToWinScene", 4);
                }
                else if (SceneManager.GetActiveScene().name == "Flat")
                {
                    Invoke("GoToSpace", 4);
                }
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
    void GoToSpace()
    {
        SceneManager.LoadScene("Level1layout");
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
}
