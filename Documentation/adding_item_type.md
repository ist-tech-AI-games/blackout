# 아이템 타입 추가 가이드

## 📋 개요

새 아이템을 ML 훈련에 반영하려면 **Unity 3곳 + 공유 config 1곳**을 수정해야 합니다.
순서대로 진행하면 누락이 없습니다.

---

## 1️⃣ Unity — ItemData ScriptableObject 생성

Project 창에서 우클릭 → **Create > Project > Item Data** 로 새 에셋 생성.  
`Assets/Project/Data/Items/` 아래에 두는 것을 권장합니다.

**필드 설정:**
- **Effects**: 아이템 효과 (기존 ItemEffect 에셋 참조 또는 신규 생성)
- **MaxItemAmount**: 최대 스택량
- **Sprite / AmountTiers**: 스프라이트

---

## 2️⃣ Unity — Inspector 3곳에 아이템 등록

아래 세 배열은 **반드시 같은 순서**를 유지해야 합니다.  
인덱스가 holdingItemId 인코딩과 semantic map ID에 직접 매핑되기 때문입니다.

### 2-1. `BlackOutEpisodeCoordinator` — Known Items

`BlackOutEpisodeCoordinator` 컴포넌트의 **Known Items** 배열 끝에 새 ItemData 추가.

> 인덱스 `i`의 아이템 → obs vector의 holdingItemId = `i + 1` (0은 "없음")

### 2-2. `SemanticMapRenderer` — Known Items

`SemanticMapRenderer` 컴포넌트의 **Known Items** 배열 끝에 동일한 ItemData 추가.  
순서가 Coordinator와 동일해야 합니다.

> 인덱스 `i`의 아이템 → semantic map 픽셀 ID = `item_id_offset + i` (기본 6 + i)

### 2-3. `LevelDirector` — Special Item Prototypes (선택)

스폰되어야 하는 아이템이면 **Special Item Prototypes** 배열에도 추가.  
스폰 불필요한 아이템(초기 배치 전용 등)이면 생략.

---

## 3️⃣ 공유 Config — `semantic_map_config.json`

`Assets/StreamingAssets/semantic_map_config.json` 의 `n_items` 값을 +1:

```json
{
  "item_id_offset": 6,
  "n_items": 2,
  "n_classes": 3,
  "ids": { ... }
}
```

이 파일은 Unity의 StreamingAssets에서 읽히고, libblackout도 동일한 파일을 참조합니다.
Unity와 Python이 같은 파일을 쓰므로 한 번만 수정하면 충분합니다.

---

## 4️⃣ Python — `semantic_map_config.json` 경로 확인

libblackout은 이 json을 직접 읽어 `n_items`를 파싱하므로 Python 코드 수정은 불필요합니다.  
단, 환경 생성 시 올바른 config 경로를 넘기고 있는지 확인하세요:

```python
env = BlackOutEnv(
    env_path="...",
    semantic_config_path="path/to/semantic_map_config.json",  # n_items가 반영된 파일
)
```

**`n_items` 변경 시 자동으로 바뀌는 항목:**

| 항목 | 계산 |
|---|---|
| `vector_obs_size` | `10 * (2 + 1 + (n_items + 1)) + n_classes + 3` |
| `n_graphic_channels` | `item_id_offset + n_items` |
| observation space shape | 위 두 값에서 자동 결정 |

policy 모델을 직접 만드는 경우 `vector_size`와 `n_channels`를 config에서 계산해야 합니다:

```python
import json
with open("semantic_map_config.json") as f:
    cfg = json.load(f)
n_items   = cfg["n_items"]
n_classes = cfg["n_classes"]
vector_size = 10 * (2 + 1 + (n_items + 1)) + n_classes + 3
n_channels  = cfg["item_id_offset"] + n_items
```

---

## ✅ 체크리스트

```
[ ] ItemData ScriptableObject 생성 및 설정
[ ] BlackOutEpisodeCoordinator.KnownItems 배열 끝에 추가
[ ] SemanticMapRenderer.KnownItems 배열 끝에 동일 순서로 추가
[ ] LevelDirector.SpecialItemPrototypes 추가 (스폰 필요 시)
[ ] semantic_map_config.json — n_items 값 +1
[ ] policy 모델 입력 크기 업데이트 (vector_size, n_channels)
```

---

## 🚨 주의사항

### 순서 변경 금지
KnownItems 배열 중간에 삽입하면 기존 체크포인트의 one-hot 인코딩이 틀어집니다.  
항상 **배열 끝에 추가**해야 합니다.

### 체크포인트 호환성
`n_items`가 달라지면 vector obs 크기와 graphic 채널 수가 바뀌므로
이전에 학습한 체크포인트는 로드할 수 없습니다.
