using TMPro;
using UnityEngine;

// 주식 탭 UI를 관리하는 클래스 — 종족/산업 탭 전환 및 주식 아이템 프리팹 생성을 담당
public class TabManager : MonoBehaviour
{
    public GameObject StockItemPrefab; // 주식 아이템을 표시할 프리팹

    public Stock[] allstock; // 게임에 등록된 모든 주식 데이터

    // 종족별 패널 (각 탭에 해당하는 주식 목록을 담는 부모 Transform)
    public Transform DwarfPanel;
    public Transform ElfPanel;
    public Transform DragonPanel;
    public Transform HumanPanel;
    public Transform OrcPanel;

    // 산업별 패널
    public Transform MiningPanel;
    public Transform BankPanel;
    public Transform ArmyPanel;
    public Transform FoodPanel;
    public Transform MagicPanel;
    public Transform ClinicPanel;
    public Transform CoopangPanel;
    public Transform ConstructPanel;
    public Transform LuxuryPanel;
    public Transform GamePanel;

    // 탭 버튼에 표시되는 텍스트
    public TextMeshProUGUI DwarfPanelText;
    public TextMeshProUGUI ElfPanelText;
    public TextMeshProUGUI DragonPanelText;
    public TextMeshProUGUI HumanPanelText;
    public TextMeshProUGUI OrcPanelText;
    public TextMeshProUGUI MiningPanelText;
    public TextMeshProUGUI BankPanelText;
    public TextMeshProUGUI ArmyPanelText;
    public TextMeshProUGUI FoodPanelText;
    public TextMeshProUGUI MagicPanelText;
    public TextMeshProUGUI ClinicPanelText;
    public TextMeshProUGUI CoopangPanelText;
    public TextMeshProUGUI ConstructPanelText;
    public TextMeshProUGUI LuxuryPanelText;
    public TextMeshProUGUI GamePanelText;

    public OrderPanel orderPanel;   // 구매/판매 확인 패널 참조
    public Transform EntirePanel;   // 전체 주식 목록 패널
    public Transform buyPanel;      // 구매/판매 입력 패널

    // 모든 패널을 숨긴 뒤 드워프 탭을 기본으로 열고, 각 탭 버튼 텍스트 초기화
    void Start()
    {
        foreach (Stock stock in allstock)
        {
            stock.runtimeIssues.Clear();
        }
        HideAllPanels();
        OnDwarfClickTab();
        DwarfPanelText.text = "dwarf";
        ElfPanelText.text = "Elf";
        DragonPanelText.text = "Dragon";
        HumanPanelText.text = "Human";
        OrcPanelText.text = "Orc";
        MiningPanelText.text = "Mining";
        BankPanelText.text = "Bank";
        ArmyPanelText.text = "Army";
        FoodPanelText.text = "Food";   
        MagicPanelText.text = "Magic";
        ClinicPanelText.text = "Clinic";
        CoopangPanelText.text = "Coopang";
        ConstructPanelText.text = "Construct";
        LuxuryPanelText.text = "Luxury";
        GamePanelText.text = "Game";
    }

    // 매 프레임 호출 (현재 사용 없음)
    void Update()
    {

    }

    // 드워프 탭 버튼 클릭 시 드워프 패널 활성화
    public void OnDwarfClickTab()
    {
        OnFamilyTabClick("Dwarf", DwarfPanel);
    }

    // 종족(FamilyTag) 기준으로 주식 아이템 프리팹을 해당 패널에 생성
    // 이미 자식 오브젝트가 있는 패널은 중복 생성을 방지하기 위해 건너뜀
    public void CreateFamilyStock(string tag, Transform panel)
    {
        if (panel.childCount > 0) return; // 이미 생성된 패널이면 함수 종료

        foreach (Stock stock in allstock)
        {
            if (stock.FamilyTag == tag) // 태그가 일치하는 주식만 해당 패널에 배치
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
                sc.orderPanel = orderPanel;
                sc.EntirePanel = EntirePanel;
                sc.BuyPanel = buyPanel;
                sc.Initialize();
            }
        }
    }

    // 산업(IndustryTag) 기준으로 주식 아이템 프리팹을 해당 패널에 생성
    // 이미 자식 오브젝트가 있는 패널은 중복 생성을 방지하기 위해 건너뜀
    public void CreateIndustryStock(string tag, Transform panel)
    {
        if (panel.childCount > 0) return; // 이미 생성된 패널이면 함수 종료

        foreach (Stock stock in allstock)
        {
            if (stock.IndustryTag == tag) // 태그가 일치하는 주식만 해당 패널에 배치
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
                sc.orderPanel = orderPanel;
                sc.EntirePanel = EntirePanel;
                sc.BuyPanel = buyPanel;
                sc.Initialize();
            }
        }
    }

    // 엘프 탭 버튼 클릭 시 엘프 패널 활성화
    public void OnElfTabClick()
    {
        OnFamilyTabClick("Elf", ElfPanel);
    }
    // 오크 탭 버튼 클릭 시 오크 패널 활성화
    public void OnOrcTabClick()
    {
        OnFamilyTabClick("Orc", OrcPanel);
    }

    // 은행 탭 버튼 클릭 시 은행 패널 활성화
    public void OnBankTabClick()
    {
        OnIndustryTabClick("Bank", BankPanel);
    }

    // 식품 탭 버튼 클릭 시 식품 패널 활성화
    public void OnFoodTabClick()
    {
        OnIndustryTabClick("Food", FoodPanel);
    }

    // 마법 탭 버튼 클릭 시 마법 패널 활성화
    public void OnMagicTabClick()
    {
        OnIndustryTabClick("Magic", MagicPanel);
    }

    // 의료 탭 버튼 클릭 시 의료 패널 활성화
    public void OnClinicTabClick()
    {
        OnIndustryTabClick("Clinic", ClinicPanel);
    }

    // 유통 탭 버튼 클릭 시 유통 패널 활성화
    public void OnCoopangTabClick()
    {
        OnIndustryTabClick("Coopang", CoopangPanel);
    }

    // 건설 탭 버튼 클릭 시 건설 패널 활성화
    public void OnConstructTabClick()
    {
        OnIndustryTabClick("Construct", ConstructPanel);
    }

    // 명품 탭 버튼 클릭 시 명품 패널 활성화
    public void OnLuxuryTabClick()
    {
        OnIndustryTabClick("Luxury", LuxuryPanel);
    }

    // 게임 탭 버튼 클릭 시 게임 패널 활성화
    public void OnGameTabClick()
    {
        OnIndustryTabClick("Game", GamePanel);
    }

    // 드래곤 탭 버튼 클릭 시 드래곤 패널 활성화
    public void OnDragonTabClick()
    {
        OnFamilyTabClick("Dragonia", DragonPanel);
    }

    // 휴먼 탭 버튼 클릭 시 휴먼 패널 활성화
    public void OnHumanTabClick()
    {
        OnFamilyTabClick("Human", HumanPanel);
    }

    // 채굴 탭 버튼 클릭 시 채굴 패널 활성화
    public void OnMiningTabClick()
    {
        OnIndustryTabClick("Mining", MiningPanel);
    }

    // 전쟁 탭 버튼 클릭 시 전쟁 패널 활성화
    public void OnArmyTabClick()
    {
        OnIndustryTabClick("Army", ArmyPanel);
    }

    // 모든 종족/산업 패널을 비활성화
    void HideAllPanels()
    {
        DwarfPanel.gameObject.SetActive(false);
        ElfPanel.gameObject.SetActive(false);
        DragonPanel.gameObject.SetActive(false);
        HumanPanel.gameObject.SetActive(false);
        OrcPanel.gameObject.SetActive(false);
        MiningPanel.gameObject.SetActive(false);
        BankPanel.gameObject.SetActive(false);
        ArmyPanel.gameObject.SetActive(false);
        FoodPanel.gameObject.SetActive(false);
        MagicPanel.gameObject.SetActive(false);
        ClinicPanel.gameObject.SetActive(false);
        CoopangPanel.gameObject.SetActive(false);
        ConstructPanel.gameObject.SetActive(false);
        LuxuryPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);
    }

    // 종족 탭 공통 처리: 전체 패널 숨김 → 해당 패널 표시 → 주식 생성 → 가격 텍스트 갱신
    public void OnFamilyTabClick(string classification, Transform panel)
    {
        HideAllPanels();
        panel.gameObject.SetActive(true);
        CreateFamilyStock(classification, panel);
        foreach (StockControl stock in panel.GetComponentsInChildren<StockControl>())
        {
            stock.UpdatePriceText();
        }
    }

    // 산업 탭 공통 처리: 전체 패널 숨김 → 해당 패널 표시 → 주식 생성 → 가격 텍스트 갱신
    public void OnIndustryTabClick(string classification, Transform panel)
    {
        HideAllPanels();
        panel.gameObject.SetActive(true);
        CreateIndustryStock(classification, panel);
        foreach (StockControl stock in panel.GetComponentsInChildren<StockControl>())
        {
            stock.UpdatePriceText();
        }
    }
}
