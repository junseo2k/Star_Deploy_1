using TMPro;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject StockItemPrefab;//프리팹 오브젝트 제어
    public Stock[] allstock;
    public Transform DwarfPanel; //각 주식 분류에 따른 패널.
    public Transform ElfPanel;
    public Transform DragonPanel;
    public Transform HumanPanel;
    public Transform MiningPanel;
    public Transform WarPanel;
    public TextMeshProUGUI DwarfPanelText; //탭 버튼 텍스트
    public TextMeshProUGUI ElfPanelText;
    public TextMeshProUGUI DragonPanelText;
    public TextMeshProUGUI HumanPanelText;
    public TextMeshProUGUI MiningPanelText;
    public TextMeshProUGUI WarPanelText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() //패널 탭에 정보 설정. 그 전에 패널을 다 숨기고, 드워프 패널을 처음에 열도록
    {
        HideAllPanels();
        OnDwarfClickTab();
        DwarfPanelText.text = "dwarf";
        ElfPanelText.text = "Elf";
        DragonPanelText.text = "Dragon";
        HumanPanelText.text = "Human";
        MiningPanelText.text = "Mining";
        WarPanelText.text = "War";
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnDwarfClickTab()
    {
        OnFamilyTabClick("Dwarf", DwarfPanel);

    }
    public void CreateFamilyStock(string tag, Transform panel) //종족 주식 생성 함수.
    {
        //주식 정보를 가져옴.
         Debug.Log($"childCount: {panel.childCount}"); // 몇인지 확인!
        if (panel.childCount > 0) return; //주식이 이미 생성된 패널이면 함수 종료
        foreach (Stock stock in allstock)
        {
            if (stock.FamilyTag == tag)//태그가 같으면 그 패널에 분류하도록 설정
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
                sc.Initialize();
            }
        }
        
    }
    public void CreateIndustryStock(string tag, Transform panel) //산업 주식 생성 함수
    {
        //주식 정보를 가져옴.
        if (panel.childCount > 0) return;
        foreach (Stock stock in allstock)
        {
            if (stock.IndustryTag == tag) //태그가 같으면 그 패널에 분류하도록 설정
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
                sc.Initialize();
            }
        }
        
    }
    public void OnElfTabClick() //탭 클릭 시 패널이 나타나도록 설정
    {
        OnFamilyTabClick("Elf", ElfPanel);
    }
    public void OnDragonTabClick()
    {
        OnFamilyTabClick("Dragon", DragonPanel);
    }
    public void OnHumanTabClick()
    {
        OnFamilyTabClick("Human", HumanPanel);
    }
    public void OnMiningTabClick()
    {
        OnIndustryTabClick("Mining", MiningPanel);
    }
    public void OnWarTabClick()
    {
        OnIndustryTabClick("War", WarPanel);
    }
    void HideAllPanels() //모든 탭 숨기기.
    {
        DwarfPanel.gameObject.SetActive(false);
        ElfPanel.gameObject.SetActive(false);
        DragonPanel.gameObject.SetActive(false);
        HumanPanel.gameObject.SetActive(false);
        MiningPanel.gameObject.SetActive(false);
        WarPanel.gameObject.SetActive(false);
    }
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
//패널이 켜지면, 그 패널의 주식들을 나타내도록 함.