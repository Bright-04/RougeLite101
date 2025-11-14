# Scene Transition Setup Guide

## Overview

A simple fade in/fade out transition system has been created for room transitions in your roguelite game.

## Files Created/Modified

### New Files:

-   `Assets/Scripts/UI/SceneTransition.cs` - Main transition controller

### Modified Files:

-   `Assets/Scripts/Dungeon/DungeonManager.cs` - Now uses fade transitions between rooms

## Unity Setup Instructions

### 1. Create the Scene Transition UI

1. **Open your main game scene** (where DungeonManager exists)

2. **Create the UI hierarchy:**

    - Right-click in Hierarchy → UI → Canvas (if you don't have one already)
    - Name it "SceneTransitionCanvas" (or use existing canvas)
    - Right-click on the Canvas → UI → Image
    - Name it "FadePanel"

3. **Configure the Canvas:**

    - Set Render Mode to "Screen Space - Overlay"
    - Set Canvas Scaler to "Scale With Screen Size" (recommended)
    - Reference Resolution: 1920x1080 (or your target resolution)

4. **Configure the FadePanel:**

    - **RectTransform**: Click the anchor preset button (top-left of RectTransform), hold Alt+Shift, and click the bottom-right option (stretch to fill)
    - This makes the panel cover the entire screen
    - Set all Position values to 0 (Left: 0, Top: 0, Right: 0, Bottom: 0)
    - **Image Component**:
        - Color: Black (R: 0, G: 0, B: 0, A: 0) - Note: Alpha starts at 0
        - Remove the Source Image (set to None)
    - **Add Raycast Target**: Uncheck "Raycast Target" to avoid blocking input

5. **Add the SceneTransition Component:**

    - Select the Canvas (or create a new empty GameObject as a child)
    - Add Component → SceneTransition script
    - In the Inspector:
        - **Fade Panel**: Drag the FadePanel Image here
        - **Fade Duration**: 0.5 (adjust to your preference, 0.3-0.8 is typical)
        - **Fade Color**: Black (or any color you prefer)

6. **Set Canvas Sorting Order:**
    - Select the Canvas
    - Set "Sort Order" to a high value (e.g., 100) to ensure it renders on top of everything

### 2. Testing

1. Enter Play Mode
2. The first room should fade in from black
3. Complete a room and enter the exit door
4. You should see a fade out → room change → fade in transition

### 3. Customization Options

In the SceneTransition component, you can adjust:

-   **Fade Duration**: How long the fade takes (in seconds)
    -   Faster transitions: 0.3 seconds
    -   Standard: 0.5 seconds
    -   Slower, dramatic: 0.8-1.0 seconds
-   **Fade Color**:
    -   Black (default): Classic fade
    -   White: Dream-like transition
    -   Any color: Match your game's aesthetic

### 4. Troubleshooting

**Problem: No fade effect**

-   Ensure SceneTransition script is active and in the scene
-   Check that Fade Panel is assigned in the Inspector
-   Verify Canvas is set to Screen Space - Overlay

**Problem: Fade panel blocks the game**

-   Uncheck "Raycast Target" on the FadePanel Image component
-   Ensure the panel alpha is 0 when not fading

**Problem: Can see UI elements during fade**

-   Increase the Canvas Sort Order to 100 or higher

**Problem: Fade is too fast/slow**

-   Adjust the "Fade Duration" value in the SceneTransition component

## Code Usage

If you want to use the transition system in other scripts:

```csharp
// Simple fade out then in
yield return SceneTransition.Instance.FadeOutAndIn(() => {
    // Code to execute when screen is black
});

// Or separate control
yield return SceneTransition.Instance.FadeOut();
// Do something
yield return SceneTransition.Instance.FadeIn();
```

## Notes

-   The SceneTransition persists between scenes (DontDestroyOnLoad)
-   Only one instance exists at a time (Singleton pattern)
-   The transition is coroutine-based, so it plays smoothly without blocking
