using System.Collections;
using UnityEngine;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Self-contained combo effect
    /// Automatically chains attacks based on combo count
    /// No controller needed - pure effect-based
    /// </summary>
    [CreateAssetMenu(fileName = "ComboEffect", menuName = "Ratchet Combat/Effects/Combo")]
    public class ComboEffect : AbilityEffect
    {
        [Header("Combo Configuration")]
        [Tooltip("Combo chain to use")]
        public ComboChain comboChain;

        [Tooltip("Hitbox spawn point")]
        public string hitboxSpawnPointName = "WrenchTip";

        [Tooltip("Hit layers")]
        public LayerMask hitLayers;

        [Tooltip("Prevent multi-hit")]
        public bool preventMultiHit = true;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            if (comboChain == null || comboChain.attacks == null || comboChain.attacks.Length == 0)
            {
                Debug.LogError("ComboEffect: No combo chain configured!");
                yield break;
            }

            // Get current combo count
            string comboKey = comboChain.comboID;
            int comboIndex = ComboTracker.GetComboCount(caller, comboKey);

            // Check if combo expired
            float lastHitTime = ComboTracker.GetLastHitTime(caller, comboKey);
            float resetTime = comboChain.resetTime;

            if (Time.time - lastHitTime > resetTime)
            {
                // Reset combo
                comboIndex = 0;
                ComboTracker.ResetCombo(caller, comboKey);
            }

            // Get next attack
            AttackData attack = comboChain.GetAttack(comboIndex);

            if (attack == null)
            {
                Debug.LogWarning($"ComboEffect: No attack at index {comboIndex}");
                yield break;
            }

            // Execute the attack (inline, no dependencies)
            yield return ExecuteAttack(caller, attack, comboIndex);

            // Increment combo
            ComboTracker.IncrementCombo(caller, comboKey);
        }

        private IEnumerator ExecuteAttack(AbilityCaster caller, AttackData attack, int comboIndex)
        {
            // Get components
            Animator animator = caller.GetComponent<Animator>();
            AudioSource audioSource = caller.GetComponent<AudioSource>();
            TrailRenderer trail = caller.GetComponentInChildren<TrailRenderer>();

            // Find spawn point
            Transform spawnPoint = FindSpawnPoint(caller.transform, hitboxSpawnPointName);

            // Track hits
            System.Collections.Generic.HashSet<Collider> hitThisSwing = new System.Collections.Generic.HashSet<Collider>();

            // Play animation
            if (animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                animator.CrossFadeInFixedTime(attack.animationTrigger, attack.crossFadeTime, attack.animationLayer);
            }

            // Play sound
            if (audioSource != null && attack.swingSound != null)
            {
                audioSource.PlayOneShot(attack.swingSound, attack.volume);
            }

            // Enable trail
            if (trail != null && attack.enableTrail)
            {
                trail.emitting = true;
                if (attack.trailColor != null)
                    trail.colorGradient = attack.trailColor;
            }

            // Spawn effects
            SpawnEffects(attack.spawnOnStart, spawnPoint.position, spawnPoint.rotation);

            // Wait for hitbox
            yield return new WaitForSeconds(attack.attackDuration * attack.hitboxStartTime);

            // Hitbox active window
            float hitboxDuration = attack.attackDuration * (attack.hitboxEndTime - attack.hitboxStartTime);
            float elapsed = 0f;

            while (elapsed < hitboxDuration)
            {
                // Hitbox check
                Vector3 hitboxPos = spawnPoint.position + spawnPoint.TransformDirection(attack.hitboxOffset);
                Collider[] hits = PerformHitboxCheck(hitboxPos, spawnPoint.rotation, attack);

                if (hits != null)
                {
                    foreach (Collider hit in hits)
                    {
                        // Skip self
                        if (hit.transform.root == caller.transform.root)
                            continue;

                        // Skip if already hit
                        if (preventMultiHit && hitThisSwing.Contains(hit))
                            continue;

                        // Deal damage
                        IDamageable damageable = hit.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            Vector3 hitPoint = hit.ClosestPoint(hitboxPos);

                            // Calculate damage with combo scaling
                            float finalDamage = attack.damage;
                            if (comboChain != null)
                            {
                                finalDamage *= comboChain.GetDamageMultiplier(comboIndex);
                            }

                            // Apply damage
                            damageable.TakeDamage(finalDamage, hitPoint, caller.transform.position);

                            // Effects
                            if (attack.hitEffectPrefab != null)
                                Object.Instantiate(attack.hitEffectPrefab, hitPoint, Quaternion.identity);

                            SpawnEffects(attack.spawnOnHit, hitPoint, Quaternion.identity);

                            // Sound
                            if (audioSource != null && attack.hitSound != null)
                                audioSource.PlayOneShot(attack.hitSound, attack.volume);

                            // Track hit
                            if (preventMultiHit)
                                hitThisSwing.Add(hit);
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Disable trail
            if (trail != null)
                trail.emitting = false;

            // Wait for remaining animation
            yield return new WaitForSeconds(attack.attackDuration * (1f - attack.hitboxEndTime));
        }

        private Collider[] PerformHitboxCheck(Vector3 position, Quaternion rotation, AttackData attack)
        {
            switch (attack.hitboxShape)
            {
                case HitboxShape.Sphere:
                    return Physics.OverlapSphere(position, attack.hitboxSize.x * attack.rangeMultiplier, hitLayers);

                case HitboxShape.Box:
                    return Physics.OverlapBox(position, (attack.hitboxSize * attack.rangeMultiplier) / 2f, rotation, hitLayers);

                case HitboxShape.Capsule:
                    float radius = attack.hitboxSize.x * attack.rangeMultiplier;
                    float height = attack.hitboxSize.y * attack.rangeMultiplier;
                    Vector3 p1 = position + Vector3.up * (height / 2f - radius);
                    Vector3 p2 = position - Vector3.up * (height / 2f - radius);
                    return Physics.OverlapCapsule(p1, p2, radius, hitLayers);

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

        private void SpawnEffects(GameObject[] prefabs, Vector3 position, Quaternion rotation)
        {
            if (prefabs == null || prefabs.Length == 0)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                    Object.Instantiate(prefab, position, rotation);
            }
        }
    }
}
