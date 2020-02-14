/*  Camera Follow C# Script (version: 1.6)
	SPACE for UNITY - Space Scene Construction Kit
	https://www.imphenzia.com/space-for-unity
	(c) 2019 Imphenzia AB

    DESCRIPTION:
    Smooth camera follow script used to follow an object (Transform)

    INSTRUCTIONS:
    Attach this script to a camera (e.g. Main Camera) and specify which target Transform to
    follow.

    Version History
    1.6     - New Imphenzia.SpaceForUnity namespace to replace SU_ prefix.
            - Moved asset into Plugins/Imphenzia/SpaceForUnity for asset best practices.
    1.5     - Added ORBIT follow mode
    1.02    - Renamed to SU_CameraFollow to avoid naming conflicts.
    1.01    - Initial Release.
*/

using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class CameraFollow : MonoBehaviour
    {

        // Using UpdateMode you can select when the camera should be updated
        // Depending on your design camera jitter may occur which may be reduced if you change
        // the update mode. In the included demo the best result is achieved when camera is
        // updated in the FixedUpdate() function.

        public enum UpdateMode { FIXED_UPDATE, UPDATE, LATE_UPDATE }

        [Tooltip("To reduce camera jitter - camera can be updated in FixedUpdate, Update, or LateUpdate")]
        public UpdateMode updateMode = UpdateMode.FIXED_UPDATE;

        // Select the chase mode (chase behind target or moving spectator)
        // CHASE = smooth chase behind target at distance and height
        // SPECTATOR = smooth look at target from a chasing spectator position
        public enum FollowMode { CHASE, SPECTATOR, ORBIT }

        [Tooltip("whether camera should chase behind Transform or as a spectator that follows the Transform")]
        public FollowMode followMode = FollowMode.SPECTATOR;

        [Tooltip("Target transform for the camera to follow")]
        public Transform target;

        [Tooltip("Distance to follow from (this is the minimum distance, depending on damping the distance will increase at speed)")]
        public float distance = 60.0f;

        [Tooltip("Height over target in chase mode")]
        public float chaseHeight = 15.0f;

        [Tooltip("Smoothness of movement, lower value is smoother")]
        public float followDamping = 0.3f;

        [Tooltip("Smoothness for rotation, lower value is smoother")]
        public float lookAtDamping = 4.0f;

        [Tooltip("Optional key to Freeze camera movement while this key is pressed")]
        public KeyCode freezeKey = KeyCode.None;

        [Tooltip("Distance for camera orbit mode")]
        public float orbitDistance = 50.0f;

        [Tooltip("Speed for orbital movement around target")]
        public Vector2 orbitSpeed = new Vector2(70.0f, 70.0f);

        [Tooltip("Minimum orbit distance allowed")]
        public float orbitDistanceMin = 20f;

        [Tooltip("Maximum orbit distance allowed")]
        public float orbitDistanceMax = 200.0f;

        // Private variables
        private Vector2 _orbitPosition = new Vector2();        
        private Transform _cacheTransform;

        void Start()
        {
            // Cache reference to transform to increase performance
            _cacheTransform = transform;
            if (followMode == FollowMode.ORBIT)
            {
                _orbitPosition = new Vector2(transform.eulerAngles.y, transform.eulerAngles.x);
            }
        }

        void FixedUpdate()
        {
            if (updateMode == UpdateMode.FIXED_UPDATE) DoCamera();
        }
        void Update()
        {
            if (updateMode == UpdateMode.UPDATE) DoCamera();
        }
        void LateUpdate()
        {
            if (updateMode == UpdateMode.LATE_UPDATE) DoCamera();
        }

        void DoCamera()
        {
            // Return if no target is set
            if (target == null) return;

            Quaternion _lookAt;

            switch (followMode)
            {
                case FollowMode.SPECTATOR:
                    // Smooth lookat interpolation
                    _lookAt = Quaternion.LookRotation(target.position - _cacheTransform.position);
                    _cacheTransform.rotation = Quaternion.Lerp(_cacheTransform.rotation, _lookAt, Time.deltaTime * lookAtDamping);
                    // Smooth follow interpolation
                    if (!Input.GetKey(freezeKey))
                    {
                        if (Vector3.Distance(_cacheTransform.position, target.position) > distance)
                        {
                            _cacheTransform.position = Vector3.Lerp(_cacheTransform.position, target.position, Time.deltaTime * followDamping);
                        }
                    }
                    break;
                case FollowMode.CHASE:
                    if (!Input.GetKey(freezeKey))
                    {
                        // Smooth lookat interpolation
                        _lookAt = target.rotation;
                        _cacheTransform.rotation = Quaternion.Lerp(_cacheTransform.rotation, _lookAt, Time.deltaTime * lookAtDamping);
                        // Smooth follow interpolation
                        _cacheTransform.position = Vector3.Lerp(_cacheTransform.position, target.position - target.forward * distance + target.up * chaseHeight, Time.deltaTime * followDamping * 10);
                    }
                    break;
                case FollowMode.ORBIT:
                    if (!Input.GetKey(freezeKey))
                    {
                        // Smooth lookat interpolation
                        _orbitPosition.x += Input.GetAxis("Mouse X") * orbitSpeed.x * distance * 0.002f;
                        _orbitPosition.y -= Input.GetAxis("Mouse Y") * orbitSpeed.y * 0.2f;
                        _orbitPosition.y = Mathf.Clamp(_orbitPosition.y, -80.0f, 80.0f);
                        Quaternion _rot = Quaternion.Euler(_orbitPosition.y, _orbitPosition.x, 0);
                        orbitDistance = Mathf.Clamp(orbitDistance - Input.GetAxis("Mouse ScrollWheel") * 200, orbitDistanceMin, orbitDistanceMax);
                        transform.rotation = _rot;
                        transform.position = (_rot * new Vector3(0, 0, -orbitDistance)) + target.position;
                    }
                    break;
            }
        }
    }
}