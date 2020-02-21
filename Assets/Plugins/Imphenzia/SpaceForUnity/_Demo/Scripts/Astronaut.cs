/*  Spaceship C# Script (version: 1.6)
    SPACE for UNITY - Space Scene Construction Kit
	https://www.imphenzia.com/space-for-unity
	(c) 2019 Imphenzia AB

    DESCRIPTION:
    Thruster, steering and weapon control script for Spaceship prefab.

    INSTRUCTIONS:
    This script is attached to the Spaceship demo prefab. Configure parameters to suit your needs.

    HINTS:
    The propulsion force of the thruster is configured for each attached thruster in the Thruster script.

    Version History
    1.6     - New Imphenzia.SpaceForUnity namespace to replace SU_ prefix.
            - Moved asset into Plugins/Imphenzia/SpaceForUnity for asset best practices.
    1.5     - Added support for SU_TravelWarp for the spaceship to travel fast in a scene with a visual warp effect.
    1.06    - Updated for Unity 5.5, removed deprecated code.
    1.02    - Prefixed with SU_Spaceship to avoid naming conflicts.
    1.01    - Initial Release.
*/

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

        // Private variables
        private Rigidbody cacheRigidbody;
        private TravelWarp travelWarp;
        private float orgWarpSpeed;
        private float orgWarpStrength;

        void Start()
        {
            // Ensure that the thrusters in the array have been linked properly
            foreach (Thruster thruster in thrusters)
                if (thruster == null)
                    Debug.LogError("Thruster array not properly configured. Attach thrusters to the game object and link them to the Thrusters array.");

            // Cache reference to rigidbody to improve performance
            cacheRigidbody = GetComponent<Rigidbody>();
            if (cacheRigidbody == null)
                Debug.LogError("Spaceship has no rigidbody - the thruster scripts will fail. Add rigidbody component to the spaceship.");

            // If there is a SU_TravelWarp component on this ship, grab the reference to it
            if (gameObject.GetComponent<TravelWarp>() != null)
                travelWarp = gameObject.GetComponent<TravelWarp>();

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
            // In the physics update...
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
    }
}