using TMPro;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject StockItemPrefab;
    public Stock[] allstock;
    public Transform DwarfPanel;
    public Transform ElfPanel;
    public Transform DragonPanel;
    public Transform HumanPanel;
    public Transform MiningPanel;
    public Transform WarPanel;
    public TextMeshProUGUI DwarfPanelText;
    public TextMeshProUGUI ElfPanelText;
    public TextMeshProUGUI DragonPanelText;
    public TextMeshProUGUI HumanPanelText;
    public TextMeshProUGUI MiningPanelText;
    public TextMeshProUGUI WarPanelText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
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
        HideAllPanels();
        DwarfPanel.gameObject.SetActive(true);
        CreateFamilyStock("Dwarf", DwarfPanel);

        //드워프 주식 생성
    }
    public void CreateFamilyStock(string tag, Transform panel)
    {
        //주식 정보를 가져옴.
        if (panel.childCount > 0) return;
        foreach (Stock stock in allstock)
        {
            if (stock.FamilyTag == tag)
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
            }
        }
        
    }
    public void CreateIndustryStock(string tag, Transform panel)
    {
        //주식 정보를 가져옴.
        if (panel.childCount > 0) return;
        foreach (Stock stock in allstock)
        {
            if (stock.IndustryTag == tag)
            {
                GameObject newStock = Instantiate(StockItemPrefab, panel);
                StockControl sc = newStock.GetComponent<StockControl>();
                sc.stockinfo = stock;
            }
        }
        
    }
    public void OnElfTabClick()
    {
        HideAllPanels();
        ElfPanel.gameObject.SetActive(true);
        CreateFamilyStock("Elf", ElfPanel);

    }
    public void OnDragonTabClick()
    {
        HideAllPanels();
        DragonPanel.gameObject.SetActive(true);
        CreateFamilyStock("Dragon", DragonPanel);
    }
    public void OnHumanTabClick()
    {
        HideAllPanels();
        HumanPanel.gameObject.SetActive(true);
        CreateFamilyStock("Human", HumanPanel);
    }
    public void OnMiningTabClick()
    {
        HideAllPanels();
        MiningPanel.gameObject.SetActive(true);
        CreateIndustryStock("Mining", MiningPanel);
    }
    public void OnWarTabClick()
    {
        HideAllPanels();
        WarPanel.gameObject.SetActive(true);
        CreateIndustryStock("War", WarPanel);
    }
    void HideAllPanels()
    {
        DwarfPanel.gameObject.SetActive(false);
        ElfPanel.gameObject.SetActive(false);
        DragonPanel.gameObject.SetActive(false);
        HumanPanel.gameObject.SetActive(false);
        MiningPanel.gameObject.SetActive(false);
        WarPanel.gameObject.SetActive(false);
    }
}
//패널이 켜지면, 그 패널의 주식들을 나타내도록 함.