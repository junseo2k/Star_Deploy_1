using UnityEngine;
using UnityEngine.UI;

// 사이드바 탭을 관리 — 화면 패널 전환 + 아이콘 밝기(스프라이트) 교체 + 선택 인디케이터 바 표시
public class SidebarTabManager : MonoBehaviour
{
    [System.Serializable]
    public class NavTab
    {
        public GameObject panel;      // 이 탭이 열 화면
        public Image icon;            // 아이콘 Image 컴포넌트
        public Sprite darkSprite;     // 비활성 이미지
        public Sprite lightSprite;    // 활성(밝은) 이미지
        public GameObject indicatorBar; // 선택 시 보이는 왼쪽 세로 바 (예: SideHomeBar)
        public ScreenType screenType;   // 이 탭이 실제로 열어야 하는 화면
    }

    public NavTab[] tabs;
    public enum ScreenType { Home, News, Stock, Portfolio }

    void Start()
    {
        OpenTab(0); // 기본으로 홈 탭 열기
    }

    // 버튼 OnClick에 인덱스 전달해서 연결 (0=홈, 1=시장, 2=종목, 3=자산, 4=경쟁자들)
    public void OpenTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = (i == index);
            if (tabs[i].panel != null) tabs[i].panel.SetActive(isSelected);
            if (tabs[i].icon != null) tabs[i].icon.sprite = isSelected ? tabs[i].lightSprite : tabs[i].darkSprite;
            if (tabs[i].indicatorBar != null) tabs[i].indicatorBar.SetActive(isSelected);
        }

        switch (tabs[index].screenType)
        {
            case ScreenType.Home:
                UIManager.instance.OpenScreen(UIManager.instance.EntirePanel);
                break;
            case ScreenType.News:
                UIManager.instance.OpenNewsPanel();
                break;
            case ScreenType.Stock:
                UIManager.instance.OpenMarketPanel();
                break;
            case ScreenType.Portfolio:
                UIManager.instance.OpenPortfolioPanel();
                break;
        }
    }

}