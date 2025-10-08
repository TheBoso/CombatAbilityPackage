using UnityEngine;
using System.Collections;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Pure effect for spawning projectiles
    /// Works with any weapon that shoots - blaster, bomb glove, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileSpawner", menuName = "Ratchet Combat/Effects/Projectile Spawner")]
    public class ProjectileSpawnerEffect : AbilityEffect
    {
        [Header("Projectile Setup")]
        [Tooltip("Projectile prefab to spawn")]
        public GameObject projectilePrefab;

        [Tooltip("Number of projectiles to spawn")]
        public int projectileCount = 1;

        [Tooltip("Delay between projectiles (if count > 1)")]
        public float delayBetweenProjectiles = 0.1f;

        [Tooltip("Spread angle for multiple projectiles")]
        public float spreadAngle = 0f;

        [Header("Spawn Point")]
        [Tooltip("Names of spawn points (e.g., WeaponMuzzle, LeftBarrel, RightBarrel). One projectile spawned per point.")]
        public string[] spawnPointNames = new string[] { "WeaponMuzzle" };

        [Tooltip("Position offset from spawn point")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Rotation offset from spawn point")]
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Projectile Properties")]
        [Tooltip("Initial velocity (forward from spawn point)")]
        public float initialVelocity = 20f;

        [Tooltip("Inherit velocity from caster?")]
        public bool inheritCasterVelocity = false;

        [Tooltip("Velocity inheritance factor")]
        [Range(0f, 1f)]
        public float velocityInheritance = 0.5f;

        [Tooltip("Projectile lifetime (seconds)")]
        public float projectileLifetime = 5f;

        [Tooltip("Damage per projectile")]
        public float damage = 10f;

        [Header("Effects")]
        [Tooltip("Muzzle flash effect")]
        public GameObject muzzleFlashEffect;

        [Tooltip("Spawn sound")]
        public AudioClip spawnSound;

        [Tooltip("Animation to play (optional)")]
        public string animationTrigger = "";

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            // Validate
            if (projectilePrefab == null)
            {
                Debug.LogError($"ProjectileSpawnerEffect: No projectile prefab assigned!");
                yield break;
            }

            // Find all spawn points
            Transform[] spawnPoints = FindSpawnPoints(caller);
            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning($"ProjectileSpawnerEffect: No spawn points found on {caller.gameObject.name}. Using caller transform.");
                spawnPoints = new Transform[] { caller.transform };
            }

            // Play animation
            if (!string.IsNullOrEmpty(animationTrigger))
            {
                Animator animator = caller.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(animationTrigger);
                }
            }

            // Play spawn sound
            if (spawnSound != null)
            {
                AudioSource audioSource = caller.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(spawnSound);
                }
            }

            // Get caster velocity for inheritance
            Vector3 casterVelocity = Vector3.zero;
            if (inheritCasterVelocity)
            {
                Rigidbody rb = caller.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    casterVelocity = rb.linearVelocity;
                }
            }

            // Spawn projectiles from each spawn point
            foreach (Transform spawnPoint in spawnPoints)
            {
                // Spawn muzzle flash at this spawn point
                if (muzzleFlashEffect != null)
                {
                    GameObject flash = Instantiate(muzzleFlashEffect, spawnPoint.position, spawnPoint.rotation);
                    Destroy(flash, 2f);
                }

                // Spawn projectiles from this spawn point
                for (int i = 0; i < projectileCount; i++)
                {
                    SpawnProjectile(spawnPoint, i, casterVelocity, caller);

                    if (i < projectileCount - 1)
                    {
                        yield return new WaitForSeconds(delayBetweenProjectiles);
                    }
                }
            }
        }

        private void SpawnProjectile(Transform spawnPoint, int index, Vector3 casterVelocity, AbilityCaster caller)
        {
            // Calculate spawn position
            Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(positionOffset);

            // Calculate spawn rotation with spread
            Quaternion baseRotation = spawnPoint.rotation * Quaternion.Euler(rotationOffset);
            Quaternion spreadRotation = Quaternion.identity;

            if (projectileCount > 1 && spreadAngle > 0f)
            {
                float angle = spreadAngle * ((index / (float)(projectileCount - 1)) - 0.5f);
                spreadRotation = Quaternion.Euler(0, angle, 0);
            }

            Quaternion finalRotation = baseRotation * spreadRotation;

            // Spawn projectile
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, finalRotation);

            // Set up projectile component
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<Projectile>();
            }

            // Calculate final velocity
            Vector3 forward = finalRotation * Vector3.forward;
            Vector3 velocity = forward * initialVelocity;

            if (inheritCasterVelocity)
            {
                velocity += casterVelocity * velocityInheritance;
            }

            // Initialize projectile
            projectile.Initialize(velocity, damage, projectileLifetime, caller.gameObject);

            // Destroy after lifetime
            Destroy(projectileObj, projectileLifetime);
        }

        private Transform[] FindSpawnPoints(AbilityCaster caller)
        {
            System.Collections.Generic.List<Transform> foundPoints = new System.Collections.Generic.List<Transform>();

            // For each spawn point name, try to find it
            foreach (string pointName in spawnPointNames)
            {
                if (string.IsNullOrEmpty(pointName))
                    continue;

                Transform found = FindSingleSpawnPoint(caller, pointName);
                if (found != null)
                {
                    foundPoints.Add(found);
                }
                else
                {
                    Debug.LogWarning($"ProjectileSpawnerEffect: Spawn point '{pointName}' not found on {caller.gameObject.name}");
                }
            }

            return foundPoints.ToArray();
        }

        private Transform FindSingleSpawnPoint(AbilityCaster caller, string pointName)
        {
            // Try to find CombatSpawnPoint first
            CombatSpawnPoint[] spawnPoints = caller.GetComponentsInChildren<CombatSpawnPoint>(true);
            foreach (var point in spawnPoints)
            {
                if (point.name == pointName)
                {
                    return point.transform;
                }
            }

            // Try to find by name
            Transform[] allTransforms = caller.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == pointName)
                {
                    return t;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Simple projectile component
    /// Add this to your projectile prefabs or let ProjectileSpawnerEffect add it automatically
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 velocity;
        private float damage;
        private GameObject shooter;
        private bool initialized = false;

        [Header("Impact")]
        [Tooltip("Impact effect on hit")]
        public GameObject impactEffect;

        [Tooltip("Impact sound")]
        public AudioClip impactSound;

        [Tooltip("Destroy on impact?")]
        public bool destroyOnImpact = true;

        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
            }
        }

        public void Initialize(Vector3 velocity, float damage, float lifetime, GameObject shooter)
        {
            this.velocity = velocity;
            this.damage = damage;
            this.shooter = shooter;
            this.initialized = true;

            if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }

        void FixedUpdate()
        {
            if (!initialized)
                return;

            // Apply velocity if no rigidbody
            if (rb == null)
            {
                transform.position += velocity * Time.fixedDeltaTime;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // Don't hit shooter
            if (other.gameObject == shooter)
                return;

            // Try to damage
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // Play impact sound
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position);
            }

            // Destroy projectile
            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }
}
