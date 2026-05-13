# Player Rework Animation Steps

Huong dan nay dung cho bo sprite moi tai:

`Assets/Sprites/Player/Player Rework`

Cong cu tu dong nam trong Unity menu:

`Tools > Player Rework`

## Muc tieu buoc dau

Buoc dau chi thay animation di chuyen co ban cua Player:

- Idle
- Walk

Controller moi duoc tao rieng tai:

`Assets/Animation/Player/Rework/Player_Rework_4Dir_Walk.controller`

Controller cu van con, nen neu ket qua sai co the gan lai controller cu trong prefab Player.

## Buoc 1: Mo Unity va doi compile

1. Mo project bang Unity.
2. Doi Unity compile xong. Neu co loi compile, xem tab Console truoc.
3. Vao folder `Assets/Sprites/Player/Player Rework`.
4. Bam vao `idle.png`, `run.png`, `walk.png` de xem Inspector.

Thong so nen co:

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple
- Pixels Per Unit: 200 cho bo `Player Rework`
- Filter Mode: Point neu muon pixel art sac net
- Compression: None hoac Low Quality

Neu Sprite Mode chua la Multiple, chon Multiple, bam Apply, mo Sprite Editor va slice lai.

## Buoc 2: Generate animation clips

Trong Unity, bam:

`Tools > Player Rework > 1. Generate row animation clips`

Ket qua se tao cac clip trong:

`Assets/Animation/Player/Rework`

Vi du:

- `Player_idle_Row0.anim`
- `Player_idle_Row1.anim`
- `Player_idle_Row2.anim`
- `Player_idle_Row3.anim`
- `Player_run_Row0.anim`
- `Player_run_Row1.anim`
- `Player_run_Row2.anim`
- `Player_run_Row3.anim`
- `Player_walk_Row0.anim`
- `Player_walk_Row1.anim`
- `Player_walk_Row2.anim`
- `Player_walk_Row3.anim`

Moi row la mot hang trong spritesheet. Buoc nay giup minh kiem tra hang nao la nhin len, xuong, trai, phai.

Quy uoc row thuc te cua `idle.png` va `walk.png` trong bo Player Rework:

- `Row0`: Up / Back
- `Row1`: Left
- `Row2`: Down / Front
- `Row3`: Right

## Buoc 3: Xem thu clip

1. Chon mot file `.anim`, vi du `Player_walk_Row3.anim`.
2. Mo tab Animation.
3. Keo preview/play de xem huong nhan vat.

Cong cu hien dang dung quy uoc nay de tao controller 4 huong. Tam thoi controller dung `walk.png` thay cho `run.png`, vi animation run cua rework dang loi.

Neu trai/phai bi nguoc:

1. Mo file `Assets/Editor/PlayerReworkAnimationBuilder.cs`.
2. Tim cac dong:

```csharp
private const int LeftRowIndex = 1;
private const int RightRowIndex = 3;
```

3. Doi gia tri cua `LeftRowIndex` va `RightRowIndex` cho nhau.
4. Quay lai Unity, doi compile xong.
5. Chay lai buoc 2 va buoc 4.

## Buoc 4: Generate movement controller 4 huong

Trong Unity, bam:

`Tools > Player Rework > 2. Generate 4-direction walk controller`

Controller moi se co:

- Parameter `moveX`
- Parameter `moveY`
- Parameter `lastMoveX`
- Parameter `lastMoveY`
- Parameter `Dash`
- State `Idle`
- State `Runniing`

Ten `Runniing` giu theo controller cu de de so sanh, nhung motion ben trong dang dung `walk`.

## Buoc 5: Gan controller vao Player prefab

Co 2 cach.

Cach tu dong:

`Tools > Player Rework > 3. Assign 4-direction controller to Player prefab`

Lenh nay cung tat `flipSpriteWithAim` tren `PlayerMovement`. Ly do: bo sprite moi da co Row1 = Left va Row2 = Right, nen neu tiep tuc lat `SpriteRenderer.flipX` theo chuot thi animation 4 huong se bi nguoc/sai.

Cach thu cong:

1. Mo prefab `Assets/Prefabs/Scenes Management/Player.prefab`.
2. Chon object root `Player`.
3. Trong component Animator, gan Controller thanh:
   `Assets/Animation/Player/Rework/Player_Rework_4Dir_Walk.controller`
4. Trong SpriteRenderer, co the gan sprite dau tien cua `idle` moi neu can.
5. Trong component PlayerMovement, bo tick `Flip Sprite With Aim`.
6. Save prefab.

## Buoc 6: Test trong Play Mode

Can test:

- Dung yen: Player dung idle moi.
- Bam WASD: Player chay animation walk moi theo 4 huong.
- Tro chuot sang trai/phai: Weapon/hand van doi ben, body sprite khong bi lat sai.
- Collider khong bi lech qua nhieu.
- Weapon van xuat hien dung vi tri tuong doi.

Neu nhan vat qua to/nho:

- Kiem tra Pixels Per Unit cua sprite moi.
- Bo `Player Rework` dang dung PPU 200 de giu kich co gan voi player cu.
- Player prefab hien dang scale root la `{x: 12, y: 12, z: 12}`, nen uu tien sua PPU cua sprite hon la sua scale root.

Neu nhan vat bi lech chan:

- Sprite moi dang co pivot theo slice la `{x: 0, y: 0}`.
- Nen chinh pivot trong Sprite Editor ve Bottom Center hoac Center tuy visual.
- Sau khi doi pivot, bam Apply va generate lai animation.

## Buoc tiep theo sau khi movement dung

Khi idle/run da on, moi tiep tuc them:

- `hurt.png` -> trigger `Hurt`
- `spellcast.png` -> trigger `Cast`
- `shoot.png` -> trigger `Shoot`
- `slash.png` hoac `1h_slash.png` -> trigger `Attack`
- `sword_slash_128.png` -> slash effect prefab

Khong nen lam tat ca cung luc, vi attack/spell con lien quan collider, prefab weapon va animation event.
