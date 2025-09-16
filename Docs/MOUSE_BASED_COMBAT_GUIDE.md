# Mouse-Based Combat System Documentation

## Overview
The character combat system has been enhanced to provide responsive mouse-based controls where the player character and attacks follow the mouse cursor direction for more intuitive combat.

## Movement System Architecture

### **Dual Script Setup**
The project uses two complementary movement scripts:

1. **`SimplePlayerMovement.cs`** - Primary movement controller
   - Physics-based movement using `Rigidbody2D.linearVelocity`
   - **Fast movement support** with Shift key (10 → 50 speed)
   - Input System compatibility with fallbacks
   - Infinite world exploration features

2. **`PlayerController.cs`** - Combat and interaction controller  
   - **Character facing direction** (mouse-based)
   - Combat system integration
   - Event broadcasting for game systems
   - **Movement disabled** to prevent conflicts

### **Input System Integration**
Fast movement supports multiple input methods:
- **Primary:** InputActionReference (fastMoveAction)
- **Secondary:** New Input System keyboard (Shift keys)
- **Fallback:** Legacy Input system (configurable key)

## Features Implemented

### ⚡ **Fast Movement System**
- Hold **Left Shift** for 5x faster movement (10 → 50 speed)
- Works with WASD, Arrow Keys, or Input System actions
- Supports both new Input System and legacy input
- Smooth physics-based acceleration and deceleration

### 🎯 **Mouse-Based Character Facing**
- Player character sprite automatically flips to face the mouse cursor
- Works with both Unity's new Input System and legacy Input system  
- Smooth, responsive controls for combat scenarios
- Replaces movement-based facing for better combat feel

### ⚔️ **Mouse-Directed Attacks**
- Sword attacks are positioned and oriented toward mouse cursor
- Attack animations appear in the correct direction relative to mouse position
- Slash effects rotate and flip appropriately based on mouse direction

### 🔥 **Spell System Integration**
- **Fireball spells** (Key: 1) fire toward mouse cursor position
- **Correct sprite orientation** - fireball shows proper back/tail as it travels
- **World space movement** ensures spells travel in intended direction
- **Rotation calculation** with 180-degree offset for proper visual appearance

### 🔧 **Robust Input System Support**
- Primary support for Unity's new Input System (InputSystem package)
- Automatic fallback to legacy Input system if new system unavailable
- Proper error handling and null checking for all input scenarios

## Technical Implementation

### PlayerController Changes

#### Key Method: `TryFaceMouseDirection()`
```csharp
private bool TryFaceMouseDirection()
{
    // Convert mouse screen position to world position
    // Compare mouse X position with player X position  
    // Flip character sprite based on mouse direction
    // Return true if successful, false for fallback
}
```

**Features:**
- Uses `Mouse.current.position.ReadValue()` for new Input System
- Falls back to `Input.mousePosition` for legacy input
- Converts screen coordinates to world coordinates using camera
- Updates both `mySpriteRender.flipX` and `FacingLeft` properties

#### Integration in `AdjustPlayerFacingDirection()`
- Called every `FixedUpdate()` for consistent updates
- Prioritizes mouse direction over movement-based facing
- Provides smooth, real-time character orientation

### Sword System Changes

#### Enhanced Mouse Following
- `MouseFollowWithOffset()` method handles sword positioning
- `PositionSlashAnimationTowardsMouse()` for attack effects
- Supports both world space and screen space calculations
- Proper rotation and flipping for left/right mouse positions

## Troubleshooting Guide

### Common Issues and Solutions

#### ❌ **Spell Visual Orientation Issues**
**Symptoms:** Spells travel correctly but sprite faces wrong direction
**Solutions:**
1. **Rotation Offset:** Add 180-degree offset if sprite faces toward player instead of target
2. **Sprite Design:** Check if sprite asset is designed facing left/right by default
3. **Movement vs Visual:** Ensure movement direction and visual rotation are properly synchronized

#### ❌ **Fast Movement Not Working**
**Symptoms:** Shift key detected but speed doesn't increase
**Solutions:**
1. **Movement Script Conflicts:** Ensure only one script controls Rigidbody2D movement
   - `SimplePlayerMovement.cs` should handle physics movement
   - `PlayerController.cs` Move() method should be disabled to prevent conflicts
2. **Input System Priority:** Check that Input System fallbacks are working correctly
3. **Inspector Values:** Verify `fastMoveSpeed` is set higher than `moveSpeed` (e.g., 50 vs 10)

#### ❌ **Character Not Flipping**
**Symptoms:** Character sprite doesn't change direction with mouse movement
**Solutions:**
1. **Check Component Status:** Ensure PlayerController script component is **enabled** in Unity Inspector
2. **Verify GameObject:** Make sure Player GameObject is active in hierarchy
3. **Input System:** Confirm InputSystem package is installed and configured

#### ❌ **No Mouse Input Detection**
**Symptoms:** System falls back to movement-based facing
**Solutions:**
1. **Camera Reference:** Ensure `Camera.main` is properly tagged and available
2. **Input System Setup:** Verify Input System is initialized in project settings
3. **Mouse Device:** Check that `Mouse.current` is available (may be null in some environments)

#### ❌ **Inconsistent Attack Direction**
**Symptoms:** Attacks don't always follow mouse correctly
**Solutions:**
1. **Sword Component:** Verify Sword script has PlayerController reference assigned
2. **Camera Setup:** Ensure camera is properly positioned for world space calculations
3. **Input Timing:** Check that Input System updates are not conflicting

### Debug Information

#### Enabling Debug Mode
To enable detailed debugging, temporarily add logging:
```csharp
// In TryFaceMouseDirection()
Debug.Log($"Mouse X: {mouseX:F2}, Player X: {playerX:F2}, Should face left: {mouseX < playerX}");
```

#### Key Debug Points
- Mouse screen position detection
- World position conversion accuracy
- Sprite flip state changes
- Input system availability

## Component Dependencies

### Required Components (PlayerController)
- `SpriteRenderer` - For character sprite flipping
- `Rigidbody2D` - For physics-based movement
- `Animator` - For character animations
- Input System package - For mouse input detection

### Required Setup
- Player GameObject must be active
- PlayerController script must be enabled
- Camera.main must be properly tagged
- Input System properly configured in Project Settings

## Performance Considerations

### Optimizations Implemented
- **Cached Components:** SpriteRenderer reference cached in Awake()
- **Efficient Calculations:** Minimal math operations per frame
- **Error Handling:** Try-catch blocks prevent performance hitches
- **Fallback Systems:** Graceful degradation when components unavailable

### Performance Tips
- Mouse direction updates run in FixedUpdate() for consistency
- World space calculations only when necessary
- Automatic fallback reduces computational overhead

## Future Enhancements

### Potential Improvements
- **Minimum Distance Threshold:** Prevent jittering with small mouse movements
- **Smoothed Rotation:** Interpolated character facing for smoother visuals
- **Input Buffering:** Queue mouse inputs for more responsive combat
- **Multi-Input Support:** Gamepad and keyboard-only support options

### Integration Opportunities
- **Animation Blending:** Smooth transitions between facing directions
- **Combat Combos:** Mouse direction-based attack combinations
- **Skill Targeting:** Mouse-based spell and ability targeting
- **Camera Integration:** Dynamic camera following based on mouse position

## Version History

### v1.0 - Mouse-Based Combat Implementation
- ✅ Character faces mouse cursor
- ✅ Attacks follow mouse direction
- ✅ Input System integration with fallback
- ✅ Robust error handling and debugging
- ✅ Component status troubleshooting resolved

---

*This system provides the foundation for responsive, mouse-driven combat that feels intuitive and precise for players.*