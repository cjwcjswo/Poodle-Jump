# 발판 타입 확률 (Dynamic Type Probability) 가이드

`PlatformSpawner`의 **Dynamic Type Probability**는 플레이어 높이에 따라 각 발판 타입이 나올 **가중치(Weight)**를 보간합니다.  
시작 높이에서는 `startWeight`, `difficultyMaxHeight` 도달 시에는 `maxWeight`로 Lerp되며, **가중치 합 대비 비율**로 타입이 결정됩니다.

---

## 인스펙터 설정 위치

- **PlatformSpawner** 컴포넌트 → **Dynamic Type Probability** → `Type Probabilities` 리스트
- 리스트에 **Platform Probability** 요소를 추가하고, 각각 `Type`, `Start Weight`, `Max Weight`를 설정합니다.

---

## "처음엔 쉽고 나중엔 지옥" 난이도 예시

처음에는 **Normal 위주**, 높이가 올라갈수록 **OneTime / Broken / Moving**이 늘고 **Normal**이 줄어들게 하려면 아래처럼 설정할 수 있습니다.

| Type     | Start Weight | Max Weight | 설명 |
|----------|--------------|------------|------|
| Normal   | **80**       | **20**     | 시작 시 주력, 후반에 감소 |
| OneTime  | 5            | 25         | 후반에 많이 등장 |
| Broken   | 2            | 20         | 후반에 함정 증가 |
| Moving   | 8            | 25         | 후반에 무빙 발판 증가 |
| HighJump | 5            | 10         | 보상용, 적당히 유지 |

- **시작 구간 (y ≈ 0)**  
  합 = 80+5+2+8+5 = 100  
  → Normal 80%, OneTime 5%, Broken 2%, Moving 8%, HighJump 5%

- **최고 난이도 (y ≥ difficultyMaxHeight)**  
  합 = 20+25+20+25+10 = 100  
  → Normal 20%, OneTime 25%, Broken 20%, Moving 25%, HighJump 10%

이렇게 하면 초반은 안정적이고, 높이에 따라 점점 특수/함정 발판 비율이 늘어나는 **“처음엔 쉽고 나중엔 지옥”** 곡선이 됩니다.

---

## 가중치 설정 시 유의사항

1. **합이 0이면 안 됨**  
   현재 높이에서 모든 타입의 가중치가 0이면 `RollPlatformType()`이 `Normal`을 반환합니다. 최소 한 타입은 양수로 두세요.

2. **비율만 중요**  
   (10, 10, 80)이든 (1, 1, 8)이든 **비율**이 같으면 등장 확률은 동일합니다. 다만 0이면 해당 타입은 절대 나오지 않습니다.

3. **같은 Type 중복**  
   리스트에 같은 `PlatformType`을 여러 번 넣으면, 그 타입의 가중치가 합쳐져서 해당 타입이 더 자주 나옵니다. 보통은 타입당 하나의 행만 두는 것을 권장합니다.

4. **difficultyMaxHeight**  
   `PlatformSpawner`의 **Difficulty** 섹션에서 `difficultyMaxHeight`(기본 500)를 조절하면, 그 높이를 기준으로 `maxWeight` 비율에 도달합니다. 값을 키우면 난이도 상승이 더 완만해집니다.

---

## 인스펙터에서 리스트 채우기 방법

1. **PlatformSpawner** 선택 → **Type Probabilities** 리스트 크기를 5로 설정
2. 각 요소마다:
   - **Type**: Normal / OneTime / Broken / Moving / HighJump 중 선택
   - **Start Weight**: 시작 높이에서의 가중치 (0 이상)
   - **Max Weight**: `difficultyMaxHeight` 도달 시 가중치 (0 이상)
3. 위 표 값을 그대로 넣으면 "처음엔 쉽고 나중엔 지옥" 예시를 바로 사용할 수 있습니다.
