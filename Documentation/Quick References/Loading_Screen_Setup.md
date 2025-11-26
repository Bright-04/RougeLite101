# Loading Screen System - Setup Guide

## Overview

The loading screen system provides smooth fade transitions between scenes and rooms in the dungeon. It includes:

-   Fade in/out effects with customizable durations
-   Progress bar for async scene loading
-   Random loading messages for variety
-   Support for both scene transitions and room-to-room transitions

## Files Created

1. **LoadingScreenManager.cs** - `Assets/Scripts/UI/LoadingScreenManager.cs`
    - Persistent singleton that manages all loading transitions
    - Handles fade effects and progress tracking

## Integration Complete

The following components have been updated to use the loading screen:

-   ✅ **DungeonManager** - Room-to-room transitions and run completion
-   ✅ **ChangeSceneOnCollide** - Scene portal transitions
-   ✅ **PlayerStats** - Death/respawn transitions

## Unity Editor Setup

### Step 1: Create Loading Screen Canvas

1. **Create Canvas GameObject**:

    - Right-click in Hierarchy → UI → Canvas
    - Name it: `LoadingScreenCanvas`
    - Set Canvas properties:
        - Render Mode: Screen Space - Overlay
        - Sort Order: 999 (ensure it's on top of everything)

2. **Add CanvasGroup Component**:

    - Select LoadingScreenCanvas
    - Add Component → Canvas Group
    - This is required for fade effects

3. **Create Fade Panel**:

    - Right-click LoadingScreenCanvas → UI → Image
    - Name it: `FadePanel`
    - Set properties:
        - Anchor: Stretch both (Alt+Shift+Click on stretch preset)
        - Left/Right/Top/Bottom: 0
        - Color: Black (0, 0, 0, 255)
        - Image Type: Simple

4. **Create Loading Text**:

    - Right-click LoadingScreenCanvas → UI → Text - TextMeshPro
    - Name it: `LoadingText`
    - Set properties:
        - Anchor: Bottom Center
        - Position Y: 150
        - Font Size: 48
        - Alignment: Center
        - Color: White
        - Text: "Loading..."

5. **Create Progress Bar (Optional)**:

    - Right-click LoadingScreenCanvas → UI → Slider
    - Name it: `ProgressBar`
    - Set properties:
        - Anchor: Bottom Center
        - Position Y: 80
        - Width: 600, Height: 30
    - Configure Slider:
        - Min Value: 0
        - Max Value: 1
        - Interactable: OFF (display only)
    - Style the Fill Area:
        - Fill: Set color to your theme color (e.g., green or blue)
        - Background: Dark gray or transparent

6. **Create Progress Bar Container** (for showing/hiding):
    - Create Empty GameObject as child of LoadingScreenCanvas
    - Name it: `ProgressBarContainer`
    - Move the ProgressBar slider as child of this container
    - This allows hiding the entire progress section

### Step 2: Add LoadingScreenManager Component

1. Select `LoadingScreenCanvas`
2. Add Component → Search for "Loading Screen Manager"
3. Configure the component in Inspector:

    **UI References**:

    - Canvas Group: Drag LoadingScreenCanvas itself
    - Fade Panel: Drag FadePanel image
    - Loading Text: Drag LoadingText TMP
    - Progress Bar: Drag ProgressBar slider
    - Progress Bar Object: Drag ProgressBarContainer

    **Fade Settings**:

    - Fade In Duration: 0.5 (adjust to preference)
    - Fade Out Duration: 0.5 (adjust to preference)
    - Minimum Display Time: 0.5 (prevents too-fast flashes)

    **Loading Messages** (customize as desired):

    - Element 0: "Loading..."
    - Element 1: "Preparing the dungeon..."
    - Element 2: "Summoning enemies..."
    - Element 3: "Lighting torches..."
    - Element 4: "Polishing treasures..."
    - Element 5: "Sharpening swords..."
    - Element 6: "Brewing potions..."

### Step 3: Mark as Persistent

The LoadingScreenCanvas must persist across scenes:

**Option A - Using GameManager (Recommended)**:

1. Find your GameManager GameObject
2. Add LoadingScreenCanvas to the `Persistent Objects` array
3. This ensures it survives scene loads

**Option B - Manual Setup**:

-   The LoadingScreenManager script already calls `DontDestroyOnLoad(gameObject)` in Awake
-   No additional setup needed if not using GameManager

### Step 4: Test the System

1. **Test Room Transitions**:

    - Start a dungeon run
    - Clear a room and go through the exit door
    - You should see a fade transition with "Transitioning..." message

2. **Test Scene Transitions**:

    - Use any portal with ChangeSceneOnCollide component
    - You should see fade + progress bar + random loading message

3. **Test Death/Respawn**:
    - Let your player die
    - Should see loading screen when returning to GameHome

## Customization Options

### Visual Customization

**Change Fade Panel Color**:

-   Select FadePanel → Set Color in Inspector
-   Black (0,0,0) for dark fade
-   White (255,255,255) for light fade
-   Any color for themed transitions

**Add Animation**:

-   Add Animator component to LoadingText
-   Create animation for rotating icon or pulsing text
-   Trigger animation in LoadingScreenManager if desired

**Add Logo/Icon**:

-   Create UI → Image as child of LoadingScreenCanvas
-   Position above LoadingText
-   Assign your game logo sprite

### Code Customization

**Adjust Timing**:

```csharp
// In LoadingScreenManager Inspector:
fadeInDuration = 0.3f;      // Faster fade in
fadeOutDuration = 0.8f;     // Slower fade out
minimumDisplayTime = 1.0f;  // Show loading longer
```

**Custom Loading Messages**:

```csharp
// Add more messages in Inspector or modify the array
loadingMessages = new string[]
{
    "Your custom message 1...",
    "Your custom message 2...",
    // etc.
};
```

**Different Fade Types**:

```csharp
// Use FadeTransition for simple fade without text/progress
LoadingScreenManager.Instance.FadeTransition(() => {
    // Do something at peak fade
    Debug.Log("Faded!");
});
```

## Disabling Loading Screen

To temporarily disable on specific transitions:

**For ChangeSceneOnCollide**:

-   Select the portal/trigger GameObject
-   In ChangeSceneOnCollide component
-   Uncheck "Use Loading Screen"
-   Will use instant SceneManager.LoadScene instead

## Troubleshooting

**Only see "Transitioning..." text without fade effect**:

-   **MOST COMMON ISSUE**: UI references not assigned in Inspector!
-   Check Console for errors: "CanvasGroup reference is missing!" or "FadePanel reference is missing!"
-   Solution: Select LoadingScreenCanvas → Inspector → LoadingScreenManager component
-   Assign all the required references:
    -   Canvas Group: LoadingScreenCanvas (the canvas itself)
    -   Fade Panel: FadePanel (black Image child)
    -   Loading Text: LoadingText (TextMeshProUGUI)
    -   Progress Bar: ProgressBar (Slider)
    -   Progress Bar Object: ProgressBarContainer (parent GameObject)
-   Without these references, the fade effect won't work!

**Loading screen not appearing at all**:

-   Check Canvas Sort Order is high (999+)
-   Verify Canvas Render Mode is Screen Space - Overlay
-   Ensure Canvas is in every scene or marked persistent
-   Verify LoadingScreenCanvas is added to GameManager's Persistent Objects array

**Fade panel not covering entire screen**:

-   Select FadePanel in hierarchy
-   Check RectTransform settings:
    -   Anchor Preset: Should be "Stretch" (both width and height)
    -   Left, Right, Top, Bottom: All should be 0
-   If using Alt+Shift+Click on stretch preset, it should auto-configure

**Progress bar not updating**:

-   Confirm Progress Bar reference is assigned
-   Check that Progress Bar Container is Active in hierarchy

**Flash effects after transition**:

-   This is normal - loading screen resets properly
-   Entities should handle their own flash state on scene load

**Multiple loading screens appearing**:

-   Ensure only ONE LoadingScreenCanvas exists
-   Check that singleton pattern is working (Instance check in Awake)
-   Verify DontDestroyOnLoad is called

## Integration with New Systems

When adding new scene transitions:

```csharp
// Instead of:
SceneManager.LoadScene("MyScene");

// Use:
if (LoadingScreenManager.Instance != null)
{
    LoadingScreenManager.Instance.LoadSceneAsync("MyScene");
}
else
{
    SceneManager.LoadScene("MyScene"); // Fallback
}
```

For custom room transitions:

```csharp
LoadingScreenManager.Instance.ShowRoomTransition(() => {
    // Your room loading logic here
    LoadRoom();
});
```

## Performance Notes

-   Loading screen uses `Time.unscaledDeltaTime` - works even if game is paused
-   Async scene loading prevents frame drops
-   Minimum display time prevents jarring ultra-fast loads
-   Progress bar updates every frame during async load

## Future Enhancements

Potential additions:

-   [ ] Add tips/hints display during loading
-   [ ] Animated background patterns
-   [ ] Sound effects for transitions
-   [ ] Different fade shapes (circle wipe, etc.)
-   [ ] Loading screen variants per theme
-   [ ] Save/load progress indicators
