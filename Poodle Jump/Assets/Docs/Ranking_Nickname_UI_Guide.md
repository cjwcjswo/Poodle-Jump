# 닉네임 수정 UI 연동 가이드

유저가 직접 닉네임을 수정할 수 있는 UI 창을 만들 때 사용할 API와 흐름입니다.

---

## 1. 호출할 메서드

닉네임을 **저장**할 때는 다음 정적 메서드를 사용합니다.

```csharp
RankingSubmitter.SaveNickname(newNickname);
```

- **매개변수**: 유저가 입력한 새 닉네임 문자열 (공백만 있으면 무시됨)
- **동작**: `PlayerPrefs`의 `SavedNickname` 키에 저장하고, 다음 게임 오버부터 이 닉네임으로 서버에 점수가 제출됩니다.
- **사용 시점**: 확인/저장 버튼 클릭, 입력 필드 적용 시 등

---

## 2. 현재 닉네임 표시용

UI에 **현재 저장된 닉네임**을 채울 때는 다음 메서드를 사용합니다.

```csharp
string current = RankingSubmitter.GetSavedNickname();
```

- 저장된 값이 없으면 빈 문자열 `""`을 반환합니다.
- 처음 실행 시에는 아직 저장된 닉네임이 없을 수 있으므로, 빈 경우 `GetOrCreateNickname()`으로 Guest_XXXX를 생성·표시할 수 있습니다.

```csharp
// 예: 설정 창을 열 때 입력 필드에 넣을 값
string displayName = RankingSubmitter.GetSavedNickname();
if (string.IsNullOrEmpty(displayName))
    displayName = RankingSubmitter.GetOrCreateNickname();
nicknameInputField.text = displayName;
```

---

## 3. UI 연동 예시 흐름

1. **설정/프로필 화면**에서 닉네임 입력 필드(InputField/TMP_InputField)를 두고, 화면을 열 때 `GetSavedNickname()` 또는 `GetOrCreateNickname()`으로 초기값을 넣습니다.
2. 유저가 수정 후 **저장/확인** 버튼을 누르면:
   - 입력값 유효성 검사(길이, 금지 문자 등) 후
   - `RankingSubmitter.SaveNickname(입력값)` 호출
3. 이후 게임 플레이 → 게임 오버 시 `RankingSubmitter`가 자동으로 이 저장된 닉네임으로 SubmitScore API를 호출합니다.

---

## 4. 타이틀 씬 연동 (TitleSceneController)

- **닉네임 표시**: `RankingSubmitter.GetOrCreateNickname()`으로 유저 이름을 가져와 표시합니다.
- **랭킹 보드**: 서버 `GetTopRankings` API로 상위 10명을 가져와 UI에 채웁니다. `ScoreMarkerManager`의 API 설정(apiBaseUrl, apiKey 등)을 그대로 사용하거나, TitleSceneController 인스펙터에서 동일하게 입력합니다.
- **내 랭킹**: 현재 서버에는 "닉네임으로 순위/점수 조회" 전용 API가 없습니다. 타이틀에서는 **상위 10명 리스트 안에 내 닉네임이 있으면** 그 순위와 점수를 표시하고, 없으면 "상위 10위 밖"으로 표시합니다.
- **내 순위 전용 API를 쓰고 싶다면**: 서버에 예를 들어 `GET /api/Ranking/GetMyRank?nickname=xxx` 같은 엔드포인트를 추가해, 해당 닉네임의 순위와 점수를 반환하도록 구현한 뒤, TitleSceneController에서 이 URL을 호출해 `myRankText`를 채우면 됩니다.

---

## 5. 참고

- `RankingSubmitter`는 **GameManager.OnGameOver**를 구독하고 있어, 게임 오버 시점에만 서버로 점수를 보냅니다.
- 닉네임은 **PlayerPrefs**에만 저장되므로, 기기/프로필이 바뀌면 다시 설정해야 합니다. 나중에 계정 시스템을 붙이면 같은 메서드를 호출하되, 서버/로컬 계정에 맞게 저장 위치만 바꾸면 됩니다.
