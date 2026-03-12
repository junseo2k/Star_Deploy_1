using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI NoticeText;
    public TextMeshProUGUI Ownedgold;
    public static UIManager instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void UpdateGoldText()
    {
        Ownedgold.text = $"현재 골드 : {GameManager.Instance.currentgold}";
    }
    public void NoticeGold(string noticetext)
    {
        NoticeText.text = noticetext;        
    }
}
