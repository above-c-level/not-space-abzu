/*  SU_AsteroidFieldEditor C# Script (version: 1.6)
    SPACE for UNITY - Space Scene Construction Kit
    https://www.imphenzia.com/space-for-unity
    (c) 2019 Imphenzia AB

    DESCRIPTION
    This is an editor script (must reside in the /Editor project folder.) The purpose of
    the script is to improve presentation and configuration of the AsteroidField.cs script
    used to create asteroid fields.

    INSTRUCTIONS
    You don't need to do anything with this script, it will automatically detect objects
    that use the C# script AsteroidField and override the inspector to simplify configuration
    of the script parameters.

    Version History
    1.6     - New Imphenzia.SpaceForUnity namespace to replace SU_ prefix.
            - Moved asset into Plugins/Imphenzia/SpaceForUnity for asset best practices.
    1.5     - Increased upper limit from 3000 to 10000 asteroids. Performance is much better since no alpha fading
              is enabled (replaced by scaling) and the GPU does most of the hard work.
            - Minor changes relating to wording of fading/scaling asteroids.
    1.03    - Removed deprecated EditorGUIUtility.LookLikeInspector() and EditorGUIUtility.LookLikeControls()
            - Changed deprecated Camera.mainCamera to Camera.main
    1.02    - Prefixed with SU_AsteroidFieldEditor to avoid naming conflicts.
    0.8     - Initial Release.
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AsteroidField))]
public class AsteroidFieldEditor : Editor
{
    // Range Values Configuration
    private int displayMinAsteroidCount = 0;
    private int displayMaxAsteroidCount = 10000;
    private int displayMinRange = 10;
    private int displayMaxRange = 100000;

    // Warning Threshholds
    private int warningHighAsteroidCount = 2500;

    // Serialized Object
    SerializedObject myTarget;

    // Serialized Properties
    SerializedProperty polyCount;
    SerializedProperty polyCountCollider;
    SerializedProperty maxAsteroids;
    SerializedProperty respawnIfOutOfRange;
    SerializedProperty respawnDestroyedAsteroids;
    SerializedProperty range;
    SerializedProperty distanceSpawn;
    SerializedProperty fadeAsteroids;
    SerializedProperty distanceFade;
    SerializedProperty minAsteroidScale;
    SerializedProperty maxAsteroidScale;
    SerializedProperty scaleMultiplier;
    SerializedProperty minAsteroidRotationLimit;
    SerializedProperty maxAsteroidRotationLimit;
    SerializedProperty rotationSpeedMultiplier;
    SerializedProperty minAsteroidVelocityLimit;
    SerializedProperty maxAsteroidVelocityLimit;
    SerializedProperty velocityMultiplier;
    SerializedProperty isRigidbody;
    SerializedProperty mass;
    SerializedProperty minAsteroidAngularVelocity;
    SerializedProperty maxAsteroidAngularVelocity;
    SerializedProperty angularVelocityMultiplier;
    SerializedProperty minAsteroidVelocity;
    SerializedProperty maxAsteroidVelocity;

    // Temporary variables since properties can't be modified directly when using Ref and/or Out paremeters
    private float minScale;
    private float maxScale;
    private float minRotationSpeed;
    private float maxRotationSpeed;
    private float minvelocity;
    private float maxvelocity;
    private float minAngularVelocity;
    private float maxAngularVelocity;
    private float minVelocity;
    private float maxVelocity;

    // Bool display collapse/expand section helpers
    private bool showPrefabs;
    private bool showWeights;

    void OnEnable()
    {
        // Reference the serialized object (instance of AsteroidField.cs)
        myTarget = new SerializedObject(target);

        // Find and reference the properties of the target object
        polyCount = myTarget.FindProperty("polyCount");
        polyCountCollider = myTarget.FindProperty("polyCountCollider");
        maxAsteroids = myTarget.FindProperty("maxAsteroids");
        respawnIfOutOfRange = myTarget.FindProperty("respawnIfOutOfRange");
        respawnDestroyedAsteroids = myTarget.FindProperty("respawnDestroyedAsteroids");
        range = myTarget.FindProperty("range");
        distanceSpawn = myTarget.FindProperty("distanceSpawn");
        fadeAsteroids = myTarget.FindProperty("fadeAsteroids");
        distanceFade = myTarget.FindProperty("distanceFade");
        minAsteroidScale = myTarget.FindProperty("minAsteroidScale");
        maxAsteroidScale = myTarget.FindProperty("maxAsteroidScale");
        scaleMultiplier = myTarget.FindProperty("scaleMultiplier");
        minAsteroidRotationLimit = myTarget.FindProperty("minAsteroidRotationLimit");
        maxAsteroidRotationLimit = myTarget.FindProperty("maxAsteroidRotationLimit");
        rotationSpeedMultiplier = myTarget.FindProperty("rotationSpeedMultiplier");
        minAsteroidVelocityLimit = myTarget.FindProperty("minAsteroidVelocityLimit");
        maxAsteroidVelocityLimit = myTarget.FindProperty("maxAsteroidVelocityLimit");
        velocityMultiplier = myTarget.FindProperty("velocityMultiplier");
        isRigidbody = myTarget.FindProperty("isRigidbody");
        minAsteroidAngularVelocity = myTarget.FindProperty("minAsteroidAngularVelocity");
        maxAsteroidAngularVelocity = myTarget.FindProperty("maxAsteroidAngularVelocity");
        angularVelocityMultiplier = myTarget.FindProperty("angularVelocityMultiplier");
        minAsteroidVelocity = myTarget.FindProperty("minAsteroidVelocity");
        maxAsteroidVelocity = myTarget.FindProperty("maxAsteroidVelocity");
        velocityMultiplier = myTarget.FindProperty("velocityMultiplier");
        mass = myTarget.FindProperty("mass");

    }

    // Override the OnInspectorGUI and present these EditorGUI gadgets instead of the default ones
    public override void OnInspectorGUI()
    {
        // Update the serialized object
        myTarget.Update();

        // Present inspector GUI gadgets/objects and modify AsteroidField.cs instances with configured values
        maxAsteroids.intValue = EditorGUILayout.IntSlider("Number of Asteroids", maxAsteroids.intValue, displayMinAsteroidCount, displayMaxAsteroidCount);
        if (maxAsteroids.intValue > warningHighAsteroidCount)
        {
            EditorGUILayout.LabelField("Warning! Many asteroids may impact performance! Consider smaller range and fewer asteroids instead.", EditorStyles.wordWrappedMiniLabel);
        }
        range.floatValue = EditorGUILayout.Slider("Range", range.floatValue, displayMinRange, displayMaxRange);
        if (range.floatValue > Camera.main.farClipPlane)
        {
            EditorGUILayout.LabelField("Warning! Main camera clipping plane is closer than asteroid range.", EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.LabelField("Range is distance from the center to the edge of the asteroid field. If the transform of the AsteroidField moves, asteroids " +
            "that become out of range will respawn to a new location at spawn distance of range.", EditorStyles.wordWrappedMiniLabel);
        respawnIfOutOfRange.boolValue = EditorGUILayout.Toggle("Respawn if Out of Range", respawnIfOutOfRange.boolValue);
        EditorGUILayout.LabelField("Note: Respawn if out of range must be enabled for endless/infinite asteroid fields", EditorStyles.wordWrappedMiniLabel);
        respawnDestroyedAsteroids.boolValue = EditorGUILayout.Toggle("Respawn if Destroyed", respawnDestroyedAsteroids.boolValue);
        EditorGUILayout.Separator();
        distanceSpawn.floatValue = EditorGUILayout.Slider("Spawn at % of Range", distanceSpawn.floatValue, 0.0f, 1.0f);
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Asteroid Scale (Min/Max Range)", EditorStyles.boldLabel);
        minScale = minAsteroidScale.floatValue;
        maxScale = maxAsteroidScale.floatValue;
        GUIContent scaleContent = new GUIContent(string.Format("Min:{0:F1}, Max:{1:F1}", minScale, maxScale));
        EditorGUILayout.MinMaxSlider(scaleContent, ref minScale, ref maxScale, 0.1f, 1.0f);
        minAsteroidScale.floatValue = minScale;
        maxAsteroidScale.floatValue = maxScale;
        scaleMultiplier.floatValue = EditorGUILayout.FloatField("Scale Multiplier", scaleMultiplier.floatValue);
        EditorGUILayout.Separator();

        // Rigidbody or non-rigidbody Asteroids
        isRigidbody.boolValue = EditorGUILayout.Toggle("Is Rigidbody", isRigidbody.boolValue);
        if (isRigidbody.boolValue)
        {
            mass.floatValue = EditorGUILayout.FloatField("Mass (scales with size)", mass.floatValue);
            EditorGUILayout.LabelField("Asteroid Angular Velocity (Min/Max Range)", EditorStyles.boldLabel);
            minAngularVelocity = minAsteroidAngularVelocity.floatValue;
            maxAngularVelocity = maxAsteroidAngularVelocity.floatValue;
            GUIContent rotationContent = new GUIContent(string.Format("Min:{0:F1}, Max:{1:F1}", minAngularVelocity, maxAngularVelocity));
            EditorGUILayout.MinMaxSlider(rotationContent, ref minAngularVelocity, ref maxAngularVelocity, 0.0f, 1.0f);
            minAsteroidAngularVelocity.floatValue = minAngularVelocity;
            maxAsteroidAngularVelocity.floatValue = maxAngularVelocity;
            angularVelocityMultiplier.floatValue = EditorGUILayout.FloatField("Rotation Speed Multiplier", angularVelocityMultiplier.floatValue);

            EditorGUILayout.LabelField("Asteroid Velocity (Min/Max Range)", EditorStyles.boldLabel);
            minVelocity = minAsteroidVelocity.floatValue;
            maxVelocity = maxAsteroidVelocity.floatValue;
            GUIContent driftContent = new GUIContent(string.Format("Min:{0:F1}, Max:{1:F1}", minVelocity, maxVelocity));
            EditorGUILayout.MinMaxSlider(driftContent, ref minVelocity, ref maxVelocity, 0.0f, 1.0f);
            minAsteroidVelocity.floatValue = minVelocity;
            maxAsteroidVelocity.floatValue = maxVelocity;
            velocityMultiplier.floatValue = EditorGUILayout.FloatField("Drift Speed Multiplier", velocityMultiplier.floatValue);
        }
        else
        {
            EditorGUILayout.LabelField("Asteroid Rotation Speed (Min/Max Range)", EditorStyles.boldLabel);
            minRotationSpeed = minAsteroidRotationLimit.floatValue;
            maxRotationSpeed = maxAsteroidRotationLimit.floatValue;
            GUIContent rotationContent = new GUIContent(string.Format("Min:{0:F1}, Max:{1:F1}", minRotationSpeed, maxRotationSpeed));
            EditorGUILayout.MinMaxSlider(rotationContent, ref minRotationSpeed, ref maxRotationSpeed, 0.0f, 1.0f);
            minAsteroidRotationLimit.floatValue = minRotationSpeed;
            maxAsteroidRotationLimit.floatValue = maxRotationSpeed;
            rotationSpeedMultiplier.floatValue = EditorGUILayout.FloatField("Rotation Speed Multiplier", rotationSpeedMultiplier.floatValue);

            EditorGUILayout.LabelField("Asteroid Drift Speed (Min/Max Range)", EditorStyles.boldLabel);
            minvelocity = minAsteroidVelocityLimit.floatValue;
            maxvelocity = maxAsteroidVelocityLimit.floatValue;
            GUIContent driftContent = new GUIContent(string.Format("Min:{0:F1}, Max:{1:F1}", minvelocity, maxvelocity));
            EditorGUILayout.MinMaxSlider(driftContent, ref minvelocity, ref maxvelocity, 0.0f, 1.0f);
            minAsteroidVelocityLimit.floatValue = minvelocity;
            maxAsteroidVelocityLimit.floatValue = maxvelocity;
            velocityMultiplier.floatValue = EditorGUILayout.FloatField("Drift Speed Multiplier", velocityMultiplier.floatValue);
        }
        EditorGUILayout.Separator();

        // Visual Settings
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        fadeAsteroids.boolValue = EditorGUILayout.Toggle("Fade Asteroids", fadeAsteroids.boolValue);
        if (fadeAsteroids.boolValue)
        {
            distanceFade.floatValue = EditorGUILayout.Slider("Fade %", distanceFade.floatValue, 0.5f, 0.98f);
            EditorGUILayout.LabelField("Visibility is 1.0 at distanceFade*distanceSpawn*range and " +
            "gradually fades out to 0.0 at distanceSpawn*range.", EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.Separator();

        // Asteroid Mesh Quality
        EditorGUILayout.LabelField("Asteroid Mesh Quality", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(polyCount);
        EditorGUILayout.PropertyField(polyCountCollider);
        if (polyCountCollider.enumValueIndex != (int)Asteroid.ChildrenContainer.STANDALONE)
        {
            EditorGUILayout.LabelField("Warning! Using detailed collider meshes may heavily impact performance or raise errors if the mesh is too detailed.", EditorStyles.wordWrappedMiniLabel);
        }

        // Asteroid Prefab (array of asteroid shapes the asteroid field should randomly consist of)
        EditorGUILayout.LabelField("Debris Prefabs", EditorStyles.boldLabel);
        showPrefabs = EditorGUILayout.Foldout(showPrefabs, "Prefabs");
        if (showPrefabs)
        {
            ArrayGUI(myTarget, "prefabAsteroids");
        }
        EditorGUILayout.Separator();

        // Asteroid Materials (array of asteroid materials the asteroid field should randomly consist of)
        // The random selection is weighted between common and rare materials.
        EditorGUILayout.LabelField("Debris Weights", EditorStyles.boldLabel);
        showWeights = EditorGUILayout.Foldout(showWeights, "Weights");
        if (showWeights)
        {
            ArrayGUI(myTarget, "asteroidWeights");
        }

        // Apply the modified properties
        myTarget.ApplyModifiedProperties();
    }


    // Function to overide and display custom object array in inspector
    void ArrayGUI(SerializedObject obj, string name)
    {
        int size = obj.FindProperty(name + ".Array.size").intValue;
        int newSize = EditorGUILayout.IntField("Size", size);
        if (newSize != size) obj.FindProperty(name + ".Array.size").intValue = newSize;
        EditorGUI.indentLevel = 3;
        for (int i = 0; i < newSize; i++)
        {
            var prop = obj.FindProperty(string.Format("{0}.Array.data[{1}]", name, i));
            EditorGUILayout.PropertyField(prop);
        }
        EditorGUI.indentLevel = 0;
    }
}
