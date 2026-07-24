using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 뉴스 패널 UI를 관리하는 클래스
// 대형 뉴스(BigNewsPanel)와 진행 중인 소형 이슈(IssuePanel) 표시를 담당
public class NewsPanel : MonoBehaviour
{
    public Transform BigNewsPanel;        // 대형 뉴스(BigTriggerNews 타입) 표시 패널
    public Transform IssueScroll;          // 진행 중인 소형 이슈 목록 표시 패널
    public GameObject StockWithImagePrefab; // 이미지+텍스트 조합 UI 프리팹 (뉴스 항목 생성에 사용)
    public Transform newspanel;           // 이 뉴스 패널 루트 오브젝트 (닫기 버튼 클릭 시 비활성화)

    void Start()
    {

    }

    void Update()
    {

    }

    // 이슈 탭 버튼 클릭 시 호출 — 진행 중인 소형/전조 뉴스를 모두 표시
    public void OnIssueClick()
    {
        foreach (Transform child in IssueScroll)
            Destroy(child.gameObject);
        IssueScroll.gameObject.SetActive(true);

        var smallNews = new List<ActiveCase>();
        foreach (ActiveCase active in GameManager.Instance.activeNewsList)
        {
            if (active.caseData.newsType == Case.NewsType.SmallIndustryNews
                || active.caseData.newsType == Case.NewsType.SmallCompanyNews
                || active.caseData.newsType == Case.NewsType.ForeshadowNews)
                smallNews.Add(active);
        }

        int start = 0;
        for (int i = start; i < smallNews.Count; i++)
        {
            ActiveCase active = smallNews[i];
            GameObject item = Instantiate(StockWithImagePrefab, IssueScroll);
            item.GetComponentInChildren<Image>().sprite = active.caseData.CaseImage;
            item.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{active.displayDescription} 남은 시간 : {active.Rday}";
        }
        Debug.Log($"전체 뉴스 수: {GameManager.Instance.activeNewsList.Count}");

        foreach (var active in GameManager.Instance.activeNewsList)
            {
                Debug.Log($"타입: {active.caseData.newsType}"); 
            }
    }


    // 대형 뉴스 패널 갱신 — 가장 최근에 추가된 BigTriggerNews 타입 뉴스를 표시
    // UIManager.OpenNewsPanel()에서 뉴스 패널 열릴 때 자동 호출
    public void BigNewsClick()
    {
        foreach (Transform child in BigNewsPanel)
            Destroy(child.gameObject);

        BigNewsPanel.gameObject.SetActive(true);

        // 뒤에서부터 탐색해 가장 최근 BigTriggerNews를 찾음
        // (같은 턴에 소형 뉴스가 뒤에 추가되어도 대형 뉴스를 놓치지 않음)
        ActiveCase bigNews = null;
        var list = GameManager.Instance.activeNewsList;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Case.NewsType t = list[i].caseData.newsType;
            if (t == Case.NewsType.BigTriggerNews)
            {
                bigNews = list[i];
                break;
            }
        }

        if (bigNews == null) return;

        GameObject item = Instantiate(StockWithImagePrefab, BigNewsPanel);
        item.GetComponentInChildren<Image>().sprite = bigNews.caseData.CaseImage;
        item.GetComponentInChildren<TextMeshProUGUI>().text = bigNews.displayDescription;
    }

}
