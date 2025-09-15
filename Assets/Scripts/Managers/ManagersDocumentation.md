# Game Managers Documentation

This document provides an overview of the comprehensive game manager system created for the RougeLite project.

## Overview

The game manager system follows the **Singleton pattern** and provides centralized control over all major game systems. Each manager is responsible for a specific aspect of the game and communicates with other managers through events and direct references.

## Manager Architecture

### 1. GameManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Central game state controller and coordination hub

#### Key Features:
- **Game State Management**: Controls main game states (MainMenu, Loading, Gameplay, Paused, GameOver, Victory, Settings)
- **Pause System**: Handles game pause/resume with proper time scale management
- **Game Flow Control**: Manages game start, restart, level completion, and player death
- **Event Integration**: Listens to player/enemy events and responds accordingly
- **Player Respawn**: Automatic player respawn system with configurable delay
- **Statistics Tracking**: Tracks enemies killed, damage dealt, and playtime

#### Key Methods:
```csharp
// State Management
void ChangeGameState(GameState newState)
void PauseGame()
void ResumeGame()

// Game Flow
void StartNewGame()
void RestartGame()
void PlayerDied()
void LevelCompleted()

// Utility
bool IsGameplayActive
GameState CurrentState
```

### 2. UIManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Complete UI system management and HUD control

#### Key Features:
- **Panel Management**: Controls all UI panels (MainMenu, GameplayHUD, Pause, GameOver, Victory, Settings, Loading)
- **HUD Updates**: Real-time health, mana, stamina, score, and timer displays
- **Event Response**: Visual feedback for damage, healing, and game events
- **Canvas Management**: Automatic canvas creation with proper scaling
- **Button Integration**: Connected to game manager for seamless flow

#### Key Methods:
```csharp
// Panel Control
void ShowPanel(UIPanel panel)
void HidePanel(UIPanel panel)
void HideAllPanels()

// Game State Response
void OnGameStateChanged(GameState previousState, GameState newState)

// Utility
bool IsPanelActive(UIPanel panel)
```

### 3. AudioManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Complete audio system with mixing and spatial audio

#### Key Features:
- **Music Control**: Background music with fade in/out transitions
- **Sound Effects**: Pooled SFX sources for efficient audio playback
- **Audio Mixing**: Support for AudioMixer groups (Master, Music, SFX, Voice)
- **Volume Control**: Individual volume controls with persistence
- **Spatial Audio**: 3D positioned sound effects
- **Audio Pools**: Dynamic SFX source pooling to prevent audio cutoffs
- **Settings Persistence**: Save/load audio preferences

#### Key Methods:
```csharp
// Music
void PlayMusic(AudioClip musicClip, bool loop = true, float fadeInTime = 1f)
void StopMusic(float fadeOutTime = 1f)
void PauseMusic()
void ResumeMusic()

// Sound Effects
void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
void PlayRandomSFX(AudioClip[] clips, ...)

// Volume Control
void SetMasterVolume(float volume)
void SetMusicVolume(float volume)
void SetSfxVolume(float volume)

// Convenience Methods
void PlayButtonClick()
void PlayPlayerAttack()
void PlayProjectileHit()
```

### 4. SaveManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Save/load system with multiple slots and backups

#### Key Features:
- **Multiple Save Slots**: Support for 3 save slots (configurable)
- **Auto-Save System**: Automatic periodic saves during gameplay
- **Backup System**: Automatic backup creation with cleanup
- **Settings Persistence**: Game settings save/load
- **Save Slot Info**: Preview save data without full loading
- **Error Handling**: Comprehensive error handling with events

#### Key Data Structures:
```csharp
public class GameSaveData
{
    // Save metadata
    public string saveName;
    public string saveDate;
    public string gameVersion;
    
    // Game progress
    public float playtime;
    public int enemiesKilled;
    public float damageDealt;
    
    // Player stats
    public float playerHealth;
    public float attackDamage;
    // ... and more
    
    // World data
    public string currentScene;
    public Vector3 playerPosition;
}
```

#### Key Methods:
```csharp
// Save/Load
bool SaveGame(int slotIndex = 0, string saveName = "")
GameSaveData LoadGame(int slotIndex = 0)
bool DeleteSave(int slotIndex)

// Utility
bool SaveExists(int slotIndex)
SaveSlotInfo GetSaveSlotInfo(int slotIndex)
List<SaveSlotInfo> GetAllSaveSlots()

// Settings
void SaveGameSettings()
void LoadGameSettings()
```

### 5. InputManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Centralized input handling with buffer system

#### Key Features:
- **Dual Input Support**: Works with both legacy Input Manager and new Input System
- **Input Buffering**: Button press buffering for responsive controls
- **Custom Key Bindings**: Configurable key mappings with persistence
- **Mouse Control**: Mouse sensitivity and Y-axis inversion
- **Input Events**: Action-based input events for decoupled systems

#### Key Methods:
```csharp
// Input Reading
Vector2 GetMovementInput()
Vector2 GetMouseDelta()
bool GetButton(string inputName)
bool GetButtonDown(string inputName)
bool GetButtonUp(string inputName)

// Input Buffering
bool IsInputBuffered(string inputName)
void ConsumeBufferedInput(string inputName)

// Settings
void SetMouseSensitivity(float sensitivity)
void SetKeyBinding(string inputName, KeyCode keyCode)
KeyCode GetKeyBinding(string inputName)
```

### 6. SceneTransitionManager
**Namespace**: `RougeLite.Managers`  
**Purpose**: Scene loading with transitions and preloading

#### Key Features:
- **Smooth Transitions**: Fade in/out effects during scene changes
- **Loading Screens**: Integration with UI loading panels
- **Scene Preloading**: Background preloading of common scenes
- **Progress Tracking**: Real-time loading progress with minimum loading times
- **Async Loading**: Non-blocking scene loading with progress updates

#### Key Methods:
```csharp
// Scene Loading
void LoadScene(string sceneName, bool useTransition = true)
void LoadMainMenu()
void LoadGameplay()
void ReloadCurrentScene()

// Preloading
void PreloadScene(string sceneName)

// Utility
bool IsSceneLoaded(string sceneName)
List<string> GetLoadedScenes()
```

## Manager Integration

### Event System Integration
All managers integrate with the existing event system:
- **GameManager**: Listens to player/enemy events, broadcasts game state changes
- **UIManager**: Responds to player damage/healing events for visual feedback
- **SceneTransitionManager**: Broadcasts scene change events

### Cross-Manager Communication
Managers communicate through:
1. **Singleton References**: Direct access to other manager instances
2. **Event Broadcasting**: Decoupled communication through event system
3. **Shared Data**: Common data structures and state information

### Initialization Order
Managers initialize in this order:
1. **GameManager**: Core game state
2. **AudioManager**: Audio system setup
3. **InputManager**: Input system configuration
4. **UIManager**: UI system with manager references
5. **SaveManager**: Load saved settings
6. **SceneTransitionManager**: Scene management ready

## Usage Examples

### Basic Game Flow
```csharp
// Start new game
GameManager.Instance.StartNewGame();

// Show pause menu
UIManager.Instance.ShowPanel(UIManager.UIPanel.Pause);

// Play victory music
AudioManager.Instance.PlayVictoryMusic();

// Save progress
SaveManager.Instance.SaveGame(0, "Victory Save");

// Load different scene
SceneTransitionManager.Instance.LoadMainMenu();
```

### Input Handling
```csharp
// Check for attack input
if (InputManager.Instance.GetButtonDown("Attack"))
{
    PerformAttack();
}

// Get movement input
Vector2 movement = InputManager.Instance.GetMovementInput();
```

### Audio Management
```csharp
// Play positioned sound effect
AudioManager.Instance.PlaySFX(explosionClip, 1f, 1f, transform.position);

// Adjust volume
AudioManager.Instance.SetMasterVolume(0.8f);
```

## Debug Features

All managers include debug information that can be displayed with `showDebugInfo = true`:
- **Real-time state information**
- **Performance metrics**
- **Quick action buttons**
- **Settings verification**

## Best Practices

1. **Always check for null** when accessing manager instances
2. **Use events** for decoupled communication between systems
3. **Save settings** when changed for persistence
4. **Handle loading states** gracefully in UI
5. **Test scene transitions** thoroughly
6. **Monitor audio pool usage** for performance

## Future Extensions

The manager system is designed to be extensible:
- **NetworkManager**: For multiplayer functionality
- **AnalyticsManager**: For game analytics and telemetry
- **AchievementManager**: For achievement and progression systems
- **LocalizationManager**: For multi-language support
- **ModManager**: For mod support and loading

This comprehensive manager system provides a solid foundation for any game project with professional-level architecture and functionality.