using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatControl : MonoBehaviour
{
    public float gravityStrength = 98f;
    public float moveSpeed = 4f;
    public float rotationSpeed = 150f;
    public float gravityTurnSpeed = 0.2f;
    public float jumpHeight = 10f;
    private Rigidbody cacheRigidbody;
    private bool playerOnGround = true;
    private float lastJumpTime = 0f;


    // Start is called before the first frame update
    void Start()
    {
        cacheRigidbody = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        Physics.SphereCast(transform.position, 1f, Vector3.down, out RaycastHit hitInfo);
        if (hitInfo.distance > 10f)
        {
            print("Out");
            cacheRigidbody.AddForce(Vector3.down * gravityStrength);
        }
        else if (Time.time - lastJumpTime > 0.5f)
        {
            print("In");
            cacheRigidbody.velocity = Vector3.zero;
        }
        if (Input.GetButtonDown("Jump") && hitInfo.distance < 12f)
        {
            print("Jump");
            cacheRigidbody.AddForce(transform.up * 4000000 * jumpHeight * Time.deltaTime);
            lastJumpTime = Time.time;
        }
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        Vector3 astronautDown = -transform.up;
        Vector3 vectorToPlanet = Vector3.down;
        Quaternion toRotation = Quaternion.FromToRotation(astronautDown, vectorToPlanet) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, gravityTurnSpeed);
    }
}
