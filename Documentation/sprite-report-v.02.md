# Sprite / Animation Assets — Kích thước & PPU 

## Tóm tắt nhanh

- Khung animation nhân vật và nhiều enemy: 64×64 px (PPU 100).
- Tileset: 16×16 px (PPU 16).
- Một số VFX/projectile lớn: 128×128 px (PPU 100).
- Một vài sprite sheet chứa khung không đồng nhất (nhiều rect sizes trong cùng file).
- Phần lớn sprite và VFX: `spritePixelsToUnits: 100` ; tile sprites dùng `spritePixelsToUnits: 16`.

## Danh sách chi tiết (file mẫu — spriteName / width × height / PPU)

- `Assets/Sprites/Player/Side animations/spr_player_right_idle.png.meta`
  - PPU: 100
  - Sprites: `spr_player_right_idle_0` … `spr_player_right_idle_11` — mỗi frame: 64 × 64 px

- `Assets/Sprites/Player/Side animations/spr_player_right_walk.png.meta`
  - PPU: 100
  - Sprites: `spr_player_right_walk_0` … `spr_player_right_walk_5` — mỗi frame: 64 × 64 px

- `Assets/Sprites/Monster/Slime/SlimeMove-Sheet.png.meta`
  - PPU: 100
  - Sprites: `SlimeMove-Sheet_0` … `SlimeMove-Sheet_12` — mỗi frame: 64 × 64 px

- `Assets/Sprites/Monster/Slime/idle-Sheet.png.meta`
  - PPU: 100
  - Sprites: nhiều khung kích thước khác nhau (ví dụ: 6×6, 18×20, 42×33, 44×37, v.v.) — sheet không đồng nhất

- `Assets/Sprites/Monster/Slime/SlimeDeath-Sheet.png.meta`
  - PPU: 100
  - Death frames: rects ~42–44 × ~33–37 px trong nhiều entry

- `Assets/Sprites/Player/Spell/Fireball-Sheet.png.meta`
  - PPU: 100
  - Sprites: `Fireball-Sheet_0` … `Fireball-Sheet_3` — mỗi frame: 128 × 128 px

- `Assets/Sprites/Projectiles/SlimeBlob.png.meta`
  - PPU: 100
  - Sprite: `SlimeBlob_0` — rect: 26 × 38 px

- `Assets/Sprites/Tilemap/Ninja/TilesetFloor.png.meta`
  - PPU: 16
  - Tiles: grid cells 16 × 16 px (ví dụ entries show width:16 height:16)

- Vegetation (ví dụ):
  - `Assets/Sprites/Vegetation/tree1.png.meta` — PPU: 100 — example rect: 23 × 42 px
  - `Assets/Sprites/Vegetation/tree.png.meta` — PPU: 100 — example rect: 45 × 64 px
  - `Assets/Sprites/Vegetation/spr_tree3.png.meta` — PPU: 100 — example rect: 44 × 67 px
  - `Assets/Sprites/Vegetation/spr_tree2.png.meta` — PPU: 100 — example rect: 40 × 60 px
  - `Assets/Sprites/Vegetation/spr_tree1.png.meta` — PPU: 16 — example rect: 48 × 64 px
  - `Assets/Sprites/Vegetation/Bush1.png.meta` — PPU: 16 — example rect: 32 × 32 px

- UI / Emoji:
  - `Assets/TextMesh Pro/Sprites/EmojiOne.png.meta` — PPU: 100 — multiple sprites 128 × 128 px

- Vũ khí / projectile nhỏ:
  - `Assets/Sprites/Player/Sword.png.meta` — PPU: 16 (example)
  - `Assets/Sprites/Player/Bow.png.meta` — PPU: 16

## Ghi chú

- Nhiều sprite sheet (đặc biệt sheets làm bằng Aseprite / export) sử dụng slicing tự động và chứa nhiều sprite với rect sizes khác nhau — cần cân nhắc khi chuẩn hoá kích thước.
- PPU thường là 100 cho nhân vật/VFX, 16 cho tilesets — khung scale game có sự tách bạch giữa tiles và entities.