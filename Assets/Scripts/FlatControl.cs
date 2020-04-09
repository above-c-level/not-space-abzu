using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatControl : MonoBehaviour
{
    public float gravityStrength = 10000f;
    public float moveSpeed = 4f;
    public float rotationSpeed = 150f;
    public float gravityTurnSpeed = 0.2f;
    public float jumpHeight = 10f;
    private Rigidbody cacheRigidbody;
    private bool playerOnGround = true;

    // Start is called before the first frame update
    void Start()
    {
        cacheRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        cacheRigidbody.AddForce(Vector3.down * gravityStrength);
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        Vector3 astronautDown = -transform.up;
        Vector3 vectorToPlanet = Vector3.down;
        Quaternion toRotation = Quaternion.FromToRotation(astronautDown, vectorToPlanet) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, gravityTurnSpeed);
        if (Input.GetButton("Jump"))
        {
            if (playerOnGround)
            {
                cacheRigidbody.AddForce(transform.up * 4000000 * jumpHeight * Time.deltaTime);
                playerOnGround = false;
            }
        }
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
}
