using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The class for managing and spawning boids
/// </summary>
public class BoidManager : MonoBehaviour
{
    // An enumerator for options of when to show the spawn region
    public enum GizmoType { Never, SelectedOnly, Always }

    // Comments are being ignored in favor of Tooltips
    [Tooltip("A boid prefab to spawn in with the boid script attached")]
    public Boid prefab;
    [Tooltip("The radius within which boids can spawn")]
    public float spawnRadius = 10;
    [Tooltip("The number of boids to spawn")]
    public int spawnCount = 10;
    [Tooltip("The color the boids should be")]
    public Color color;
    [Tooltip("When to show the spawn region")]
    public GizmoType showSpawnRegion;


    [Tooltip("The target the boids should try to fly towards")]
    public Transform target;

    [Tooltip("The settings file that the boids should follow")]
    public BoidSettings settings;
    [Tooltip("The compute shader used to speed up calculations by using the GPU")]
    public ComputeShader compute;
    // This has to do with calculations related to the compute shader
    const int threadGroupSize = 1024;
    // All of the boids, in array form
    Boid[] boids;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        // Find all the boids
        boids = FindObjectsOfType<Boid>();
        // Then, for each of them
        foreach (Boid boid in boids)
        {
            // Initialize them with the settings file and aiming for `target`
            boid.Initialize(settings, target);
        }

    }

    void Update()
    {
        if (boids != null)
        {

            int numBoids = boids.Length;
            var boidData = new BoidData[numBoids];

            for (int i = 0; i < boids.Length; i++)
            {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
            }

            var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
            boidBuffer.SetData(boidData);

            compute.SetBuffer(0, "boids", boidBuffer);
            compute.SetInt("numBoids", boids.Length);
            compute.SetFloat("viewRadius", settings.perceptionRadius);
            compute.SetFloat("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
            compute.Dispatch(0, threadGroups, 1, 1);

            boidBuffer.GetData(boidData);

            for (int i = 0; i < boids.Length; i++)
            {
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centerOfFlockmates = boidData[i].flockCenter;
                boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

                boids[i].UpdateBoid();
            }

            boidBuffer.Release();
        }
    }

    // This struct should be exactly the same as the one in `BoidCompute` so that
    // the CPU can communicate with the GPU using the buffer. The names should be
    // fairly straightforward.
    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 flockHeading;
        public Vector3 flockCenter;
        public Vector3 avoidanceHeading;
        public int numFlockmates;
        public static int Size
        {
            get
            {
                // `sizeof` returns the space something takes up (I believe in bytes)
                // A Vector3 is 3 floats, and there are 5 of them above.
                // The struct also contains an int.
                return sizeof(float) * 3 * 5 + sizeof(int);
            }
        }
    }

    /// <summary>
    /// Callback to draw gizmos that are pickable and always drawn.
    /// </summary>
    void OnDrawGizmos()
    {
        // If the sphere should be shown in settings
        if (settings.showEpsilonSphere)
        {
            // Then draw a wire sphere showing the range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, settings.distanceEpsilon);
        }
        // If the spawn region variable is set to `Always`
        if (showSpawnRegion == GizmoType.Always)
        {
            // Then draw the associated gizmos
            DrawGizmos();
        }
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        // This iterates `spawnCount` times
        for (int i = 0; i < spawnCount; i++)
        {
            // This gets a random point within the spawn sphere, with
            // an offset of the spawner's position
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Boid boid = Instantiate(prefab);
            boid.transform.position = pos;
            boid.transform.forward = Random.insideUnitSphere;

            boid.SetColour(color);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (showSpawnRegion == GizmoType.SelectedOnly)
        {
            DrawGizmos();
        }
    }

    void DrawGizmos()
    {

        Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}