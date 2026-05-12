# 데이터 설정 가이드

## 📋 개요

이 프로젝트는 **ScriptableObject** 패턴을 사용하여 게임 데이터를 관리합니다.

---

## 📦 데이터 타입 목록

| 타입 | 경로 | 설명 |
|------|------|------|
| GameBalanceConfig | `Assets/Project/Runtime/Resources/ScriptableObjects/Config/` | 전역 게임 설정 |
| UnitData | `Assets/Project/Runtime/Resources/ScriptableObjects/UnitData/` | 유닛 클래스 (Worker, Guard, Carrier) |
| ItemData | `Assets/Project/Runtime/Resources/ScriptableObjects/ItemData/` | 아이템 (Battery, Buff) |
| TeamData | `Assets/Project/Runtime/Resources/ScriptableObjects/Teams/` | 팀 설정 |
| MapTileData | `Assets/Project/Runtime/Resources/ScriptableObjects/MapTileData/` | 타일 설정 |
| ItemEffect | `Assets/Project/Runtime/Resources/ScriptableObjects/ItemEffects/` | 아이템 효과 |

---

## ⚙️ GameBalanceConfig

우클릭 → Create → Project → Game Balance Config

### 주요 필드

```
┌─────────────────────────────────────┐
│ GameBalanceConfig                   │
├─────────────────────────────────────┤
│ TargetScore: 200                    │ ← 목표 점수
│ MaxEpisodeTime: 300                 │ ← 최대 시간(초)
│ AbsorptionInterval: 10              │ ← 흡수 주기(초)
│ SpecialItemSpawnCooldown: 30        │ ← 특수 아이템 쿨다운
│ ItemPoolInitialSize: 50             │ ← 아이템 풀 초기 크기
│ ItemPoolMaxSize: 200                │ ← 아이템 풀 최대 크기
└─────────────────────────────────────┘
```

### 유효성 검사

OnValidate()가 자동으로 다음을 확인:
- ✅ 모든 시간 값이 양수인지
- ✅ PoolMaxSize ≥ PoolInitialSize
- ✅ TargetScore > 0

---

## 🤖 UnitData

Create → Project → Unit Data

### 필드 설정

#### 1. 기본 정보
```
┌─────────────────────────────────────┐
│ UnitData - Worker                   │
├─────────────────────────────────────┤
│ Sprite: [WorkerSprite]              │ ← 유닛 스프라이트
│ BaseSpeed: 3.0                      │ ← 이동 속도
│ Collectable: ✓                      │ ← 아이템 수집 가능 여부
└─────────────────────────────────────┘
```

#### 2. CollisionBound
```
┌─────────────────────────────────────┐
│ Collision Bound                     │
├─────────────────────────────────────┤
│ Type: Circle                        │ ← Circle / Square
│ Width: 0.8                          │ ← 반지름 / 한 변 길이
└─────────────────────────────────────┘
```

**Type 선택:**
- `Circle`: 원형 충돌 (유닛에 권장)
- `Square`: 사각형 충돌 (타일에 권장)

#### 3. Beats (상성 관계)
```
┌─────────────────────────────────────┐
│ Beats (Array)                       │
├─────────────────────────────────────┤
│ Size: 1                             │
│ Element 0: [WorkerData]             │ ← Guard는 Worker를 이김
└─────────────────────────────────────┘
```

**상성 관계 설정 예:**
```
Worker:  Beats = [Carrier]
Guard:   Beats = [Worker, Guard, Carrier]
Carrier: Beats = [Carrier]
```
주: 현 프로젝트에서는 Guard가 Hunter로, Worker가 Collector로 명명되어 있음.

---

## 🎁 ItemData

Create → Project → Item Data

### 필드 설정

#### 1. 기본 정보
```
┌─────────────────────────────────────┐
│ ItemData - Battery                  │
├─────────────────────────────────────┤
│ MaxItemAmount: 10                   │ ← 최대 수량
│ InteractionOption: All              │ ← 수집 조건
│ CollisionBound: Circle, 0.5         │ ← 충돌 범위
└─────────────────────────────────────┘
```

#### 2. InteractionOption

```
All          : 모든 팀이 수집 가능
IgnoreFriend : 아군은 상호작용 불가
IgnoreEnemy  : 적은 상호작용 불가
```

현재는 IgnoreFriend가 기본값. 아이템은 창고에 있을 때만 팀 소속이 있으므로, 아군은 창고에서 꺼내 갈 수 없음.

#### 3. Amount Tiers (수량별 스프라이트)
```
┌─────────────────────────────────────┐
│ Amount Tiers (Array)                │
├─────────────────────────────────────┤
│ Size: 3                             │
│ ┌─ Element 0 ────────────────────┐  │
│ │ MinAmount: 1                   │  │
│ │ TierSprite: [SmallBattery]     │  │
│ └────────────────────────────────┘  │
│ ┌─ Element 1 ────────────────────┐  │
│ │ MinAmount: 5                   │  │
│ │ TierSprite: [MediumBattery]    │  │
│ └────────────────────────────────┘  │
│ ┌─ Element 2 ────────────────────┐  │
│ │ MinAmount: 10                  │  │
│ │ TierSprite: [LargeBattery]     │  │
│ └────────────────────────────────┘  │
└─────────────────────────────────────┘
```

**동작 방식:**
```
ItemAmount = 3  → SmallBattery 스프라이트
ItemAmount = 7  → MediumBattery 스프라이트
ItemAmount = 10 → LargeBattery 스프라이트
```

#### 4. Effects (아이템 효과)
```
┌─────────────────────────────────────┐
│ Effects (Array)                     │
├─────────────────────────────────────┤
│ Size: 1                             │
│ Element 0: [ScoreItemEffect]        │ ← 배터리는 점수 효과
└─────────────────────────────────────┘
```

또는 버프 아이템:
```
┌─────────────────────────────────────┐
│ Effects (Array)                     │
├─────────────────────────────────────┤
│ Size: 1                             │
│ Element 0: [BuffItemEffect]         │ ← 버프 효과
└─────────────────────────────────────┘
```

### 유효성 검사

OnValidate()가 자동으로 확인:
- ✅ MaxItemAmount ≥ 1
- ✅ AmountTiers 배열이 MinAmount 기준 정렬됨
- ✅ 모든 TierSprite가 할당됨

**경고 예:**
```
[ItemData:Battery] MaxItemAmount must be at least 1. Resetting to 1.
[ItemData:Battery] AmountTiers[1] has MinAmount 10, which is not greater than previous tier (5). This may cause incorrect sprite selection.
[ItemData:SpeedBoost] AmountTiers[0] is missing TierSprite.
```

---

## 🎨 ItemEffect

### ScoreItemEffect (점수 아이템)

Create → Project → Effects → Score Item Effect

배터리의 기본 효과. ItemAmount만큼 점수를 변화시킴.
하나의 객체만 있으면 되므로, 추가할 일은 없음.

---

### BuffItemEffect (스탯 변경)

Create → Project → Effects → Buff Item Effect

#### 설정

##### 1. 대상 선택
```
┌─────────────────────────────────────┐
│ BuffItemEffect                      │
├─────────────────────────────────────┤
│ TargetStrategy: OwnerTeam           │
└─────────────────────────────────────┘
```

**옵션:**
- `OwnerTeam`: 수집한 팀에 적용 (버프)
- `OpponentTeam`: 상대 팀에 적용 (디버프)
- `AllTeams`: 모든 팀에 적용 (중립)

##### 2. 모디파이어 설정
```
┌─────────────────────────────────────┐
│ Modifiers (Array)                   │
├─────────────────────────────────────┤
│ Size: 1                             │
│ ┌─ Element 0 ────────────────────┐  │
│ │ Type: MoveSpeed                │  │
│ │ Operation: Multiply            │  │
│ │ Value: 0.3                     │  │ ← 30% 증가
│ │ UnitFilter: AllUnits           │  │
│ │ FilterUnitData: null           │  │
│ └────────────────────────────────┘  │
└─────────────────────────────────────┘
```

**Type (스탯 종류):**
- `MoveSpeed`: 이동 속도

**Operation (연산 방식):**
- `Add`: 절대값 더하기 (예: +1.0)
- `Multiply`: 비율 더하기 (예: 0.3 = +30%)
  - 음수로 설정 가능 (예: -0.4 = -40%)
- `Override`: 고정값으로 설정. 다른 모든 연산 무시.

**UnitFilter:**
- `AllUnits`: 모든 유닛에 적용
- `SpecificClass`: 특정 클래스만 (FilterUnitData 지정 필요)

##### 3. 유닛 필터 (옵션)
```
┌─────────────────────────────────────┐
│ UnitFilter: SpecificClass           │
│ FilterUnitData: [WorkerData]        │ ← Worker만
└─────────────────────────────────────┘
```

**Worker만 30% 속도 증가:**
```
UnitFilter: SpecificClass
FilterUnitData: Worker
Type: MoveSpeed
Operation: Multiply
Value: 0.3
```

---

### 효과 조합 예시

#### 예시 1: 자기 팀 속도 증가
```
ItemData: SpeedBoost
Effects:
  - BuffItemEffect
      TargetStrategy: OwnerTeam
      Modifiers:
        - Type: MoveSpeed
          Operation: Multiply
          Value: 0.5 (50% 증가)
          UnitFilter: AllUnits
```

#### 예시 2: 상대 팀 속도 감소
```
ItemData: SlowTrap
Effects:
  - BuffItemEffect
      TargetStrategy: OpponentTeam
      Modifiers:
        - Type: MoveSpeed
          Operation: Multiply
          Value: -0.3 (30% 감소)
          UnitFilter: AllUnits
```

#### 예시 3: 복합 효과 (점수 + 버프)
```
ItemData: GoldenBattery
Effects:
  - ScoreItemEffect
  - BuffItemEffect (OwnerTeam, MoveSpeed +20%)
```

---

## 🏢 TeamData

Create → Project → Team Data

### 필드 설정

```
┌─────────────────────────────────────┐
│ TeamData - Team A                   │
├─────────────────────────────────────┤
│ TeamName: "Team A"                  │ ← 팀 이름
│ TeamColor: (빨강)                   │ ← UI 표시 색상
│ Opponent: [TeamB]                   │ ← 상대 팀 참조
└─────────────────────────────────────┘
```

### 유효성 검사

OnValidate()가 자동으로 확인:
- ✅ TeamName이 비어있지 않은지
- ✅ Opponent가 자기 자신이 아닌지
- ✅ Opponent 관계가 대칭인지 (A의 적이 B면, B의 적도 A)

**경고 예:**
```
[TeamData:TeamA] TeamName is empty. Please set a team name.
[TeamData:TeamA] Opponent is set to self! This will cause logic errors.
[TeamData:TeamA] Opponent relationship is not symmetric. TeamA.Opponent=TeamB, but TeamB.Opponent=Neutral
```

---

## 🗺️ MapTileData

Create → Project → Map Tile Data

### 필드 설정

#### 1. 기본 정보
```
┌─────────────────────────────────────┐
│ MapTileData - Ground                │
├─────────────────────────────────────┤
│ TileData: [GroundTileBase]          │ ← Unity Tilemap TileBase
│ CollisionBound: Square, 1.0         │ ← 충돌 범위
└─────────────────────────────────────┘
```

**TileData 설정:**
1. Unity Tilemap Palette에서 타일 생성
2. 생성된 TileBase를 이 필드에 할당

#### 2. 충돌 옵션
```
┌─────────────────────────────────────┐
│ TileCollisionOption: Pass           │
└─────────────────────────────────────┘
```

**옵션:**
- `Pass`: 모두 통과 (일반 바닥)
- `BlockAll`: 모두 차단 (벽)
- `BlockFriendly`: 아군 차단
- `BlockEnemy`: 적군 차단 (각 팀 기지)

### 유효성 검사

OnValidate()가 자동으로 확인:
- ✅ TileData(TileBase)가 할당되었는지

**경고 예:**
```
[MapTileData:Ground] TileData (TileBase) is not assigned. This tile cannot be rendered.
```

---

## 🔧 실전 작업 흐름

### 새로운 버프 아이템 추가

**목표:** "Shield" 아이템 추가 (Guard 속도 2배)

1. **BuffItemEffect 생성**
   - Create → Project → Effects → Buff Item Effect
   - 이름: `ShieldEffect`
   - 설정:
     ```
     TargetStrategy: OwnerTeam
     Modifiers[0]:
       Type: MoveSpeed
       Operation: Multiply
       Value: 1.0 (100% 증가 = 2배)
       UnitFilter: SpecificClass
       FilterUnitData: [Guard]
     ```

2. **ItemData 생성**
   - Create → Project → Item Data
   - 이름: `Shield`
   - 설정:
     ```
     MaxItemAmount: 1
     InteractionOption: All
     Effects[0]: [ShieldEffect]
     ```

3. **맵에 추가**
   - `MapGenerator` 스크립트 또는
   - `LevelDirector.specialItemPrototypes` 배열에 추가

4. **ML 관련 업데이트 (ML 학습 씬 사용 시)**
   - `BlackOutEpisodeCoordinator` Inspector → **Known Items** 배열 끝에 새 ItemData 추가
   - `SemanticMapRenderer` Inspector → **Known Items** 배열 끝에 동일하게 추가
   - 두 배열의 순서가 일치해야 함 (인덱스 = holdingItemId - 1)
   - `Assets/StreamingAssets/semantic_map_config.json`의 `item_id_offset`은 변경 불필요 (6 고정)
   - Python `BlackOutEnvWrapper`의 `N_ITEMS` 상수를 새 아이템 수에 맞게 수정

5. **테스트**
   - Play 모드에서 Shield 수집
   - Guard 속도 확인 (3.5 → 7.0)

---

### 새로운 게임 모드 프로파일

**목표:** "Fast Mode" 프로파일 (빠른 훈련용)

1. **GameBalanceConfig 복제**
   - 기존 Config 선택 → Ctrl+D
   - 이름: `FastModeConfig`

2. **설정 변경**
   ```
   TargetScore: 50 (낮게)
   MaxEpisodeTime: 60 (짧게)
   AbsorptionInterval: 5 (빠르게)
   SpecialItemSpawnCooldown: 15
   ```

3. **GameScenario에 할당**
   - Hierarchy에서 GameScenario 선택
   - Balance Config 필드에 FastModeConfig 할당

4. **테스트**
   - 에피소드 길이 확인
   - 학습 속도 측정

---

## ✅ 체크리스트

### UnitData 생성 시
- [ ] Sprite 할당
- [ ] BaseSpeed > 0
- [ ] Collectable 설정 (Worker 계열은 true)
- [ ] CollisionBound 설정
- [ ] Beats 배열 설정 (상성 관계)
- [ ] OnValidate 경고 없음

### ItemData 생성 시
- [ ] MaxItemAmount ≥ 1
- [ ] InteractionOption 선택
- [ ] CollisionBound 설정
- [ ] AmountTiers 정렬 확인
- [ ] 모든 TierSprite 할당
- [ ] Effects 할당
- [ ] OnValidate 경고 없음
- [ ] (ML 사용 시) `BlackOutEpisodeCoordinator` Known Items 배열에 추가
- [ ] (ML 사용 시) `SemanticMapRenderer` Known Items 배열에 동일 순서로 추가
- [ ] (ML 사용 시) Python `BlackOutEnvWrapper`의 `N_ITEMS` 수정

### BuffItemEffect 생성 시
- [ ] TargetStrategy 선택 (의도 확인)
- [ ] Modifiers 설정
  - [ ] Type 선택
  - [ ] Operation 선택
  - [ ] Value 범위 확인 (-1 ~ 1 권장)
  - [ ] UnitFilter 설정
- [ ] 밸런스 테스트

### GameBalanceConfig 수정 시
- [ ] 모든 시간 값 > 0
- [ ] PoolMaxSize ≥ PoolInitialSize
- [ ] TargetScore > 0
- [ ] 밸런스 테스트 (에피소드 길이, 승률)

---

## 🚨 주의사항

### 1. 순환 참조 금지
❌ **잘못된 예:**
```
TeamA.Opponent = TeamB
TeamB.Opponent = Neutral
Neutral.Opponent = TeamA
```
- 3개 이상의 순환 참조는 로직 오류 발생

✅ **올바른 예:**
```
TeamA.Opponent = TeamB
TeamB.Opponent = TeamA
Neutral.Opponent = null
```

### 2. 모디파이어 Value 범위
⚠️ **권장 범위:**
```
Multiply Operation: -0.9 ~ 2.0
  - -1.0 이하: 역방향 (비권장)
  - 0.0: 변화 없음
  - 1.0: 2배
  - 2.0: 3배

Add Operation: -5.0 ~ 5.0
```