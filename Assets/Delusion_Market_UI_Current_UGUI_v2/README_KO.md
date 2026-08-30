# 딜루전 시장 화면 UI — 현재 첨부본 기준

이 패키지는 `ChatGPT Image 2026년 8월 23일 오전 01_29_53.png`만을 최종 기준으로 다시 제작한 Unity UGUI 파일이다. 예전 홈 UI, 구형 시장 패널, 78px 로고 슬롯을 재사용하지 않는다.

## 이번 수정에서 바뀐 점

- 좌측 4아이콘 사이드바와 선택 배경을 현재 화면대로 패키지 안에서 생성
- 상단 `현재 시대 / 현재 골드 / 현재 날짜` 바를 현재 화면대로 생성
- 시장 제목, 11개 업종 필터, 2열×4행 종목 카드, 우측 스크롤바를 모두 포함
- 로고 슬롯을 현재 화면 크기인 `118×124px`로 확대
- 현재 화면에 보이는 최신 로고 8종을 실제 투명 PNG로 포함
- `브루트포지`가 아니라 현재 확정명인 `브루트스틸 철강 / 원 오르쿠스 · 광업` 적용
- 40개 기업 데이터와 필터 동작 포함
- 화면에 없는 32개 로고에는 예전 로고나 임시 문장을 넣지 않음

## Unity 적용

1. ZIP 안의 `Assets/Delusion/UI/MarketCurrent` 폴더를 Unity 프로젝트의 `Assets/Delusion/UI` 안에 복사한다.
2. 시장 Scene의 전체 화면 Canvas 아래에 빈 UI 오브젝트를 만들고 `CurrentMarketScreen` 컴포넌트를 붙인다.
3. Canvas Scaler를 `Scale With Screen Size`, Reference Resolution을 `1920×1080`, Match를 `0.5`로 둔다.
4. `Fonts/NotoSansKR-400.ttf`, `NotoSansKR-700.ttf`로 Dynamic TMP Font Asset을 만든 뒤 Inspector의 `Korean Regular`, `Korean Bold`에 넣는다.
5. Inspector의 `현재 첨부본 시장 UI 생성 / 갱신` 버튼을 누른다.

기존 패키지의 `Assets/Delusion/UI/Market` 폴더는 이 패키지에 포함되지 않는다. 새 화면은 `Assets/Delusion/UI/MarketCurrent`와 `Delusion.UI.MarketCurrent` 네임스페이스만 사용한다.

## 정확한 화면 기준

- 기준 해상도: 1920×1080
- 사이드바: 142×1080
- 상단 상태 바: x=920, y=42, 790×74
- 제목: x=241, y=86
- 필터: x=241, y=165, 높이 51
- 카드 시작점: x=241, y=250
- 카드: 779×165
- 카드 간격: 가로 23, 세로 18
- 로고 슬롯: 카드 내부 x=16, y=18, 118×124
- 우측 스크롤바: x=1847, y=248, 7×722

전체 좌표와 색상은 `Layout/current-market-layout-1920x1080.json`에 정리되어 있다.

## 로고 파일

현재 화면에 보이는 아래 8개는 `Resources/DelusionMarketCurrentUI/Logos`에 들어 있다.

1. 윈드시어 매직스
2. 드래곤 금고
3. 아이언하트 공업
4. 블레이즈마인 광산
5. 아우렐리온 캐피탈
6. 브루트스틸 철강
7. 에이더니얼 자산관리
8. 코타 상업은행

스크롤 아래의 32개 기업은 `CurrentMarketScreen.cs`에 각 V5 로고 파일명이 연결되어 있다. 같은 이름의 Sprite를 `Logos` 폴더에 넣으면 자동으로 표시된다. 파일이 없을 때는 예전 로고 대신 투명 슬롯을 사용한다.

## 동봉 파일

- `Runtime/CurrentMarketScreen.cs`: 전체 화면 생성, 상태 바, 사이드바, 필터, 40개 기업 데이터
- `Runtime/CurrentMarketCardView.cs`: 카드 Hover와 클릭
- `Runtime/CurrentMarketFilterButton.cs`: 업종 필터
- `Runtime/CurrentMarketSparkline.cs`: 격자와 미니 차트
- `Editor/CurrentMarketScreenEditor.cs`: Inspector 생성 버튼
- `Resources/DelusionMarketCurrentUI`: 현재 화면 전용 PNG와 9-Slice 메타
- `Reference`: 사용자가 지정한 원본 및 1920×1080 기준 이미지
- `Layout`: 정확한 좌표·색상 JSON
- `Fonts`: Noto Sans KR TTF 및 라이선스
- `Preview`: 원본 화면과 에셋 비교판

## 데이터 연결

- `SetCompanies()`로 실제 40종목 데이터를 넣을 수 있다.
- `SetStatus()`로 시대, 골드, 날짜를 갱신할 수 있다.
- 카드 클릭 시 `OnCompanyClicked(string companyName)` 이벤트가 실행된다.
- `logoOverride`에 Sprite를 직접 넣으면 Resources 파일보다 우선한다.
