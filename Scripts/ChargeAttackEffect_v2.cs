using System.Collections;
using UnityEngine;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Self-contained charge attack effect
    /// No controller dependency
    /// </summary>
    [CreateAssetMenu(fileName = "ChargeAttack", menuName = "Ratchet Combat/Effects/Charge Attack v2")]
    public class ChargeAttackEffect_v2 : AbilityEffect
    {
        [Header("Charge Settings")]
        [Tooltip("Time to fully charge")]
        public float chargeTime = 1.5f;

        [Tooltip("Attack to execute when released")]
        public AttackData chargedAttack;

        [Tooltip("Minimum charge to release (0-1)")]
        [Range(0f, 1f)]
        public float minimumChargePercent = 0.8f;

        [Header("Input Detection")]
        [Tooltip("Button to hold (checks WrenchInput component)")]
        public string buttonToHold = "hyperstrike";

        [Header("Hitbox")]
        [Tooltip("Spawn point for hitbox")]
        public string hitboxSpawnPointName = "WrenchTip";

        [Tooltip("Hit layers")]
        public LayerMask hitLayers;

        [Header("Visual Feedback")]
        [Tooltip("Charge effect prefab")]
        public GameObject chargeEffectPrefab;

        [Tooltip("Spawn point for effect")]
        public string effectSpawnPoint = "WrenchBase";

        [Tooltip("Scale with charge")]
        public bool scaleWithCharge = true;

        [Tooltip("Color gradient")]
        public Gradient chargeColorGradient;

        [Header("Audio")]
        [Tooltip("Charge loop sound")]
        public AudioClip chargeSound;

        [Tooltip("Release sound")]
        public AudioClip releaseSound;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            if (chargedAttack == null)
            {
                Debug.LogError("ChargeAttackEffect: No charged attack assigned!");
                yield break;
            }

            // Get input component
            WrenchInput input = caller.GetComponent<WrenchInput>();
            if (input == null)
            {
                Debug.LogWarning("ChargeAttackEffect: No WrenchInput found, using time-based charge");
            }

            // Get audio source
            AudioSource audioSource = caller.GetComponent<AudioSource>();

            // Find spawn points
            Transform effectSpawn = FindSpawnPoint(caller.transform, effectSpawnPoint);

            // Spawn charge effect
            GameObject chargeEffect = null;
            if (chargeEffectPrefab != null && effectSpawn != null)
            {
                chargeEffect = Object.Instantiate(chargeEffectPrefab, effectSpawn.position, effectSpawn.rotation, effectSpawn);
            }

            // Play charge sound
            if (audioSource != null && chargeSound != null)
            {
                audioSource.clip = chargeSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Charging loop
            float chargeProgress = 0f;
            bool isCharging = true;

            while (isCharging && chargeProgress < chargeTime)
            {
                chargeProgress += Time.deltaTime;
                float chargePercent = Mathf.Clamp01(chargeProgress / chargeTime);

                // Update charge effect
                if (chargeEffect != null)
                {
                    if (scaleWithCharge)
                    {
                        chargeEffect.transform.localScale = Vector3.one * chargePercent;
                    }

                    ParticleSystem ps = chargeEffect.GetComponent<ParticleSystem>();
                    if (ps != null && chargeColorGradient != null)
                    {
                        var main = ps.main;
                        main.startColor = chargeColorGradient.Evaluate(chargePercent);
                    }
                }

                // Check if released early
                if (input != null)
                {
                    bool stillHolding = CheckInputHeld(input, buttonToHold);

                    if (!stillHolding)
                    {
                        if (chargePercent < minimumChargePercent)
                        {
                            // Not charged enough - cancel
                            CleanupCharge(chargeEffect, audioSource);
                            yield break;
                        }
                        else
                        {
                            // Release!
                            isCharging = false;
                        }
                    }
                }

                yield return null;
            }

            // Cleanup charge visuals
            CleanupCharge(chargeEffect, audioSource);

            // Play release sound
            if (audioSource != null && releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound);
            }

            // Execute the charged attack using MeleeAttackEffect logic inline
            yield return ExecuteChargedAttack(caller, chargedAttack, hitboxSpawnPointName, hitLayers);
        }

        private IEnumerator ExecuteChargedAttack(AbilityCaster caller, AttackData attack, string spawnPointName, LayerMask hitLayers)
        {
            // Inline melee attack execution (no dependency on other effects)
            Animator animator = caller.GetComponent<Animator>();
            AudioSource audioSource = caller.GetComponent<AudioSource>();
            TrailRenderer trail = caller.GetComponentInChildren<TrailRenderer>();

            Transform spawnPoint = FindSpawnPoint(caller.transform, spawnPointName);
            HashSet<Collider> hitThisSwing = new HashSet<Collider>();

            // Animation
            if (animator != null && !string.IsNullOrEmpty(attack.animationTrigger))
            {
                animator.CrossFadeInFixedTime(attack.animationTrigger, attack.crossFadeTime, attack.animationLayer);
            }

            // Sound
            if (audioSource != null && attack.swingSound != null)
            {
                audioSource.PlayOneShot(attack.swingSound, attack.volume);
            }

            // Trail
            if (trail != null && attack.enableTrail)
            {
                trail.emitting = true;
                if (attack.trailColor != null)
                    trail.colorGradient = attack.trailColor;
            }

            // Wait for hitbox
            yield return new WaitForSeconds(attack.attackDuration * attack.hitboxStartTime);

            // Hitbox active
            float hitboxDuration = attack.attackDuration * (attack.hitboxEndTime - attack.hitboxStartTime);
            float elapsed = 0f;

            while (elapsed < hitboxDuration)
            {
                Vector3 hitboxPos = spawnPoint.position + spawnPoint.TransformDirection(attack.hitboxOffset);
                Collider[] hits = PerformHitboxCheck(hitboxPos, spawnPoint.rotation, attack, hitLayers);

                if (hits != null)
                {
                    foreach (Collider hit in hits)
                    {
                        if (hit.transform.root == caller.transform.root || hitThisSwing.Contains(hit))
                            continue;

                        IDamageable damageable = hit.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            Vector3 hitPoint = hit.ClosestPoint(hitboxPos);
                            damageable.TakeDamage(attack.damage, hitPoint, caller.transform.position);

                            if (attack.hitEffectPrefab != null)
                                Object.Instantiate(attack.hitEffectPrefab, hitPoint, Quaternion.identity);

                            if (audioSource != null && attack.hitSound != null)
                                audioSource.PlayOneShot(attack.hitSound, attack.volume);

                            hitThisSwing.Add(hit);
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Cleanup
            if (trail != null)
                trail.emitting = false;

            yield return new WaitForSeconds(attack.attackDuration * (1f - attack.hitboxEndTime));
        }

        private Collider[] PerformHitboxCheck(Vector3 position, Quaternion rotation, AttackData attack, LayerMask layers)
        {
            switch (attack.hitboxShape)
            {
                case HitboxShape.Sphere:
                    return Physics.OverlapSphere(position, attack.hitboxSize.x * attack.rangeMultiplier, layers);
                case HitboxShape.Box:
                    return Physics.OverlapBox(position, (attack.hitboxSize * attack.rangeMultiplier) / 2f, rotation, layers);
                default:
                    return null;
            }
        }

        private bool CheckInputHeld(WrenchInput input, string button)
        {
            switch (button.ToLower())
            {
                case "hyperstrike":
                    return input.attackHeld || input.GetHyperstrikeHoldTime() > 0;
                case "attack":
                    return input.attackHeld;
                default:
                    return false;
            }
        }

        private void CleanupCharge(GameObject effect, AudioSource audio)
        {
            if (effect != null)
                Object.Destroy(effect);

            if (audio != null)
            {
                audio.loop = false;
                audio.Stop();
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
