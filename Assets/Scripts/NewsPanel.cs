using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 뉴스 패널 UI를 관리하는 클래스
// 대형 뉴스(BigNewsPanel) 표시를 담당 — 소형 이슈 목록은 새 뉴스 탭 화면이 대신한다
public class NewsPanel : MonoBehaviour
{
    public Transform BigNewsPanel;          // 대형 뉴스(BigTriggerNews 타입) 표시 패널
    public GameObject StockWithImagePrefab; // 이미지+텍스트 조합 UI 프리팹 (뉴스 항목 생성에 사용)

    // 대형 뉴스 패널 갱신 — 가장 최근에 추가된 BigTriggerNews 타입 뉴스를 표시
    // UIManager.OpenNewsPanel()에서 뉴스 패널 열릴 때 자동 호출
    public void BigNewsClick()
    {
        // 패널을 지웠으면 아무것도 하지 않는다 (새 뉴스 탭 화면이 대신 표시한다)
        if (BigNewsPanel == null) return;

        ClearChildren(BigNewsPanel);
        BigNewsPanel.gameObject.SetActive(true);

        // 뒤에서부터 탐색해 가장 최근 BigTriggerNews를 찾음
        // (같은 턴에 소형 뉴스가 뒤에 추가되어도 대형 뉴스를 놓치지 않음)
        var list = GameManager.Instance.activeNewsList;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].caseData.newsType != Case.NewsType.BigTriggerNews) continue;

            CreateNewsItem(BigNewsPanel, list[i].caseData.CaseImage, list[i].displayDescription);
            return;
        }
    }

    // 뉴스 항목 프리팹 하나를 생성하고 이미지·텍스트를 채운다
    void CreateNewsItem(Transform parent, Sprite image, string text)
    {
        GameObject item = Instantiate(StockWithImagePrefab, parent);
        item.GetComponentInChildren<Image>().sprite = image;
        item.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    static void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }
}
