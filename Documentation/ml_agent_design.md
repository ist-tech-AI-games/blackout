# ML-Agents 브릿지 설계 (Black Out)

## 📋 개요

이 문서는 Unity ML-Agents와 Python(libblackout) 사이의 통신 구조, obs/action 처리 방식, 설정 파일 관리를 설명합니다.

---

## 🏗️ 아키텍처

10개 유닛 에이전트 + 1개 맵 전용 에이전트 구조입니다.

```
BlackOutEpisodeCoordinator (MonoBehaviour)
├── SemanticMapRenderer          ← 매 스텝 semantic ID map 렌더링
├── GameScenario (Facade)
├── BlackOutAgent × 10           ← BehaviorName = "BlackOutUnit"
│   ├── Team A: unitIndex 0~4, TeamId=0
│   └── Team B: unitIndex 5~9, TeamId=1
└── MapObsAgent × 1              ← BehaviorName = "BlackOutMap"
    └── DynamicRTSensorComponent ← 팀 A semantic map 송신
```

**핵심 설계 — MapObsAgent 분리**

BlackOutAgent 10개가 각각 visual obs를 전송하면 매 스텝 10개의 그래픽 패킷이 gRPC로 전송됩니다.
`MapObsAgent`를 분리해 팀 A 맵 1개만 전송하고, Python이 ally/enemy 채널을 스왑해 팀 B 맵을 생성하므로 그래픽 전송이 **10회 → 1회**로 감소합니다.

---

## ⚙️ Behavior Parameters

### BlackOutUnit (10개 에이전트)

| 항목 | 값 |
|---|---|
| Behavior Name | `BlackOutUnit` |
| Vector Observation Size | `45` (raw, 전처리 전) |
| Stacked Vectors | 1 |
| Continuous Actions | 2 |
| Discrete Actions | 없음 |
| Visual Obs | 없음 (MapObsAgent가 담당) |
| TeamId (Team A) | 0 |
| TeamId (Team B) | 1 |

### BlackOutMap (1개 에이전트)

| 항목 | 값 |
|---|---|
| Behavior Name | `BlackOutMap` |
| Vector Observation Size | 0 |
| Continuous Actions | 0 |
| Visual Obs | DynamicRTSensor 1개 (팀 A semantic map, 1채널 grayscale) |

---

## 📡 관찰 벡터

### Raw (45 floats, Unity → Python)

| 인덱스 | 내용 | 설명 |
|---|---|---|
| 0~39 | 유닛 블록 × 10 | 유닛당 4개: absPos(x, y), teamSign(+1/-1), holdingItemId |
| 40 | 자신의 클래스 ID | float-cast int. Worker / Guard / Carrier 순서는 `coordinator.KnownClasses` 기준 |
| 41 | 아군 점수 | `ownScore / targetScore` |
| 42 | 적군 점수 | `oppScore / targetScore` |
| 43 | 남은 시간 | `1 - timer.Ratio` |
| 44 | unitIndex | 0~9. **Python 라우팅 전용** — 전처리 시 제거 |

**각 필드 상세:**
- `absPos`: `(u.GlobalPos - mapOrigin) / mapBounds` 정규화
- `teamSign`: 아군 `+1`, 적군 `-1`
- `holdingItemId`: 0.0 = 없음, 1.0~N.0 = 아이템 인덱스+1 (float-cast int). one-hot 변환은 Python에서 처리
- `classId`: float-cast int. one-hot 변환은 Python에서 처리
- `unitIndex`: Python의 `agent_id → unit_name` 매핑에 사용. 학습 obs에는 포함되지 않음

### 전처리 후 (`vector_obs_size` floats, Python)

원본 float 값들을 one-hot으로 확장한 결과:

| 원본 | 확장 후 | 크기 |
|---|---|---|
| absPos × 10 | 그대로 | 20 |
| teamSign × 10 | 그대로 | 10 |
| holdingItemId × 10 | one-hot[N_items + 1] × 10 | 10 × (N_items + 1) |
| classId | one-hot[N_classes] | N_classes |
| ownScore, oppScore, timeLeft | 그대로 | 3 |
| unitIndex | **제거** | — |

**총 크기 공식:** `20 + 10 + 10×(N_items + 1) + N_classes + 3`  
기본값 (N_items=1, N_classes=3): **56 floats**

---

## 🗺️ Visual Obs — Semantic ID 맵

### ID 인코딩

| ID | 의미 |
|---|---|
| 0 | empty |
| 1 | wall |
| 2 | ally_storage |
| 3 | enemy_storage |
| 4 | ally_unit |
| 5 | enemy_unit |
| 6 + i | 아이템 타입 i (item_id_offset + KnownItems 인덱스) |

`ally/enemy` 기준은 팀 A 시점으로 고정. 팀 B는 Python이 ch2↔ch3, ch4↔ch5 스왑.

### 해상도

유닛/아이템은 타일에 스냅되지 않으므로 타일보다 높은 해상도를 사용합니다:

```
텍스처 크기 = mapWidth × resolutionScale, mapHeight × resolutionScale
기본값: resolutionScale = 4 → 24×24 맵에서 96×96 출력
```

### Unity → Python 파이프라인

```
SemanticMapRenderer
├── Texture2D에 semantic ID (R=G=B=id) 기록 (byte, 0~255)
├── Graphics.Blit → RenderTexture (sRGB, ARGB32)
└── → MapObsAgent.DynamicRTSensor

DynamicRTSensor (ISensor 구현)
├── ReadPixels from RT
├── R 채널만 추출 → id / 255f  (grayscale 1채널)
└── ML-Agents에 float32[1, H, W] 전송

Python _collect_map_obs()
├── visual_obs: float32[1, H, W]  (C, H, W 순서)
├── transpose → float32[H, W, 1]
├── preprocess_graphic(): id_map = round(val × 255) → binary channel masks
└── flip_team_perspective(): 팀 B = ch2↔ch3, ch4↔ch5 스왑
```

### 전처리 후 graphic 크기

`float32[H × W × (item_id_offset + N_items)]`  
기본값 (N_items=1, item_id_offset=6): **96×96×7**

**채널 구성:**
- ch 0: empty
- ch 1: wall
- ch 2: ally_storage
- ch 3: enemy_storage
- ch 4: ally_unit
- ch 5: enemy_unit
- ch 6+i: 아이템 타입 i

---

## 🌐 gRPC 통신

### 흐름

```
[Unity]                                  [Python]
  │  UnityOutput (obs + reward + done)       │
  │ ───────────────────────────────────────> │  env.step() 반환
  │                                          │  trainer policy 업데이트
  │  UnityInput (actions)                    │
  │ <─────────────────────────────────────── │
  │  OnActionReceived() 실행                 │
  └──────────────────── (반복) ──────────────┘
```

단계당 전체 에이전트 obs가 **하나의 protobuf 메시지**로 묶여 전송됩니다.  
gRPC 메시지 한계: **4MB** (초과 시 `ResourceExhausted` 에러).

### 패킷 용량 (step당)

**Vector obs (BlackOutUnit × 10)**
```
10 에이전트 × 45 floats × 4 bytes = 1,800 bytes ≈ 1.8 KB/step
```

**Visual obs (MapObsAgent × 1, 1채널, CompressionType.None)**

| 맵 크기 | 텍스처 크기 | 비압축 | Vector 포함 합계 |
|---|---|---|---|
| 24×24 (현재) | 96×96 | **9.2 KB** | **≈ 11 KB** |
| 48×48 | 192×192 | **36.9 KB** | **≈ 38.7 KB** |
| 72×72 | 288×288 | **82.9 KB** | **≈ 84.7 KB** |
| 96×96 | 384×384 | **147.5 KB** | **≈ 149.3 KB** |

3채널 RGB 대비 1/3, 이전 설계(10 에이전트 각각 3채널) 대비 **30배 감소** (96×96 기준).

---

## 🎮 액션 공간

| 인덱스 | 내용 | 범위 |
|---|---|---|
| `ContinuousActions[0]` | X축 이동 | -1 ~ 1 |
| `ContinuousActions[1]` | Y축 이동 | -1 ~ 1 |

---

## 🏆 보상 설계

| 이벤트 | 보상 |
|---|---|
| 아군 득점 (중간) | +0.1 |
| 적군 득점 (중간) | -0.1 |
| 승리 (종료) | +1.0 |
| 패배 (종료) | -1.0 |
| 무승부 (종료) | 0.0 |

- 중간 보상: `TeamContext.OnScoreChanged` 이벤트로 수신
- `Reset()` 시 `OnScoreChanged(0)` 오발 방지 → `score > 0` 조건 사용
- 시간 초과도 점수로 승패 판정 → 모든 에피소드 종료는 `EndEpisode()` (truncation 없음)

---

## 🔄 에피소드 생명주기

```
Awake:  GameScenario.Initialize()
        SideChannelManager.RegisterSideChannel(SeedChannel)
        SemanticMapRenderer.CreateTextures()
        agent.Setup() × 10 + MapObsAgent.Setup()
        OnGameEnded 구독

Start:  GameScenario.EpisodeBegin()

[Python reset(seed=N) 호출 시]
SeedChannel.OnMessageReceived(N) → Random.InitState(N)  ← EpisodeBegin 이전 처리

[에피소드 진행]
FixedUpdate: GameScenario.EpisodeUpdate()
             SemanticMapRenderer.Render()
             MapObsAgent.RequestDecision()
OnActionReceived: GameScenario.MoveUnit()

[에피소드 종료 — 득점 목표 달성 또는 시간 초과]
OnGameEnded → SetReward() + EndEpisode() × 10
  → OnEpisodeBegin() × 10
  → episodeBeginCount == 10 → GameScenario.EpisodeBegin()
```

---

## 📬 SideChannel

### SeedChannel

Python `env.reset(seed=N)` 호출 시 시드를 Unity로 전달해 에피소드별 재현 가능한 맵/아이템 배치를 지원합니다.

| 항목 | 값 |
|---|---|
| Channel UUID | `7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d` |
| 방향 | Python → Unity (단방향) |
| 페이로드 | `int32` seed |
| Unity 처리 | `UnityEngine.Random.InitState(seed)` |

- SideChannel 메시지는 Unity의 `reset()` 커맨드 처리 전에 소비됨 → `OnEpisodeBegin()` 시점에는 시드 적용 완료
- `seed` 미전달 시 시드 메시지 없음 → Unity Random 상태 유지

---

## 🐍 Python API

### 전처리 파이프라인

```
Unity gRPC (step마다 1 protobuf 패킷)
    │
    ├── BlackOutUnit × 10
    │     raw_vector: float32[45]
    │
    └── MapObsAgent × 1
          visual_obs: float32[1, 96, 96]  (C, H, W)
    │
    ▼
BlackOutEnv._collect_obs()
    │
    ├── _collect_map_obs()
    │     transpose (C,H,W) → (H,W,C)
    │     preprocess_graphic() → float32[96, 96, N_channels]  (TeamA)
    │     flip_team_perspective() → float32[96, 96, N_channels]  (TeamB)
    │     cache: _team_graphics[0] = TeamA, _team_graphics[1] = TeamB
    │
    └── _extract_step() per BlackOutUnit
          preprocess_vector(raw[0:44])
            holdingItemId → one-hot[N_items+1]
            classId       → one-hot[N_classes]
            나머지 float 그대로
          graphic = _team_graphics[team_id]
          → obs[agent] = {"vector": ..., "graphic": ...}
    │
    ▼
모델 입력
  vector:  float32[56]         (N_items=1, N_classes=3 기준)
  graphic: float32[96, 96, 7]  (N_items=1 기준)
```

### Self-Play 구조

```
BlackOutTrainer
├── policy (shared — Team A / Team B 동일 파라미터)
├── opponent_policy (frozen snapshot, 주기적으로 업데이트)
└── training loop:
      매 step:
        Team A obs → policy          → Team A actions
        Team B obs → opponent_policy → Team B actions
        env.step(A_actions + B_actions)
        Team A rewards → policy gradient 업데이트
      N 에피소드마다:
        opponent_policy ← policy.snapshot()
```

---

## ⚠️ Obs 추가 시 유의사항

### Vector obs에 float 추가하는 경우

1. **Unity**: `BlackOutAgent.CollectObservations()`에서 `sensor.AddObservation(value)` 추가
2. **Unity**: `BehaviorParameters.Vector Observation Size` 값 +1
3. **Python**: `obs_preprocessor.py`의 `RAW_VECTOR_SIZE`, 인덱스 상수 (`RAW_CLASS_SLOT` 이후 값들) 업데이트
4. **Python**: `preprocess_vector()`에서 새 값을 `parts`에 추가 (또는 그대로 pass-through)
5. **Python**: `vector_obs_size` 재계산 확인
6. **주의**: `unitIndex`는 반드시 마지막 슬롯 유지 (`RAW_UNIT_INDEX_SLOT`). Python 라우팅 로직이 이 위치에 의존합니다.

### Visual obs에 새 semantic 채널을 추가하는 경우

1. `semantic_map_config.json`의 `item_id_offset` 또는 `n_items` 수정 → `n_graphic_channels` 자동 갱신
2. `SemanticMapRenderer`에서 새 ID 픽셀 기록 로직 추가
3. `flip_team_perspective()`에서 ally/enemy 스왑이 필요한 채널이면 로직 추가
4. `ObsPreprocessor._channel_ids`는 `n_graphic_channels`에서 자동 생성 → 별도 수정 불필요

### 체크포인트 호환성

`vector_obs_size` 또는 `n_graphic_channels`가 바뀌면 **기존 체크포인트는 로드 불가**. 재훈련 필요.

### MapObsAgent DynamicRTSensor 교체 시

- `ObservationSpec.Visual(channels, height, width)` 순서 사용 (ML-Agents `0f918b8a6271` 기준)
- 채널 수를 바꾸면 `ObservationSpec`도 함께 변경 필요
- `SetRenderTexture()`는 `CreateSensors()` 전후 어느 시점에 호출해도 동작 → `_pendingTexture` 패턴으로 구현

---

## 🗂️ JSON 관리 프로퍼티 — `semantic_map_config.json`

**경로:** `Assets/StreamingAssets/semantic_map_config.json`  
Unity와 Python이 동일한 파일을 참조합니다. Unity는 런타임에 StreamingAssets에서 읽고, Python은 `BlackOutEnv(semantic_config_path=...)` 로 경로를 전달받아 읽습니다.

```json
{
  "resolution_scale": 4,
  "n_items": 1,
  "n_classes": 3,
  "ids": {
    "empty": 0,
    "wall": 1,
    "ally_storage": 2,
    "enemy_storage": 3,
    "ally_unit": 4,
    "enemy_unit": 5
  },
  "item_id_offset": 6
}
```

| 키 | 타입 | 사용처 | 변경 시 영향 |
|---|---|---|---|
| `resolution_scale` | int | 문서화 (참고용) | 직접 영향 없음. Unity Inspector의 `resolutionScale`과 맞출 것 |
| `n_items` | int | Python: `vector_obs_size`, `n_graphic_channels` 계산 | vector 크기 + graphic 채널 수 변경 → 재훈련 필요 |
| `n_classes` | int | Python: `vector_obs_size` 계산 (class one-hot 크기) | vector 크기 변경 → 재훈련 필요 |
| `ids` | object | Python: `flip_team_perspective()` 채널 인덱스 참조 | ally/enemy 스왑 로직에 영향 |
| `item_id_offset` | int | Python: `n_graphic_channels = item_id_offset + n_items` / Unity: `SemanticMapRenderer.itemIdOffset` | graphic 채널 수 변경 → 재훈련 필요 |

**주의:** `ids` 안의 숫자가 바뀌면 `SemanticMapRenderer`의 하드코딩 상수(`ID_WALL=1` 등)도 함께 변경해야 합니다.

**Python에서 config 로드 예시:**

```python
import json
with open("semantic_map_config.json") as f:
    cfg = json.load(f)
n_items     = cfg["n_items"]
n_classes   = cfg["n_classes"]
vector_size = 10 * (2 + 1 + (n_items + 1)) + n_classes + 3
n_channels  = cfg["item_id_offset"] + n_items
```

---

## 🔧 주요 컴포넌트

### BlackOutEpisodeCoordinator

**역할:** ML 훈련 씬 전용 진입점 (GameBootstrapper와 동시 사용 불가)

**주요 동작:**
- `Awake()` 첫 번째 호출: `GameScenario.Initialize()` — TimerManager 등이 여기서 초기화되므로 반드시 먼저 호출
- `NotifyAgentEpisodeBegin()`: 10개 에이전트가 모두 준비되면 `EpisodeBegin()` 호출
- `OnGameEnded()`: 승패 보상 부여 후 모든 에이전트 `EndEpisode()`
- `SeedChannel` 등록/해제 (`Awake` / `OnDestroy`)

### BlackOutAgent

**역할:** 유닛 1개당 에이전트 1개

**주요 동작:**
- `Setup()`: 레퍼런스 초기화 및 점수 이벤트 구독
- `OnEpisodeBegin()`: coordinator에 준비 완료 통보
- 람다를 필드로 저장 → `OnDestroy()`에서 정상 구독 해제

### MapObsAgent

**역할:** 팀 A semantic 맵 송신 전용 에이전트

**주요 동작:**
- `FixedUpdate()`: 매 스텝 `RequestDecision()` 호출 — SemanticMapRenderer.Render() 이후 실행
- `Setup(RenderTexture)`: `DynamicRTSensorComponent.SetRenderTexture()` 호출
- `Awake()`: `RenderTextureSensorComponent` 제거 후 `DynamicRTSensorComponent` 추가 (legacy 컴포넌트 혼용 방지)

### SemanticMapRenderer

**역할:** semantic ID map 렌더링

**주요 동작:**
- `CreateTextures()`: `Awake`에서 호출. RenderTexture를 미리 생성해 `MapObsAgent.Setup()` 전에 RT 레퍼런스 확보
- `SubscribeEvents()`: `OnEpisodeStarted` 구독 (맵 재로드 시 background 재렌더)
- `Render()`: 매 FixedUpdate 호출. background 복사 → item 오버레이 → unit 오버레이
- `RenderBackground()`: 에피소드 시작 시 1회 — tile 레이어(wall, storage, empty) 사전 렌더링

### DynamicRTSensor (ISensor)

**역할:** RenderTexture를 ML-Agents에 1채널 grayscale로 전달

**주요 동작:**
- `_pendingTexture` 패턴: `SetRenderTexture()`가 `CreateSensors()` 전후 어느 시점에 호출되어도 동작
- `Write()`: `ReadPixels` → `GetPixels32()` → R 채널만 추출 → `id / 255f` → `writer[0, y, x]`
- Y축 플립 적용 (Unity 텍스처 좌표 하단=0, ML-Agents 관례 상단=0)

---

## ✅ 씬 설정 체크리스트

- [ ] `BlackOutEpisodeCoordinator` GameObject 배치
- [ ] `GameScenario` 레퍼런스 연결
- [ ] `BlackOutAgent` × 10 배열 연결 (배열 인덱스 = unitIndex)
- [ ] `MapObsAgent` × 1 연결
- [ ] `SemanticMapRenderer` 연결 및 `KnownItems` 배열 설정
- [ ] `MapBounds` 설정 (기본값 20×20)
- [ ] `KnownClasses` 설정 (Worker, Guard, Carrier 순서)
- [ ] 각 `BlackOutAgent`의 `Behavior Parameters` → TeamId 설정 (0 또는 1)
- [ ] `GameBootstrapper` 제거 (ML 훈련 씬에서)
