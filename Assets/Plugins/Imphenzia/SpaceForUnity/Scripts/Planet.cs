// Planet C# Script (version: 1.6)
// SPACE UNITY - Space Scene Construction Kit
// http://www.imphenzia.com/space-for-unity
// (c) 2019 Imphenzia AB

// DESCRIPTION:
// Script for the rotational behaviour of planets.

// INSTRUCTIONS:
// This script is attached to the planet prefabs and rotation speed around its own axis can be configured.
// The SpaceSceneConstructionKit window will automatically configure random rotation speed.

// Version History
// 1.6  - New Imphenzia.SpaceForUnity namespace to replace SU_ prefix.
//      - Moved asset into Plugins/Imphenzia/SpaceForUnity for asset best practices.
// 1.02 - Prefixed with SU_Planet to avoid naming conflicts.
// 1.01 - Initial Release.

using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class Planet : MonoBehaviour
    {
        [Tooltip("The rotational vector (axis and speed) of the planet.")]
        public Vector3 planetRotation;

        // Private variables
        private Transform _cacheTransform;

        void Start()
        {
            // Cache reference to transform to improve performance
            _cacheTransform = transform;
        }

        void Update()
        {
            // Rotate the planet based on the rotational vector
            if (_cacheTransform != null)
            {
                _cacheTransform.Rotate(planetRotation * Time.deltaTime);
            }
        }
    }
}