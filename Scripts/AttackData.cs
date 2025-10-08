using UnityEngine;

namespace RatchetCombat
{
    /// <summary>
    /// ScriptableObject defining a single attack
    /// Used for wrench swings, combos, and special attacks
    /// </summary>
    [CreateAssetMenu(fileName = "New Attack", menuName = "Ratchet Combat/Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("Attack Identity")]
        [Tooltip("Unique ID for this attack")]
        public string attackID;

        [Tooltip("Display name for UI/debug")]
        public string attackName;

        [Header("Damage")]
        [Tooltip("Base damage dealt by this attack")]
        public float damage = 10f;

        [Tooltip("Damage multiplier based on combo position")]
        public AnimationCurve comboDamageMultiplier = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Timing")]
        [Tooltip("Duration of the attack animation")]
        public float attackDuration = 0.4f;

        [Tooltip("When the hitbox becomes active (0-1, % of duration)")]
        public float hitboxStartTime = 0.2f;

        [Tooltip("When the hitbox deactivates (0-1, % of duration)")]
        public float hitboxEndTime = 0.8f;

        [Tooltip("Time window to chain into next attack")]
        public float comboWindow = 0.5f;

        [Header("Movement")]
        [Tooltip("Forward lunge distance")]
        public float lungeDistance = 0.5f;

        [Tooltip("Lunge curve over attack duration")]
        public AnimationCurve lungeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Movement speed multiplier during attack")]
        public float movementSpeedMultiplier = 0.6f;

        [Tooltip("Lock rotation during attack")]
        public bool lockRotation = false;

        [Header("Hitbox")]
        [Tooltip("Hitbox range multiplier")]
        public float rangeMultiplier = 1f;

        [Tooltip("Hitbox shape")]
        public HitboxShape hitboxShape = HitboxShape.Sphere;

        [Tooltip("Hitbox size (radius for sphere, extents for box)")]
        public Vector3 hitboxSize = new Vector3(1.5f, 1.5f, 1.5f);

        [Tooltip("Hitbox offset from spawn point")]
        public Vector3 hitboxOffset = Vector3.zero;

        [Header("Animation")]
        [Tooltip("Animation state/trigger name")]
        public string animationTrigger;

        [Tooltip("Animation layer")]
        public int animationLayer = 0;

        [Tooltip("Cross-fade time")]
        public float crossFadeTime = 0.1f;

        [Header("Effects")]
        [Tooltip("Hit effect prefab")]
        public GameObject hitEffectPrefab;

        [Tooltip("Swing trail effect")]
        public bool enableTrail = true;

        [Tooltip("Trail color")]
        public Gradient trailColor;

        [Tooltip("Spawn objects on attack start")]
        public GameObject[] spawnOnStart;

        [Tooltip("Spawn objects on hit")]
        public GameObject[] spawnOnHit;

        [Header("Audio")]
        [Tooltip("Swing sound")]
        public AudioClip swingSound;

        [Tooltip("Hit sound")]
        public AudioClip hitSound;

        [Tooltip("Volume")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Header("Conditions")]
        [Tooltip("Can perform while moving")]
        public bool canUseWhileMoving = true;

        [Tooltip("Can perform in air")]
        public bool canUseInAir = false;

        [Tooltip("Can perform while grounded")]
        public bool canUseOnGround = true;

        [Tooltip("Requires minimum height")]
        public float minimumHeight = 0f;

        [Header("Combo Chain")]
        [Tooltip("Next attack in combo chain (optional)")]
        public AttackData nextAttackInCombo;

        [Tooltip("Alternative combo branches (based on input)")]
        public ComboVariation[] comboVariations;

        [Header("Advanced")]
        [Tooltip("Cancel windows - can be interrupted during these times (0-1)")]
        public Vector2[] cancelWindows;

        [Tooltip("Super armor frames - cannot be interrupted (0-1)")]
        public Vector2[] superArmorWindows;

        [Tooltip("Apply force to user")]
        public Vector3 appliedForce = Vector3.zero;
    }

    [System.Serializable]
    public class ComboVariation
    {
        [Tooltip("Input required (e.g., 'forward', 'jump', 'crouch')")]
        public string inputRequirement;

        [Tooltip("Attack to chain to")]
        public AttackData targetAttack;
    }

    public enum HitboxShape
    {
        Sphere,
        Box,
        Capsule,
        Cone
    }
}
