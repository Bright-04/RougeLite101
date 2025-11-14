# Quick Setup Checklist

## Scene Transition - Quick Setup

### ✅ Step-by-Step Setup (5 minutes)

1. **Create UI Hierarchy:**

    ```
    Canvas (SceneTransitionCanvas)
    └── Image (FadePanel)
        └── [SceneTransition Script Component]
    ```

2. **FadePanel Settings:**

    - Anchor: Stretch to fill (Alt+Shift + click bottom-right anchor preset)
    - Color: Black, Alpha = 0
    - Raycast Target: ❌ Unchecked

3. **Canvas Settings:**

    - Render Mode: Screen Space - Overlay
    - Sort Order: 100

4. **SceneTransition Component:**
    - Attach to Canvas or separate GameObject
    - Drag FadePanel to "Fade Panel" field
    - Fade Duration: 0.5
    - Fade Color: Black

### ✅ Testing

-   Enter Play Mode
-   First room should fade in from black
-   Complete room → exit door should trigger fade out/in

### 🎨 Customization Tips

-   **Fast transitions**: 0.3 seconds
-   **Cinematic**: 0.8-1.0 seconds
-   **White fade**: Change Fade Color to white
-   **Custom color**: Set any color for themed transitions

---

## How It Works

```
Room Transition Flow:
┌─────────────┐
│ Player      │
│ enters door │
└──────┬──────┘
       │
       v
┌─────────────┐
│ Fade Out    │ (Screen → Black)
│ 0.5 seconds │
└──────┬──────┘
       │
       v
┌─────────────┐
│ Load New    │ (Destroy old room,
│ Room        │  spawn new room)
└──────┬──────┘
       │
       v
┌─────────────┐
│ Fade In     │ (Black → Clear)
│ 0.5 seconds │
└──────┬──────┘
       │
       v
┌─────────────┐
│ Player can  │
│ move again  │
└─────────────┘
```

## Implementation Details

-   **SceneTransition.cs**: Singleton that manages fade panel
-   **DungeonManager.cs**: Modified to call fade transitions
-   **First room**: Fades in when game starts
-   **Room transitions**: Fade out → change → fade in
-   **No input blocking**: Transition system doesn't interfere with gameplay

## Advanced Usage

Use in other scripts for custom transitions:

```csharp
// In any MonoBehaviour script
using System.Collections;

public class MyScript : MonoBehaviour
{
    public void DoSomethingWithFade()
    {
        StartCoroutine(FadeTransition());
    }

    private IEnumerator FadeTransition()
    {
        // Fade out
        yield return SceneTransition.Instance.FadeOut();

        // Do your thing while screen is black
        // (load scene, teleport player, etc.)

        // Fade in
        yield return SceneTransition.Instance.FadeIn();
    }
}
```
