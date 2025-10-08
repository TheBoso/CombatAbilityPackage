using UnityEngine;

namespace RatchetCombat
{
    /// <summary>
    /// Defines a complete combo chain
    /// Links multiple AttackData together with timing and conditions
    /// </summary>
    [CreateAssetMenu(fileName = "New Combo Chain", menuName = "Ratchet Combat/Combo Chain")]
    public class ComboChain : ScriptableObject
    {
        [Header("Combo Identity")]
        [Tooltip("Unique ID for this combo")]
        public string comboID;

        [Tooltip("Display name")]
        public string comboName;

        [Header("Combo Sequence")]
        [Tooltip("Attacks in sequence")]
        public AttackData[] attacks;

        [Tooltip("Global combo window (overrides individual attack windows if > 0)")]
        public float globalComboWindow = 0f;

        [Header("Damage Scaling")]
        [Tooltip("Damage multiplier per combo step")]
        public AnimationCurve damageScaling = AnimationCurve.Linear(0, 1, 1, 1.5f);

        [Header("Finisher")]
        [Tooltip("Special finisher attack (optional)")]
        public AttackData finisherAttack;

        [Tooltip("Can repeat combo after finisher")]
        public bool canLoop = true;

        [Tooltip("Reset combo after this many seconds of no input")]
        public float resetTime = 2f;

        [Header("Requirements")]
        [Tooltip("Minimum combo count to use finisher")]
        public int finisherMinCombo = 3;

        [Tooltip("Can start combo in air")]
        public bool canStartInAir = false;

        [Tooltip("Can continue combo in air")]
        public bool canContinueInAir = true;

        [Header("Branching")]
        [Tooltip("Alternative combo paths based on directional input")]
        public ComboBranch[] branches;

        /// <summary>
        /// Get attack at combo index
        /// </summary>
        public AttackData GetAttack(int comboIndex)
        {
            if (attacks == null || attacks.Length == 0)
                return null;

            // Clamp to valid range
            int index = Mathf.Clamp(comboIndex, 0, attacks.Length - 1);
            return attacks[index];
        }

        /// <summary>
        /// Get next attack in sequence
        /// </summary>
        public AttackData GetNextAttack(int currentIndex)
        {
            if (attacks == null || attacks.Length == 0)
                return null;

            int nextIndex = currentIndex + 1;

            // Check if we should use finisher
            if (finisherAttack != null && nextIndex >= finisherMinCombo && nextIndex >= attacks.Length)
            {
                return finisherAttack;
            }

            // Loop if enabled
            if (canLoop && nextIndex >= attacks.Length)
            {
                return attacks[0];
            }

            // End of combo
            if (nextIndex >= attacks.Length)
            {
                return null;
            }

            return attacks[nextIndex];
        }

        /// <summary>
        /// Check if combo can continue
        /// </summary>
        public bool CanContinueCombo(int currentIndex, float timeSinceLastAttack)
        {
            if (currentIndex < 0 || currentIndex >= attacks.Length)
                return false;

            float window = globalComboWindow > 0 ? globalComboWindow : attacks[currentIndex].comboWindow;

            return timeSinceLastAttack <= window;
        }

        /// <summary>
        /// Get damage multiplier for combo position
        /// </summary>
        public float GetDamageMultiplier(int comboIndex)
        {
            if (attacks == null || attacks.Length == 0)
                return 1f;

            float normalizedIndex = (float)comboIndex / attacks.Length;
            return damageScaling.Evaluate(normalizedIndex);
        }

        /// <summary>
        /// Check for combo branch based on input
        /// </summary>
        public AttackData GetBranchAttack(int currentIndex, string input)
        {
            if (branches == null || branches.Length == 0)
                return null;

            foreach (var branch in branches)
            {
                if (branch.triggerAtComboIndex == currentIndex && branch.inputRequirement == input)
                {
                    return branch.branchAttack;
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public class ComboBranch
    {
        [Tooltip("Combo index where this branch is available")]
        public int triggerAtComboIndex = 1;

        [Tooltip("Required input (e.g., 'forward', 'back', 'up', 'down')")]
        public string inputRequirement;

        [Tooltip("Attack to branch to")]
        public AttackData branchAttack;

        [Tooltip("Does this branch reset combo counter?")]
        public bool resetsCombo = false;
    }
}
