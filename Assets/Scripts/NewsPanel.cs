using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 뉴스 패널 UI를 관리하는 클래스
// 대형 뉴스(BigNewsPanel)와 진행 중인 소형 이슈(IssuePanel) 표시를 담당
public class NewsPanel : MonoBehaviour
{
    public Transform BigNewsPanel;        // 대형 뉴스(BigRandom/BigTrigger) 표시 패널
    public Transform IssuePanel;          // 진행 중인 소형 이슈 목록 표시 패널
    public GameObject StockWithImagePrefab; // 이미지+텍스트 조합 UI 프리팹 (뉴스 항목 생성에 사용)
    public Transform EntirePanel;         // 전체 주식 목록 패널 (뉴스 패널 닫을 때 복귀 대상)
    public Transform newspanel;           // 이 뉴스 패널 루트 오브젝트 (닫기 버튼 클릭 시 비활성화)

    void Start()
    {

    }

    void Update()
    {

    }

    // 이슈 탭 버튼 클릭 시 호출 — 현재 진행 중인 소형 뉴스 목록을 IssuePanel에 표시
    // 활성 사건이 4개 미만이면 전체 표시, 4개 이상이면 최근 4개만 표시
    public void OnIssueClick()
    {
        foreach (Transform child in IssuePanel) // 이전에 생성된 이슈 항목 제거
        {
            Destroy(child.gameObject);
        }
        IssuePanel.gameObject.SetActive(true);
        int count = GameManager.Instance.activeNewsList.Count;
        if (count < 4) // 활성 사건이 4개 미만이면 전체 표시
        {
            foreach(ActiveCase active in GameManager.Instance.activeNewsList)
            {
                // 소형 뉴스 타입만 이슈 패널에 표시 (대형 뉴스는 BigNewsPanel에서 표시)
                if (active.caseData.newsType == Case.NewsType.SmallIndustryNews
                    || active.caseData.newsType == Case.NewsType.SmallCompanyNews
                    || active.caseData.newsType == Case.NewsType.SmallVibeNews)
                {
                    GameObject ActiveIssue = Instantiate(StockWithImagePrefab, IssuePanel);
                    Image Icon = ActiveIssue.GetComponentInChildren<Image>();
                    Icon.sprite = active.caseData.CaseImage;
                    TextMeshProUGUI IssueText = ActiveIssue.GetComponentInChildren<TextMeshProUGUI>();
                    IssueText.text = $"{active.displayDescription} 남은 시간 : {active.Rday}";
                }
            }
        }
        else // 활성 사건이 4개 이상이면 최근 4개만 표시
        {
           List<ActiveCase> recentTwo = GameManager.Instance.activeNewsList.GetRange(count - 4, 4);
           foreach (ActiveCase c in recentTwo)
            {
                if (c.caseData.newsType == Case.NewsType.SmallIndustryNews
                    || c.caseData.newsType == Case.NewsType.SmallCompanyNews
                    || c.caseData.newsType == Case.NewsType.SmallVibeNews)
                {
                    GameObject RecentIssue = Instantiate(StockWithImagePrefab, IssuePanel);
                    Image Icon = RecentIssue.GetComponentInChildren<Image>();
                    Icon.sprite = c.caseData.CaseImage;
                    TextMeshProUGUI IssueText = RecentIssue.GetComponentInChildren<TextMeshProUGUI>();
                    IssueText.text = $"{c.displayDescription} 남은 시간 : {c.Rday}";
                }
            }
        }
    }

    // 닫기 버튼 클릭 시 뉴스 패널 닫고 전체 주식 목록 패널로 복귀
    // IssuePanel의 자식 오브젝트를 정리하여 다음에 열 때 중복 표시 방지
    public void OnCancelClick()
    {
        newspanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
        foreach (Transform child in IssuePanel) // 기존 이슈 항목 정리
        {
            Destroy(child.gameObject);
        }
    }

    // 대형 뉴스 패널 갱신 — 가장 최근에 추가된 활성 사건 중 대형 뉴스(BigRandom/BigTrigger)를 표시
    // UIManager.OpenNewsPanel()에서 뉴스 패널 열릴 때 자동 호출
    public void BigNewsClick()
    {
        foreach (Transform child in BigNewsPanel) // 이전에 생성된 대형 뉴스 항목 제거
        {
            Destroy(child.gameObject);
        }
        BigNewsPanel.gameObject.SetActive(true);
        int count = GameManager.Instance.activeNewsList.Count;
        if (count == 0) return; // 활성 사건이 없으면 표시 없이 종료
        List<ActiveCase> recentOne = GameManager.Instance.activeNewsList.GetRange(count - 1, 1); // 가장 최근 사건 1개
        foreach(ActiveCase c in recentOne)
            {
                // 대형 뉴스 타입만 BigNewsPanel에 표시
                if (c.caseData.newsType == Case.NewsType.BigRandomNews
                    || c.caseData.newsType == Case.NewsType.BigTriggerNews)
                {
                    GameObject RecentIssue = Instantiate(StockWithImagePrefab, BigNewsPanel);
                    Image Icon = RecentIssue.GetComponentInChildren<Image>();
                    Icon.sprite = c.caseData.CaseImage;
                    TextMeshProUGUI IssueText = RecentIssue.GetComponentInChildren<TextMeshProUGUI>();
                    IssueText.text = c.displayDescription;
                }
            }
    }

}
