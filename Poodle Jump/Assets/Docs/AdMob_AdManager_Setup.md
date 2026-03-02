# AdManager 유니티 에디터 세팅 가이드

AdMob 전면 광고를 사용하려면 **Google Mobile Ads Unity 플러그인** 설치 후, 씬에 `AdManager`를 올리고 아래처럼 설정합니다.

---

## 1. 플러그인 설치

1. **Window → Package Manager**에서 **Add package by name** 선택.
2. 패키지명: `com.google.ads.mobile` (또는 구글 공식 문서의 Unity 패키지 주소 사용).
3. 또는 [Google Mobile Ads Unity Plugin](https://developers.google.com/admob/unity/quick-start)에서 Unity 패키지(.unitypackage)를 받아 **Assets → Import Package**로 임포트.

설치 후 프로젝트에 `GoogleMobileAds.Api` 네임스페이스가 있어야 `AdManager` 스크립트가 컴파일됩니다.

---

## 2. AdManager 오브젝트 세팅

### 2-1. 오브젝트 생성

1. **Hierarchy**에서 우클릭 → **Create Empty**.
2. 이름을 `AdManager` 등으로 변경.
3. **Add Component** → `AdManager` 스크립트를 붙입니다.

### 2-2. Inspector 설정

| 항목 | 설명 | 권장 |
|------|------|------|
| **Android Ad Unit Id** | Android 전면 광고 유닛 ID | 테스트: `ca-app-pub-3940256099942544/1033173712` |
| **Ios Ad Unit Id** | iOS 전면 광고 유닛 ID | 테스트: `ca-app-pub-3940256099942544/4411468910` |
| **Ads Interval** | 게임 오버 N회마다 1회 노출 | 기본값 `3` (3판당 1회) |
| **Game Manager** | GameManager 참조 | 비워두면 씬에서 자동 탐색 |

- **Game Manager**가 비어 있으면 런타임에 `FindFirstObjectByType<GameManager>()`로 찾습니다. 같은 씬에 `GameManager`가 있으면 그대로 두어도 됩니다.
- 실제 배포 시에는 [AdMob 콘솔](https://admob.google.com/)에서 만든 **앱 → 광고 단위 → 전면 광고** ID로 위 두 필드를 교체합니다.

### 2-3. 씬 구성

- `AdManager`는 **게임이 로드되는 메인 씬**에 한 번만 두면 됩니다.
- DontDestroyOnLoad를 쓰지 않으므로, 광고가 필요한 씬이 여러 개면 각 씬에 AdManager를 두거나, 부트스트랩 씬에서 한 번만 초기화하도록 구성할 수 있습니다.

---

## 3. 동작 요약

- **Start()**: `MobileAds.Initialize()` 호출 후, 콜백에서 전면 광고 **미리 로드**.
- **OnGameOver** (GameManager): 게임 오버 횟수를 올리고, `adsInterval`마다 `ShowInterstitialAd()` 호출.
- **광고 열림**: `Time.timeScale = 0`, `AudioListener.volume = 0`으로 일시정지·뮤트.
- **광고 닫힘**: `Time.timeScale`·`AudioListener.volume` 복구 후, 다음 전면 광고 **즉시 로드**.
- **로드 실패 / 미준비**: 로그만 남기고 게임은 그대로 진행.

---

## 4. 테스트 시 주의

- 위 ID는 **구글 제공 테스트 ID**이므로, 실제 앱에서 반드시 본인 AdMob 광고 단위 ID로 바꿔야 합니다.
- 테스트 중에는 **테스트 기기**를 AdMob 콘솔에 등록해 두면 정책 위반을 줄일 수 있습니다.
