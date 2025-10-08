using UnityEngine;
using System.Collections.Generic;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// Manages weapon switching by equipping/unequipping abilities
    /// Weapons are just ability containers - no separate weapon system needed!
    /// </summary>
    [RequireComponent(typeof(AbilityCaster))]
    public class WeaponSwitcher : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("All available weapons")]
        public WeaponData[] availableWeapons;

        [Tooltip("Starting weapon (optional)")]
        public WeaponData startingWeapon;

        [Tooltip("Parent transform for weapon models")]
        public Transform weaponParent;

        [Header("Current State")]
        [SerializeField] private WeaponData currentWeapon;
        [SerializeField] private GameObject currentWeaponModel;
        [SerializeField] private int currentWeaponIndex = -1;

        private AbilityCaster abilityCaster;
        private AudioSource audioSource;
        private Dictionary<string, Transform> attachmentPoints = new Dictionary<string, Transform>();

        #region Events
        public System.Action<WeaponData> OnWeaponEquipped;
        public System.Action<WeaponData> OnWeaponUnequipped;
        public System.Action<WeaponData, WeaponData> OnWeaponSwitched;
        #endregion

        void Start()
        {
            abilityCaster = GetComponent<AbilityCaster>();
            audioSource = GetComponent<AudioSource>();

            // Find attachment points
            CacheAttachmentPoints();

            // Equip starting weapon
            if (startingWeapon != null)
            {
                EquipWeapon(startingWeapon);
            }
        }

        void Update()
        {
            // Example: Number keys for weapon switching
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToWeaponIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToWeaponIndex(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToWeaponIndex(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchToWeaponIndex(3);

            // Mouse wheel weapon switching
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0) CycleWeaponNext();
            if (scroll < 0) CycleWeaponPrevious();
        }

        #region Weapon Switching

        /// <summary>
        /// Equip a weapon by reference
        /// </summary>
        public void EquipWeapon(WeaponData weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("WeaponSwitcher: Cannot equip null weapon!");
                return;
            }

            // Store previous weapon
            WeaponData previousWeapon = currentWeapon;

            // Unequip current weapon first
            if (currentWeapon != null)
            {
                UnequipCurrentWeapon();
            }

            // Set new weapon
            currentWeapon = weapon;

            // Update index
            currentWeaponIndex = System.Array.IndexOf(availableWeapons, weapon);

            // Learn weapon abilities
            if (weapon.abilities != null && weapon.abilities.Length > 0)
            {
                foreach (var ability in weapon.abilities)
                {
                    if (ability != null)
                    {
                        abilityCaster.LearnAbility(ability);
                    }
                }
            }

            // Spawn weapon model
            SpawnWeaponModel(weapon);

            // Play equip sound
            if (audioSource != null && weapon.equipSound != null)
            {
                audioSource.PlayOneShot(weapon.equipSound);
            }

            // Spawn equip effect
            if (weapon.equipEffect != null)
            {
                Instantiate(weapon.equipEffect, transform.position, Quaternion.identity);
            }

            // Fire events
            OnWeaponEquipped?.Invoke(weapon);
            if (previousWeapon != null)
            {
                OnWeaponSwitched?.Invoke(previousWeapon, weapon);
            }

            Debug.Log($"Equipped weapon: {weapon.weaponName}");
        }

        /// <summary>
        /// Unequip current weapon
        /// </summary>
        public void UnequipCurrentWeapon()
        {
            if (currentWeapon == null)
                return;

            // Unlearn weapon abilities
            if (currentWeapon.abilities != null && currentWeapon.abilities.Length > 0)
            {
                foreach (var ability in currentWeapon.abilities)
                {
                    if (ability != null)
                    {
                        abilityCaster.UnLearnAbility(ability);
                    }
                }
            }

            // Destroy weapon model
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                currentWeaponModel = null;
            }

            // Play unequip sound
            if (audioSource != null && currentWeapon.unequipSound != null)
            {
                audioSource.PlayOneShot(currentWeapon.unequipSound);
            }

            // Fire event
            OnWeaponUnequipped?.Invoke(currentWeapon);

            currentWeapon = null;
            currentWeaponIndex = -1;
        }

        /// <summary>
        /// Switch to weapon by index
        /// </summary>
        public void SwitchToWeaponIndex(int index)
        {
            if (index < 0 || index >= availableWeapons.Length)
                return;

            if (availableWeapons[index] == null)
                return;

            EquipWeapon(availableWeapons[index]);
        }

        /// <summary>
        /// Cycle to next weapon
        /// </summary>
        public void CycleWeaponNext()
        {
            if (availableWeapons == null || availableWeapons.Length == 0)
                return;

            int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
            SwitchToWeaponIndex(nextIndex);
        }

        /// <summary>
        /// Cycle to previous weapon
        /// </summary>
        public void CycleWeaponPrevious()
        {
            if (availableWeapons == null || availableWeapons.Length == 0)
                return;

            int prevIndex = currentWeaponIndex - 1;
            if (prevIndex < 0)
                prevIndex = availableWeapons.Length - 1;

            SwitchToWeaponIndex(prevIndex);
        }

        #endregion

        #region Weapon Model Management

        private void SpawnWeaponModel(WeaponData weapon)
        {
            if (weapon.weaponModelPrefab == null)
                return;

            // Find attachment point
            Transform attachPoint = GetAttachmentPoint(weapon.attachmentPoint);

            if (attachPoint == null)
            {
                Debug.LogWarning($"WeaponSwitcher: Attachment point '{weapon.attachmentPoint}' not found! Using weaponParent.");
                attachPoint = weaponParent != null ? weaponParent : transform;
            }

            // Spawn model
            currentWeaponModel = Instantiate(weapon.weaponModelPrefab, attachPoint);

            // Apply offsets
            currentWeaponModel.transform.localPosition = weapon.positionOffset;
            currentWeaponModel.transform.localRotation = Quaternion.Euler(weapon.rotationOffset);
        }

        private void CacheAttachmentPoints()
        {
            // Find common attachment points in children
            CombatSpawnPoint[] spawnPoints = GetComponentsInChildren<CombatSpawnPoint>(true);

            foreach (var point in spawnPoints)
            {
                if (!attachmentPoints.ContainsKey(point.name))
                {
                    attachmentPoints[point.name] = point.transform;
                }
            }

            // Also cache by GameObject name
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (!attachmentPoints.ContainsKey(t.name))
                {
                    attachmentPoints[t.name] = t;
                }
            }
        }

        private Transform GetAttachmentPoint(string pointName)
        {
            if (string.IsNullOrEmpty(pointName))
                return weaponParent != null ? weaponParent : transform;

            if (attachmentPoints.TryGetValue(pointName, out Transform point))
            {
                return point;
            }

            return weaponParent != null ? weaponParent : transform;
        }

        #endregion

        #region Public Getters

        /// <summary>
        /// Get currently equipped weapon
        /// </summary>
        public WeaponData GetCurrentWeapon()
        {
            return currentWeapon;
        }

        /// <summary>
        /// Get current weapon index
        /// </summary>
        public int GetCurrentWeaponIndex()
        {
            return currentWeaponIndex;
        }

        /// <summary>
        /// Check if a specific weapon is equipped
        /// </summary>
        public bool IsWeaponEquipped(WeaponData weapon)
        {
            return currentWeapon == weapon;
        }

        /// <summary>
        /// Get all available weapons
        /// </summary>
        public WeaponData[] GetAvailableWeapons()
        {
            return availableWeapons;
        }

        #endregion
    }
}
