﻿using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectOrbit : MonoBehaviour
{
    public float speedMultiplier = 2;
    public float apoapsis = 4; // Apoapsis
    public float periapsis = 1; // Periapsis
    public float inclinationNode = 0; // Values between 0 and 2PI
    public float inclinationAngle = 0; // Values between -PI/2 and PI/2
    public float mass = 1;
    public float currentPos = 0;
    public Vector3 rotationAxis = new Vector3(1, 2, 3);
    public float rotationSpeed = 1.75f;
    public Transform focusPoint;

    private float semiMajorAxis;
    private float semiMinorAxis;
    private float eccentricity;
    private float posX = 0;
    private float posZ = 0;
    private float posY = 0;
    private float r;
    private float timer = 0; // Values between 0 and 2PI
    private float G = 6.67408f;

    private Vector3 prev;
    private Vector3 temp;

    struct orbitPoint
    {
        public Vector3 position;
        public float velocity;
    }
    void Start()
    {
        orbit = new orbitPoint[721];
        generateOrbit();
    }

    void Update()
    {
        moveSatellite();
        rotateSatellite();
    }

    orbitPoint[] orbit;

    void generateOrbit()
    {
        int index = 0;
        semiMajorAxis = (apoapsis + periapsis) / 2;
        semiMinorAxis = Mathf.Sqrt(apoapsis * periapsis);
        eccentricity = Mathf.Sqrt(1 - (semiMinorAxis * semiMinorAxis) / (semiMajorAxis * semiMajorAxis));

        float movOffset = ((apoapsis + periapsis) / 2) - periapsis;

        while (timer < Mathf.PI * 2)
        {
            posX = semiMajorAxis * Mathf.Cos(timer) - movOffset;
            posZ = semiMinorAxis * Mathf.Sin(timer);

            r = Mathf.Sqrt(posX * posX + posZ * posZ);

            float nPosX = posX * Mathf.Cos(inclinationAngle) - posY * Mathf.Sin(inclinationAngle);
            float nPosY = posY * Mathf.Cos(inclinationAngle) + posX * Mathf.Sin(inclinationAngle);

            float nPosX2 = nPosX * Mathf.Cos(inclinationNode) - posZ * Mathf.Sin(inclinationNode);
            float nPosZ = posZ * Mathf.Cos(inclinationNode) + nPosX * Mathf.Sin(inclinationNode);

            prev = temp;

            temp = new Vector3(nPosX2, nPosY, nPosZ);
            float tempVel = speedMultiplier * Mathf.Sqrt((G * mass) * ((2 / r) - (1 / semiMajorAxis)));

            orbitPoint tempOrbitPoint;
            tempOrbitPoint.position = temp;
            tempOrbitPoint.velocity = tempVel;

            orbit[index] = tempOrbitPoint;


            timer += (Mathf.PI * 2) / 720;
            index++;
        }
        timer = 0;
    }

    void moveSatellite()
    {
        try
        {
            int index1 = Mathf.FloorToInt(currentPos);
            int index2 = index1 + 1;
            if (index2 == 1)
            {
                print("loop");
            }

            float param = currentPos - index1;
            Vector3 currentPosition = (1 - param) * orbit[index1].position + param * (orbit[index2].position);
            transform.position = currentPosition + focusPoint.position;

            float currentVel = (1 - param) * orbit[index1].velocity + param * (orbit[index2].velocity);

            currentPos += currentVel;

            if (currentPos > 720)
            {
                currentPos = 0;
            }
        }
        catch
        {
            currentPos += 1;
            print("broken");
        }

    }

    void rotateSatellite()
    {
        transform.rotation *= Quaternion.Euler(rotationAxis.normalized * rotationSpeed);
    }


}
