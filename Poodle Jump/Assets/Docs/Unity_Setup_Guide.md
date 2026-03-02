# 날아라 칸쵸 (Poodle Jump) — Unity 에디터 세팅 가이드

이 문서는 SOLID 기반 코어 플레이 스크립트를 씬에 연결하고 동작시키기 위한 **단계별 Unity 에디터 세팅**을 정리한 것입니다.

---

## 1. Input System 설정

### 1.1 C# 클래스 생성 (선택)

- **Project** 창에서 `Assets/InputSystem_Actions.inputactions` 선택.
- **Inspector**에서 **Generate C# Class** 체크.
- **Apply** 클릭 후 생성 경로 확인 (기본: 같은 폴더에 `InputSystem_Actions.cs`).

> 모바일 터치만 사용할 경우 생성하지 않아도 됩니다. `PlayerInputHandler`는 **Input Action Reference** 또는 **Player Input** 컴포넌트로 Move 액션을 참조합니다.

### 1.2 Move 액션 참조 (PlayerInputHandler용)

**방법 A: Input Action Reference 사용 (권장)**

1. **Assets** 우클릭 → **Create** → **Input Actions** 사용하지 말고, 기존 `InputSystem_Actions.inputactions` 사용.
2. **Project**에서 `InputSystem_Actions` 선택 → **Inspector**에서 **Player** 맵의 **Move** 액션을 찾습니다.
3. **Move** 액션 행 왼쪽 **+** (또는 오른쪽 클릭) → **Add Binding** 등으로 바인딩이 있는지 확인.
4. 플레이어 오브젝트에 붙일 **PlayerInputHandler**에서 **Move Action Ref** 필드에 할당하려면:
   - **Assets** 우클릭 → **Create** → **Input Action Reference**가 없다면, **Player Input** 컴포넌트 사용(방법 B).
5. 또는 **Player Input** 컴포넌트를 사용해 같은 오브젝트에 추가하고, **Actions**에 `InputSystem_Actions` 에셋을 할당한 뒤 **Default Map**을 **Player**로 두면, `PlayerInputHandler`가 자동으로 **Player** 맵의 **Move** 액션을 찾습니다.

**방법 B: Player Input 컴포넌트 사용**

1. 플레이어 오브젝트에 **Player Input** 컴포넌트 추가.
2. **Actions** 필드에 `Assets/InputSystem_Actions.inputactions` 에셋 드래그.
3. **Default Map**을 **Player**로 설정.
4. **PlayerInputHandler**의 **Move Action Ref**는 비워 둬도 됩니다. 스크립트가 같은 오브젝트의 **Player Input**에서 **Player/Move** 액션을 자동으로 사용합니다.

### 1.3 모바일 터치 (선택)

- **InputSystem_Actions**의 **Player** 맵에서 **Move** 액션에 **Add Binding** → **Touch** → **Primary Touch** → **Delta** 또는 **Position** 추가하면 터치 드래그로 좌우 입력 가능.
- `PlayerInputHandler`는 이미 **Primary Touch Delta**를 보조로 읽어 `MoveInput`에 반영합니다. **Touch Sensitivity** 값을 Inspector에서 조절하세요.

---

## 2. 태그 설정

1. **Edit** → **Project Settings** → **Tags and Layers**.
2. **Tags** 목록에서 **+** 로 새 태그 추가.
3. 이름: **Platform**.
4. 점프 발판이 될 모든 오브젝트에 이 태그를 지정합니다.

---

## 3. 씬 계층 구조

권장 구조 예시:

```
Scene
├── Player          ← 빈 오브젝트 또는 루트 (스크립트·Rigidbody·Collider 부착)
│   └── Model       ← 캐릭터 메시 (선택)
├── Main Camera     ← CameraYTracker 부착
├── Platform_01     ← Tag: Platform, Collider 부착
├── Platform_02
└── ...
```

---

## 4. 플레이어 오브젝트 세팅

### 4.1 생성 및 계층

1. **Hierarchy** 우클릭 → **Create Empty** → 이름을 **Player**로 변경.
2. **Player**를 선택한 뒤 자식으로 **Create Empty** → 이름 **Visual Model** (또는 **Model**)으로 변경. 이 자식에만 3D 메시(캐릭터 모델)를 넣습니다.

#### 4.1.1 스쿼시 앤 스트레치용 구조 (Root vs Visual Model) — 물리 버그 방지

DOTween으로 점프 시 스케일(스쿼시 앤 스트레치)을 줄 때, **스케일을 적용하는 Transform**과 **Rigidbody/Collider가 붙어 있는 Transform**을 반드시 분리해야 합니다.

- **루트(Player)**: Rigidbody, Collider, PlayerController, CylinderMovement, PlayerInputHandler, **PlayerVisualController**를 붙입니다. 이 오브젝트의 **Transform 스케일은 (1,1,1)로 고정**하고, 위치/회전만 물리·스크립트로 제어합니다.
- **자식(Visual Model)**: 메시(MeshFilter/MeshRenderer) 또는 프리팹 인스턴스만 넣습니다. **Rigidbody·Collider를 붙이지 않습니다.** PlayerVisualController의 **Visual Model** 참조에 이 자식 Transform을 연결하면, DOTween이 **이 자식의 localScale만** 변경합니다.

이렇게 하면 충돌 체적과 물리 시뮬레이션은 루트의 Collider로 일정하게 유지되고, 보이는 모양만 자식 스케일로 바뀌어 물리 버그(충돌 튐, 잘못된 접촉)가 발생하지 않습니다.

| 오브젝트 | 붙이는 것 | 스케일 |
|----------|-----------|--------|
| **Player** (루트) | Rigidbody, Collider, 모든 플레이어 스크립트 | **(1, 1, 1) 유지** |
| **Visual Model** (자식) | 메시 또는 캐릭터 모델만 | DOTween으로 Squash & Stretch |

### 4.2 컴포넌트 부착

**Player** (루트) 오브젝트에 아래 컴포넌트를 **반드시** 붙입니다.

| 컴포넌트 | 필수 여부 | 비고 |
|----------|-----------|------|
| **PlayerInputHandler** | 필수 | IPlayerInput 구현, 입력 처리 |
| **CylinderMovement** | 필수 | 원기둥 수학 (RequireComponent로 자동 추가 가능) |
| **PlayerController** | 필수 | 파사드 (RequireComponent로 Rigidbody, CylinderMovement 요구) |
| **PlayerVisualController** | 선택 | 점프 시 스쿼시 앤 스트레치 연출 (Visual Model 참조 필요) |
| **Rigidbody** | 필수 | 중력·점프 처리 |
| **Capsule Collider** (또는 Sphere/Box) | 필수 | 플랫폼과의 충돌 감지 |
| **Player Input** | 선택 | InputSystem_Actions 연결 시 사용 (방법 B) |

**PlayerController**를 먼저 추가하면 **Rigidbody**와 **CylinderMovement**는 RequireComponent로 자동 추가됩니다.

### 4.3 Rigidbody 설정

- **Mass**: 1
- **Use Gravity**: ✅ On
- **Constraints**: **Freeze Rotation** — X, Z 체크 (캐릭터가 넘어지지 않도록)
- **Collision Detection**: Discrete (기본) 또는 필요 시 Continuous
- **Interpolate**: **Interpolate** (카메라/움직임 부드럽게)

### 4.4 Collider 설정

- **Capsule** (또는 Sphere): 플레이어 크기에 맞게 **Radius**, **Height** 조절.
- **Is Trigger**: ❌ Off (물리 충돌로 점프 판정).

### 4.5 스크립트별 Inspector 참조 및 값

| 스크립트 | Inspector 항목 | 설정 방법 |
|----------|----------------|-----------|
| **PlayerInputHandler** | Move Action Ref | Move 액션이 있는 InputActionReference 할당. 비워두면 같은 오브젝트의 Player Input에서 **Player/Move** 자동 사용. |
| | Touch Sensitivity | 0.01~1. 모바일 터치 반응 강도. (기본 0.2) |
| **CylinderMovement** | Radius | 원기둥 반지름. (예: 5) |
| **PlayerController** | Turn Speed | 좌우 회전 속도. (예: 2) |
| | Jump Force | Platform 충돌 시 적용할 위쪽 속도. (예: 12) |
| | Input Provider | (선택) IPlayerInput을 구현한 컴포넌트 드래그. 비워두면 같은 오브젝트에서 자동 탐색. |
| **PlayerVisualController** | Visual Model | **Player 루트가 아닌** 자식(Visual Model) Transform 드래그. 여기에만 스케일 트윈이 적용됩니다. |
| | Squash / Stretch / Recover Duration, Scale | 착지·도약·복구 시간 및 스케일 값. 기본값으로도 동작합니다. |

### 4.6 플레이어 초기 위치

- **Transform**: 원기둥 표면 위로 두기.  
  - 예: **X** = Radius×Cos(0), **Z** = Radius×Sin(0) → **X** = Radius, **Z** = 0.  
  - **Y**는 시작 플랫폼 높이에 맞춤.

---

## 5. 메인 카메라 세팅

1. **Hierarchy**에서 **Main Camera** 선택.
2. **Add Component** → **Camera Y Tracker** 검색 후 추가.
3. **Inspector** 설정:
   - **Target**: **Player** 오브젝트의 Transform 드래그.
   - **Lerp Speed**: Y 추적 부드러움 (예: 5).
   - **Camera Distance**: 원기둥 중심(0,0,0)으로부터 카메라까지의 거리. 궤도(Orbit) 반경.
   - **Look At Height Offset**: LookAt 대상 Y = 카메라 Y − 이 값. 시선이 너무 아래를 향하지 않도록 조절.

카메라는 플레이어와 **같은 각도**에 위치하며, **Camera Distance**만큼 원점에서 떨어져 있어 좌우 조작이 반전되지 않습니다. **LookAt**으로 원기둥 축을 바라보며, **Look At Height Offset**으로 시선 높이를 보정합니다.

### 5.1 칸쵸의 등짝을 자연스럽게 보기 — 권장값 가이드

| 항목 | 권장 범위 | 설명 |
|------|-----------|------|
| **Camera Distance** | **10 ~ 15** | 원기둥 반지름(예: 5)보다 충분히 크게 두면 플레이어 등짝이 잘 보입니다. **12** 정도가 기본으로 무난합니다. 너무 크면 캐릭터가 작게 보이고, 너무 작으면 원기둥에 가려질 수 있습니다. |
| **Look At Height Offset** | **2 ~ 5** | **3**을 기본으로 두고, 값이 **클수록** 카메라가 더 아래를 보게 됩니다(캐릭터 발쪽). **작을수록** 수평에 가깝게 봅니다. 등짝 위주로 보려면 **2~3**, 발과 플랫폼이 더 보이게 하려면 **4~5**로 조절하세요. |

- **CylinderMovement Radius**가 5라면: **Camera Distance = 12**, **Look At Height Offset = 3**부터 시작해 플레이해 보며 미세 조정하는 것을 권장합니다.

---

## 6. 플랫폼(점프 발판) 세팅

1. **Hierarchy**에서 점프 발판 오브젝트 선택 (Quad, Cube, Plane 등).
2. **Tag** → **Platform** 선택.
3. **Collider** (Box, Mesh 등)가 있어야 합니다. **원웨이 통과(Trigger)** 를 사용하므로 **Is Trigger**는 **반드시 켭니다** (자세한 이유는 6.1 참고).
4. **Rigidbody**는 필요 없습니다 (발판은 정적 Collider만 있으면 됨).

### 6.1 Trigger 로직이 정상 작동하기 위한 발판(Platform) 프리팹 Collider 세팅

PlayerController는 **OnTriggerEnter(Collider)** 로만 발판을 감지합니다. **OnCollisionEnter**를 사용하지 않으므로, 발판의 Collider 설정이 아래와 같아야 합니다.

| 항목 | 설정 값 | 이유 |
|------|----------|------|
| **Collider** | Box Collider 또는 Mesh Collider 등 | 플레이어와 겹치는 순간을 감지해야 함. |
| **Is Trigger** | **✅ On (체크)** | Trigger로 두어야 플레이어가 **발판을 통과**할 수 있음. 위로 올라갈 때 머리가 발판 밑에 막히는 물리 충돌이 사라짐(원웨이 플랫폼). |
| **Tag** | **Platform** | 스크립트가 `other.gameObject.CompareTag("Platform")`으로만 점프를 처리함. |
| **Rigidbody** | 없음 | 발판은 움직이지 않으므로 Collider만 있으면 됨. |

**동작 요약**

- 플레이어에는 **Rigidbody**와 **Collider**(Is Trigger 꺼짐)가 붙어 있음.
- 발판에는 **Collider(Is Trigger = true)** 만 붙어 있음.
- 플레이어가 **아래로 떨어지다가** 발판 트리거 영역에 들어가면 (`_rb.linearVelocity.y <= 0f`) 그때만 점프가 실행됨. 위로 올라가는 중에 발판을 통과해도 트리거는 발생하지만, 조건문 때문에 점프가 실행되지 않아 **착지 시에만** 점프가 난다.

**에디터에서 확인할 것**

- 발판 프리팹을 연 뒤 **Inspector** → 해당 **Collider** 컴포넌트에서 **Is Trigger** 체크가 되어 있는지 확인.
- 이미 씬에 놓인 발판이 있다면, 해당 프리팹을 사용하는 인스턴스도 동일하게 적용됨. 프리팹 루트의 Collider만 올바르게 설정하면 됨.

---

## 7. 발판 무한 스폰 (PlatformPool · PlatformSpawner)

발판을 풀링으로 재사용하면서 원기둥 표면을 따라 나선형 계단처럼 무한 스폰하려면 아래 순서대로 설정합니다.

### 7.1 타입별 발판 프리팹(Prefab) 만들기

**타입 하나당 프리팹 하나**를 만듭니다. (Normal, OneTime, Broken, Moving, HighJump → 최소 5개, 또는 쓰는 타입만.)

**공통 단계 (타입마다 반복)**

1. **Hierarchy**에서 **Create Empty** 또는 **3D Object** → **Quad** / **Cube** 등으로 발판 오브젝트 생성.
2. **이름**: 타입을 구분하기 쉽게 예) `Platform_Normal`, `Platform_OneTime`, `Platform_Broken`, `Platform_Moving`, `Platform_HighJump`.
3. **Transform**: Scale은 원기둥 반지름에 맞게, Rotation (0,0,0). 스폰 시 스크립트가 회전을 넣습니다.
4. **Tag** → **Platform** 지정.
5. **Collider** 추가 (Box 또는 Mesh Collider). **Is Trigger** ✅ **On** (섹션 6.1 참고).
6. **Platform** 스크립트 추가 후, **Platform Type** 드롭다운에서 **이 프리팹이 해당하는 타입**을 선택 (예: Normal 프리팹이면 Normal, OneTime 프리팹이면 One Time).
7. **Mesh Renderer**는 루트 또는 자식에 두어 색상 적용(`ApplyColor`)이 되도록 합니다.
8. (선택) **Moving** 타입만 **Moving Speed** / **Moving Range**, **HighJump** 타입만 **High Jump Multiplier**를 Inspector에서 조정.
9. **Project** 창으로 드래그해 `Assets/Prefabs/Platform_Normal.prefab` 등 **타입별로 다른 프리팹**으로 저장.

> 각 프리팹 루트에 **Platform** 태그, **Collider(Is Trigger = On)**, **Platform** 컴포넌트와 **Platform Type**이 맞게 설정되어 있으면, 풀에서 꺼낼 때 타입이 정해지고 `Interact()`가 정상 동작합니다.

### 7.2 PlatformPool · PlatformSpawner 오브젝트 구성

1. **Hierarchy** 우클릭 → **Create Empty** → 이름 `PlatformPool` 로 변경.
2. **PlatformPool** 오브젝트에 **Platform Pool** 스크립트 추가.
3. **Inspector**에서 **Prefab Mappings** 리스트를 사용합니다. **Platform Prefab** 단일 필드는 없습니다. (7.2.1 참고)
4. (선택) **Pool Container**: 비활성 발판들이 정리될 부모 Transform. **Default Capacity**, **Max Size**는 타입별 풀 공통 적용.
5. **Platform Spawner**를 같은 오브젝트 또는 별도 빈 오브젝트에 추가.
6. **Platform Spawner** Inspector에서 **Platform Pool**, **Cylinder Movement**, **Player** 참조 연결.

#### 7.2.1 PlatformPool Prefab Mappings 연결 (타입별 전용 프리팹)

- **Prefab Mappings**는 **타입 → 프리팹** 한 쌍씩 등록하는 리스트입니다.
- **Size**를 사용할 타입 개수만큼으로 두고 (최소 5: Normal, OneTime, Broken, Moving, HighJump), 각 요소마다:
  - **Type**: 드롭다운에서 `Normal`, `One Time`, `Broken`, `Moving`, `High Jump` 중 하나.
  - **Prefab**: 그 타입 전용으로 만든 **발판 프리팹**을 드래그 (예: Platform_Normal, Platform_HighJump …).
- **같은 타입을 리스트에 두 번 넣지 마세요.** 타입당 하나의 프리팹만 등록하면 됩니다.
- **Normal**은 반드시 등록하는 것을 권장합니다. (시작 발판이 Normal 풀에서 나옵니다.)
- 등록한 타입만 `Get(PlatformType)`으로 스폰할 수 있습니다. Spawner의 **RollPlatformType()** 확률에 없는 타입은 프리팹만 넣어두면 되고, 있으면 반드시 매핑해야 합니다.

### 7.3 Spawner Inspector 권장값 — 자연스러운 나선형 계단

| 항목 | 권장 범위 | 권장 기본값 | 설명 |
|------|-----------|-------------|------|
| **Min / Max Spawn Height Step** | 1.5~2.5 / 3~5 | **2 / 4** | Y축 발판 간격. 난이도에 따라 min→max로 보간됨. **Max는 플레이어 jumpForce 한계 이하로** (7.3.1 참고). |
| **Difficulty Max Height** | 300~1000 | **500** | 이 높이에서 난이도가 최고조(간격·각도 최대)에 달함. |
| **Min / Max Spawn Angle Step** | 20~40 / 80~120 (도) | **30 / 100** | 매 스폰마다 난이도에 따라 min~max 범위에서 랜덤 각도, 50% 확률로 좌(－) 또는 우(＋) 방향. |
| **Spawn Ahead Distance** | 10 ~ 25 | **15** | 플레이어보다 이 높이만큼 위까지 미리 생성. 15면 위쪽으로 여유 있게 생성됩니다. |
| **Despawn Distance** | 5 ~ 15 | **10** | 플레이어보다 이만큼 아래에 있는 발판은 풀 반환. 10이면 아래쪽이 깔끔하게 정리됩니다. |

**PlatformPool** 권장값:

| 항목 | 권장 범위 | 설명 |
|------|-----------|------|
| **Default Capacity** | 16 ~ 64 | **32** | 풀 초기 생성 개수. 발판이 많아지면 자동 확장됩니다. |
| **Max Size** | 64 ~ 256 | **128** | 풀 최대 개수. 이 이상은 반환 시 Destroy됩니다. |

- **CylinderMovement**의 **Radius**가 5일 때: **Min/Max Spawn Height Step = 2/4**, **Difficulty Max Height = 500**, **Min/Max Spawn Angle Step = 30/100**, **Spawn Ahead Distance = 15**, **Despawn Distance = 10** 으로 시작한 뒤, 플레이해 보며 간격과 난이도에 맞게 조절하세요.

#### 7.3.1 동적 난이도 — jumpForce에 맞춘 Max Spawn Height Step 세팅 (도달 가능한 쫄깃한 난이도)

높이에 따라 발판 간격이 **Min Spawn Height Step** → **Max Spawn Height Step**으로 넓어지므로, **Max Spawn Height Step**이 플레이어의 점프 한계를 넘으면 도달 불가 발판이 나옵니다. 아래를 참고해 **PlayerController의 Jump Force**와 **Platform Spawner의 Max Spawn Height Step**을 맞추세요.

**감각적 가이드**

- Unity 중력이 기본 **-9.81**일 때, **jumpForce ≈ 12** 정도면 초기 velocity 12로 올라갔다가 중력으로 감속되며 **대략 4~5 유닛** 높이까지 도달 가능한 경우가 많습니다. (정확한 값은 Rigidbody mass, drag, 중력 스케일 등에 따라 다름.)
- **경험식**: `maxSpawnHeightStep`은 **실제 플레이로 “가장 높이 점프했을 때 도달한 Y 차이”의 약 85~95%** 로 두면, “겨우 닿는” 쫄깃한 난이도가 됩니다. 100%에 가깝게 두면 한 번만 실수해도 못 밟을 수 있으므로, **4~4.5** 정도가 jumpForce 12 전후에서 무난한 상한선인 경우가 많습니다.
- **절대 한계**: `maxSpawnHeightStep`을 **점프로 실제로 도달 가능한 최대 높이 차이보다 크게 두지 마세요.** 테스트 시 “이 높이 차이는 절대 못 넘겠다”라고 느껴지는 값보다 **조금 아래**에서 막아 두는 것이 안전합니다.

**권장 절차**

1. **PlayerController**에서 **Jump Force**를 게임에 맞게 고정 (예: 12).
2. 플레이 테스트로 **한 번 점프에 도달 가능한 최대 Y 차이**를 대략 측정 (예: 4.5).
3. **Max Spawn Height Step**을 그 값의 **0.85~0.95** 구간으로 설정 (예: 3.8~4.3). Inspector에서 4로 두고, 플레이해 보며 “너무 못 밟는다”면 낮추고, “너무 쉽다”면 소폭 올리는 식으로 미세 조정.

이렇게 하면 낮은 구간은 넉넉하고, 높이 올라갈수록 아슬아슬한 **도달 가능한 쫄깃한 난이도**가 유지됩니다.

### 7.5 특수 발판 — 타입별 프리팹 및 확률 조절

- **타입별 전용 프리팹**을 쓰므로, 각 프리팹의 **Platform** 컴포넌트에서 **Platform Type**만 해당 타입으로 맞춰 두면 됩니다. (Visual Mappings 없음.)
- **Platform Pool**의 **Prefab Mappings**에 타입별 프리팹을 한 쌍씩 등록 (7.2.1 참고).
- **Platform Spawner**의 **Special Platforms** 확률로 어떤 타입이 나올지만 조절하면 됩니다.

**Special Platforms 확률 (Platform Spawner)**

| 항목 | 권장 범위 | 설명 |
|------|-----------|------|
| **One Time Chance** | 0.05 ~ 0.15 | **0.1** — 밟으면 한 번 점프 후 사라짐. |
| **Broken Chance** | 0.03 ~ 0.08 | **0.05** — 함정. 밟으면 점프 안 되고 떨어지며 사라짐. |
| **Moving Chance** | 0.08 ~ 0.15 | **0.1** — 좌우 왕복 이동. |
| **High Jump Chance** | 0.05 ~ 0.12 | **0.08** — 슈퍼 점프. |

- **Normal**은 위 네 확률을 뺀 나머지로 적용됩니다. 네 값 합이 1을 넘지 않게 하고, 특수 합은 **0.3~0.4** 정도가 무난합니다.

### 7.4 좌우 랜덤 배치 · 발판/배경 색상 변화

- **다이내믹 각도 스폰**: Platform Spawner는 난이도에 따라 **Current Min Angle**~**Max Spawn Angle Step** 범위에서 매번 랜덤 각도를 구하고, 50% 확률로 좌(－) 또는 우(＋) 방향으로 적용합니다. Y 간격은 **Current Height Step**(난이도 보간)만큼 증가하므로 발판이 겹치지 않습니다.
- **발판 색상**: **Platform Color Gradient**, **Max Height For Color**를 설정하면 높이 비율(`spawnY / maxHeightForColor`)로 Gradient 색상을 추출해 **MaterialPropertyBlock**으로만 적용합니다. `Renderer.material`을 건드리지 않아 머티리얼 인스턴스가 늘어나지 않으므로 모바일 메모리에 유리합니다.
- **배경 색상**: **Camera Color Manager** 스크립트를 카메라(또는 별도 오브젝트)에 붙이고, **Player**, **Background Color Gradient**, **Max Height**를 연결하면 플레이어 Y에 따라 `Camera.main.backgroundColor`가 Gradient로 바뀝니다. **Main Camera의 Clear Flags는 Solid Color**로 두어야 배경색이 보입니다.

#### 7.4.1 Gradient 예쁘게 세팅하기

| 용도 | 추천 설정 |
|------|-----------|
| **Platform Color Gradient** | 시간 0(낮은 높이): 어두운 색(예: 남색 #1a1a2e, 회색). 시간 1(높은 높이): 밝은 색(예: 하늘색 #a8d8ea, 연보라). 중간 키 1~2개 넣어 그라데이션을 부드럽게. |
| **Background Color Gradient** | 시간 0: 지면 느낌(어두운 청록, 남색). 시간 1: 하늘/우주 느낌(밝은 파랑, 보라). 게임 톤에 맞춰 채도·밝기만 조절해도 효과적. |

- **팁**: Gradient 창에서 **Mode = Blend**, 키 포인트 **Alpha**는 1로 통일. 색상 키는 2~4개면 충분하고, 너무 많은 키는 불필요하게 복잡해질 수 있습니다.

#### 7.4.2 MaterialPropertyBlock이 잘 동작하기 위한 발판 머티리얼 세팅

- **Platform**의 **ApplyColor()**가 **현재 활성화된 Visual Model** 안의 **MeshRenderer**에만 MaterialPropertyBlock을 적용합니다. 따라서 타입별 외형 자식에 **MeshRenderer**와 **공유 머티리얼**을 두면 됩니다. 스크립트는 `_BaseColor`(URP Lit) 또는 `_Color`(Built-in)만 덮어씌우므로 **Renderer.material**을 건드리지 않습니다.
- **URP**: 발판 머티리얼 셰이더가 **Universal Render Pipeline/Lit**(또는 Lit 계열)이면 **Base Map**에 대응하는 프로퍼티가 `_BaseColor`입니다. 별도 설정 없이 위 Gradient·스폰 로직만으로 색이 적용됩니다.
- **Built-in**: **Standard** 셰리얼을 쓰면 **Albedo**에 대응하는 `_Color`가 적용됩니다. 스크립트는 `_BaseColor`를 먼저 시도하고, 없으면 `_Color`를 사용합니다.
- **주의**: 셰이더에 `_BaseColor`/`_Color` 프로퍼티가 없으면 PropertyBlock 적용이 무시됩니다. 커스텀 셰이더를 쓰는 경우 해당 이름의 Color 프로퍼티를 추가하거나, Lit/Standard처럼 위 프로퍼티를 가진 셰이더를 사용하세요.

#### 7.4.3 분기 발판(Branching Paths) — 리스크 앤 리턴이 확실한 레벨 디자인

**Platform Spawner**의 **Branching Paths** 섹션에서 다음 값을 조절하면, "한쪽만 가면 막다른 길"이 되지 않고 **리스크 앤 리턴**이 분명한 구조를 만들 수 있습니다.

| 항목 | 추천 범위 | 효과 |
|------|------------|------|
| **Double Spawn Chance** | **0.25 ~ 0.4** | 너무 높으면(0.5 이상) 거의 매번 양갈래라 메인 경로가 흐려지고, 너무 낮으면(0.1 이하) 분기가 거의 느껴지지 않음. **0.3** 전후면 가끔 반대편에 서브 발판이 생겨 "저쪽으로 갈까?" 선택이 생김. |
| **Sub Platform Height Offset** | **0.5 ~ 1.5** | 서브 발판이 메인보다 **살짝 높거나 낮게** 나오면, "더 높은 쪽 = 위험하지만 점수/진행 보상", "더 낮은 쪽 = 안전한 회귀"처럼 느껴지게 할 수 있음. **1f** 정도면 한 칸 정도 차이로 리스크가 과하지 않음. 2 이상이면 점프 난이도가 크게 달라질 수 있음. |

**레벨 디자인 팁**

- **Double Spawn Chance ≈ 0.3**: 메인 흐름은 한 방향으로 유지되고, 일정 확률로만 반대편에 서브가 생기므로 "가끔 나오는 선택지"가 됨. 0.5로 올리면 양갈래가 많아져 난이도·패턴이 달라질 수 있음.
- **Sub Platform Height Offset**: 서브를 **조금만** 위/아래로 두면(0.5~1.5) "조금 더 높은 발판으로 도전 vs 안전한 쪽" 같은 리스크 앤 리턴이 분명해짐. 너무 크게 주면 한쪽이 지나치게 어렵거나 쉬워질 수 있음.
- 메인 발판 기준으로만 `_lastSpawnY` / `_lastSpawnAngleRad`가 갱신되므로, 서브는 "옆길"일 뿐 다음 스폰 기준에는 영향을 주지 않음. 막다른 길이 되어도 다음 메인은 항상 메인 경로 위로만 이어짐.

---

## 9. 게임 루프 및 점수/UI (GameManager · UIManager)

게임 상태(Playing / GameOver), 점수(최고 도달 높이), 추락 판정, 재시작은 **GameManager**가 담당하고, 화면에 보이는 UI는 **UIManager**가 담당합니다. 두 매니저는 C# 이벤트(`OnGameOver`)로만 연결됩니다.

### 9.1 TextMeshPro 준비

1. 메뉴 **Window** → **TextMeshPro** → **Import TMP Essential Resources** 실행 (최초 1회).
2. UI 텍스트는 **TextMeshPro - Text (UI)** 를 사용합니다 (기본 Text 대신).

### 9.2 Canvas 및 UI 계층 구조

1. **Hierarchy** 우클릭 → **UI** → **Canvas** 생성. (EventSystem은 자동 생성됨.)
2. Canvas **Render Mode**: **Screen Space - Overlay**. **UI Scale Mode**: **Scale With Screen Size**, **Reference Resolution** 예: 1080×1920 (세로), **Match**: 0.5.
3. Canvas 아래 자식으로 다음 구조를 만듭니다.

```
Canvas
├── ScoreText           ← TextMeshPro - Text (UI), 인게임 점수 (상단 등)
├── GameOverPanel       ← 빈 오브젝트 (RectTransform) + Canvas Group 컴포넌트
│   ├── Background      ← Image (어두운 반투명 등, 선택)
│   ├── FinalScoreText ← TextMeshPro - Text (UI), "최종 점수: 0"
│   └── RestartButton   ← UI Button - TMP (또는 Button + 자식 Text)
```

4. **ScoreText**: 화면 상단 등 원하는 위치에 배치. 초기 텍스트는 "0". **GameOverPanel**보다 **뒤(위)** 에 두면 게임 오버 시 가립니다.
5. **GameOverPanel**: RectTransform을 **Stretch Full** (앵커 좌우상하 꽉 참)로 두어 전체 화면을 덮게 합니다. **Canvas Group** 컴포넌트 추가 → **Alpha** 0으로 두거나, 스크립트가 초기화 시 0으로 설정합니다.
6. **GameOverPanel** 자식 **FinalScoreText**: 가운데 등에 배치, "최종 점수: 0" 등 문구.
7. **RestartButton**: 버튼 레이블을 "재시작" 등으로 설정.

### 9.3 GameManager 배치 및 연결

1. **Hierarchy** 우클릭 → **Create Empty** → 이름 **GameManager**.
2. **Game Manager** 스크립트 추가.
3. **Inspector** 설정:
   - **Player**: **Player** 오브젝트의 **Transform** 드래그.
   - **Fall Death Distance**: 추락 판정 거리 (예: **15**). 플레이어 Y가 (최고 도달 Y − 이 값)보다 낮아지면 게임 오버.

### 9.4 UIManager 배치 및 연결

1. **Canvas** 오브젝트를 선택하거나, Canvas와 형제로 **빈 오브젝트**를 만들어 이름 **UIManager**로 지정.
2. **UI Manager** 스크립트 추가.
3. **Inspector**에서 다음 참조를 연결합니다.

| 항목 | 연결 대상 |
|------|-----------|
| **Score Text** | 인게임 점수용 TextMeshProUGUI (ScoreText) |
| **Game Over Panel** | 게임 오버 시 보일 패널 GameObject (GameOverPanel) |
| **Game Over Panel Canvas Group** | GameOverPanel에 붙인 **Canvas Group** 컴포넌트 드래그 |
| **Final Score Text** | 게임 오버 시 최종 점수를 표시할 TextMeshProUGUI |
| **Restart Button** | 재시작 버튼 (Button 컴포넌트) |
| **Game Over Fade Duration** | 페이드인 시간 (예: 0.4) |

4. **GameManager**는 UIManager가 **FindObjectOfType**으로 런타임에 찾습니다. 씬에 **GameManager** 오브젝트가 하나 있으면 됩니다.
5. 재시작 버튼의 **OnClick**에는 아무것도 넣지 않아도 됩니다. UIManager가 **Awake**에서 `restartButton.onClick.AddListener(GameManager.RestartGame)` 로 연결합니다.

### 9.5 동작 요약

- **Playing** 중: GameManager가 플레이어 최고 Y를 점수로 갱신하고, UIManager가 **Update**에서 인게임 점수 텍스트를 해당 점수로 표시.
- **추락 판정**: 플레이어 Y &lt; (최고 Y − Fall Death Distance) → GameManager가 **GameOver**로 전환 후 **OnGameOver** 발생.
- **OnGameOver** 수신 시 UIManager: 인게임 점수 숨김 → 게임 오버 패널 활성화 → 최종 점수 표기 → 패널의 **CanvasGroup.alpha**를 DOTween으로 0→1 페이드인.
- **재시작 버튼** 클릭 → GameManager.**RestartGame()** → 현재 씬 재로드.

---

## 10. 요약 체크리스트

- [ ] Input System: **Player** 맵의 **Move** 액션이 키보드/게임패드(및 필요 시 터치)에 바인딩됨.
- [ ] 태그 **Platform** 생성 후 발판 오브젝트에 지정.
- [ ] **Player** 오브젝트에 **PlayerInputHandler**, **CylinderMovement**, **PlayerController**, **Rigidbody**, **Collider** 부착. (선택) **PlayerVisualController** 추가 시 **Visual Model**에 자식 Transform만 연결 (섹션 4.1.1 참고).
- [ ] **PlayerController**의 Turn Speed, Jump Force 설정.
- [ ] **CylinderMovement**의 Radius 설정.
- [ ] **PlayerInputHandler**에 Move 액션 연결(또는 Player Input으로 자동 사용).
- [ ] **Main Camera**에 **Camera Y Tracker** 부착, **Target**에 Player 할당, **Camera Distance**(예: 12), **Look At Height Offset**(예: 3) 설정.
- [ ] 플랫폼 오브젝트에 Tag **Platform** 및 Collider 설정.
- [ ] **발판 프리팹** 생성 후 **PlatformPool**에 할당, **PlatformSpawner**에 Pool / CylinderMovement / Player 연결 및 권장값 설정 (섹션 7 참고). (선택) **Platform Color Gradient**, **Max Height For Color** 설정 및 발판 머티리얼은 _BaseColor/_Color 지원 셰이더 사용 (7.4.2 참고).
- [ ] (선택) **Main Camera**에 **Camera Color Manager** 부착, **Player**·**Background Color Gradient**·**Max Height** 연결, 카메라 **Clear Flags = Solid Color** (7.4 참고).
- [ ] **GameManager** 빈 오브젝트 생성, **Player**·**Fall Death Distance** 연결 (섹션 9 참고).
- [ ] **Canvas**·**ScoreText**·**GameOverPanel**·**RestartButton** 구성 후 **UIManager**에 모든 UI 참조 및 **Game Over Panel Canvas Group** 연결 (섹션 9 참고).

위 순서대로 설정하면 코어 플레이(원기둥 위 이동, 플랫폼 점프, 카메라 Y 추적, 발판 무한 스폰)와 게임 루프(점수, 추락 판정, 게임 오버, 재시작)가 동작합니다.
