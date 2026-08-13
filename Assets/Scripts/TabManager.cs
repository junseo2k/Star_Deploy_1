using TMPro;
using UnityEngine;

// 주식 탭 UI를 관리하는 클래스 — 종족/산업 탭 전환 및 주식 아이템 프리팹 생성을 담당
public class TabManager : MonoBehaviour
{
    public GameObject StockItemPrefab; // 주식 아이템을 표시할 프리팹
    public Stock[] allstock;           // 게임에 등록된 모든 주식 데이터

    [Header("종족별 패널")]
    public Transform DwarfPanel;
    public Transform ElfPanel;
    public Transform DragonPanel;
    public Transform HumanPanel;
    public Transform OrcPanel;

    [Header("산업별 패널")]
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

    [Header("탭 버튼 텍스트")]
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

    // 탭 하나의 구성 요소 — 패널, 버튼 라벨, 필터링에 쓸 태그
    private struct TabEntry
    {
        public Transform panel;
        public TextMeshProUGUI label;
        public string labelText; // 버튼에 표시할 문구 (태그와 다를 수 있음: Dragonia → "Dragon")
        public string tag;       // FamilyTag 또는 IndustryTag 값
        public bool byFamily;    // true면 FamilyTag, false면 IndustryTag로 필터링

        public TabEntry(Transform panel, TextMeshProUGUI label, string labelText, string tag, bool byFamily)
        {
            this.panel = panel;
            this.label = label;
            this.labelText = labelText;
            this.tag = tag;
            this.byFamily = byFamily;
        }
    }

    private TabEntry[] tabs;

    // 탭 목록 구성 → 모든 패널을 숨긴 뒤 드워프 탭을 기본으로 열고, 각 탭 버튼 텍스트 초기화
    void Start()
    {
        foreach (Stock stock in allstock)
            stock.runtimeIssues.Clear();

        tabs = new[]
        {
            new TabEntry(DwarfPanel,     DwarfPanelText,     "dwarf",     "Dwarf",     true),
            new TabEntry(ElfPanel,       ElfPanelText,       "Elf",       "Elf",       true),
            new TabEntry(DragonPanel,    DragonPanelText,    "Dragon",    "Dragonia",  true),
            new TabEntry(HumanPanel,     HumanPanelText,     "Human",     "Human",     true),
            new TabEntry(OrcPanel,       OrcPanelText,       "Orc",       "Orc",       true),
            new TabEntry(MiningPanel,    MiningPanelText,    "Mining",    "Mining",    false),
            new TabEntry(BankPanel,      BankPanelText,      "Bank",      "Bank",      false),
            new TabEntry(ArmyPanel,      ArmyPanelText,      "Army",      "Army",      false),
            new TabEntry(FoodPanel,      FoodPanelText,      "Food",      "Food",      false),
            new TabEntry(MagicPanel,     MagicPanelText,     "Magic",     "Magic",     false),
            new TabEntry(ClinicPanel,    ClinicPanelText,    "Clinic",    "Clinic",    false),
            new TabEntry(CoopangPanel,   CoopangPanelText,   "Coopang",   "Coopang",   false),
            new TabEntry(ConstructPanel, ConstructPanelText, "Construct", "Construct", false),
            new TabEntry(LuxuryPanel,    LuxuryPanelText,    "Luxury",    "Luxury",    false),
            new TabEntry(GamePanel,      GamePanelText,      "Game",      "Game",      false),
        };

        foreach (TabEntry tab in tabs)
        {
            if (tab.label != null)
                tab.label.text = tab.labelText;
        }

        OnDwarfClickTab();
    }

    // ── 탭 버튼 (인스펙터 OnClick에서 직접 연결되므로 이름을 바꾸지 말 것) ──

    public void OnDwarfClickTab() { OnFamilyTabClick("Dwarf", DwarfPanel); }
    public void OnElfTabClick() { OnFamilyTabClick("Elf", ElfPanel); }
    public void OnOrcTabClick() { OnFamilyTabClick("Orc", OrcPanel); }
    public void OnDragonTabClick() { OnFamilyTabClick("Dragonia", DragonPanel); }
    public void OnHumanTabClick() { OnFamilyTabClick("Human", HumanPanel); }

    public void OnMiningTabClick() { OnIndustryTabClick("Mining", MiningPanel); }
    public void OnBankTabClick() { OnIndustryTabClick("Bank", BankPanel); }
    public void OnArmyTabClick() { OnIndustryTabClick("Army", ArmyPanel); }
    public void OnFoodTabClick() { OnIndustryTabClick("Food", FoodPanel); }
    public void OnMagicTabClick() { OnIndustryTabClick("Magic", MagicPanel); }
    public void OnClinicTabClick() { OnIndustryTabClick("Clinic", ClinicPanel); }
    public void OnCoopangTabClick() { OnIndustryTabClick("Coopang", CoopangPanel); }
    public void OnConstructTabClick() { OnIndustryTabClick("Construct", ConstructPanel); }
    public void OnLuxuryTabClick() { OnIndustryTabClick("Luxury", LuxuryPanel); }
    public void OnGameTabClick() { OnIndustryTabClick("Game", GamePanel); }

    // 종족 탭 공통 처리
    public void OnFamilyTabClick(string classification, Transform panel)
    {
        OpenTab(classification, panel, true);
    }

    // 산업 탭 공통 처리
    public void OnIndustryTabClick(string classification, Transform panel)
    {
        OpenTab(classification, panel, false);
    }

    // 탭 공통 처리: 전체 패널 숨김 → 해당 패널 표시 → 주식 생성 → 가격 텍스트 갱신
    void OpenTab(string classification, Transform panel, bool byFamily)
    {
        HideAllPanels();
        panel.gameObject.SetActive(true);
        CreateStock(classification, panel, byFamily);

        foreach (StockControl stock in panel.GetComponentsInChildren<StockControl>())
            stock.UpdatePriceText();
    }

    // 태그(종족 또는 산업) 기준으로 주식 아이템 프리팹을 해당 패널에 생성
    // 이미 자식 오브젝트가 있는 패널은 중복 생성을 방지하기 위해 건너뜀
    void CreateStock(string tag, Transform panel, bool byFamily)
    {
        if (panel.childCount > 0) return; // 이미 생성된 패널이면 함수 종료

        foreach (Stock stock in allstock)
        {
            if (stock.isDelisted) continue; // 상장폐지된 회사는 목록에 슬롯을 만들지 않는다

            string stockTag = byFamily ? stock.FamilyTag : stock.IndustryTag;
            if (stockTag != tag) continue; // 태그가 일치하는 주식만 해당 패널에 배치

            GameObject newStock = Instantiate(StockItemPrefab, panel);
            StockControl sc = newStock.GetComponent<StockControl>();
            sc.stockinfo = stock;
            sc.Initialize();
        }
    }

    // 모든 종족/산업 패널을 비활성화
    void HideAllPanels()
    {
        foreach (TabEntry tab in tabs)
        {
            if (tab.panel != null)
                tab.panel.gameObject.SetActive(false);
        }
    }
}
