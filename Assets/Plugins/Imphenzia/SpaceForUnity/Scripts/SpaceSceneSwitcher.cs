// INSTRUCTIONS:
// Drag the SpaceSceneSwitcher Prefab (SpaceUnity/Prefabs/Tools/SpaceSceneSwitcher) into your scene,
// or drag this script onto a game object in your scene.
// Using the inspector, configure the array Space Scenes by dragging Space Scenes prefabs onto the array.
// Set mode to LOAD_ALL_AT_STARTUP or LOAD_ON_DEMAND depending on when you want to instantiate the space scene prefabs
// Set sceneIndexLoadFirst to the index of the element/prefab in the array you want to be loaded once initialied

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Imphenzia.SpaceForUnity
{
    public class SpaceSceneSwitcher : MonoBehaviour
    {
        // Modes available:
        //  LOAD_ALL_AT_STARTUP = load all space scene prefabs at startup and instantiate (but disable) them
        //  LOAD_ON_DEMAND (default) = load space scene prefabs and instantiate when needed (destroy previous instance)
        public enum Mode { LOAD_ALL_AT_STARTUP, LOAD_ON_DEMAND }
        // The mode which can be configured in the inspector
        public Mode mode = Mode.LOAD_ON_DEMAND;
        // The static mode which cannot be configured through inspector. The static value is set to the same value as the
        // value for non-static at startup.
        static public Mode staticMode;
        // Array of Space Scene prefabs, configured in the inspector, that can be switched between
        public GameObject[] spaceScenes;
        // Static list of Space Scene prefabs (static variables cannot be configured through the inspector so the
        // values are transferred from spaceScenes to this static list upon start.
        static public List<GameObject> staticSpaceScenes = new List<GameObject>();
        //static public GameObject[] staticSpaceScenes = new GameObject[32];
        // Hashtable containing instantiated scenes when LOAD_ALl_AT_STARTUP mode is used
        static public Hashtable hashedSpaceScenes = new Hashtable();
        // Reference to the current space scene so we can destroy it if mode is LOAD_ON_DEMAND as we switch mode
        static public GameObject currentSpaceScene;
        // Space Scene Prefab to instantiate/activate first by index in array once initiated
        public int sceneIndexLoadFirst = 0;


        // Configure as the script is started
        void Start()
        {
            // Set the staticMode to mode configured in Inspector
            staticMode = mode;

            // If the Space Scenes array has been configured in inspector...
            if (spaceScenes.Length > 0)
            {
                // And if the static space scenes list has not been populated yet...
                if (staticSpaceScenes.Count == 0)
                {
                    // Loop through all scenes configured in the inspector
                    for (int _i = 0; _i < spaceScenes.Length; _i++)
                    {
                        // Add the space scene prefab to the static list
                        staticSpaceScenes.Add(spaceScenes[_i]);
                    }
                }
            }
            else
            {
                Debug.LogError("No Space Scene Prefabs configured for the Space Scene array. Populate array in the inspector with Space Scene prefabs " +
                    "from the Project window. Note! You have to create Prefabs(!) of the space scenes - you cannot assign Unity Scenes to the array.");
            }

            // If mode is LOAD_ALL_AT_STARTUP all space scenes prefabs should be instantiated (but disabled)...
            if (staticMode == Mode.LOAD_ALL_AT_STARTUP)
            {
                // Clear the hashtable of instantiated prefabs, if there are any.
                hashedSpaceScenes.Clear();
                // Loop through the space scene prefabs in the array configured in the inspector
                foreach (GameObject _spaceScene in spaceScenes)
                {
                    // Instantiate the space scenes as game objects
                    GameObject _instantiated = (GameObject)GameObject.Instantiate(_spaceScene, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
                    // Add the instanitated game objects to the hashtable
                    hashedSpaceScenes.Add(_spaceScene.name, _instantiated);
                    // Disable the instantiated object to hide it
                    SetActive(_instantiated, false);
                }
            }

            if (sceneIndexLoadFirst >= spaceScenes.Length)
            {
                // sceneIndexLoadFirst is greater than the array size... that's no good, load the first instead
                Debug.LogWarning("Scene Index Load First value is greater than the number of Space Scene prefabs in the array. " +
                    "Loading scene with index 0 instead.");
                sceneIndexLoadFirst = 0;
                Switch(sceneIndexLoadFirst);

            }
            else
            {
                Switch(sceneIndexLoadFirst);
            }
        }

        /// <summary>
        /// Switch between Space Scene prefabs by array index (int)
        /// </summary>
        /// <param name='_arrayIndex'>
        ///  The integer array index of the space scene prefab to switch to
        /// </param>
        static public void Switch(int _arrayIndex)
        {
            if (staticSpaceScenes.Count > 0)
            {
                Switch(staticSpaceScenes[_arrayIndex].name);
            }
        }
        /// <summary>
        /// Switch between Space Scene prefabs
        /// </summary>
        /// <param name='sceneName'>
        ///  The name (case sensitive) of the space scene prefab to be instantiated / enabled
        /// </param>
        static public void Switch(string sceneName)
        {
            // TODO: Oh my god this is some awful looking code. Future Jesse,
            //       when you have time, please fix this so it's not 80 nested
            //       if statements.
            // Loop through the space scenes configured in the inspector (which have been copied to the static list)
            for (int i = 0; i < staticSpaceScenes.Count; i++)
            {
                // Reference the space scene prefab from the list
                GameObject spaceScene = staticSpaceScenes[i];
                // If the space scene is not null...
                if (spaceScene != null)
                {
                    // ...and the space scene name matches the space scene we want to switch to...
                    if (spaceScene.name == sceneName)
                    {
                        // ...and if the mode was set to LOAD_ALL_AT STARTUP...
                        if (staticMode == Mode.LOAD_ALL_AT_STARTUP)
                        {
                            // ...and the hashtable entry for the space scene prefab name is not null...
                            if (hashedSpaceScenes[sceneName] != null)
                            {
                                // We need to flag if we found and enabled the space scene game object
                                bool found = false;
                                // Loop through all the entries in the hashtable...
                                foreach (DictionaryEntry entry in hashedSpaceScenes)
                                {
                                    // if the hashtable entry is the scene we want to switch to...
                                    if (entry.Key.ToString() == sceneName)
                                    {
                                        // Set the space scene to active
                                        SetActive((GameObject)entry.Value, true);
                                        found = true;
                                    }
                                    else
                                    {
                                        // This is not the scene we want to switch to, make sure it is disabled
                                        SetActive((GameObject)entry.Value, false);
                                    }
                                }
                                // If the instantiated space scene game object was found and enabled, return to avoid throwing the error below
                                if (found) return;
                            }
                        }
                        else
                        {
                            // mode was set to LOAD_ON_DEMAND
                            // Destroy the current instantiated space scene
                            Destroy(currentSpaceScene);
                            // instantiate the new space scene
                            currentSpaceScene = (GameObject)GameObject.Instantiate(spaceScene, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
                            return;
                        }
                    }
                }
            }
            Debug.LogWarning("Tried to switch to a space scene named " + sceneName + " but the scene was not found. " +
                "Ensure that you configured the array on the SpaceSceneSwitcher prefab correctly and that you typed the name " +
                "of the space scene prefab correctly (case sensitive) for the Switch function call");
        }

        // Since SetActiveRecursively has been deprecated this function performs game object
        // activation correctly based regardless of Unity version.
        public static void SetActive(GameObject currentObject, bool isActive)
        {
            if (currentObject != null)
            {
                currentObject.SetActive(isActive);
            }
        }
    }
}
