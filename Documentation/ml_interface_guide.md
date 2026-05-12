# ML-Agent 통합 가이드

## 📋 개요

이 프로젝트는 **Facade 패턴**을 사용하여 ML-Agent 훈련을 위한 깔끔한 인터페이스를 제공합니다.

**핵심 클래스:** `GameScenario`
- 모든 게임 로직의 단일 진입점
- Unity LifeCycle과 독립적인 수동 제어
- Deterministic Simulation 지향

---

## 🎮 GameScenario API

사용 방식은 직접 조작용으로 만든 `GameBootstrapper.cs`와 `PlayerUnitController.cs` 참조.

### 초기화

```csharp
public class MyMLAgent : Agent
{
    [SerializeField] private GameScenario gameScenario;

    void Awake()
    {
        gameScenario.Initialize();  // 한 번만 호출. 재시작 때는 호출하지 않음.
    }
}
```

### 에피소드 관리

#### 1. 에피소드 시작
```csharp
public override void OnEpisodeBegin()
{
    gameScenario.EpisodeBegin();
    // 맵 재생성, 유닛 리셋, 타이머 초기화 자동 처리
}
```

**내부 동작:**
- 맵 재생성 (랜덤 배치)
- 모든 유닛 스폰 위치로 리셋
- 점수 초기화
- 타이머 리셋 (에피소드 타이머, 흡수 타이머)
- 아이템 풀 정리

#### 2. 프레임 업데이트
```csharp
void FixedUpdate()
{
    gameScenario.EpisodeUpdate(Time.fixedDeltaTime);
}
```

**내부 동작:**
- 타이머 틱
- 특수 아이템 스폰 체크
- UI 업데이트

예시는 `FixedUpdate`로 들었지만, 위 동작이 필요한 적절한 시점에 호출 가능.
유닛 제어는 아래 방식으로 별도 필요.

#### 3. 유닛 제어
```csharp
public override void OnActionReceived(ActionBuffers actions)
{
    // 대충 의사 코드
    for (int i = 0; i < 10; i++) // 10개 유닛
    {
        Vector2 moveInput = new Vector2(
            actions.ContinuousActions[i * 2],
            actions.ContinuousActions[i * 2 + 1]
        );
        gameScenario.MoveUnit(i, moveInput, Time.fixedDeltaTime);
    }
}
```

**파라미터:**
- `unitIndex`: 0~9 (Team A: 0~4, Team B: 5~9)
- `moveInput`: 정규화된 2D 방향 벡터 (-1 ~ 1)
- `deltaTime`: 프레임 시간 (Unity 게임 루프와 분리되어 있으므로, 별도 계산 필요.)

---

## 🔍 디버깅 팁

### 1. 게임 상태 확인
```csharp
Debug.Log($"Game State: {gameScenario.CurrentState}");
Debug.Log($"Episode Time: {gameScenario.EpisodeTimer.CurrentTime}/{gameScenario.EpisodeTimer.Duration}");
Debug.Log($"Score A: {teamA.Score}, Score B: {teamB.Score}");
```

### 2. 유닛 상태 확인
```csharp
foreach (var unit in matchManager.Units)
{
    Debug.Log($"Unit {unit.gameObject.name}: Pos={unit.GlobalPos}, Class={unit.UnitData.name}, HoldingItem={unit.HoldingItem?.ItemData.name}");
}
```

### 3. 이벤트 로깅
```csharp
// GameEventBus 이벤트 구독으로 게임 흐름 파악
matchManager.EventBus.Flow.OnEpisodeStarted += (mm, gs) => Debug.Log("Episode Started!");
matchManager.EventBus.Flow.OnGameEnded += (winner) => Debug.Log($"Game Ended! Winner: {winner?.name}");
matchManager.EventBus.World.OnAbsorptionInterval += () => Debug.Log("Absorption!");
```

### 4. 성능 모니터링
```csharp
// Unity Profiler에서 확인할 항목:
// - GameScenario.EpisodeUpdate()
// - Unit.Move()
// - ItemObject pool operations
// - TeamContext.CalculateStat()
```