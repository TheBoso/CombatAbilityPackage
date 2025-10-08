# Combat Ability Package - Claude Context

## Purpose
This package provides a **pure effects-based combat system** for YAAS (Yet Another Ability System) in Unity. It follows a strict zero-coupling architecture where all combat functionality is executed through ScriptableObject-based `AbilityEffect` classes.

## Core Philosophy
- **Zero Coupling**: No controllers, no managers, no dependencies between components
- **Data-Driven**: Create attacks as ScriptableObject assets without writing code
- **Pure Effects**: All logic lives in `AbilityEffect` classes that execute autonomously
- **YAAS Integration**: Built on top of YAAS for cooldowns, requirements, and consumables

## Architecture

### ScriptableObject Data
- **AttackData.cs**: Defines a single attack (damage, timing, hitbox, animations, effects)
- **ComboChain.cs**: Defines a sequence of attacks with damage scaling
- **WeaponData.cs**: Defines a weapon as a collection of abilities (no separate weapon system)

### MonoBehaviour Components
- **ComboTracker.cs**: Tracks combo state per character (AI-compatible, tracks hits not input)
- **CombatSpawnPoint.cs**: Marker component for spawn points (e.g., "WrenchTip", "WeaponMuzzle")
- **WeaponSwitcher.cs**: Equips/unequips weapons by learning/unlearning their abilities

### AbilityEffect Classes (Pure Effects)
All effects inherit from YAAS `AbilityEffect` and implement `IEnumerator PerformEffect(AbilityCaster caller)`:

- **MeleeAttackEffect_v2.cs**: Self-contained melee attack using AttackData
- **ComboEffect.cs**: Auto-chaining combo system using ComboChain
- **ChargeAttackEffect_v2.cs**: Charge-and-release attacks (e.g., Hyperstrike)
- **AOEAttackEffect_v2.cs**: Area-of-effect attacks with multiple shapes
- **ProjectileSpawnerEffect.cs**: Spawns projectiles for shooting weapons

## Key Design Decisions

### Why No Controllers?
Effects execute everything inline within their coroutines. They don't call external managers - they just DO the attack themselves. This eliminates coupling entirely.

### Why MonoBehaviour for ComboTracker?
Originally static, changed to MonoBehaviour for:
- Inspector debugging (visible combo state in editor)
- Automatic cleanup on GameObject destruction
- Still provides static helpers for backward compatibility

### Why Weapons = Abilities?
Weapons are just containers of abilities. Switching weapons = learning/unlearning ability sets. This eliminates the need for a separate weapon management package.

### Why IDamageable Instead of Health Package?
The `IDamageable` interface decouples damage from any specific health system. Targets implement the interface however they want (CoreHealth, custom, etc).

### Why AI-Compatible Combo Tracking?
ComboTracker tracks **hit sequences**, not input. This means AI can use the same combo system as players without needing to simulate button presses.

## Common Workflows

### Creating a Basic Attack
1. Create **AttackData** asset (damage, hitbox, timing, animation)
2. Create **MeleeAttackEffect_v2** asset (references AttackData)
3. Create **AbilityDef** asset (references MeleeAttackEffect_v2)
4. Wire input: `if (input.attackPressed) caster.TryUseAbility(abilityDef);`

### Creating a Combo
1. Create multiple **AttackData** assets (one per combo step)
2. Create **ComboChain** asset (references all AttackData in order)
3. Create **ComboEffect** asset (references ComboChain)
4. Create **AbilityDef** asset (references ComboEffect)
5. Add **ComboTracker** component to character
6. Wire input: `if (input.attackPressed) caster.TryUseAbility(comboAbility);`

### Creating a Shooting Weapon
1. Create **ProjectileSpawnerEffect** asset (projectile prefab, velocity, damage)
2. Create **AbilityDef** asset (references ProjectileSpawnerEffect)
3. Create **WeaponData** asset (add AbilityDef to abilities array)
4. Add **WeaponSwitcher** component to character
5. Equip weapon: `weaponSwitcher.EquipWeapon(weaponData);`

## File Locations
- **Scripts**: `D:\Projects\Git Submodules\CombatAbilityPackage\Scripts\`
- **Documentation**: `D:\Projects\Git Submodules\CombatAbilityPackage\Documentation.html`

## Integration Points

### YAAS (Required)
- All effects inherit from `AbilityEffect`
- Uses `AbilityCaster.TryUseAbility(AbilityDef)`
- Leverages cooldowns, requirements, consumables from YAAS

### Unity Physics (Required)
- Hitbox detection uses `Physics.OverlapBox/Sphere/Capsule`
- Projectiles use `Rigidbody` and `OnTriggerEnter`

### Optional Dependencies
- **WrenchInput.cs**: Custom input component for charge attacks (optional)
- **Health Package**: Any system can implement `IDamageable` interface

## When Adding New Features

### Adding a New Effect Type
1. Create a new class inheriting from `AbilityEffect`
2. Implement `IEnumerator PerformEffect(AbilityCaster caller)`
3. Execute ALL logic inline (find spawn points, play animations, detect hits, etc.)
4. Add `[CreateAssetMenu]` attribute for ScriptableObject creation
5. **DO NOT** create managers/controllers - keep it self-contained

### Adding to Existing Effects
- Read the target effect's code first
- Maintain zero-coupling principle
- Don't add external dependencies
- Keep logic self-contained within the effect's coroutine

### Testing New Features
- Create test ScriptableObject assets
- Add to character's AbilityCaster
- Wire input to `TryUseAbility(abilityDef)`
- Check inspector for debug info (ComboTracker shows active combos)

## Common Pitfalls

### ❌ Don't Create Controllers
```csharp
// BAD - Creates coupling
public class AttackController : MonoBehaviour
{
    public void ExecuteAttack(AttackData data) { ... }
}
```

```csharp
// GOOD - Effect executes itself
public class MeleeAttackEffect : AbilityEffect
{
    public override IEnumerator PerformEffect(AbilityCaster caller)
    {
        // Execute attack inline
    }
}
```

### ❌ Don't Store Input in ComboTracker
ComboTracker tracks **hits**, not button presses. This keeps it AI-compatible.

### ❌ Don't Hard-Reference Health Systems
Use `IDamageable` interface instead. Let targets decide their health implementation.

## Questions to Ask

When planning new features:
1. Can this be a pure effect? (Answer: Usually yes)
2. Does this need a controller? (Answer: Usually no)
3. Can this work for AI? (Answer: Should be yes)
4. Does this couple to external systems? (Answer: Should be no)

## Typical Debugging Steps

1. **Attack not dealing damage?**
   - Check enemy has `IDamageable` interface
   - Verify enemy layer in effect's Hit Layers mask
   - Check spawn point exists (e.g., "WrenchTip")
   - Verify AttackData hitbox size/timing

2. **Combo not chaining?**
   - Check ComboTracker component exists
   - Verify combo window time in ComboEffect
   - Debug log `ComboTracker.GetComboCount()` in inspector
   - Check input timing

3. **Weapon not switching?**
   - Verify WeaponSwitcher component exists
   - Check weapon has abilities assigned
   - Confirm AbilityCaster is present
   - Check attachment points exist

## Version History

- **v1**: Initial implementation with ModularCombatController (DEPRECATED)
- **v2**: Removed controllers, pure effects architecture
- **v2.1**: Changed ComboTracker from static to MonoBehaviour
- **v2.2**: Added weapon system (WeaponData, WeaponSwitcher)
- **v2.3**: Added ProjectileSpawnerEffect for shooting weapons

## Key Files Reference

| File | Type | Purpose |
|------|------|---------|
| AttackData.cs | ScriptableObject | Defines single attack parameters |
| ComboChain.cs | ScriptableObject | Defines combo sequences |
| WeaponData.cs | ScriptableObject | Defines weapons as ability containers |
| ComboTracker.cs | MonoBehaviour | Tracks combo state (AI-compatible) |
| WeaponSwitcher.cs | MonoBehaviour | Equips/unequips weapons |
| MeleeAttackEffect_v2.cs | AbilityEffect | Melee attack execution |
| ComboEffect.cs | AbilityEffect | Auto-chaining combo execution |
| ChargeAttackEffect_v2.cs | AbilityEffect | Charge attack execution |
| AOEAttackEffect_v2.cs | AbilityEffect | Area-of-effect attack execution |
| ProjectileSpawnerEffect.cs | AbilityEffect | Projectile spawning execution |
| IDamageable.cs | Interface | Damage system interface |
| CombatSpawnPoint.cs | MonoBehaviour | Spawn point marker |

## Future Considerations

### If Adding Weapon Upgrades
- Add upgrade data to WeaponData
- Create UpgradeEffect that modifies weapon stats
- Keep upgrade logic in effects, not controllers

### If Adding Ammo System
- WeaponData already has currentAmmo/maxAmmo
- Consume ammo in ProjectileSpawnerEffect
- Check `weaponData.HasAmmo()` before shooting

### If Adding Hit Reactions
- Create HitReactionEffect
- Trigger from IDamageable.TakeDamage()
- Keep reaction logic self-contained

## Philosophy Reminder

**"Everything should be handled by the effects of the ability"** - User requirement

This package exists to prove that combat systems don't need controllers, managers, or complex architectures. Pure effects + ScriptableObjects = maximum flexibility with zero coupling.
