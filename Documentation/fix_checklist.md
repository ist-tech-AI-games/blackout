# 🛠️ 수정 체크리스트

확인된 버그와 수정 방향 정리. 수정 후 체크.

---

## ⏱️ TimerManager 재진입으로 EpisodeTimer 삭제

- [x] 수정 (`Clear()` 세대 번호 + 참조 기반 제거)
- [x] 재현 테스트 통과

**재현 결과** (2026-10-31, anodi_workstation, Linux 빌드 + Xvfb, time_scale 20)
- Python `env.reset()` 없이 66,000스텝 연속 실행, 행동은 전부 0 (점수 도달 없음 → 시간초과로만 종료)
- 수정 전: 21,005스텝에 1회 종료 후 45,000스텝 동안 다시 종료되지 않음
- 수정 후: 21,005, 42,009스텝에 정상 종료
- `BlackOutEnv`/`run_match`는 매 에피소드 `env.reset()`을 호출해 타이머가 다시 만들어지므로 이 경로에서는 드러나지 않음. reset 없이 에피소드가 이어지는 학습 루프(ML-Agents trainer 등)에서 발생

**증상**
- 시간초과로 끝난 에피소드 다음부터 시간초과가 다시 발생하지 않음
- 재시작 직후 `EpisodeTimer.CurrentTime`이 0.00에 멈추고 `TimerManager` 리스트에서 사라짐
- `AbsorptionTimer`는 정상 동작, `TargetScore` 도달 종료도 정상
- 틱 기반 로직이라 timeScale과 무관하게 발생

**원인** — `TimerManager.cs`, `GameScenario.cs` `EpisodeBegin()`
1. `TimerManager.Tick()`이 리스트를 순회하다 만료된 `EpisodeTimer`의 `OnComplete` 호출
2. 콜백(`PublishTimeExpired`)이 같은 호출 스택 안에서 게임 종료 → `BlackOutEpisodeCoordinator.OnGameEnded` → `EndEpisode()` → `BeginEpisode()` → `GameScenario.EpisodeBegin()`까지 실행
3. `EpisodeBegin()`이 `TimerManager.Clear()` 후 새 `EpisodeTimer`(index 0), `AbsorptionTimer`(index 1) 추가
4. 원래 `Tick()` 루프로 돌아와 `timers.RemoveAt(i)` 실행 → 이전 타이머 대신 **새 `EpisodeTimer`가 삭제됨**

**수정 방향**
- `Clear()` 호출 시 세대 번호를 올리고, `Tick()`은 콜백 후 세대가 바뀌었으면 즉시 중단
- 인덱스(`RemoveAt(i)`) 대신 객체 참조(`Remove(timer)`)로 제거

```csharp
private int generation;

public void Tick(float dt)
{
    int gen = generation;
    for (int i = timers.Count - 1; i >= 0; i--)
    {
        var timer = timers[i];
        bool done = timer.Tick(dt);
        if (gen != generation) return; // 콜백 안에서 Clear() 발생
        if (done) timers.Remove(timer);
    }
}

public void Clear() { timers.Clear(); generation++; }
```

---

## 🗺️ graphic obs 맵 가장자리 값 오염 (Xvfb에서만 재현)

- [x] 수정 (`AllocateAll()`에 Point + Clamp 추가, 커밋 전)
- [x] Linux + Xvfb 빌드에서 Windows 빌드와 결과 일치 확인

**증상**
- graphic obs의 맵 가장자리 픽셀에 semantic ID가 아닌 값이 찍힘
- Windows에서는 재현 안 됨, Linux에서 Xvfb로 렌더링할 때만 발생

**재현 결과** (2026-10-11, anodi_workstation, Ubuntu 24.04 + Xvfb, Renderer: llvmpipe)
- 같은 코드로 만든 Windows/Linux 빌드를 같은 시드(`reset(seed=1)`)로 61스텝 실행하고 원시 ID 맵 비교
- 수정 전: 매 프레임 약 94~103픽셀 불일치
  - 아이템/유닛 픽셀 값이 1 감소 (battery 6→5, enemy_unit 5→4, ally_unit 4→3), 49개 배터리 픽셀 중 33개가 enemy_unit으로 오인됨
  - 상하좌우 이웃 빈칸이 0→1(wall)로 바뀜. 섞이는 방향이 픽셀마다 다름
  - RT가 8비트라 섞인 값도 정수로 반올림됨 → "정수가 아닌 값" 검사로는 잡히지 않음. 정답 프레임과 비교해야 보임
  - 맵 테두리는 전부 벽(1)이라 `Repeat`로 반대편과 섞여도 값이 변하지 않음. 실제 오염은 테두리가 아니라 **아이템/유닛 주변**에서 발생
- 수정 후: 61프레임 전부 Windows와 픽셀 단위 일치 (불일치 0)

**원인** — `SemanticMapRenderer.cs` `Render()`의 `Graphics.Blit`, `AllocateAll()`의 `Texture2D` 생성
- `textureA/B`를 만들 때 `filterMode`, `wrapMode`를 지정하지 않아 기본값 **Bilinear + Repeat**로 생성됨
- `Blit`은 전체 화면 사각형을 그리면서 원본 텍스처를 샘플링함. 샘플 좌표가 텍셀 중심에 정확히 맞으면 보간이 일어나지 않지만, 렌더러에 따라 좌표가 조금만 어긋나도 이웃 텍셀과 섞임
  - Windows(D3D11)는 텍셀 중심에 정확히 맞아서 문제가 드러나지 않음
  - Xvfb 환경은 대개 GPU 없이 Mesa 소프트웨어 OpenGL(llvmpipe)로 그려서 래스터화/좌표 정밀도가 달라짐
- 가장자리는 `Repeat` 때문에 **반대편 끝 텍셀**과 섞임 → 가장자리에서 특히 이상한 값
- 내부에서도 섞이면 다른 ID로 오인될 수 있음 (예: 0과 4 사이 → 2 = ally_storage)

**수정 방향**
- `AllocateAll()`에서 원본 텍스처를 Point + Clamp로 설정 (최소 수정)

```csharp
textureA.filterMode = FilterMode.Point;
textureA.wrapMode = TextureWrapMode.Clamp;
textureB.filterMode = FilterMode.Point;
textureB.wrapMode = TextureWrapMode.Clamp;
RenderTextureTeamA.filterMode = FilterMode.Point;
RenderTextureTeamB.filterMode = FilterMode.Point;
```

- 근본 해결: GPU 왕복 자체를 없애고 `pixelsA/B`를 센서에 직접 전달 (헤드리스 빌드용 Custom ISensor 리팩토링과 같은 작업, 스텝당 약 4.3ms 절감 효과도 있음)
