# 리워드 광고 부활 UI 연동 가이드

유저가 추락 후 리워드 광고를 시청해 부활할 수 있도록 UI를 연결하는 방법입니다.

---

## 0. 유니티 세팅 확인 (필수)

- **씬에 GameManager가 하나만 있는지 확인**  
  GameManager가 2개 이상이면 이벤트·상태가 꼬일 수 있습니다. 실행 시 콘솔에 `[GameManager] 씬에 GameManager가 N개 있습니다` 경고가 뜨면 중복 오브젝트를 제거하세요.
- **부활 버튼의 Inspector > Button > On Click ()**  
  **오직 `UIManager.OnReviveButtonClicked`만** 연결되어 있어야 합니다.  
  `GameManager.Revive`가 직접 연결되어 있으면 제거하세요. 부활은 반드시 광고 시청 완료 후 UIManager를 통해 호출되어야 합니다.

---

## 1. 게임 오버 패널에 버튼 구성

- **재시작 / 포기** 버튼: 기존처럼 사용. 클릭 시 `GameManager.NotifyGiveUp()` → `GameManager.RestartGame()` 호출 (UIManager에서 이미 연결 가능).
- **광고 보고 부활** 버튼: 리워드 광고를 보여주고, 시청 완료 시 `GameManager.Revive()` 호출.

---

## 2. 부활 버튼 연결 방법

### 2-1. AdManager / GameManager 참조

- 부활 버튼을 처리할 스크립트(또는 UIManager)에서 `AdManager`, `GameManager`를 SerializeField로 두거나 `FindFirstObjectByType`으로 찾습니다.

### 2-2. 클릭 시 리워드 광고 표시

```csharp
// 부활 버튼 클릭 시
public void OnReviveByAdClicked()
{
    if (_adManager == null || _gameManager == null) return;
    _adManager.ShowRewardedAd(() =>
    {
        _gameManager.Revive();
        // 게임 오버 패널 숨기기, 점수 텍스트 다시 표시 등
    });
}
```

- `ShowRewardedAd(Action onRewardSuccess)`의 인자(보상 콜백)는 **광고가 완전히 닫힌 후(OnAdFullScreenContentClosed)** 에 한 번만 실행됩니다. 광고 도중 게임이 풀리는 현상을 방지하기 위한 보상 대기열 방식입니다.
- 그 콜백 안에서 `GameManager.Revive()`를 호출하면 플레이어가 부활하고, 게임 오버 패널을 닫고 인게임 UI를 다시 켜면 됩니다.

### 2-3. 광고 미로드 시 버튼 비활성화

- 리워드 광고가 아직 로드되지 않았으면 버튼을 비활성화해 클릭을 막습니다.

```csharp
void Update()
{
    if (reviveButton != null && _adManager != null)
        reviveButton.interactable = _adManager.IsRewardedAdReady;
}
```

- 또는 게임 오버 패널을 열 때 한 번만 체크해도 됩니다.

```csharp
void HandleGameOver()
{
    // ...
    if (reviveButton != null && _adManager != null)
        reviveButton.interactable = _adManager.IsRewardedAdReady;
    if (!_adManager.IsRewardedAdReady)
        Debug.Log("[UI] Rewarded ad not ready. Revive button disabled.");
}
```

---

## 3. 랭킹 제출(중복 방지) 정리

- **RankingSubmitter**는 `OnGameOver`가 아니라 **`OnFinalGameOver`**만 구독합니다.
- **OnFinalGameOver**는 다음 경우에만 발생합니다.
  - 유저가 **포기/재시작** 버튼을 눌러 `GameManager.NotifyGiveUp()`가 호출된 경우
  - 이미 **한 번 부활한 뒤** 다시 추락해 게임 오버된 경우
- 따라서 **부활한 뒤 계속 플레이하다가 다시 죽을 때**에만 점수가 제출되며, 첫 번째 사망 후 부활한 경우에는 제출되지 않습니다.
- 재시작 버튼에는 **NotifyGiveUp()**을 먼저 호출한 뒤 **RestartGame()**을 호출해, 포기 시에만 최종 게임 오버로 간주하고 점수가 제출되도록 합니다. (UIManager 예시에서 `OnRestartClicked`로 처리 가능.)

---

## 4. 부활 후 UI 처리

- `Revive()` 호출 후 게임 상태는 다시 `Playing`이 됩니다.
- 게임 오버 패널을 비활성화하고, 인게임 점수 텍스트 등을 다시 켜주세요.

```csharp
_adManager.ShowRewardedAd(() =>
{
    _gameManager.Revive();
    if (gameOverPanel != null) gameOverPanel.SetActive(false);
    if (scoreText != null) scoreText.gameObject.SetActive(true);
});
```

---

## 5. 예외 처리 요약

| 상황 | 권장 처리 |
|------|-----------|
| 리워드 광고 미로드 | `IsRewardedAdReady == false`일 때 부활 버튼 `interactable = false` 또는 로그 후 무시 |
| 광고 시청 중도 이탈 | `onRewardSuccess`는 호출되지 않음. 별도 처리 불필요. |
| Revive() 호출 후 | 게임 오버 패널 숨기기, 인게임 UI 복구 |

---

## 6. 부활 시 카메라 즉시 스냅

부활 시 플레이어가 최고 도달 높이로 순간이동하므로, 카메라도 그 높이로 즉시 맞춰 주는 것이 좋습니다.

- **GameManager**에 `OnPlayerTeleported` 이벤트가 있습니다. `Revive()` 시점에 부활 위치(`Vector3`)를 인자로 한 번 호출됩니다.
- 플레이어를 따라가는 카메라 스크립트에서 이 이벤트를 구독해, 인자로 받은 위치의 **Y**로 카메라(또는 follow 타깃) Y를 **즉시 스냅**하면 됩니다.

**예: CameraYTracker 사용 시**  
프로젝트의 `CameraYTracker.cs`는 이미 `GameManager.OnPlayerTeleported`를 구독하고, 부활 시 `_maxReachedY`와 카메라 위치를 해당 Y로 한 번에 갱신합니다. 별도 연동 없이 동작합니다.

**다른 카메라 스크립트에서 직접 구독하는 예:**

```csharp
void Start()
{
    var gm = FindFirstObjectByType<GameManager>();
    if (gm != null) gm.OnPlayerTeleported += (Vector3 pos) =>
    {
        // 카메라(또는 follow 타깃)의 Y를 pos.y로 즉시 설정
        transform.position = new Vector3(transform.position.x, pos.y, transform.position.z);
    };
}
```

이 가이드대로 연결하면 리워드 시청 완료 시에만 부활하고, 랭킹은 최종 사망(또는 포기) 시에만 한 번 제출됩니다.
