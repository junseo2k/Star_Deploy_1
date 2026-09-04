# 딜루전 자산(금고) UI — Unity UGUI

확정된 6차 자산 탭을 실제 데이터로 갱신할 수 있게 UGUI로 재구성한 패키지입니다. 완성 화면을 통이미지로 띄우는 방식이 아닙니다.

## 설치

1. 이 패키지의 `Assets/Delusion/UI/Vault` 폴더를 Unity 프로젝트의 같은 경로로 복사합니다.
2. Package Manager에서 `TextMeshPro`와 `Unity UI (UGUI)`를 사용할 수 있는지 확인합니다.
3. `Fonts/NotoSansKR-400.ttf`, `Fonts/NotoSansKR-700.ttf`를 각각 선택하고 `Create > TextMeshPro > Font Asset`으로 Dynamic Font Asset을 만듭니다.
4. Unity 메뉴에서 `Tools > Delusion > Create Vault Screen Prefab`을 실행합니다.
5. 생성된 `DelusionVaultScreen` 컴포넌트에 두 TMP Font Asset을 연결하고 `확정 자산 탭 UI 생성 / 갱신`을 누릅니다.
6. 생성 결과는 `Assets/Delusion/UI/Vault/Prefabs/DelusionVaultCanvas.prefab`에 저장됩니다.

기존 홈·시장·뉴스 화면이 쓰는 집무실 배경이 있다면 `Shared Office Background`에 연결하세요. 연결하지 않으면 동봉된 어두운 대체 배경이 사용됩니다.

## 확정 레이아웃

- 상단 자산 요약은 높이 100px의 얇은 한 줄입니다.
- 보유 종목은 평가액이 큰 순으로 자동 정렬됩니다.
- 130px 높이의 큰 종목 행 4개가 한 화면에 보입니다.
- 5번째 종목부터 세로 `ScrollRect`로 내려갑니다.
- 행 왼쪽은 로고·회사명·수량·평단·현재가, 오른쪽은 평가액·손익률입니다.
- 손익은 빨강/파랑 글자만 사용합니다. 막대, 알약 배경, 미니 차트는 없습니다.
- 대주주 카드는 정적인 짙은 청록 배경, 금색 한 줄 테두리와 왕관만 사용합니다.

정확한 좌표와 색은 `Layout/vault-layout-1920x1080.json`, 동작 확인용 값은 `Data/sample-vault-data.json`을 참고하세요.

## 런타임 연결

`DelusionVaultScreen`의 공개 API:

- `SetStatus(era, gold, day)` — 상단 시대·골드·날짜 갱신
- `SetPortfolio(cash, holdings)` — 현금과 보유 종목 갱신 및 자산 요약 재계산
- `SetMajorShareholders(items)` — 대주주 카드 갱신
- `SetData(data)` — 화면 전체 데이터 교체
- `LoadJson(textAsset)` — `JsonUtility` 형식 JSON 로드
- `RecalculateSummary()` — 현금과 평가액 합계로 요약 재계산
- `OnCompanyClicked` — 보유 종목이나 대주주 카드 클릭 시 회사명 전달

예시:

```csharp
using System.Collections.Generic;
using Delusion.UI.Vault;

public void RefreshVault(DelusionVaultScreen screen)
{
    screen.SetPortfolio(5000, new List<VaultHoldingItem>
    {
        new VaultHoldingItem
        {
            companyName = "윈드시어 매직스",
            logoResource = "17_windshire-magics",
            quantity = 3,
            averagePrice = 820,
            currentPrice = 850
        }
    });
}
```

## 회사 로고

`Resources/DelusionVaultUI/Logos`에 V5 기업 엠블럼 40개가 포함되어 있습니다. `logoResource`에는 확장자를 제외한 파일명을 넣습니다. 전체 연결표는 `Data/company-logo-manifest.csv`에 있습니다.

## 빈 상태

- 보유 종목이 없으면 `아직 보유한 종목이 없습니다`를 표시합니다.
- 대주주 회사가 없으면 밝은 카드로 `아직 대주주인 기업이 없습니다`를 표시합니다.

## 구현 제한 준수

추이 그래프, 도넛, 손익 막대·알약, 파티클, 빛 번짐, 애니메이션은 포함하지 않았습니다. 모든 시각 요소는 Unity에서 정적으로 재현 가능한 PNG, 라인, 텍스트와 UGUI 구성요소입니다.
