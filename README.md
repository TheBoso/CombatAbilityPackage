# Combat Ability Package

Combat effects and utilities for YAAS (Yet Another Ability System).

## Contents

### Core Scripts

**ComboTracker.cs**
- Static utility for tracking combo state across attacks
- No MonoBehaviour coupling
- Supports multiple combo chains per character
- Auto-cleanup to prevent memory leaks

**AttackData.cs**
- ScriptableObject defining complete attack parameters
- Damage, timing, hitboxes, animations, effects
- Supports combo chaining and branching
- Reusable across multiple weapons/abilities

**ComboChain.cs**
- ScriptableObject defining combo sequences
- Links multiple AttackData together
- Damage scaling, finishers, branching
- Auto-reset timing

### Combat Effects (YAAS AbilityEffect)

**CreateMeleeEffect.cs**
- Simple melee attack effect
- Instant sphere overlap damage
- Uses BosoHealth system
- Good for basic melee attacks

**MeleeAttackEffect_v2.cs**
- Advanced melee using AttackData
- Attack duration with hitbox windows
- Multiple hitbox shapes
- Animation, audio, trail integration
- Optional combo tracking
- Uses IDamageable interface

**ChargeAttackEffect_v2.cs**
- Charge-and-release attacks
- Visual/audio feedback during charge
- Input detection (WrenchInput)
- Executes AttackData on release

**AOEAttackEffect_v2.cs**
- Area-of-effect damage
- Multiple shapes (sphere, box, cylinder)
- Knockback support
- Impact effects

**ComboEffect.cs**
- Auto-chaining combo system
- Uses ComboChain ScriptableObject
- Automatic combo tracking
- Damage scaling per combo step

### Utilities

**CombatSpawnPoint.cs**
- Marker component for spawn points
- Used by effects to find hitbox positions, effect spawns, etc.

**FlinchController.cs**
- Handles hit reactions and flinching

## Effect Comparison

### When to use CreateMeleeEffect:
- Simple instant melee hits
- Using BosoHealth system
- Don't need timing/duration
- Basic sphere overlap is enough

### When to use MeleeAttackEffect_v2:
- Complex attack systems (Ratchet & Clank style)
- Need attack duration and timing
- Want animations, trails, effects
- Using AttackData system
- Need combo support

## Usage Examples

### Simple Melee (CreateMeleeEffect)

```csharp
// Create as ScriptableObject asset
// Set damage, radius, spawn point
// Assign to AbilityDef.AbilityEffects
```

### Advanced Melee (MeleeAttackEffect_v2)

```csharp
// 1. Create AttackData asset
Right-click → Create → Ratchet Combat → Attack Data

// 2. Create MeleeAttackEffect_v2 asset
Right-click → Create → Ratchet Combat → Effects → Melee Attack v2
- Attack Data: [Your AttackData]
- Hitbox Spawn Point: "WrenchTip"
- Hit Layers: Enemy

// 3. Create AbilityDef
Right-click → Create → YAAS → AbilityDef
- Ability Effects: [Your MeleeAttackEffect_v2]
```

### Combo System (ComboEffect)

```csharp
// 1. Create multiple AttackData assets (Swing1, Swing2, etc.)

// 2. Create ComboChain asset
Right-click → Create → Ratchet Combat → Combo Chain
- Attacks: [Swing1, Swing2, Swing3, Swing4]

// 3. Create ComboEffect asset
Right-click → Create → Ratchet Combat → Effects → Combo
- Combo Chain: [Your ComboChain]

// 4. Create AbilityDef
- Ability Effects: [Your ComboEffect]
```

### Charge Attack (ChargeAttackEffect_v2)

```csharp
// 1. Create AttackData for charged attack

// 2. Create ChargeAttackEffect_v2 asset
Right-click → Create → Ratchet Combat → Effects → Charge Attack v2
- Charge Time: 1.5
- Charged Attack: [Your AttackData]
- Charge Effect Prefab: [Glow effect]

// 3. Create AbilityDef
- Ability Effects: [Your ChargeAttackEffect_v2]
```

### ComboTracker Usage

```csharp
using RatchetCombat;
using YAAS;

// Get current combo count
int count = ComboTracker.GetComboCount(caster, "wrench_combo");

// Increment combo
ComboTracker.IncrementCombo(caster, "wrench_combo");

// Reset combo
ComboTracker.ResetCombo(caster, "wrench_combo");

// Cleanup when destroyed
void OnDestroy()
{
    ComboTracker.CleanupCaster(GetComponent<AbilityCaster>());
}
```

## Architecture

### Simple (CreateMeleeEffect)
```
CreateMeleeEffect → Instant damage → BosoHealth
```

### Advanced (AttackData System)
```
AttackData.asset (defines attack)
    ↓
MeleeAttackEffect_v2/ComboEffect/etc. (executes attack)
    ↓
AbilityDef (wraps with cooldowns/requirements)
    ↓
AbilityCaster (activates)
```

## Dependencies

- **Required**: YAAS (Yet Another Ability System)
- **Required**: Unity Physics
- **Optional**: Boso.CoreHealth (only for CreateMeleeEffect)
- **Optional**: WrenchInput (only for ChargeAttackEffect_v2)

## Integration with Projects

### For Ratchet & Clank Style Combat:
Use the v2 effects with AttackData system for full control over timing, combos, and effects.

### For Simple Combat:
Use CreateMeleeEffect for quick, straightforward melee attacks.

### For Other Projects:
Mix and match! Use ComboTracker and AttackData for any game that needs combo systems.

## File Structure

```
CombatAbilityPackage-main/Scripts/
├── Core Data
│   ├── AttackData.cs
│   └── ComboChain.cs
├── Effects (Simple)
│   └── CreateMeleeEffect.cs
├── Effects (Advanced)
│   ├── MeleeAttackEffect_v2.cs
│   ├── ChargeAttackEffect_v2.cs
│   ├── AOEAttackEffect_v2.cs
│   └── ComboEffect.cs
├── Utilities
│   ├── ComboTracker.cs
│   ├── CombatSpawnPoint.cs
│   └── FlinchController.cs
└── README.md
```

## License

[Your License Here]
