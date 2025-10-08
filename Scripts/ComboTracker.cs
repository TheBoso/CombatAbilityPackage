using UnityEngine;
using System.Collections.Generic;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// MonoBehaviour for tracking combo state across attacks
    /// Add this component to the same GameObject as AbilityCaster
    /// Supports multiple combo chains per character
    /// </summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class ComboTracker : MonoBehaviour
    {
        [System.Serializable]
        private class ComboState
        {
            public string comboKey;
            public int count;
            public float lastHitTime;

            public ComboState(string key)
            {
                comboKey = key;
                count = 0;
                lastHitTime = 0f;
            }
        }

        [Header("Debug Info")]
        [SerializeField] private List<ComboState> activeCombos = new List<ComboState>();

        // comboKey -> state
        private Dictionary<string, ComboState> combos = new Dictionary<string, ComboState>();

        #region Public Methods

        /// <summary>
        /// Get current combo count for a specific combo chain
        /// </summary>
        public int GetComboCount(string comboKey)
        {
            ComboState state = GetOrCreateState(comboKey);
            return state.count;
        }

        /// <summary>
        /// Get time of last hit in combo
        /// </summary>
        public float GetLastHitTime(string comboKey)
        {
            ComboState state = GetOrCreateState(comboKey);
            return state.lastHitTime;
        }

        /// <summary>
        /// Increment combo count
        /// </summary>
        public void IncrementCombo(string comboKey)
        {
            ComboState state = GetOrCreateState(comboKey);
            state.count++;
            state.lastHitTime = Time.time;

            UpdateDebugList();
        }

        /// <summary>
        /// Reset combo to 0
        /// </summary>
        public void ResetCombo(string comboKey)
        {
            ComboState state = GetOrCreateState(comboKey);
            state.count = 0;
            state.lastHitTime = 0f;

            UpdateDebugList();
        }

        /// <summary>
        /// Reset all combos
        /// </summary>
        public void ResetAllCombos()
        {
            foreach (var combo in combos.Values)
            {
                combo.count = 0;
                combo.lastHitTime = 0f;
            }

            UpdateDebugList();
        }

        /// <summary>
        /// Check if combo should auto-reset based on time
        /// </summary>
        public bool ShouldResetCombo(string comboKey, float resetTime)
        {
            ComboState state = GetOrCreateState(comboKey);
            return Time.time - state.lastHitTime > resetTime;
        }

        #endregion

        #region Internal Methods

        private ComboState GetOrCreateState(string comboKey)
        {
            if (!combos.ContainsKey(comboKey))
            {
                ComboState newState = new ComboState(comboKey);
                combos[comboKey] = newState;
                UpdateDebugList();
            }

            return combos[comboKey];
        }

        private void UpdateDebugList()
        {
            activeCombos.Clear();
            foreach (var combo in combos.Values)
            {
                activeCombos.Add(combo);
            }
        }

        #endregion

        #region Static Helper (for backward compatibility)

        /// <summary>
        /// Static helper to get ComboTracker from AbilityCaster
        /// Maintains compatibility with existing code
        /// </summary>
        public static ComboTracker GetTracker(AbilityCaster caster)
        {
            if (caster == null)
            {
                Debug.LogError("ComboTracker: AbilityCaster is null!");
                return null;
            }

            ComboTracker tracker = caster.GetComponent<ComboTracker>();

            if (tracker == null)
            {
                Debug.LogWarning($"ComboTracker: No ComboTracker found on {caster.gameObject.name}. Adding one automatically.");
                tracker = caster.gameObject.AddComponent<ComboTracker>();
            }

            return tracker;
        }

        /// <summary>
        /// Static helper - Get combo count (backward compatibility)
        /// </summary>
        public static int GetComboCount(AbilityCaster caster, string comboKey)
        {
            ComboTracker tracker = GetTracker(caster);
            return tracker != null ? tracker.GetComboCount(comboKey) : 0;
        }

        /// <summary>
        /// Static helper - Get last hit time (backward compatibility)
        /// </summary>
        public static float GetLastHitTime(AbilityCaster caster, string comboKey)
        {
            ComboTracker tracker = GetTracker(caster);
            return tracker != null ? tracker.GetLastHitTime(comboKey) : 0f;
        }

        /// <summary>
        /// Static helper - Increment combo (backward compatibility)
        /// </summary>
        public static void IncrementCombo(AbilityCaster caster, string comboKey)
        {
            ComboTracker tracker = GetTracker(caster);
            tracker?.IncrementCombo(comboKey);
        }

        /// <summary>
        /// Static helper - Reset combo (backward compatibility)
        /// </summary>
        public static void ResetCombo(AbilityCaster caster, string comboKey)
        {
            ComboTracker tracker = GetTracker(caster);
            tracker?.ResetCombo(comboKey);
        }

        /// <summary>
        /// Static helper - Reset all combos (backward compatibility)
        /// </summary>
        public static void ResetAllCombos(AbilityCaster caster)
        {
            ComboTracker tracker = GetTracker(caster);
            tracker?.ResetAllCombos();
        }

        /// <summary>
        /// Static helper - Should reset combo (backward compatibility)
        /// </summary>
        public static bool ShouldResetCombo(AbilityCaster caster, string comboKey, float resetTime)
        {
            ComboTracker tracker = GetTracker(caster);
            return tracker != null ? tracker.ShouldResetCombo(comboKey, resetTime) : true;
        }

        #endregion

        #region Unity Lifecycle

        void OnDestroy()
        {
            // Automatic cleanup when component is destroyed
            combos.Clear();
            activeCombos.Clear();
        }

        #endregion
    }
}
