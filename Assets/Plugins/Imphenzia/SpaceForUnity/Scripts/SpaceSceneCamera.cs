using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    [ExecuteInEditMode]
    public class SpaceSceneCamera : MonoBehaviour
    {
        [Tooltip("Reference to parent camera to sync rotation and FOV (field of view) - defaults to main camera.")]
        public Camera parentCamera;

        [Tooltip("Whether or not SpaceCamera should change FOV if parent camera FOV is changed")]
        public bool inheritFOV = true;

        [Tooltip("Relative speed if you wish to move within the space scene. Use with caution as you will go through planets and beyond nebulas unless you create boundaries yourself.")]
        public float relativeSpeed = 0.0f;

        [Tooltip("Specify which object should be relative in focus for camera rotations, usually the Main Camera. If not specified, the rotation may look fake as if it was rotating thousands of km away.")]
        public Transform relativeFocusObject = null;

        // Private variables
        private Vector3 originalPosition;
        private Transform cachedTransform;
        private Transform transformParentCamera;
        private Camera cachedCamera;
        private Vector3 relativeObjectPreviousPosition;


        // The space camera must have a reference to a parent camera so it knows how to rotate the background
        // This script allows you to specify a parent camera (parentCamera) which will act as reference
        // If you do not specify a camera, the script will assume you are using the main camera and select that as reference
        void Awake()
        {
            // Cache the transform to increase performance
            cachedTransform = transform;

            if (parentCamera == null)
            {
                // No parent camera has been set, assume that main camera is used
                if (Camera.main != null)
                {
                    // Set parent camera to main camera.
                    parentCamera = Camera.main;
                }
                else
                {
                    // No main camera found
                    Debug.LogWarning("You have not specified a parent camera to the space background camera and there is no main camera in your scene. " +
                                      "The space scene will not rotate properly unless you set the parentCamera in this script.");
                }
            }

            if (parentCamera != null)
            {
                // Cache the transform to the parent camera to increase performance
                transformParentCamera = parentCamera.transform;
            }

            // Cache the Camera component so we don't have to do it frequently in the update method
            cachedCamera = gameObject.GetComponent<Camera>();
            if (relativeFocusObject == null)
            {
                relativeFocusObject = transformParentCamera;
            }
            relativeObjectPreviousPosition = relativeFocusObject.position;
        }

        void LateUpdate()
        {
            if (transformParentCamera != null)
            {
                // Update the rotation of the space camera so the background rotates
                cachedTransform.rotation = transformParentCamera.rotation;

                // Inherit field of view of the main camera if applicable
                if (inheritFOV)
                {
                    cachedCamera.fieldOfView = parentCamera.fieldOfView;
                }
            }

            // Update the relative position of the space camera so you can travel in the space scene if necessary
            // Note! You will fly out of bounds of the space scene if your relative speed is high unless you restrict the movement in your own code.
            Vector3 relativeDelta = relativeFocusObject.position - relativeObjectPreviousPosition;
            relativeObjectPreviousPosition = relativeFocusObject.position;

            if (relativeSpeed > 0.00001f)
            {
                Move(relativeDelta * relativeSpeed);
            }
        }

        // Public method that you can call to move the space scene camera by a vector. SU_TravelWarp does this do move within the virtual space scene.
        public void Move(Vector3 _vector)
        {
            cachedTransform.position += _vector;
        }
    }
}
