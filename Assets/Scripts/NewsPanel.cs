using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class NewsPanel : MonoBehaviour
{
    public Transform BigNewsPanel;
    public Transform IssuePanel;
    public GameObject StockWithImagePrefab;
    public Transform EntirePanel; 
    public Transform newspanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnIssueClick()
    {
        foreach (Transform child in IssuePanel)
        {
            Destroy(child.gameObject);
        }
        IssuePanel.gameObject.SetActive(true);
        int count = GameManager.Instance.activeNewsList.Count;
        if (count < 4)
        {
            foreach(ActiveCase active in GameManager.Instance.activeNewsList)
            {
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
        else
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
    public void OnCancelClick()
    {
        newspanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
        foreach (Transform child in IssuePanel)
        {
            Destroy(child.gameObject);
        }
    }
    public void BigNewsClick()
    {
        foreach (Transform child in BigNewsPanel)
        {
            Destroy(child.gameObject);
        }
        BigNewsPanel.gameObject.SetActive(true);
        int count = GameManager.Instance.activeNewsList.Count;
        if (count == 0) return;
        List<ActiveCase> recentOne = GameManager.Instance.activeNewsList.GetRange(count - 1, 1);
        foreach(ActiveCase c in recentOne)
            {
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
