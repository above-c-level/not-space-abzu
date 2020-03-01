using UnityEngine;
using System.Collections;

namespace Imphenzia.SpaceForUnity
{
    public class SpaceParticles : MonoBehaviour
    {
        // Maximum number of particles in the sphere (configure to your needs for look and performance)
        public int maxParticles = 1000;
        // Range of particle sphere (when particles are beyond this range from its
        // parent they will respawn (relocate) to within range at distanceSpawn of range.
        public float range = 200.0f;
        // Distance percentile of range to relocate/spawn particles to
        public float distanceSpawn = 0.95f;
        // Minimum size of particles
        public float minParticleSize = 0.5f;
        // Maximum size of particles
        public float maxParticleSize = 1.0f;
        // Multiplier of size
        public float sizeMultiplier = 1.0f;
        // Minimum drift/movement speed of particles
        public float minParticleDriftSpeed = 0.0f;
        // Maximum drift/movement speed of particles
        public float maxParticleDriftSpeed = 1.0f;
        // Multiplier of driftSpeed
        public float driftSpeedMultiplier = 1.0f;
        // Fade particles in/out of range (usually not necessary for small particles)
        public bool fadeParticles = true;
        // Distance percentile of range to start fading particles (should be lower than distanceSpawn)
        public float distanceFade = 0.5f;
        // Used by SU_TravelWarp to move particles away fast when warping
        public Vector3 WarpParticles { get; set; }

        // Private variables
        private float distanceToSpawn;
        private float distanceToFade;
        private ParticleSystem cachedParticleSystem;
        private Transform cachedTransform;


        void Start()
        {
            // Cache transform and particle system to improve performance
            cachedTransform = transform;
            cachedParticleSystem = GetComponent<ParticleSystem>();
            // Calculate the actual spawn and fade distances
            distanceToSpawn = range * distanceSpawn;
            distanceToFade = range * distanceFade;
            // Initialize WarpParticles vector to zero
            WarpParticles = Vector3.zero;

            // Scale particles
            if (cachedParticleSystem == null)
            {
                // Throw an error if the object of this script has no particle system
                Debug.LogError("This script must be attached to a GameObject with a particle system. It is strongly recommended " +
                                "that you use the SpaceParticles or SpaceFog prefab which have suitable particle systems)");
            }

            // Spawn all new particles within a sphere in range of the object
            for (int i = 0; i < maxParticles; i++)
            {
                ParticleSystem.Particle _newParticle = new ParticleSystem.Particle();
                _newParticle.position = cachedTransform.position + (Random.insideUnitSphere * distanceToSpawn);
                _newParticle.remainingLifetime = Mathf.Infinity;
                Vector3 _velocity = new Vector3(
                    Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier,
                    Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier,
                    Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier);
                _newParticle.velocity = _velocity;
                _newParticle.startSize = Random.Range(minParticleSize, maxParticleSize) * sizeMultiplier;
                GetComponent<ParticleSystem>().Emit(1);

            }
        }


        void Update()
        {
            int _numParticles = cachedParticleSystem.particleCount;
            // Get the particles from the particle system
            ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[_numParticles];
            cachedParticleSystem.GetParticles(_particles);

            // Iterate through the particles and relocation (spawn) and fading
            for (int i = 0; i < _particles.Length; i++)
            {
                // Calculate distance to particle from transform position
                float _distance = Vector3.Distance(_particles[i].position, cachedTransform.position);

                // If distance is greater than range...
                if (_distance > range)
                {
                    // reposition (respawn) particle according to spawn distance
                    _particles[i].position = Random.onUnitSphere * distanceToSpawn + cachedTransform.position;
                    // Re-calculate distance to particle for fading
                    _distance = Vector3.Distance(_particles[i].position, cachedTransform.position);
                    // Set a new velocity of the particle
                    Vector3 _velocity = new Vector3(
                        Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier,
                        Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier,
                        Random.Range(minParticleDriftSpeed, maxParticleDriftSpeed) * driftSpeedMultiplier);
                    _particles[i].velocity = _velocity;
                    // Set a new size of the particle
                    _particles[i].startSize = Random.Range(minParticleSize, maxParticleSize) * sizeMultiplier;
                }

                // Move particles by WarpParticles vector (normally warp is zero so it has no effect unless an external script, like SU_TravelWarp, is setting a value)
                _particles[i].position += WarpParticles;

                // If particle fading is enabled...
                if (fadeParticles)
                {
                    // Get the original color of the particle
                    Color _col = _particles[i].startColor;
                    if (_distance > distanceToFade)
                    {
                        // Fade alpha value of particle between fading distance and spawnin distance
                        _particles[i].startColor = new Color(_col.r, _col.g, _col.b, Mathf.Clamp01(1.0f - ((_distance - distanceToFade) / (distanceToSpawn - distanceToFade))));
                    }
                    else
                    {
                        // Particle is within range so ensure it has full alpha value
                        _particles[i].startColor = new Color(_col.r, _col.g, _col.b, 1.0f);
                    }
                }
            }

            // Set the particles according to above modifications
            cachedParticleSystem.SetParticles(_particles, _numParticles);
        }
    }
}