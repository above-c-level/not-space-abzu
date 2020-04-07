using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public GameObject closestPlanet;
    public GameObject playerPlaceHolder;
    public float speed = 4;
    public float JumpHeight = 1.2f;

    public float gravity = 1000f;

    private bool playerOnGround = false;
    private bool outsideSOI;
    private float distanceToGround;
    private Vector3 Groundnormal;
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
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * speed;

        transform.Translate(x, 0, z);

        //Local Rotation
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, 150 * Time.deltaTime, 0);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -150 * Time.deltaTime, 0);
        }




        //GroundControl

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(transform.position, -transform.up, out hit, 10))
        {
            distanceToGround = hit.distance;
            Groundnormal = hit.normal;

            if (distanceToGround <= 0.3f)
            {
                playerOnGround = true;

            }
            else
            {
                playerOnGround = false;
            }

        }


        //GRAVITY and ROTATION

        Vector3 gravDirection = (closestPlanet.transform.position - transform.position).normalized;

        if (playerOnGround == false && !outsideSOI)
        {
            rb.AddForce(gravDirection * gravity);
        }

        Quaternion toRotation = Quaternion.FromToRotation(transform.up, Groundnormal) * transform.rotation;
        transform.rotation = toRotation;
    }

    /// <summary>
    /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// </summary>
    void FixedUpdate()
    {
        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && playerOnGround)
        {
            rb.AddForce(transform.up * 40000 * JumpHeight * Time.deltaTime);
            playerOnGround = false;
        }
        if (outsideSOI)
        {
            Vector3 forceToApply = Vector3.zero;

            for (int i = 0; i < allPlanets.Length; i++)
            {
                GameObject planet = allPlanets[i];
                print("Applying gravity for planet " + i);
                // Squared distance between player and planets
                float dSquared = Vector3.SqrMagnitude(transform.position - planet.transform.position);
                // Apply gravity to transform
                Vector3 gravDirection = (planet.transform.position - transform.position).normalized;
                forceToApply += (6.67408f * gravity * gravDirection * 50) / dSquared;
            }
            rb.AddForce(forceToApply);
        }
    }

    /// <summary>
    /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    void OnTriggerExit(Collider other)
    {
        print("leaving SOI");
        outsideSOI = true;
    }

    //CHANGE PLANET

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag != "Planet")
        {
            return;
        }
        print("entering SOI");
        outsideSOI = false;
        if (collision.transform != closestPlanet.transform)
        {
            closestPlanet = collision.transform.gameObject;

            Vector3 gravDirection = (transform.position - closestPlanet.transform.position).normalized;

            Quaternion toRotation = Quaternion.FromToRotation(transform.up, gravDirection) * transform.rotation;
            transform.rotation = toRotation;

            rb.velocity = Vector3.zero;
            rb.AddForce(gravDirection * gravity);


            playerPlaceHolder.GetComponent<TutorialPlayerPlaceholder>().NewPlanet(closestPlanet);
        }
    }




}
