# 딜루전 뉴스 탭 Unity UGUI

확정된 뉴스 탭 시안을 Unity에서 실제 데이터로 운용할 수 있도록 재구성한 패키지다. 화면 캡처 한 장을 통째로 띄우는 방식이 아니다. 시대 카드의 분위기 일러스트만 이미지이고, 시대명·진행도·산업 태그·뉴스·등락 종목·분기 정보는 모두 TextMeshPro와 런타임 데이터로 생성된다.

## Unity에 넣기

1. ZIP의 `Assets/Delusion/UI/News` 폴더를 Unity 프로젝트의 `Assets/Delusion/UI` 아래에 복사한다.
2. TextMeshPro가 없다면 Unity Package Manager에서 설치하고 `TMP Essential Resources`를 가져온다.
3. `Fonts/NotoSansKR-400.ttf`, `NotoSansKR-700.ttf`를 각각 Dynamic TMP Font Asset으로 만든다.
4. 메뉴 `Tools → Delusion → Create News Screen Prefab`을 누른다.
5. 생성된 `Assets/Delusion/UI/News/Prefabs/DelusionNewsCanvas.prefab`을 연다.
6. `DelusionNewsScreen` 컴포넌트의 `Korean Regular`, `Korean Bold`에 만든 TMP Font Asset을 연결하고 Inspector의 `확정 뉴스 탭 UI 생성 / 갱신`을 누른다.

Canvas Scaler는 생성 메뉴가 자동으로 아래 값으로 맞춘다.

- UI Scale Mode: `Scale With Screen Size`
- Reference Resolution: `1920 × 1080`
- Screen Match Mode: `Match Width Or Height`
- Match: `0.5`

## 기존 집무실 배경 연결

패키지에는 화면이 보이도록 어두운 기본 배경만 들어 있다. 홈·시장 탭에서 쓰는 실제 집무실 배경 Sprite를 `Shared Office Background`에 연결하면 뉴스 탭도 같은 공간으로 보인다.

- 공통 배경을 Canvas 아래에서 따로 관리한다면 `Draw Bundled Dark Background`를 끈다.
- 뉴스 탭에는 리나를 생성하는 코드가 없다.
- 시대 카드 외의 카드에는 일러스트가 없다.

## 데이터 연결

`DelusionNewsScreen.Data` 또는 아래 메서드를 사용한다.

```csharp
newsScreen.SetData(todayData);
newsScreen.SetStatus("마법의 시대", 5000, 42);
newsScreen.LoadJson(newsJsonTextAsset);
newsScreen.ShowQuietDay();
```

`DelusionNewsScreenData`의 주요 필드:

- `currentEra`, `currentGold`, `currentDay`: 상단 상태 바
- `eraName`, `eraDay`, `eraEstimatedDays`, `eraSummary`: 시대 카드
- `eraBackgroundResource`: `Resources/DelusionNewsUI/Era`의 배경 이름
- `industryTrends`: 상승/하락 산업 태그
- `todayNews`: 오늘의 뉴스. 0건이면 조용한 날 문구, 3건 이상이면 카드 내부 스크롤
- `gainers`, `losers`: 각 최대 3개 표시
- `daysToQuarter`, `quarterLength`: 분기 숫자와 게이지

샘플 JSON은 `Data/sample-news-data.json`에 있다.

## 종목 상세 연결

뉴스 행이나 급상승·급하락 행을 누르면 `On Company Clicked(string companyName)`이 실행된다. 기존 종목 상세 창을 이 이벤트에 연결하면 된다.

좌측 탭은 다음 이벤트로 연결한다.

- `On Home Requested`
- `On Market Requested`
- `On Wallet Requested`

뉴스 아이콘은 현재 탭이라 별도 클릭 이벤트가 없다.

## 회사 로고

V5 회사 엠블럼 40개가 `Resources/DelusionNewsUI/Logos`에 전부 들어 있다. 데이터의 `logoResource`에는 확장자를 뺀 파일명을 넣는다.

예시:

- 윈드시어 매직스: `17_windshire-magics`
- 블레이즈마인 광산: `01_blazemine-mining`
- 아이언하트 공업: `09_ironheart-industries`
- 코타 상업은행: `08_kota-commercial-bank`
- 에이더니얼 자산관리: `07_aethernial-asset-management`
- 드워프 트레일 수송: `28_dwarf-trail-transport`

직접 Sprite를 연결하려면 각 데이터의 `logoOverride`를 사용한다.

## 시대 배경 교체

현재 제공되는 배경은 `Era/magic_era_background.png`다. 다른 시대는 같은 크기와 어두운 톤으로 배경 PNG를 추가하고 `eraBackgroundResource`에 파일명을 넣으면 된다.

- 기준 크기: `1504 × 196`
- 인물·얼굴·군중 금지
- 글자와 버튼을 배경 이미지에 포함하지 않음
- 마탑·마법진·성벽처럼 풍경과 상징만 사용

## 포함 파일

- `Runtime/DelusionNewsData.cs`: 시대·뉴스·등락·분기 데이터 모델
- `Runtime/DelusionNewsScreen.cs`: 전체 화면 생성과 런타임 갱신
- `Editor/DelusionNewsScreenEditor.cs`: Inspector 버튼과 프리팹 생성 메뉴
- `Resources/DelusionNewsUI`: 9-Slice UI, 공통 아이콘, 시대 배경, V5 로고 40개
- `SourceSVG`: 아이콘과 기본 UI 조각의 편집용 벡터 원본
- `Layout/news-layout-1920x1080.json`: 정확한 좌표와 색상
- `Reference/news-screen-approved-1920x1080.png`: 확정 시안
- `Data/sample-news-data.json`: 샘플 런타임 데이터

## 디자인 고정 규칙

- 좌측 아이콘은 홈 화면 기준 모양·간격을 사용한다.
- 뉴스 탭에서는 세 번째 선그래프만 금색이며 짧은 금색 세로선이 함께 표시된다.
- 청록색 선택 띠는 사용하지 않는다.
- 상승은 빨강, 하락은 파랑, 보합은 회색이다.
- 금색 장식 프레임과 픽셀 아트를 사용하지 않는다.
- 일러스트는 시대 카드에만 들어간다.
