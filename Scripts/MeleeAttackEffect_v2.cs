using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Fully self-contained melee attack effect
    /// No controller coupling - everything handled in the effect
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeAttack", menuName = "Ratchet Combat/Effects/Melee Attack v2")]
    public class MeleeAttackEffect_v2 : AbilityEffect
    {
        [Header("Attack Data")]
        [Tooltip("The attack to execute")]
        public AttackData attackData;

        [Header("Hitbox Configuration")]
        [Tooltip("Spawn point name for hitbox (optional)")]
        public string hitboxSpawnPointName = "WrenchTip";

        [Tooltip("Layers that can be hit")]
        public LayerMask hitLayers;

        [Tooltip("Prevent hitting same target multiple times")]
        public bool preventMultiHit = true;

        [Header("Combo Tracking (Optional)")]
        [Tooltip("Track combo count across attacks")]
        public bool trackCombo = false;

        [Tooltip("Combo state key (must match across combo attacks)")]
        public string comboStateKey = "DefaultCombo";

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            if (attackData == null)
            {
                Debug.LogError("MeleeAttackEffect: No AttackData assigned!");
                yield break;
            }

            // Get components (only what we need)
            Animator animator = caller.GetComponent<Animator>();
            AudioSource audioSource = caller.GetComponent<AudioSource>();

            // Combo tracking (optional, uses static dict to avoid coupling)
            int comboCount = 0;
            if (trackCombo)
            {
                comboCount = ComboTracker.GetComboCount(caller, comboStateKey);
                ComboTracker.IncrementCombo(caller, comboStateKey);
            }

            // Track hits for multi-hit prevention
            HashSet<Collider> hitThisSwing = new HashSet<Collider>();

            // Find spawn point for hitbox
            Transform spawnPoint = FindSpawnPoint(caller.transform, hitboxSpawnPointName);

            // Find or create trail renderer
            TrailRenderer trail = FindTrailRenderer(caller.transform);

            // Play animation
            if (animator != null && !string.IsNullOrEmpty(attackData.animationTrigger))
            {
                animator.CrossFadeInFixedTime(attackData.animationTrigger, attackData.crossFadeTime, attackData.animationLayer);
            }

            // Play swing sound
            if (audioSource != null && attackData.swingSound != null)
            {
                audioSource.PlayOneShot(attackData.swingSound, attackData.volume);
            }

            // Enable trail
            if (trail != null && attackData.enableTrail)
            {
                trail.emitting = true;
                if (attackData.trailColor != null)
                {
                    trail.colorGradient = attackData.trailColor;
                }
            }

            // Spawn on-start effects
            SpawnEffects(attackData.spawnOnStart, spawnPoint.position, spawnPoint.rotation);

            // Wait until hitbox activates
            float hitboxStartTime = attackData.attackDuration * attackData.hitboxStartTime;
            yield return new WaitForSeconds(hitboxStartTime);

            // Hitbox active window
            float hitboxDuration = attackData.attackDuration * (attackData.hitboxEndTime - attackData.hitboxStartTime);
            float elapsed = 0f;

            while (elapsed < hitboxDuration)
            {
                // Perform hitbox detection
                Vector3 hitboxPosition = spawnPoint.position + spawnPoint.TransformDirection(attackData.hitboxOffset);
                Collider[] hits = PerformHitboxCheck(hitboxPosition, spawnPoint.rotation, attackData);

                if (hits != null)
                {
                    foreach (Collider hit in hits)
                    {
                        // Skip self
                        if (hit.transform.root == caller.transform.root)
                            continue;

                        // Skip already hit
                        if (preventMultiHit && hitThisSwing.Contains(hit))
                            continue;

                        // Deal damage
                        IDamageable damageable = hit.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            Vector3 hitPoint = hit.ClosestPoint(hitboxPosition);

                            // Calculate damage (with combo scaling if tracking)
                            float finalDamage = attackData.damage;
                            if (trackCombo && attackData.comboDamageMultiplier != null)
                            {
                                finalDamage *= attackData.comboDamageMultiplier.Evaluate(comboCount);
                            }

                            // Apply damage
                            damageable.TakeDamage(finalDamage, hitPoint, caller.transform.position);

                            // Spawn hit effects
                            if (attackData.hitEffectPrefab != null)
                            {
                                Object.Instantiate(attackData.hitEffectPrefab, hitPoint, Quaternion.LookRotation(caller.transform.forward));
                            }

                            SpawnEffects(attackData.spawnOnHit, hitPoint, Quaternion.identity);

                            // Play hit sound
                            if (audioSource != null && attackData.hitSound != null)
                            {
                                audioSource.PlayOneShot(attackData.hitSound, attackData.volume);
                            }

                            // Track hit
                            if (preventMultiHit)
                            {
                                hitThisSwing.Add(hit);
                            }
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Disable trail
            if (trail != null)
            {
                trail.emitting = false;
            }

            // Wait for remaining animation
            float remainingTime = attackData.attackDuration * (1f - attackData.hitboxEndTime);
            yield return new WaitForSeconds(remainingTime);
        }

        private Collider[] PerformHitboxCheck(Vector3 position, Quaternion rotation, AttackData attack)
        {
            switch (attack.hitboxShape)
            {
                case HitboxShape.Sphere:
                    float radius = attack.hitboxSize.x * attack.rangeMultiplier;
                    return Physics.OverlapSphere(position, radius, hitLayers);

                case HitboxShape.Box:
                    Vector3 extents = attack.hitboxSize * attack.rangeMultiplier;
                    return Physics.OverlapBox(position, extents / 2f, rotation, hitLayers);

                case HitboxShape.Capsule:
                    float capsuleRadius = attack.hitboxSize.x * attack.rangeMultiplier;
                    float capsuleHeight = attack.hitboxSize.y * attack.rangeMultiplier;
                    Vector3 point1 = position + Vector3.up * (capsuleHeight / 2f - capsuleRadius);
                    Vector3 point2 = position - Vector3.up * (capsuleHeight / 2f - capsuleRadius);
                    return Physics.OverlapCapsule(point1, point2, capsuleRadius, hitLayers);

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

            // Fallback: search by name
            Transform found = FindChildRecursive(root, name);
            return found != null ? found : root;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private TrailRenderer FindTrailRenderer(Transform root)
        {
            // Look for trail on children (wrench model)
            return root.GetComponentInChildren<TrailRenderer>();
        }

        private void SpawnEffects(GameObject[] prefabs, Vector3 position, Quaternion rotation)
        {
            if (prefabs == null || prefabs.Length == 0)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    Object.Instantiate(prefab, position, rotation);
                }
            }
        }
    }

    /// <summary>
    /// Static combo tracker - no MonoBehaviour coupling
    /// Tracks combo state across multiple attacks
    /// </summary>
    public static class ComboTracker
    {
        private class ComboState
        {
            public int count;
            public float lastHitTime;
        }

        private static Dictionary<int, Dictionary<string, ComboState>> combos = new Dictionary<int, Dictionary<string, ComboState>>();

        public static int GetComboCount(AbilityCaster caster, string comboKey)
        {
            int instanceID = caster.GetInstanceID();

            if (!combos.ContainsKey(instanceID))
                combos[instanceID] = new Dictionary<string, ComboState>();

            if (!combos[instanceID].ContainsKey(comboKey))
                combos[instanceID][comboKey] = new ComboState();

            ComboState state = combos[instanceID][comboKey];

            // Check if combo expired (2 seconds default)
            if (Time.time - state.lastHitTime > 2f)
            {
                state.count = 0;
            }

            return state.count;
        }

        public static void IncrementCombo(AbilityCaster caster, string comboKey)
        {
            int instanceID = caster.GetInstanceID();

            if (!combos.ContainsKey(instanceID))
                combos[instanceID] = new Dictionary<string, ComboState>();

            if (!combos[instanceID].ContainsKey(comboKey))
                combos[instanceID][comboKey] = new ComboState();

            ComboState state = combos[instanceID][comboKey];
            state.count++;
            state.lastHitTime = Time.time;
        }

        public static void ResetCombo(AbilityCaster caster, string comboKey)
        {
            int instanceID = caster.GetInstanceID();

            if (combos.ContainsKey(instanceID) && combos[instanceID].ContainsKey(comboKey))
            {
                combos[instanceID][comboKey].count = 0;
            }
        }
    }
}
