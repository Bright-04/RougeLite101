# Null Checks Quick Reference

## 🚀 Quick Implementation Patterns

### Component Validation Template
```csharp
private void Awake()
{
    component = GetComponent<RequiredComponent>();
    if (component == null)
    {
        Debug.LogError($"{GetType().Name}: {nameof(RequiredComponent)} missing on {gameObject.name}!", this);
        enabled = false; // Optional: disable script
    }
}
```

### Method Guard Clause Template
```csharp
private void SomeMethod()
{
    if (criticalComponent == null)
    {
        Debug.LogWarning($"{GetType().Name}: Cannot execute {nameof(SomeMethod)}, component is null.");
        return;
    }
    
    // Safe to proceed
    criticalComponent.DoSomething();
}
```

### Array Validation Template
```csharp
if (array != null && index >= 0 && index < array.Length && array[index] != null)
{
    // Safe array access
    array[index].DoSomething();
}
```

## 📝 Error Message Format

**Template:**
```
[ScriptName]: [ComponentType] [missing/null] on [ObjectName]! [Impact description].
```

**Example:**
```
PlayerController: Rigidbody2D missing on Player! Movement will not work.
```

## 🎯 Common Null Check Locations

| Script Type | Critical Components | Check Location |
|-------------|-------------------|----------------|
| **Player Controller** | Rigidbody2D, Animator, SpriteRenderer | `Awake()` + Runtime |
| **Weapon Systems** | Parent components, Prefab references | `Awake()` + Attack methods |
| **Spell Systems** | PlayerStats, Arrays, Camera | `Awake()` + Cast methods |
| **AI Systems** | Movement components, Player reference | `Awake()` + Behavior methods |

## ⚡ Performance Tips

- ✅ Cache null check results for components that don't change
- ✅ Use null-conditional operators (`?.`) for simple cases  
- ✅ Validate in `Awake()`/`Start()`, not `Update()`
- ❌ Avoid redundant checks in tight loops

## 🔧 Unity-Specific Patterns

```csharp
// Parent/Child component searching
playerController = GetComponentInParent<PlayerController>();
weaponCollider = GetComponentInChildren<Collider2D>();

// Safe Unity object destruction check
if (gameObject != null && !gameObject.Equals(null))
{
    // Safe to use GameObject
}

// Input system null checks
if (Mouse.current != null && Keyboard.current != null)
{
    // Safe to read input
}
```