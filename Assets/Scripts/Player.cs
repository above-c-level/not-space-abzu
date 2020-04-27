using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public GameObject closestPlanet;
    public TutorialPlayerPlaceholder playerPlaceHolder;
    public float moveSpeed = 4f;
    public float rotationSpeed = 150f;
    public float JumpHeight = 1.2f;
    public float gravity = 1000f;
    public float gravityTurnSpeed;
    public float parabolicDistancePull;

    private bool playerOnGround = false;
    private bool outsideSOI;
    private float distanceToGround;
    private Vector3 groundNormal;
    private Rigidbody rb;
    private GameObject[] allPlanets;


    // Start is called before the first frame update
    void Start()
    {
        allPlanets = GameObject.FindGameObjectsWithTag("Planet");
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        outsideSOI = false;
    }

    // Update is called once per frame
    void Update()
    {
        //MOVEMENT
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        //GroundControl

        // RaycastHit hit = new RaycastHit();
        // if (Physics.Raycast(transform.position, -transform.up, out hit, 100))
        // {
        //     distanceToGround = hit.distance;
        //     groundNormal = hit.normal;
        //     if (distanceToGround <= 0.3f)
        //     {
        //         playerOnGround = true;

        //     }
        //     else
        //     {
        //         playerOnGround = false;
        //     }
        // }

        // Jump
        if (Input.GetButton("Jump"))
        {
            if (playerOnGround)
            {
                rb.AddForce(transform.up * 4000000 * JumpHeight * Time.deltaTime);
                playerOnGround = false;
            }
        }

        // Get the planet that is closest to the player
        Transform championPlanet = transform;
        float championDistance = float.PositiveInfinity;
        for (int i = 0; i < allPlanets.Length; i++)
        {
            float squaredDistance = Vector3.SqrMagnitude(transform.position - allPlanets[i].transform.position);
            if (squaredDistance < championDistance)
            {
                championPlanet = allPlanets[i].transform;
                championDistance = squaredDistance;
            }
        }

        Vector3 astronautDown = -transform.up;
        Vector3 vectorToPlanet = (championPlanet.position - transform.position);
        Quaternion toRotation = Quaternion.FromToRotation(astronautDown, vectorToPlanet) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, gravityTurnSpeed);
        if (closestPlanet.transform.position != championPlanet.transform.position)
        {
            closestPlanet = championPlanet.gameObject;
            playerPlaceHolder.NewPlanet(closestPlanet);
        }
    }

    /// <summary>
    /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// </summary>
    void FixedUpdate()
    {
        Vector3 forceToApply = Vector3.zero;

        for (int i = 0; i < allPlanets.Length; i++)
        {
            GameObject planet = allPlanets[i];
            // Squared distance between player and planets
            float dSquared = Vector3.SqrMagnitude(transform.position - planet.transform.position);
            // Apply gravity to transform
            Vector3 gravDirection = (planet.transform.position - transform.position).normalized;
            Vector3 parabolaPull = Vector3.zero;
            if (dSquared > 25000f)
            {
                float coefficient = parabolicDistancePull * parabolicDistancePull;
                parabolaPull = (coefficient * (dSquared - 25000f)) * gravDirection;
            }

            forceToApply += ((6.67408f * gravity * gravDirection * 50) / dSquared) + parabolaPull;
            // print("Force from planet " + i + ": " + forceToApply);
        }
        rb.AddForce(forceToApply);
    }
    /// <summary>
    /// OnCollisionEnter is called when this collider/rigidbody has begun
    /// touching another rigidbody/collider.
    /// </summary>
    /// <param name="other">The Collision data associated with this collision.</param>
    void OnCollisionEnter(Collision other)
    {
        playerOnGround = true;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag != "Planet")
        {
            return;
        }
        // print("entering SOI");
        // outsideSOI = false;
        if (collision.transform != closestPlanet.transform)
        {
            closestPlanet = collision.transform.gameObject;

            Vector3 gravDirection = (transform.position - closestPlanet.transform.position).normalized;

            Quaternion toRotation = Quaternion.FromToRotation(transform.up, gravDirection) * transform.rotation;
            transform.rotation = toRotation;

            rb.velocity = Vector3.zero;
            rb.AddForce(gravDirection * gravity);


            playerPlaceHolder.NewPlanet(closestPlanet);
        }
    }




}
