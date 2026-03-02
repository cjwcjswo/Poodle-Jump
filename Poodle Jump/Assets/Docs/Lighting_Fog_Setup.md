# Unity Lighting / Fog 설정 가이드

`EnvironmentManager`가 `RenderSettings.fogColor`를 테마별로 덮어쓰므로, **안개(Fog)를 켜두어야** 색상 변화가 보입니다. 아래 순서대로 설정하면 됩니다.

---

## 1. Fog 켜기 (방법 A – Lighting 창)

1. **Window → Rendering → Lighting** (또는 **Window → Lighting**) 으로 Lighting 창을 연다.
2. **Environment** 탭을 선택한다.
3. **Other Settings** 섹션을 펼친다.
4. **Fog** 체크박스를 켠다.
5. **Fog Color**는 `EnvironmentManager`가 매 프레임 덮어쓰므로, 초기값은 아무 색이나 둬도 된다.

---

## 2. Fog 켜기 (방법 B – Render Settings)

1. **Edit → Project Settings** 가 아니라, **씬(Scene)이 열린 상태**에서 **Hierarchy**에서 아무 오브젝트도 선택하지 않는다.
2. **Inspector** 맨 위에 **Scene** 설정이 보이면, 그 안의 **Lighting** / **Environment** 관련에서 **Fog**를 찾아 켠다.

또는:

1. 메뉴에서 **Edit → Render Settings** 를 연다.  
   (Unity 버전에 따라 **Window → General → Render Settings** 등으로 있을 수 있다.)
2. **Fog** 체크박스를 켠다.
3. **Fog Color**는 스크립트가 테마에 따라 바꾸므로 기본값은 무관하다.

---

## 3. Fog 모드 / 거리 설정 (선택)

- **Fog Mode**
  - **Linear**: 거리로 안개가 선형 증가. **Start / End** 로 구간 지정.
  - **Exponential (Exp)** / **Exponential Squared (Exp2)**: **Density** 로 농도 조절.
- 엔드리스 점프처럼 **원근감**을 주려면 **Linear** + **Fog End** 를 50~100 정도로 두고 조절하는 것을 권장한다.

---

## 4. EnvironmentManager와 함께 쓸 때

- **Fog**만 켜두면, `EnvironmentManager`가 `RenderSettings.fogColor`를 고도에 따라 `Color.Lerp`로 부드럽게 바꾼다.
- **Fog Mode / Start / End / Density** 는 스크립트가 건드리지 않으므로, Lighting(또는 Render Settings)에서 원하는 대로 설정하면 된다.
- 카메라 **Clear Flags**를 **Skybox**로 두면 `EnvironmentManager`가 적용하는 **Ambient Light** 및(선택) **Skybox Tint**와 함께 배경이 테마에 맞게 변한다.

---

## 5. 요약

| 항목 | 설정 위치 | 권장 |
|------|-----------|------|
| Fog 켜기 | Lighting(Environment) 또는 Render Settings | **Fog** 체크 ON |
| Fog Color | 같은 화면 (초기값만) | EnvironmentManager가 덮어씀 |
| Fog Mode | 같은 화면 | Linear 또는 Exp2 |
| Fog Start/End (Linear) | 같은 화면 | Start 10, End 60~100 등으로 테스트 |

이렇게 설정한 뒤 플레이하면, 고도에 따라 `EnvironmentManager`의 테마 색상이 안개에 반영된다.
