# 아키텍처 상세

## 📋 개요

이 문서는 프로젝트의 내부 시스템 구조를 상세히 설명합니다. 코드를 수정하거나 시스템을 확장할 때 참고하세요.

---

## 🏗️ 전체 아키텍처

```
┌─────────────────────────────────────────────────────────┐
│                    GameScenario                         │
│                  (Facade Pattern)                       │
│  - EpisodeBegin(), EpisodeUpdate(), MoveUnit()          │
└───────────────────┬─────────────────────────────────────┘
                    │
    ┌───────────────┼───────────────┐
    │               │               │
┌───▼────┐    ┌────▼─────┐    ┌───▼────────┐
│ Match  │    │  Level   │    │    UI      │
│Manager │    │ Director │    │  Manager   │
└───┬────┘    └────┬─────┘    └────────────┘
    │              │
    │         ┌────▼─────┐
    │         │   Map    │
    │         │ Manager  │
    │         └──────────┘
    │
┌───▼────────────────────────┐
│  TeamContext (×3)          │
│  - Score, Modifiers        │
└────────────────────────────┘
```

---

## 🎮 핵심 매니저

### GameScenario (Facade)

**역할:** 모든 게임 로직의 단일 진입점

**주요 메서드:**
```csharp
void Initialize()                                    // 한 번만 호출 (GameBootstrapper)
void EpisodeBegin()                                  // 에피소드 시작
void EpisodeUpdate(float deltaTime)                  // 매 프레임 업데이트
void MoveUnit(int unitIndex, Vector2 input, float dt) // 유닛 제어
```

**관리 항목:**
- GameEventBus (이벤트 시스템)
- TimerManager (타이머 관리)
- GameState (Initializing, Playing, GameEnded)

**설계 이점:**
- ✅ ML-Agent가 하나의 인터페이스만 알면 됨
- ✅ 내부 구현 변경 시 외부 영향 최소화
- ✅ 테스트 및 디버깅 용이

---

### MatchManager

**역할:** 팀, 유닛, 점수 관리

**주요 책임:**
1. **팀 컨텍스트 관리**
   ```csharp
   Dictionary<TeamData, TeamContext> teamContextTable
   ```
   - Team A, Team B, Neutral 각각의 점수/모디파이어 관리

2. **승리 조건 체크**
   ```csharp
   private void CheckWinCondition(TeamContext context)
   ```
   - 목표 점수 도달 감지
   - 시간 종료 시 점수 비교

3. **유닛 생명주기**
   ```csharp
   void RespawnUnit(Unit unit)
   void ResetAllUnits(MapData mapData)
   ```

**이벤트 구독:**
- `OnScoreChanged` → CheckWinCondition
- `OnTimeExpired` → 승자 결정

---

### LevelDirector

**역할:** 맵, 아이템, 월드 이벤트 관리

**주요 책임:**
1. **맵 생성**
   ```csharp
   MapData mapData = mapGenerator.GenerateMapData()
   ```
   - 매 에피소드마다 랜덤 맵 생성
   - 배터리 초기 배치

2. **아이템 풀링**
   ```csharp
   IObjectPool<ItemObject> itemPool
   ```
   - 배터리 재사용으로 GC 감소
   - 성능 최적화

3. **특수 아이템 스폰**
   ```csharp
   private void UpdateSpecialItemSpawner(float dt)
   ```
   - 주기적으로 버프 아이템 생성
   - 중립 지역에만 스폰

4. **흡수 이벤트 처리**
   ```csharp
   private void OnAbsorption()
   ```
   - 창고에 있는 배터리 점수화

**생명주기:**
```
Initialize() → StartEpisode() → ManualLateUpdate() (매 프레임) → EndEpisode()
```

---

### MapManager

**역할:** 타일 데이터, 충돌, 좌표 변환

**주요 기능:**
1. **타일 쿼리**
   ```csharp
   MapTile GetTile(Vector2Int cellPos)
   MapTile GetTileAtWorldPos(Vector3 worldPos)
   MapTile GetRandomTile(Predicate<MapTile> filter)
   ```

2. **이동 가능 여부 체크**
   ```csharp
   bool IsWalkable(Vector2Int cellPos, TeamData team)
   ```
   - TileCollisionOption에 따라 판정 (Pass, BlockFriendly, BlockEnemy, BlockAll)

3. **좌표 변환**
   ```csharp
   Vector3 CellToCenterWorld(Vector2Int cellPos)
   Vector2Int WorldToCell(Vector3 worldPos)
   ```

**최적화:**
- `allTilesCache`: 모든 타일을 미리 캐싱
- `GetRandomTile()`: O(n) 결정론적 스캔

---

### UIManager

**역할:** UI 표시 및 업데이트

**주요 뷰:**
- `TeamScoreView` - 팀별 점수
- `GameTimerView` - 타이머
- `ActiveEffectsView` - 활성 버프/디버프
- `ResultPanelView` - 게임 종료 결과

**이벤트 기반 초기화:**
```csharp
void Awake()  // Unity MonoBehaviour 메시지를 사용하고 있으나, 로직이 아니라서 괜찮을 것으로 판단
{
    gameScenario.EventBus.Flow.OnEpisodeStarted += OnEpisodeStarted;
}
```

---

## 📡 이벤트 시스템 (GameEventBus)

### 구조

```csharp
public class GameEventBus
{
    public readonly GameFlowEvents Flow;     // 게임 흐름 이벤트
    public readonly WorldEvents World;       // 월드 이벤트
    public readonly UnitEvents Unit;         // 유닛 이벤트
}
```

### 이벤트 카테고리

#### 1. GameFlowEvents (게임 흐름)
```csharp
event Action OnTimeExpired                         // 시간 종료
event Action<TeamData> OnGameEnded                 // 게임 종료
event Action<MatchManager, GameScenario> OnEpisodeStarted  // 에피소드 시작
```

**사용 예:**
```csharp
eventBus.Flow.OnGameEnded += (winner) =>
{
    Debug.Log($"Winner: {winner?.name}");
    // ML-Agent: EndEpisode(), AddReward()
};
```

#### 2. WorldEvents (월드)
```csharp
event Action OnAbsorptionInterval  // 흡수 타이밍
```

**사용 예:**
```csharp
eventBus.World.OnAbsorptionInterval += () =>
{
    // 창고의 배터리를 점수로 변환
};
```

#### 3. UnitEvents (유닛)
```csharp
event Action<Unit, ItemObject> OnItemPickedUp  // 아이템 획득
event Action<Unit> OnUnitDead                  // 유닛 사망
```

**사용 예:**
```csharp
eventBus.Unit.OnItemPickedUp += (unit, item) =>
{
    Debug.Log($"{unit.name} picked up {item.ItemData.name}");
    // ML-Agent: AddReward(0.01f)
};
```

### 이벤트 생명주기

```
Initialize()
  ↓
GameEventBus 생성
  ↓
각 매니저가 이벤트 구독
  ↓
EpisodeBegin()
  ↓ (이벤트는 유지됨)
에피소드 진행
  ↓
OnGameEnded 발행
  ↓
EpisodeBegin() (재시작)
  ↓
반복...
```

**중요:** `GameEventBus.Reset()`은 호출하지 않음!
- Initialize()에서 등록한 핸들러는 모든 에피소드에서 유지
- UIManager는 unsubscribe-before-subscribe 패턴 사용

---

## 🎯 오브젝트 풀링 시스템

### 개념

```
┌──────────────────────────────────┐
│       Object Pool                │
│                                  │
│  [Active Items]    [Pooled]     │
│  - Item1 (맵)      - Item10     │
│  - Item2 (유닛)    - Item11     │
│  - Item3 (맵)      - Item12     │
│  ...               ...           │
└──────────────────────────────────┘
```

### 생명주기

```csharp
// 1. 풀에서 가져오기
ItemObject item = itemPool.Get();
  → OnGetItem() 호출
  → ResetState()
  → activeItems에 추가

// 2. 사용 중
item.RegisterToMap(data, amount, cellPos)

// 3. 반환
item.OnDestroyed()
  → itemPool.Release(this)
  → OnReleaseItem() 호출
  → activeItems에서 제거
  → gameObject.SetActive(false)
```

---

## 📊 스탯 계산 시스템

### 구조

```
Unit.MoveSpeed 요청
  ↓
Unit.GetCachedStat(StatType.MoveSpeed, baseSpeed)
  ↓
캐시 있음? → 반환
  ↓ 캐시 없음
TeamContext.CalculateStat(unit, statType, baseValue)
  ↓
모든 activeModifiers 순회
  ↓
  - Override: 즉시 반환
  - Add: 절대값 추가
  - Multiply: 비율 누적
  ↓
최종 값 계산 및 캐싱
  ↓
반환
```

### 모디파이어 연산 순서

```csharp
float finalValue = baseValue;          // 기본값: 3.0
float percentAdd = 0f;

// 1. Add 연산
foreach (modifier if Add)
    finalValue += modifier.Value;      // +0.5 → 3.5

// 2. Multiply 연산 (누적)
foreach (modifier if Multiply)
    percentAdd += modifier.Value;      // 0.3 + 0.2 = 0.5 (50%)

finalValue *= (1f + percentAdd);       // 3.5 * 1.5 = 5.25

// 3. Override (최우선)
foreach (modifier if Override)
    return modifier.Value;             // 즉시 반환
```

### 캐시 무효화

**트리거:**
1. **모디파이어 추가**
   ```csharp
   TeamContext.OnModifierAdded += (mod) => InvalidateStatCache()
   ```

2. **모디파이어 제거**
   ```csharp
   TeamContext.OnModifierRemoved += (mod) => InvalidateStatCache()
   ```

3. **유닛 클래스 변경**
   ```csharp
   Unit.SetUnitClass(newData) → InvalidateStatCache()
   ```

4. **리셋**
   ```csharp
   Unit.ResetState() → InvalidateStatCache()
   ```

**캐시 무효화 시:**
```csharp
private void InvalidateStatCache()
{
    cachedStats.Clear();  // 다음 요청 시 재계산
}
```

### 성능 최적화 효과

**Before (캐싱 없음):**
```
매 프레임마다:
  MoveSpeed 계산 → 모든 모디파이어 순회 (O(n))
  10 유닛 × 60 FPS = 600번/초 계산
```

**After (캐싱 사용):**
```
모디파이어 변경 시만 재계산:
  버프 획득/소멸 시에만 계산
  10 유닛 × 1번 (버프 변경 시) = 10번
```

**성능 향상:** 계산 횟수 98% 감소

---

## 🎨 효과(Effect) 시스템

### 아키텍처

```
ItemData
  └─ Effects[]
       ├─ ScoreItemEffect (점수)
       └─ BuffItemEffect (스탯 변경)
```

### Dependency Injection Pattern

**문제점 (Before):**
```csharp
// ItemEffect가 ItemObject 내부를 직접 알아야 함
public abstract void OnEnterStorage(ItemObject item)
{
    TeamContext ctx = item.GetCurrentContext(); // ❌ 강한 결합
    ctx.AddScore(10);
}
```

**해결 (After):**
```csharp
// ItemEffectContext로 의존성 주입
public abstract void OnEnterStorage(ItemEffectContext context)
{
    context.OwnerContext.AddScore(10); // ✅ 느슨한 결합
}
```

### ItemEffectContext 구조

```csharp
public class ItemEffectContext
{
    public TeamContext OwnerContext;           // 소유 팀
    public int ItemAmount;                     // 아이템 양
    public ItemExitReason ExitReason;          // 퇴장 이유
    public MatchManager MatchManager;          // 매니저 접근

    // 모디파이어 관리 콜백
    Action<TeamContext, StatModifier> addModifier;
    Action<TeamContext, StatModifier> removeModifier;

    public void AddModifier(TeamContext target, StatModifier mod);
    public void RemoveModifier(TeamContext target, StatModifier mod);
    public List<TeamContext> ResolveTargetContexts(EffectTargetStrategy);
}
```

### Effect 구현 예시

#### ScoreItemEffect (배터리)
```csharp
public override void OnEnterStorage(ItemEffectContext context)
{
    // 창고에 들어갈 때 아무것도 안 함 (흡수 대기)
}

public override void OnExitStorage(ItemEffectContext context)
{
    if (context.ExitReason == ItemExitReason.Absorbed)
    {
        // 흡수 시에만 점수 추가
        int score = context.ItemAmount * ScorePerAmount;
        context.OwnerContext.AddScore(score);
    }
    // Destroyed/Dropped는 점수 없음
}
```

#### BuffItemEffect (버프)
```csharp
public override void OnEnterStorage(ItemEffectContext context)
{
    // 창고에 들어가면 즉시 버프 적용
    var targets = context.ResolveTargetContexts(TargetStrategy);

    foreach (var modifier in Modifiers)
    {
        foreach (TeamContext target in targets)
        {
            context.AddModifier(target, modifier);
            // ItemObject가 추적 리스트에 자동 추가
        }
    }
}

public override void OnExitStorage(ItemEffectContext context)
{
    // 창고에서 나가면 버프 제거 (ItemObject가 자동 처리)
    // appliedModifiers 리스트 기반으로 모두 제거
}
```

### 모디파이어 추적 시스템

**문제:** 여러 버프가 중첩되면 어떤 모디파이어를 제거해야 할까?

**해결:** ItemObject가 추적 리스트 관리
```csharp
// ItemObject.cs
private List<(TeamContext context, StatModifier modifier)> appliedModifiers = new();

private void AddModifierCallback(TeamContext target, StatModifier mod)
{
    target.AddModifier(mod);
    appliedModifiers.Add((target, mod));  // 추적
}

private void RemoveModifierCallback(TeamContext target, StatModifier mod)
{
    target.RemoveModifier(mod);
    appliedModifiers.Remove((target, mod));  // 추적 제거
}
```

**퇴장 시 자동 정리:**
```csharp
// ExitCurrentRegion()에서 자동 호출
for (int i = appliedModifiers.Count - 1; i >= 0; i--)
{
    var (ctx, mod) = appliedModifiers[i];
    ctx.RemoveModifier(mod);  // 모든 모디파이어 제거
}
appliedModifiers.Clear();
```

---

## 🔄 에피소드 생명주기 상세

### 전체 흐름

```
[GameBootstrapper.Start()]   # 직접 조작 시 사용. ML-Agent 연동 시에는 없이 만들어도 됨.
  ↓
GameScenario.Initialize()
  ├─ MatchManager.Initialize()
  │    └─ 모든 Unit.Initialize()
  └─ LevelDirector.Initialize()
       └─ ItemPool 생성

[ML-Agent.OnEpisodeBegin()]
  ↓
GameScenario.EpisodeBegin()
  ├─ TimerManager.Clear()
  ├─ 타이머 생성 (EpisodeTimer, AbsorptionTimer)
  └─ LevelDirector.StartEpisode()
       ├─ 아이템 풀 정리
       ├─ MapGenerator.GenerateMapData()
       ├─ MapManager.Initialize(mapData)
       ├─ MatchManager.ResetAllUnits()
       │    ├─ 점수 초기화
       │    ├─ 모디파이어 제거
       │    └─ 유닛 리셋 + 스폰
       └─ EventBus.Flow.PublishEpisodeStarted()
            └─ UIManager.OnEpisodeStarted()

[ML-Agent.FixedUpdate()]
  ↓
GameScenario.EpisodeUpdate(deltaTime)
  ├─ TimerManager.Tick()
  │    ├─ EpisodeTimer.Tick()
  │    │    └─ OnTimeExpired 발행 (종료 시)
  │    └─ AbsorptionTimer.Tick()
  │         └─ OnAbsorptionInterval 발행 (주기적)
  ├─ LevelDirector.ManualLateUpdate()
  │    └─ UpdateSpecialItemSpawner()
  └─ UIManager.UpdateUI()

GameScenario.MoveUnit(index, input, dt)
  ↓
Unit.Move(input, dt)
  ├─ UnitMovementSystem.CalculateNextPosition()
  ├─ ChangePos()
  │    ├─ MapTile 진입/퇴장 처리
  │    └─ MapRegion 진입/퇴장 처리
  └─ UnitInteractionSystem.ProcessInteractions()
       └─ 아이템 충돌 체크

[점수 변경 시]
TeamContext.AddScore()
  ↓
OnScoreChanged 발행
  ↓
MatchManager.CheckWinCondition()
  ↓ (목표 점수 도달 시)
EventBus.Flow.PublishGameEnded(winner)
  ↓
GameScenario.OnGameEnded()
  ↓
CurrentState = GameEnded
  ↓
[ML-Agent.OnGameEnded() 콜백]
  ↓
EndEpisode()
```

---

## 🧩 확장 가이드

### 새로운 Effect 추가

1. **ScriptableObject 생성**
   ```csharp
   [CreateAssetMenu(fileName = "TeleportEffect", menuName = "Project/Effects/Teleport")]
   public class TeleportItemEffect : ItemEffect
   {
       public Vector2Int TargetPosition;

       public override void OnEnterStorage(ItemEffectContext context)
       {
           // 텔레포트 로직
       }

       public override void OnExitStorage(ItemEffectContext context)
       {
           // 정리 로직
       }
   }
   ```

2. **ItemData에 추가**
   - Inspector에서 Effects 배열에 추가

3. **테스트**
   - Play 모드에서 아이템 수집

### 새로운 StatType 추가

1. **Enum 추가**
   ```csharp
   public enum StatType
   {
       MoveSpeed,
       CollectRange,  // ← 새로 추가
   }
   ```

2. **Unit에 프로퍼티 추가**
   ```csharp
   public float CollectRange =>
       GetCachedStat(StatType.CollectRange, UnitData.BaseCollectRange);
   ```

3. **UnitData에 기본값 추가**
   ```csharp
   [SerializeField] private float baseCollectRange = 1.0f;
   public float BaseCollectRange => baseCollectRange;
   ```

4. **사용**
   - BuffItemEffect에서 CollectRange 모디파이어 생성 가능

---

## 📚 참고 자료

### 관련 문서
- [ML-Agent 통합 가이드](./ml_interface_guide.md) - API 사용법
- [데이터 설정 가이드](./data_setting_guide.md) - ScriptableObject 설정

### 코드 참조
- `GameScenario.cs` - Facade 패턴
- `GameEventBus.cs` - 이벤트 시스템
- `ItemEffectContext.cs` - Dependency Injection
- `Unit.cs` - 스탯 캐싱
- `LevelDirector.cs` - 오브젝트 풀링
