# RougeLite101 Diagrams

This page collects high-level diagrams (Mermaid) of core systems and flows. View on a Markdown renderer that supports Mermaid (e.g., GitHub).

## Event System Overview

```mermaid
graph TD
  subgraph Gameplay & UI
    A[Producer\\n(e.g., PlayerStats, SpellCaster, Projectile)] -->|Broadcast<T>| EM
    L1[Listener\\n(e.g., PlayerUI, GameManager)]
    L2[Listener\\n(e.g., Debug Console, Audio)]
  end
  subgraph Core
    EM[EventManager\\n(Type->Listeners, Queue)]
  end
  EM -->|Dispatch<T>| L1
  EM -->|Dispatch<T>| L2
  EB[EventBehaviour\\nhelpers] -.-> A
  EB -.-> L1
```

## Runtime Gameplay Flow

```mermaid
sequenceDiagram
  participant Input as Input System
  participant Move as SimplePlayerMovement
  participant Face as PlayerController
  participant Spells as SpellCaster
  participant Proj as Projectile/Launcher
  participant UI as Player UI
  participant GM as GameManager
  participant World as InfiniteWorld/WorldManager
  Note over Input: WASD/Arrows/Shifts/Digits
  Input->>Move: Read actions
  Move->>Move: Apply velocity (RB/Transform)
  Move->>Face: Position changes → face mouse
  Move->>GM: PlayerMovementEvent (optional)
  Move->>World: Player chunk changed
  World->>GM: ChunkGenerated/Unloaded
  Input->>Spells: Digit key (1/2/3)
  Spells->>Spells: Check cooldown/mana
  Spells->>Proj: Launch prefab/projectile
  Proj->>Target: OnTriggerEnter2D → IDamageable.TakeDamage
  Target->>GM: EnemyDeath/PlayerDamaged
  GM->>UI: Indirect via events
  Spells->>UI: PlayerManaUsedEvent
  GM->>GM: Pause/Victory/GameOver
```

## Manager Relationships

```mermaid
graph LR
  IM[InputManager] --- GM[GameManager]
  UIM[UIManager] --- GM
  AM[AudioManager] --- GM
  SM[SaveManager] --- GM
  STM[SceneTransitionManager] --- GM
  GM -->|Broadcast| EM[EventManager]
  UIM -->|Subscribe| EM
  Note right of GM: Singleton, state machine
  Note left of IM: New + Legacy input
```

## Projectile Lifecycle

```mermaid
stateDiagram-v2
  [*] --> Pooled
  Pooled --> Active: Get() from pool
  Active --> Flying: Initialize(position, dir, speed)
  Flying --> Hit: TriggerEnter2D (valid target)
  Hit --> Returning: DestroyProjectile()
  Returning --> Pooled: Return() to pool
  Flying --> Expired: lifetime <= 0
  Expired --> Returning
```

## Infinite World Chunks

```mermaid
flowchart TB
  P[Player] -->|GetChunkPosition| C[Current Chunk]
  C --> G[Generate Around Player\\n(-N..+N in grid)]
  C --> U[Unload Distant]
  G --> CE[ChunkGeneratedEvent]
  U --> CU[ChunkUnloadedEvent]
  subgraph Biome
    B1[BiomeDataSO/Struct]
    B1 -->|rates, prefabs| G
  end
```

---

For detailed narrative explanations, see:
- Architecture: ARCHITECTURE.md
- Runtime Flow: RUNTIME_FLOW.md
- Code Map: CODEMAP.md
