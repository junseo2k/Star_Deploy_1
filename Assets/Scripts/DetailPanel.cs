using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// 주식 상세 정보 패널을 관리하는 클래스
// 기본 정보·서비스·중요 지표·최근 이슈 탭 전환과 주식 데이터 표시를 담당
public class DetailPanel : MonoBehaviour
{
    public GameObject StockWithImagePrefab;   // 이미지 포함 주식 아이템 프리팹
    public Image StockImage;                 // 주식 아이콘 이미지
    public TextMeshProUGUI StockName;         // 주식 이름 표시 텍스트
    public TextMeshProUGUI StockPrice;        // 현재 주식 가격 표시 텍스트
    public TextMeshProUGUI StockChangePrice;  // 이전 가격 대비 변동량 표시 텍스트 (비교 함수 필요)
    public TextMeshProUGUI StockDescription;  // 주식 요약 설명 표시 텍스트
    public Transform DetailInfoPanel;         // 기본 정보 탭 패널
    public Transform ServicePanel;            // 서비스 탭 패널
    public Transform ImportantPanel;          // 중요 지표 탭 패널
    public TextMeshProUGUI StockDetailDescription; // 기업 상세 설명 표시 텍스트
    public TextMeshProUGUI revenuePrice;      // 매출액 표시 텍스트
    public TextMeshProUGUI ProfitPrice;       // 영업이익 표시 텍스트
    public TextMeshProUGUI MarketCapPrice;    // 시가총액 표시 텍스트
    public TextMeshProUGUI TotalSharePrice;   // 총 발행 주식 수 표시 텍스트               
    public Stock currentstock;               // 현재 상세 패널에 표시 중인 주식 데이터
    public Transform EntirePanel;            // 전체 주식 목록 패널 (상세 패널 닫을 때 복귀 대상)
    public Transform DetailwithPanel;        // 상세 패널 전체 루트 (구매/판매 패널 전환 시 비활성화)
    public int maxmount;                     // 구매 또는 판매 가능한 최대 수량
    public Transform BuyPanel;              // 구매/판매 입력 패널 (구매·판매 버튼 클릭 시 활성화)
    public OrderPanel orderPanel;           // 구매/판매 수량 확인 패널 참조



    void Start()
    {
        HideAllPanels(); // 시작 시 모든 탭 패널을 숨김
    }

    void Update()
    {

    }

    // 주식 데이터를 받아 상세 패널 UI 텍스트를 갱신
    public void ShowDetail(Stock stock)
    {
        currentstock = stock;
        StockName.text = stock.StockName;
        StockImage.sprite = stock.StockIcon;
        StockPrice.text = stock.StockCurrentPrice.ToString();
        StockDescription.text = stock.StockDescription;
        StockDetailDescription.text = stock.StockDetailDescription;
        revenuePrice.text = $"{stock.revenue} G";
        ProfitPrice.text = $"{stock.OperatingProfit} G";
        MarketCapPrice.text = $"{stock.marketCap} G";
        TotalSharePrice.text = $"{stock.TotalShare} stock";
        StockChangePrice.text = stock.StockChangePrice.ToString();
        
    }

    // 모든 탭 패널을 비활성화 (탭 전환 전 초기화에 사용)
    public void HideAllPanels()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
    }

    // 기본 정보 탭 버튼 클릭 시 기본 정보 패널만 활성화
    public void OnDetailInfoClick()
    {
        DetailInfoPanel.gameObject.SetActive(true);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(false);
    }

    // 서비스 탭 버튼 클릭 시 서비스 패널만 활성화
    public void OnServiceClick()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(true);
        ImportantPanel.gameObject.SetActive(false);
        foreach (Transform child in ServicePanel)
        {
            Destroy(child.gameObject);
        }
        foreach (Stock.ProductInfo product in currentstock.products)
        {
            GameObject ServicePrefab = Instantiate(StockWithImagePrefab, ServicePanel);
            Image ServiceImage = ServicePrefab.GetComponentInChildren<Image>();
            ServiceImage.sprite = product.productIcon;
            TextMeshProUGUI ServiceText = ServicePrefab.GetComponentInChildren<TextMeshProUGUI>();
            ServiceText.text = product.productName;
        }

        
    }

    // 중요 지표 탭 버튼 클릭 시 중요 지표 패널만 활성화
    public void OnImportantClick()
    {
        DetailInfoPanel.gameObject.SetActive(false);
        ServicePanel.gameObject.SetActive(false);
        ImportantPanel.gameObject.SetActive(true);
    }
    // 닫기 버튼 클릭 시 서비스·이슈 프리팹 정리 후 상세 패널 닫고 전체 목록 패널로 복귀
    public void OnExitButton()
    {
        foreach (Transform child in ServicePanel)
        {
            Destroy(child.gameObject);
        }
        DetailwithPanel.gameObject.SetActive(false);
        EntirePanel.gameObject.SetActive(true);
    }
    // 구매 버튼 클릭 시 상세 패널 닫고 구매 패널 열기
    // 현재 주식에 해당하는 StockControl을 찾아 OrderPanel에 연결하고 최대 구매 수량 계산
    public void OnBuyButtonClick()
    {
        DetailwithPanel.gameObject.SetActive(false);
        BuyPanel.gameObject.SetActive(true);
        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StockControl sc in allStocks)
        {
            if (sc.stockinfo.StockName == currentstock.StockName)
            {
                orderPanel.stockcontrol = sc; // 찾은 StockControl 넘기기
                orderPanel.OwnedNum = sc.ownedAmount; // ← 여기서 가져오기!
                break;
            }
        }
        maxmount = GameManager.Instance.currentgold / currentstock.StockCurrentPrice; // 최대 구매 가능 수량 계산
        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.PurchaseText.text = $"OwnedStock : 1, \n {currentstock.StockCurrentPrice} gold.";
        orderPanel.isBuyMode = true; // 구매 모드로 설정
    }
    // 판매 버튼 클릭 시 상세 패널 닫고 판매 패널 열기
    // 현재 주식에 해당하는 StockControl을 찾아 OrderPanel에 연결하고 최대 판매 수량(보유량) 계산
    public void OnSellButtonClick()
    {
        DetailwithPanel.gameObject.SetActive(false);
        BuyPanel.gameObject.SetActive(true);
        StockControl[] allStocks = FindObjectsByType<StockControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StockControl sc in allStocks)
        {
            if (sc.stockinfo.StockName == currentstock.StockName)
            {
                orderPanel.stockcontrol = sc; // 찾은 StockControl 넘기기
                orderPanel.OwnedNum = sc.ownedAmount; // ← 여기서 가져오기!
                maxmount = sc.ownedAmount;
                break;
            }
        }
        orderPanel.currentText.text = $"Maximum: {maxmount}";
        orderPanel.StockNum = 1;
        orderPanel.PurchaseText.text = $"OwnedStock : {orderPanel.StockNum}, \n {currentstock.StockCurrentPrice} gold.";
        orderPanel.isBuyMode = false; // 판매 모드로 설정
    }


}
