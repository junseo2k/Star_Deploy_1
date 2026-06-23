using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance;

    public SlotUI[] slots;

    [Header("새 게임 팝업 (메인메뉴 + 빈 슬롯)")]
    public GameObject newGamePopupPanel;
    public TextMeshProUGUI newGameMessageText;

    [Header("로드 팝업 (메인메뉴 + 저장된 슬롯)")]
    public GameObject loadPopupPanel;
    public TextMeshProUGUI loadMessageText;

    [Header("로드 완료 팝업")]
    public GameObject loadCompletePanel;

    [Header("덮어쓰기 팝업")]
    public GameObject overwritePopupPanel;
    public TextMeshProUGUI overwriteMessageText;

    [Header("덮어쓰기 완료 팝업")]
    public GameObject overwriteCompletePanel;

    [Header("저장 팝업")]
    public GameObject savePopupPanel;
    public TextMeshProUGUI saveMessageText;

    [Header("저장 완료 팝업")]
    public GameObject saveCompletePanel;
    public GameObject overlay; // 반투명 배경



    private int selectedSlot = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 모든 팝업 비활성화
        newGamePopupPanel.SetActive(false);
        loadPopupPanel.SetActive(false);
        loadCompletePanel.SetActive(false);
        overwritePopupPanel.SetActive(false);
        overwriteCompletePanel.SetActive(false);
        savePopupPanel.SetActive(false);
        saveCompletePanel.SetActive(false);
        overlay.SetActive(false);

        // 인게임에서 왔을 때만 저장 버튼 표시
        bool fromMainMenu = PlayerPrefs.GetInt("FromMainMenu", 0) == 1;

        RefreshAllSlots();
    }

    // 모든 슬롯 UI 갱신
    public void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetSlotIndex(i + 1);
            slots[i].Refresh();
        }
    }

    // ── 슬롯 클릭 처리 ────────────────────────

    public void ShowLoadPopup(int slotIndex)
    {
        selectedSlot = slotIndex;
        bool fromMainMenu = PlayerPrefs.GetInt("FromMainMenu", 0) == 1;

        if (fromMainMenu)
        {
            // 메인메뉴에서 왔으면 로드 팝업
            loadMessageText.text = $"슬롯 {slotIndex}을 불러오시겠습니까?";
            loadPopupPanel.SetActive(true);
        }
        else
        {
            // 인게임에서 왔으면 덮어쓰기 팝업
            overwriteMessageText.text = $"슬롯 {slotIndex}에 덮어쓰시겠습니까?";
            overlay.SetActive(true);
            overwritePopupPanel.SetActive(true);
        }
    }

    public void ShowNewGamePopup(int slotIndex)
    {
        selectedSlot = slotIndex;
        bool fromMainMenu = PlayerPrefs.GetInt("FromMainMenu", 0) == 1;

        if (fromMainMenu)
        {
            // 메인메뉴에서 왔으면 새 게임 팝업
            newGameMessageText.text = $"슬롯 {slotIndex}에 새 게임을 시작하시겠습니까?";
            newGamePopupPanel.SetActive(true);
        }
        else
        {
            // 인게임에서 왔으면 저장 팝업
            saveMessageText.text = $"슬롯 {slotIndex}에 저장하시겠습니까?";
            overlay.SetActive(true);
            savePopupPanel.SetActive(true);
        }
    }

    // ── 새 게임 팝업 ──────────────────────────

    public void OnNewGameConfirm()
    {
        newGamePopupPanel.SetActive(false);
        PlayerPrefs.SetInt("SelectedSlot", selectedSlot);
        PlayerPrefs.Save();
        SceneManager.LoadScene("StoryMode");
    }

    public void OnNewGameCancel()
    {
        overlay.SetActive(false);
        newGamePopupPanel.SetActive(false);
        selectedSlot = -1;
    }

    // ── 로드 팝업 ─────────────────────────────

    public void OnLoadConfirm()
    {
        loadPopupPanel.SetActive(false);
        PlayerPrefs.SetInt("SelectedSlot", selectedSlot);
        PlayerPrefs.Save();
        loadCompletePanel.SetActive(true);
    }

    public void OnLoadCancel()
    {
        overlay.SetActive(false);
        loadPopupPanel.SetActive(false);
        selectedSlot = -1;
    }

    public void OnLoadCompleteConfirm()
    {
        overlay.SetActive(false);
        loadCompletePanel.SetActive(false);
        SceneManager.LoadScene("StoryMode"); // 이게 있어야 함
    }

    // ── 덮어쓰기 팝업 ─────────────────────────

    public void OnOverwriteConfirm()
    {
        overwritePopupPanel.SetActive(false);
        PlayerPrefs.SetInt("SelectedSlot", selectedSlot);
        PlayerPrefs.Save();
        GameManager.Instance.SaveGame();
        RefreshAllSlots();
        overwriteCompletePanel.SetActive(true);
    }

    public void OnOverwriteCancel()
    {
        overlay.SetActive(false);
        overwritePopupPanel.SetActive(false);
        selectedSlot = -1;
    }

    public void OnOverwriteCompleteConfirm()
    {
        overlay.SetActive(false);
        overwriteCompletePanel.SetActive(false);
    }

    // ── 저장 팝업 ─────────────────────────────

    public void ShowSavePopup(int slotIndex)
    {
        selectedSlot = slotIndex;
        saveMessageText.text = $"슬롯 {slotIndex}에 저장하시겠습니까?";
        overlay.SetActive(true);
        savePopupPanel.SetActive(true);
    }

    public void OnSaveConfirm()
    {
        savePopupPanel.SetActive(false);
        PlayerPrefs.SetInt("SelectedSlot", selectedSlot);
        PlayerPrefs.Save();
        GameManager.Instance.SaveGame();
        RefreshAllSlots();
        saveCompletePanel.SetActive(true);
    }

    public void OnSaveCancel()
    {
        overlay.SetActive(false);
        savePopupPanel.SetActive(false);
        selectedSlot = -1;
    }

    public void OnSaveCompleteConfirm()
    {
        saveCompletePanel.SetActive(false);
        overlay.SetActive(false);
    }
    // ── 기타 ──────────────────────────────────

    public void OnBackClick()
    {
        bool fromMainMenu = PlayerPrefs.GetInt("FromMainMenu", 0) == 1;
    
        if (fromMainMenu)
            SceneManager.LoadScene("MainMenu"); // 메인메뉴에서 왔으면 메인메뉴로
        else
            SceneManager.LoadScene("StoryMode");     // 인게임에서 왔으면 게임으로
}
}