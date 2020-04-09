using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPlayerPlaceholder : MonoBehaviour
{

    public GameObject Player;
    public GameObject Planet;



    // Update is called once per frame
    void Update()
    {
       //SMOOTH


        //POSITION
        transform.position = Vector3.Lerp(transform.position, Player.transform.position, 0.5f);

        Vector3 gravDirection = (transform.position - Planet.transform.position).normalized;


        //ROTATION
        Quaternion toRotation = Quaternion.FromToRotation(transform.up, gravDirection) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              toRotation,
                                              0.05f * Mathf.Abs(Quaternion.Dot(transform.rotation, toRotation)));

    }


    public void NewPlanet(GameObject newPlanet)
    {
        Planet = newPlanet;

    }


}
