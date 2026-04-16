# libblackout 개발자 가이드

## 📋 개요

이 문서는 libblackout 패키지 내부 구조와 코드 수정 방법을 설명합니다.
패키지 사용법은 `libblackout/README.md`, Unity↔Python 통신 구조는 `ml_agent_design.md`를 참고하십시오.

---

## 🏗️ 패키지 구조

```
libblackout/
├── pyproject.toml           — 패키지 빌드 설정, 의존성
├── test_env.py              — 환경 연결 없이 실행 가능한 단위 테스트
└── blackout/
    ├── __init__.py          — 공개 API 재출
    ├── competition/         — 대회용 유틸 (run_match, load_checkpoint 등)
    ├── env/
    │   ├── blackout_env.py  — BlackOutEnv: PettingZoo ParallelEnv 구현
    │   ├── obs_preprocessor.py  — ObsPreprocessor: raw obs 변환
    │   ├── seed_channel.py  — SeedChannel: Python → Unity 시드 전달
    │   ├── semantic_id.py   — SemanticId: graphic 채널 인덱스 상수
    │   └── constants.py     — 에이전트 이름/팀 유틸리티
    └── model/               — BaseModel, CheckpointModel 추상 클래스
```

---

## ⚙️ 핵심 클래스

### BlackOutEnv

**파일:** `blackout/env/blackout_env.py`  
**역할:** mlagents_envs.UnityEnvironment를 PettingZoo ParallelEnv로 래핑

**초기화 흐름:**

```
__init__()
├── semantic_map_config.json 로드 → n_items, n_classes 파싱
├── ObsPreprocessor 생성 (vector_obs_size, n_graphic_channels 계산)
├── observation_space / action_space 정의
├── SeedChannel, EngineConfigurationChannel 생성
└── UnityEnvironment 연결 (gRPC)
```

**step 당 흐름:**

```
step(actions)
├── _send_actions()      — 각 에이전트에 ActionTuple 전송. MapObsAgent에는 빈 액션 전송
├── unity_env.step()     — gRPC 1 라운드트립
└── _collect_obs()
    ├── _collect_map_obs()
    │     MapObsAgent.DecisionSteps 에서 visual_obs 수신
    │     preprocess_graphic() → TeamA graphic
    │     flip_team_perspective() → TeamB graphic
    │     _team_graphics[0], [1] 캐시 갱신
    └── BlackOutUnit × 10
          _extract_step() → (agent_name, raw_vector)
          _preprocess()   → {"vector": ..., "graphic": _team_graphics[team_id]}
          _extract_scalars() → score_0, score_1, time_left
```

**내부 상태:**

| 속성 | 타입 | 설명 |
|---|---|---|
| `_preprocessor` | `ObsPreprocessor` | obs 변환기 |
| `_team_graphics` | `dict[int, ndarray]` | 팀별 graphic 캐시. 0=TeamA, 1=TeamB |
| `_agent_name_cache` | `dict[tuple, str]` | (behavior, agent_id) → agent_name 매핑 |
| `_latest_rewards` | `dict[str, float]` | 직전 step 보상 |
| `_latest_terminations` | `dict[str, bool]` | 직전 step 종료 여부 |
| `_latest_winner` | `int \| None` | 0=TeamA, 1=TeamB, -1=무승부, None=진행 중 |
| `_latest_scalars` | `dict[str, float]` | score_0, score_1, time_left |

---

### ObsPreprocessor

**파일:** `blackout/env/obs_preprocessor.py`  
**역할:** Unity raw obs → 모델 입력 변환. 스테이트리스

**생성:**

```python
cfg = load_semantic_config("semantic_map_config.json")
preprocessor = ObsPreprocessor(cfg, n_items=1, n_classes=3)
```

**주요 속성:**

| 속성 | 설명 |
|---|---|
| `vector_obs_size` | 전처리 후 vector 크기 = `20 + 10 + 10×(n_items+1) + n_classes + 3` |
| `n_graphic_channels` | graphic 채널 수 = `item_id_offset + n_items` |
| `RAW_VECTOR_SIZE` | raw vector 크기 (45) |
| `RAW_CLASS_SLOT` | raw vector에서 classId 위치 (40) |
| `RAW_SCALAR_START` | own_score 시작 위치 (41) |
| `RAW_UNIT_INDEX_SLOT` | unitIndex 위치 (44) — 전처리 시 제거 |

**메서드:**

| 메서드 | 입력 | 출력 | 설명 |
|---|---|---|---|
| `preprocess_vector(raw)` | `float32[45]` | `float32[vector_obs_size]` | holdingItemId/classId → one-hot 변환 |
| `preprocess_graphic(raw)` | `float32[H, W, 1 or 3]` | `float32[H, W, C]` | 픽셀 ID → binary channel masks |
| `flip_team_perspective(graphic)` | `float32[H, W, C]` | `float32[H, W, C]` | ally↔enemy 채널 스왑 |

---

### SemanticId

**파일:** `blackout/env/semantic_id.py`  
**역할:** 전처리 후 graphic 채널 인덱스를 가독성 있는 상수로 제공

```python
from blackout import SemanticId

# 채널 인덱스 상수
SemanticId.EMPTY          # 0
SemanticId.WALL           # 1
SemanticId.ALLY_STORAGE   # 2
SemanticId.ENEMY_STORAGE  # 3
SemanticId.ALLY_UNIT      # 4
SemanticId.ENEMY_UNIT     # 5
SemanticId.ITEM_ID_OFFSET # 6

# 아이템 채널 계산
SemanticId.item_channel(0)   # 6 (첫 번째 아이템)
SemanticId.item_channel(1)   # 7 (두 번째 아이템)
SemanticId.item_index(6)     # 0
SemanticId.is_item(7, n_items=2)  # True
SemanticId.name(4)           # "ally_unit"
SemanticId.all_channels(n_items=2)  # [0, 1, 2, 3, 4, 5, 6, 7]

# 모델에서 사용 예시
ally_mask = graphic[:, :, SemanticId.ALLY_UNIT]   # float32[H, W]
item0     = graphic[:, :, SemanticId.item_channel(0)]
```

---

### 상수 및 유틸리티 (`constants.py`)

```python
from blackout.env.constants import (
    BEHAVIOR_NAME,      # "BlackOutUnit"
    MAP_BEHAVIOR_NAME,  # "BlackOutMap"
    N_AGENTS,           # 10
    N_TEAM_A,           # 5
    agent_name,         # unit_index → "unit_N"
    unit_index,         # "unit_N" → int
    team_of,            # "unit_N" → 0 or 1
    team_a_agents,      # ["unit_0", ..., "unit_4"]
    team_b_agents,      # ["unit_5", ..., "unit_9"]
    all_agents,         # ["unit_0", ..., "unit_9"]
)
```

팀별 obs 분리 예시:

```python
from blackout.env.constants import team_of

a_obs = {k: v for k, v in obs.items() if team_of(k) == 0}
b_obs = {k: v for k, v in obs.items() if team_of(k) == 1}
```

---

### SeedChannel

**파일:** `blackout/env/seed_channel.py`  
**역할:** Python → Unity 단방향 int32 시드 전달

- Channel UUID: `7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d` (Unity `SeedChannel.cs`와 동일)
- `reset(seed=N)` 호출 시 `send_seed(N)` → Unity `Random.InitState(N)`
- `reset()` (seed 없음) 시 메시지 미전송 → Unity Random 상태 유지

---

## 🗺️ Observation 수정 가이드

### Vector obs에 float 추가하는 경우

1. **Unity** `BlackOutAgent.CollectObservations()` 에 `sensor.AddObservation(value)` 추가
2. **Unity** `BehaviorParameters.Vector Observation Size` 값 +1
3. **Python** `obs_preprocessor.py` 의 `RAW_VECTOR_SIZE` 및 이후 인덱스 상수 업데이트
4. **Python** `preprocess_vector()` 에서 `parts`에 새 값 추가 (또는 pass-through)
5. **Python** `vector_obs_size` 재계산 확인
6. `unitIndex`는 반드시 마지막 슬롯 유지 (`RAW_UNIT_INDEX_SLOT`). `_extract_step()` 라우팅 로직이 이 위치에 의존합니다.

### Graphic에 새 semantic 채널 추가하는 경우

→ `adding_item_type.md` 및 `ml_agent_design.md` 의 "Visual obs 추가" 섹션 참고

1. `semantic_map_config.json` 의 `n_items` +1
2. `SemanticMapRenderer` 에서 새 ID 픽셀 기록 로직 추가
3. ally/enemy 관계 있는 채널이면 `flip_team_perspective()` 에도 스왑 로직 추가
4. `_channel_ids`는 `n_graphic_channels`에서 자동 갱신 → 별도 수정 불필요

### 체크포인트 호환성

`vector_obs_size` 또는 `n_graphic_channels` 변경 시 **기존 체크포인트 로드 불가**. 재훈련 필요.

---

## 🧪 테스트

**파일:** `libblackout/test_env.py`  
Unity 프로세스 없이 실행 가능한 단위 테스트입니다.

```bash
python test_env.py
```

테스트 항목:

| 테스트 | 내용 |
|---|---|
| `test_preprocessor` | ObsPreprocessor 초기화, vector/graphic 크기, 값 범위 |
| `test_flip_team_perspective` | ally/enemy 채널 스왑 정확성 |
| `test_semantic_id` | SemanticId 상수, item_channel, is_item |
| `test_constants` | agent_name, unit_index, team_of, team_a/b_agents |
| `test_spaces` | observation_space, action_space 크기 |

새 기능 추가 시 `test_env.py`에 검증 로직을 함께 추가하십시오.

---

## 📦 패키지 빌드 및 의존성

**`pyproject.toml` 핵심 설정:**

```toml
[project]
name = "libblackout"
requires-python = ">=3.10"
dependencies = [
    "pettingzoo>=1.24.0",
    "gymnasium>=0.29.0",
    "numpy>=1.23.5",
    "mlagents-envs>=1.1.0,<2.0.0",
]
```

**버전 제약 주의사항:**
- Python 3.10.x 필수 (`mlagents-envs 1.1.0`이 3.11+ 미지원)
- `mlagents-envs 1.1.0`은 `pettingzoo==1.15.0` 의존성 선언 → 실제 임포트는 없으므로 `--no-deps` 선행 설치 후 libblackout 설치
- Unity `com.unity.ml-agents 4.0.2` ↔ Python `mlagents-envs 1.1.0` 버전 대응

**개발 환경 설치:**

```bash
uv pip install "mlagents-envs==1.1.0" --no-deps
uv pip install -e ".[dev]"  # editable install
```

---

## 🔧 공개 API (`blackout/__init__.py`)

외부에서 임포트 가능한 심볼:

```python
from blackout import (
    BlackOutEnv,           # 환경 클래스
    SemanticId,            # graphic 채널 상수
    ObsPreprocessor,       # obs 전처리기
    load_semantic_config,  # config JSON 로더
    team_of,               # agent_name → team int
    team_a_agents,         # Team A 에이전트 목록
    team_b_agents,         # Team B 에이전트 목록
    all_agents,            # 전체 에이전트 목록
    # competition utils
    run_match,
    run_series,
    load_checkpoint,
    BaseModel,
)
```
