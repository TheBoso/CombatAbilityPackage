using UnityEngine;
using YAAS;

namespace RatchetCombat
{
    /// <summary>
    /// ScriptableObject defining a weapon and its abilities
    /// Weapons are just collections of abilities that get equipped/unequipped
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Ratchet Combat/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Identity")]
        [Tooltip("Unique weapon ID")]
        public string weaponID;

        [Tooltip("Display name")]
        public string weaponName;

        [Tooltip("Weapon description")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("Weapon icon for UI")]
        public Sprite weaponIcon;

        [Header("Weapon Model")]
        [Tooltip("Weapon model prefab")]
        public GameObject weaponModelPrefab;

        [Tooltip("Weapon attachment point (e.g., RightHand, Back)")]
        public string attachmentPoint = "RightHand";

        [Tooltip("Position offset from attachment point")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Rotation offset from attachment point")]
        public Vector3 rotationOffset = Vector3.zero;

        [Header("Abilities")]
        [Tooltip("All abilities this weapon grants")]
        public AbilityDef[] abilities;

        [Header("Weapon Stats")]
        [Tooltip("Weapon level/upgrade tier")]
        public int weaponLevel = 1;

        [Tooltip("Max weapon level")]
        public int maxWeaponLevel = 5;

        [Tooltip("Current ammo (if applicable, -1 = unlimited)")]
        public int currentAmmo = -1;

        [Tooltip("Max ammo (if applicable)")]
        public int maxAmmo = -1;

        [Header("Audio")]
        [Tooltip("Equip sound")]
        public AudioClip equipSound;

        [Tooltip("Unequip sound")]
        public AudioClip unequipSound;

        [Header("Effects")]
        [Tooltip("Effect to spawn on equip")]
        public GameObject equipEffect;

        /// <summary>
        /// Check if weapon uses ammo
        /// </summary>
        public bool UsesAmmo()
        {
            return maxAmmo > 0;
        }

        /// <summary>
        /// Check if weapon has ammo
        /// </summary>
        public bool HasAmmo()
        {
            return !UsesAmmo() || currentAmmo > 0;
        }

        /// <summary>
        /// Consume ammo
        /// </summary>
        public bool ConsumeAmmo(int amount = 1)
        {
            if (!UsesAmmo())
                return true;

            if (currentAmmo >= amount)
            {
                currentAmmo -= amount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add ammo
        /// </summary>
        public void AddAmmo(int amount)
        {
            if (!UsesAmmo())
                return;

            currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        }

        /// <summary>
        /// Refill ammo to max
        /// </summary>
        public void RefillAmmo()
        {
            if (UsesAmmo())
            {
                currentAmmo = maxAmmo;
            }
        }
    }
}
