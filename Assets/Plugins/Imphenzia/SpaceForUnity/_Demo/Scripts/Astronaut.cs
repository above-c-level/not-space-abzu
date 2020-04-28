using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;



public class Astronaut : MonoBehaviour
{
    [Tooltip("The thruster force to be applied when active")]
    public float thrusterForce = 10000;
    [Tooltip("Thruster array containing reference to thrusters prefabs attached to the ship for propulsion")]
    public Thruster[] thrusters;

    [Tooltip("Specify the roll rate (multiplier for rolling the ship when steering left/right)")]
    public float rollRate = 100.0f;

    [Tooltip("Specify the yaw rate (multiplier for rudder/steering the ship when steering left/right)")]
    public float yawRate = 30.0f;

    [Tooltip("Specify the pitch rate (multiplier for pitch when steering up/down)")]
    public float pitchRate = 100.0f;
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
    public AudioClip introAudio;
    public AudioClip onFirstCollect;
    public AudioClip onSecondCollect;
    public AudioClip onFinalCollect;
    public AudioClip pushClip;
    public AudioClip[] damageClips;
    public AudioClip[] idleClips;
    public AudioClip[] windClips;

    private Vector3 startPosition;

    // Private variables
    private int hitCount;
    private Rigidbody cacheRigidbody;
    private Vector3 rollAxis = new Vector3(0, 1, 0);
    private Vector3 pitchAxis = new Vector3(1, 0, 0);
    private Vector3 yawAxis = new Vector3(0, 0, 1);
    private bool inSolarWind = false;
    private SolarWind solarWindArea;
    private AudioSource astronautAudio;
    private List<Transform> followList = new List<Transform>();
    private List<Vector3> breadcrumbs = new List<Vector3>();
    private bool playerHasKey = false;
    private bool playerHasLock = false;
    public int collectedStarPieces = 0;
    private float lastAudioPlaytime;

    private GameObject keyObject;
    private AsyncOperation asyncLoad;
    private bool alreadyPlayedPush = false;
    void Start()
    {
        lastAudioPlaytime = Time.time;
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
        Invoke("DelayedStartAudio", 1f);
    }
    void DelayedStartAudio()
    {
        lastAudioPlaytime = Time.time + 5f;
        astronautAudio.PlayOneShot(introAudio, 1f);
    }

    void Update()
    {
        // Start all thrusters when pressing Fire 1
        if (Input.GetButtonDown("Fire1"))
        {
            foreach (Thruster thruster in thrusters)
            {
                thruster.StartThruster(thrusterForce);
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
        TryPlayIdleAudio();


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
            followList[0].position = pos;
            followList[0].rotation = Quaternion.Slerp(Quaternion.LookRotation(breadcrumbs[0] - breadcrumbs[1]),
                                                               Quaternion.LookRotation(transform.position - breadcrumbs[0]),
                                                               leadingDisplacement / closeEnough);
            for (int i = 1; i < followList.Count; i++)
            {
                pos = Vector3.Lerp(breadcrumbs[i + 1], breadcrumbs[i], leadingDisplacement / closeEnough);
                followList[i].position = pos;
                followList[i].rotation = Quaternion.Slerp(Quaternion.LookRotation(breadcrumbs[i] - breadcrumbs[i + 1]),
                                                                   Quaternion.LookRotation(breadcrumbs[i - 1] - breadcrumbs[i]),
                                                                   leadingDisplacement / closeEnough);
            }
        }
    }

    void TryPlayIdleAudio()
    {
        if (idleClips.Length == 0)
        {
            return;
        }
        float audioWaitTime = 30f;
        if (Time.time - lastAudioPlaytime > audioWaitTime)
        {
            audioWaitTime = 30f + Random.Range(0f, 30f);
            AudioClip clip = idleClips[Random.Range(0, idleClips.Length - 1)];
            if (Random.Range(0f, 1f) > 0.95f)
            {
                clip = idleClips[idleClips.Length];
            }
            if (!alreadyPlayedPush && collectedStarPieces == 0 && pushClip != null)
            {
                clip = pushClip;
            }
            astronautAudio.PlayOneShot(clip);
            lastAudioPlaytime = Time.time;
        }
    }

    /// <summary>
    /// OnCollisionEnter is called when this collider/rigidbody has begun
    /// touching another rigidbody/collider.
    /// </summary>
    /// <param name="other">The Collision data associated with this collision.</param>
    void OnCollisionEnter(Collision other)
    {
        if (other.collider.tag == "Debris"
            || other.collider.tag == "Lock")
        {
            astronautAudio.PlayOneShot(bonkSound);
            Invoke("PlayDamageAudio", 1f);
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

    void PlayDamageAudio()
    {
        if (damageClips.Length == 0)
        {
            return;
        }
        if (Random.Range(0f, 1f) < 0.25f && Time.time - lastAudioPlaytime > 8f)
        {
            astronautAudio.PlayOneShot(damageClips[Random.Range(0, damageClips.Length)]);
            lastAudioPlaytime = Time.time;
        }
    }

    /// <summary>
    /// OnTriggerEnter is called when the Collider other enters the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Key")
        {
            // starpiece.parent = this.transform;
            other.GetComponent<Collider>().enabled = false;
            StartCoroutine(FadeIntensity(other.transform.GetChild(3).GetComponent<Light>()));
            followList.Add(other.transform);
            breadcrumbs.Add(other.transform.position);
            playerHasKey = true;
            PlayCollectionSound();
            keyObject = other.gameObject;
        }

        if (other.tag == "Lock" && playerHasKey)
        {
            PlayCollectionSound();
            collectedStarPieces++;
            StartCoroutine(Crossfade(keyObject.transform.GetChild(1),
                                     keyObject.transform.GetChild(0)));
            StartCoroutine(ShrinkLock(other.transform));
        }

        if (other.tag == "StarPiece")
        {
            PlayCollectionSound();

            // other.GetComponent<ObjectOrbit>().enabled = true;
            Transform starpiece = other.GetComponent<Transform>();

            // starpiece.parent = this.transform;
            other.GetComponent<Collider>().enabled = false;
            StartCoroutine(FadeIntensity(other.transform.GetChild(1).GetComponent<Light>()));
            followList.Add(other.transform);
            breadcrumbs.Add(other.transform.position);
            collectedStarPieces++;
            if (collectedStarPieces == 1)
            {
                Invoke("PlayFirstCollect", 1f);
            }
            else if (collectedStarPieces == 2)
            {
                Invoke("PlaySecondCollect", 1f);
            }
            else if (collectedStarPieces == 5)
            {
                Invoke("PlayFinalCollect", 1f);
            }

        }
        else if (other.tag == "Wind")
        {
            inSolarWind = true;
            solarWindArea = other.GetComponent<SolarWind>();
            solarWindArea.isActive = true;
            solarWindArea.windForce = solarWindPushForce;
            solarWindArea.astronautBody = cacheRigidbody;
            Invoke("PlayWindClip", 0.1f);
        }
        if (other.tag == "Portal")
        {
            LoadNextScene();
        }
    }
    void PlayWindClip()
    {
        if (windClips.Length == 0)
        {
            return;
        }
        if (Random.Range(0f, 1f) < 0.25f && Time.time - lastAudioPlaytime > 8f)
        {
            astronautAudio.PlayOneShot(windClips[Random.Range(0, windClips.Length)]);
            lastAudioPlaytime = Time.time;
        }
    }
    void PlayFirstCollect()
    {
        if (onFirstCollect != null && Time.time - lastAudioPlaytime > 8f)
        {
            astronautAudio.PlayOneShot(onFirstCollect);
            lastAudioPlaytime = Time.time;
        }
    }
    void PlaySecondCollect()
    {
        if (onSecondCollect != null && Time.time - lastAudioPlaytime > 8f)
        {
            astronautAudio.PlayOneShot(onSecondCollect);
            lastAudioPlaytime = Time.time;
        }
    }
    void PlayFinalCollect()
    {
        if (onFinalCollect != null && Time.time - lastAudioPlaytime > 8f)
        {
            astronautAudio.PlayOneShot(onFinalCollect);
            lastAudioPlaytime = Time.time;
        }
    }
    void PlayCollectionSound()
    {
        if (collectedStarPieces <= 4)
        {
            astronautAudio.PlayOneShot(collectStarPiece);
        }
        else
        {
            astronautAudio.PlayOneShot(collectFinalStarPiece);
        }
    }
    string GetSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
    void LoadNextScene()
    {
        Invoke("DelayedSceneLoad", 0);
    }

    void DelayedSceneLoad()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

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
        while (lightSource.intensity > 0f)
        {
            // Decrease the intensity by 0.001
            lightSource.intensity -= 0.001f;
            // But yield to other processes in the meantime
            yield return null;
        }
    }
    /// <summary>
    /// Fades the intensity of a light source down to 0.05 when started as
    /// coroutine
    /// </summary>
    /// <param name="lightSource">The light source to fade</param>
    IEnumerator Crossfade(Transform smallToBig, Transform bigToSmall)
    {
        while (smallToBig.localScale.x < 1f || bigToSmall.localScale.x >= 0.001)
        {
            if (bigToSmall.localScale.x <= 0.05)
            {
                bigToSmall.gameObject.SetActive(false);
            }
            smallToBig.localScale = Vector3.Lerp(smallToBig.localScale, Vector3.one, 0.01f);
            bigToSmall.localScale = Vector3.Lerp(bigToSmall.localScale, Vector3.zero, 0.01f);
            yield return null;
        }
    }
    IEnumerator ShrinkLock(Transform lockAsteroid)
    {
        while (lockAsteroid.localScale.x > 0)
        {
            if (lockAsteroid.localScale.x <= 0.001f)
            {
                lockAsteroid.gameObject.SetActive(false);
            }
            lockAsteroid.localScale = Vector3.Lerp(lockAsteroid.localScale, Vector3.zero, 0.01f);
            yield return null;
        }
    }
}
