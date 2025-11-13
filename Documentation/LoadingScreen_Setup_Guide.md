# Loading Screen Setup Guide

This guide will help you set up the loading screen that appears between room transitions.

## What You've Got

I've created a `LoadingScreen.cs` script that manages the loading screen with:
- Smooth fade in/out animations
- Progress bar
- Random loading tips
- Configurable timings

## Setup Instructions

### Step 1: Create Loading Screen UI

1. **Open the Dungeon scene** (`Assets/Scenes/Dungeon.unity`)

2. **Create a new Canvas** (if you want a separate one) or use the existing `Canvas_UI`:
   - Right-click in Hierarchy → UI → Canvas
   - Name it: `LoadingScreenCanvas`
   - Set Render Mode to **Screen Space - Overlay**
   - Set Canvas Scaler to **Scale With Screen Size** (Reference Resolution: 1920x1080)

3. **Create the Loading Screen Panel**:
   - Right-click on Canvas → UI → Panel
   - Name it: `LoadingScreen`
   - Set Color to black or dark color (RGBA: 0, 0, 0, 200-255 for transparency)
   - Stretch it to full screen using Anchors (set to stretch in both directions)

4. **Add CanvasGroup Component**:
   - Select `LoadingScreen` panel
   - Add Component → Canvas Group
   - This will be used for fade in/out effects

5. **Create Loading Text**:
   - Right-click on LoadingScreen → UI → Text - TextMeshPro
   - Name it: `LoadingText`
   - Position: Center-Top (adjust Y to around -100)
   - Text: "Loading..."
   - Font Size: 48-60
   - Alignment: Center
   - Color: White

6. **Create Progress Bar**:
   - Right-click on LoadingScreen → UI → Slider
   - Name it: `ProgressBar`
   - Position: Center (Y around -200)
   - Delete the "Handle Slide Area" child (we don't need it)
   - Select "Fill Area" → "Fill":
     - Set color to something nice (green, blue, etc.)
   - Select the Slider component and set:
     - Interactable: OFF (we just want to display progress)
     - Transition: None
   - **Keep reference to the Fill Image** - we'll use this in the script

7. **Create Tip Text**:
   - Right-click on LoadingScreen → UI → Text - TextMeshPro
   - Name it: `TipText`
   - Position: Bottom-Center (Y around -400)
   - Text: "Tip: This is a helpful tip"
   - Font Size: 24-32
   - Alignment: Center
   - Color: Light gray or yellow
   - Enable Word Wrapping
   - Set max width to around 1400-1600

### Step 2: Configure LoadingScreen Script

1. **Add LoadingScreen component** to the `LoadingScreen` panel
2. **Assign References**:
   - Canvas Group: Drag the LoadingScreen's Canvas Group component
   - Loading Text: Drag the LoadingText TextMeshPro component
   - Progress Bar: Drag the **Fill Image** from ProgressBar → Fill Area → Fill
   - Tip Text: Drag the TipText TextMeshPro component

3. **Configure Settings** (or leave defaults):
   - Fade In Duration: 0.3 seconds
   - Fade Out Duration: 0.3 seconds
   - Minimum Display Time: 0.5 seconds
   - Loading Tips: Add or modify tips in the array

### Step 3: Make it Persistent (Optional but Recommended)

Since we want the loading screen to work across rooms:

1. **Create a Prefab**:
   - Drag the entire `LoadingScreenCanvas` (or just `LoadingScreen` panel) to `Assets/Prefabs/UI/`
   - Name it: `LoadingScreen.prefab`

2. **Ensure it persists**:
   - The LoadingScreen script already has `DontDestroyOnLoad(gameObject)` built in
   - Just make sure the LoadingScreen GameObject is active at the start of the scene

### Step 4: Test It

1. **Play the game**
2. Clear a room and enter the exit door
3. You should see:
   - Loading screen fades in (0.3 seconds)
   - Shows a random tip
   - Progress bar fills
   - Stays visible for minimum 0.5 seconds
   - Fades out (0.3 seconds)
   - Next room loads

## Customization Options

### Change Timing
In the LoadingScreen component:
- **Fade In Duration**: How fast the screen appears
- **Fade Out Duration**: How fast the screen disappears
- **Minimum Display Time**: Minimum time to show (prevents flickering)

### Change Appearance
- Modify the Panel color/transparency
- Change text colors and fonts
- Adjust progress bar colors
- Add background images or animations

### Add More Tips
In the LoadingScreen component, expand the "Loading Tips" array and add more helpful hints!

### Advanced: Add Animations
You can add an Animator component to the LoadingScreen panel and create animations like:
- Pulsing "Loading..." text
- Rotating loading icon
- Animated background
- Particle effects

## Quick Visual Hierarchy

```
Canvas_LoadingScreen
└── LoadingScreen (Panel + CanvasGroup + LoadingScreen.cs)
    ├── LoadingText (TextMeshPro)
    ├── ProgressBar (Slider)
    │   └── Fill Area
    │       └── Fill (Image) ← This is what we reference
    └── TipText (TextMeshPro)
```

## Troubleshooting

**Loading screen doesn't appear:**
- Check that LoadingScreen GameObject is active in the scene
- Verify the Canvas Group is assigned
- Check that Canvas is set to Screen Space - Overlay

**Loading screen appears but doesn't fade:**
- Verify Canvas Group component is assigned in LoadingScreen script
- Check that Fade In/Out Duration is greater than 0

**No tips showing:**
- Check that TipText is assigned in LoadingScreen script
- Verify the Loading Tips array has at least one entry

**Progress bar doesn't fill:**
- Make sure you assigned the Fill **Image**, not the Slider component
- Check that the Image component's Image Type is set to "Filled"

**Screen flickers too fast:**
- Increase the Minimum Display Time in LoadingScreen component

## Integration is Already Done!

The `DungeonManager.cs` has already been updated to use the loading screen. It will automatically:
1. Show the loading screen when transitioning
2. Wait for fade in
3. Load the new room
4. Wait minimum display time
5. Fade out

No additional code needed! Just set up the UI as described above.
