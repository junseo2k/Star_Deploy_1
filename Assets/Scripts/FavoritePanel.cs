using UnityEngine;

// 관심 종목(즐겨찾기 주식) 표시를 관리
// - 홈 화면(EntirePanel) 하단: 3개 고정
// - 전체 보기 화면: 전체 표시(가로 스크롤)
public class FavoritePanel : MonoBehaviour
{
    [Header("홈 화면 (3개 고정)")]
    public Transform homeContainer;     // 홈 화면 내 카드 컨테이너
    public int homeDisplayCount = 3;    // 홈 최대 표시 개수

    [Header("전체 보기 (스크롤)")]
    public Transform fullContainer;     // 전체 보기 컨테이너 (ScrollView Content)

    [Header("공용")]
    public GameObject cardPrefab;       // 관심 종목 카드 프리팹

    // 홈 화면 관심 종목 갱신 — 별표 토글·하루 넘길 때·홈 열릴 때 호출
    public void RefreshHome()
    {
        RefreshContainer(homeContainer, homeDisplayCount);
    }

    // 전체 보기 갱신 — 전체 보기 패널 열릴 때 호출
    public void RefreshFull()
    {
        RefreshContainer(fullContainer, int.MaxValue);
    }

    // 공용 갱신 로직: 즐겨찾기 주식을 순서대로 카드 인스턴스화
    void RefreshContainer(Transform container, int maxCount)
    {
        if (container == null || cardPrefab == null) return;

        // 기존 카드 전부 제거
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // 즐겨찾기된 주식만 카드로 생성
        int count = 0;
        foreach (Stock stock in GameManager.Instance.tabmanager.allstock)
        {
            if (!stock.isFavorite) continue;
            if (count >= maxCount) break;

            GameObject card = Instantiate(cardPrefab, container);
            FavoriteCard fc = card.GetComponent<FavoriteCard>();
            if (fc != null) fc.Setup(stock);
            count++;
        }
    }

    // "전체 보기" 버튼 클릭 시 호출
    public void OnFullViewClick()
    {
        UIManager.instance.OpenFavoriteFullPanel();
    }

    // 전체 보기 화면 닫기 버튼 클릭 시 호출
    public void OnCloseFullClick()
    {
        UIManager.instance.OpenScreen(UIManager.instance.EntirePanel);
    }
}