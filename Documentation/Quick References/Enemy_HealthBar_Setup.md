## Overview

The healthbar system consists of:
- **EnemyHealthBar.cs** - Script that manages the healthbar UI and positioning
- **SlimeHealth.cs** - Updated to integrate with the healthbar system

## Setup Instructions

### Step 1: Create the HealthBar UI Prefab

1. **Create a new Canvas for the healthbar (World Space)**:
   - Right-click in the Hierarchy → UI → Canvas
   - Name it "EnemyHealthBarCanvas"
   - In the Inspector, set:
     - Render Mode: **World Space**
     - Canvas Scaler → Dynamic Pixels Per Unit: 10

2. **Create the healthbar container**:
   - Right-click on the Canvas → Create Empty
   - Name it "HealthBarContainer"
   - Add a Canvas Group component (optional, for fade effects)

3. **Create the background**:
   - Right-click on HealthBarContainer → UI → Image
   - Name it "HealthBarBackground"
   - Set the color to a dark color (e.g., dark red or black)
   - Adjust the RectTransform:
     - Width: 100
     - Height: 10
     - Anchors: Center

4. **Create the health fill**:
   - Right-click on HealthBarBackground → UI → Image
   - Name it "HealthBarFill"
   - Set the color to green
   - Set Image Type to **Filled**
   - Set Fill Method to **Horizontal**
   - Adjust the RectTransform to match the background (stretch to fill)

5. **Scale the Canvas**:
   - Select the EnemyHealthBarCanvas
   - Set Scale to (0.01, 0.01, 0.01) or adjust as needed

### Step 2: Add the Script

1. Select the **EnemyHealthBarCanvas** GameObject
2. Add the **EnemyHealthBar** component
3. Assign the references:
   - **Health Bar Fill**: Drag the "HealthBarFill" Image
   - **Health Bar Container**: Drag the "HealthBarContainer" GameObject
4. Adjust settings:
   - **Offset**: Vertical offset above the enemy (e.g., 0, 1.5, 0)
   - **Hide When Full**: Check this to hide the healthbar when at full health
   - **Always Face Camera**: Keep checked so the healthbar always faces the player

### Step 3: Create a Prefab

1. Drag the **EnemyHealthBarCanvas** from the Hierarchy to the Prefabs folder
2. Name it "EnemyHealthBar"
3. Delete the instance from the scene

### Step 4: Add to Enemy Prefab

1. Open the **Slime.prefab** (or your enemy prefab)
2. Drag the **EnemyHealthBar** prefab into the Slime prefab as a child
3. Select the Slime GameObject
4. In the **SlimeHealth** component, drag the EnemyHealthBar child into the **Health Bar** field
5. Save the prefab

### Step 5: Test

1. Play the scene
2. Attack the enemy
3. The healthbar should appear and update as the enemy takes damage
4. The healthbar color should change:
   - **Green** when health > 50%
   - **Yellow** when health is between 25-50%
   - **Red** when health < 25%

## Customization Options

### Colors
Edit the `UpdateHealthBar()` method in `EnemyHealthBar.cs` to customize colors:
```csharp
if (fillAmount > 0.5f)
    healthBarFill.color = Color.green;  // Change these values
else if (fillAmount > 0.25f)
    healthBarFill.color = Color.yellow;
else
    healthBarFill.color = Color.red;
```

### Position
Adjust the **Offset** in the Inspector to position the healthbar:
- Y value controls height above the enemy
- X and Z values can offset horizontally

### Size
Adjust the Canvas scale and RectTransform sizes to make the healthbar larger or smaller.

### Visibility
- **Hide When Full**: Uncheck to always show the healthbar
- **Always Face Camera**: Uncheck if you want the healthbar to rotate with the enemy

## Tips

- For better performance with many enemies, consider using an object pool for healthbars
- You can add smooth transitions by lerping the fill amount in the Update method
- Consider adding a fade-out effect when the enemy dies
- The healthbar automatically destroys when the enemy is destroyed

## Troubleshooting

**Healthbar doesn't show up:**
- Make sure the Canvas is set to World Space
- Check that the EnemyHealthBar reference is assigned in SlimeHealth
- Verify the Canvas isn't too far from the camera (adjust scale)

**Healthbar is the wrong size:**
- Adjust the Canvas scale (try 0.005 to 0.02)
- Modify the RectTransform width/height of the background and fill

**Healthbar doesn't face camera:**
- Ensure "Always Face Camera" is checked
- Verify Camera.main is working (check if main camera has MainCamera tag)

**Colors don't change:**
- Check that the HealthBarFill Image reference is assigned
- Make sure you're not overriding the color elsewhere
