using System.Collections;
using UnityEngine;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Self-contained AOE attack effect
    /// No controller dependency - pure effect
    /// </summary>
    [CreateAssetMenu(fileName = "AOEAttack", menuName = "Ratchet Combat/Effects/AOE Attack v2")]
    public class AOEAttackEffect_v2 : AbilityEffect
    {
        [Header("Damage")]
        [Tooltip("Damage dealt in AOE")]
        public float damage = 40f;

        [Tooltip("AOE radius")]
        public float radius = 4f;

        [Tooltip("Shape of AOE")]
        public AOEShape shape = AOEShape.Sphere;

        [Tooltip("Layers that can be damaged")]
        public LayerMask hitLayers;

        [Header("Positioning")]
        [Tooltip("Spawn point name (empty = character position)")]
        public string spawnPointName = "";

        [Tooltip("Offset from spawn point")]
        public Vector3 offset = Vector3.zero;

        [Header("Timing")]
        [Tooltip("Delay before dealing damage")]
        public float delay = 0f;

        [Header("Effects")]
        [Tooltip("Impact effect prefab")]
        public GameObject impactEffectPrefab;

        [Tooltip("Effect scale")]
        public float effectScale = 1f;

        [Tooltip("Hit effect per enemy")]
        public GameObject hitEffectPrefab;

        [Header("Knockback")]
        [Tooltip("Apply knockback")]
        public bool applyKnockback = true;

        [Tooltip("Knockback force")]
        public float knockbackForce = 10f;

        [Tooltip("Upward force")]
        public float knockbackUpwardForce = 3f;

        [Header("Audio")]
        [Tooltip("Impact sound")]
        public AudioClip impactSound;

        [Tooltip("Volume")]
        [Range(0f, 1f)]
        public float volume = 1f;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            // Delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            // Find spawn point
            Transform spawnPoint = FindSpawnPoint(caller.transform, spawnPointName);
            Vector3 aoePosition = spawnPoint.position + offset;

            // Play impact sound
            AudioSource audioSource = caller.GetComponent<AudioSource>();
            if (audioSource != null && impactSound != null)
            {
                audioSource.PlayOneShot(impactSound, volume);
            }

            // Spawn impact effect
            if (impactEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(impactEffectPrefab, aoePosition, Quaternion.identity);
                effect.transform.localScale = Vector3.one * effectScale;
                Object.Destroy(effect, 5f);
            }

            // Perform AOE damage
            Collider[] hits = PerformAOECheck(aoePosition, spawnPoint.rotation);

            if (hits != null)
            {
                foreach (Collider hit in hits)
                {
                    // Skip self
                    if (hit.transform.root == caller.transform.root)
                        continue;

                    // Damage
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector3 hitPoint = hit.ClosestPoint(aoePosition);
                        damageable.TakeDamage(damage, hitPoint, caller.transform.position);

                        // Spawn hit effect
                        if (hitEffectPrefab != null)
                        {
                            Object.Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
                        }
                    }

                    // Knockback
                    if (applyKnockback)
                    {
                        Rigidbody rb = hit.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 direction = (hit.transform.position - aoePosition).normalized;
                            Vector3 force = direction * knockbackForce + Vector3.up * knockbackUpwardForce;
                            rb.AddForce(force, ForceMode.Impulse);
                        }
                    }
                }
            }

            yield return null;
        }

        private Collider[] PerformAOECheck(Vector3 position, Quaternion rotation)
        {
            switch (shape)
            {
                case AOEShape.Sphere:
                    return Physics.OverlapSphere(position, radius, hitLayers);

                case AOEShape.Box:
                    Vector3 extents = Vector3.one * radius;
                    return Physics.OverlapBox(position, extents / 2f, rotation, hitLayers);

                case AOEShape.Cylinder:
                    // Approximate with sphere
                    return Physics.OverlapSphere(position, radius, hitLayers);

                default:
                    return null;
            }
        }

        private Transform FindSpawnPoint(Transform root, string name)
        {
            if (string.IsNullOrEmpty(name))
                return root;

            CombatSpawnPoint[] points = root.GetComponentsInChildren<CombatSpawnPoint>(true);
            foreach (var point in points)
            {
                if (point.name == name)
                    return point.transform;
            }

            return root;
        }
    }
}
