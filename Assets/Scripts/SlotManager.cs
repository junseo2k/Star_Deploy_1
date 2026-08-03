using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 세이브/로드 화면의 슬롯 선택과 각종 확인 팝업을 관리하는 클래스
// 메인메뉴에서 진입했는지(FromMainMenu) 인게임에서 진입했는지에 따라 같은 슬롯 클릭이
// 불러오기/새 게임 흐름 또는 덮어쓰기/저장 흐름으로 갈린다
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
        CloseAllPopups();
        RefreshAllSlots();
    }

    // 메인메뉴에서 진입했는지 여부 (아니면 인게임에서 저장하러 온 것)
    private static bool FromMainMenu => PlayerPrefs.GetInt("FromMainMenu", 0) == 1;

    // 모든 팝업과 배경을 닫는다
    void CloseAllPopups()
    {
        newGamePopupPanel.SetActive(false);
        loadPopupPanel.SetActive(false);
        loadCompletePanel.SetActive(false);
        overwritePopupPanel.SetActive(false);
        overwriteCompletePanel.SetActive(false);
        savePopupPanel.SetActive(false);
        saveCompletePanel.SetActive(false);
        overlay.SetActive(false);
    }

    // 팝업 하나를 배경과 함께 표시
    void ShowPopup(GameObject popup, TextMeshProUGUI messageText, string message)
    {
        messageText.text = message;
        overlay.SetActive(true);
        popup.SetActive(true);
    }

    // 선택한 슬롯을 저장 대상으로 확정
    void ConfirmSelectedSlot()
    {
        PlayerPrefs.SetInt("SelectedSlot", selectedSlot);
        PlayerPrefs.Save();
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

    // 저장된 슬롯 클릭 — 메인메뉴에서 왔으면 불러오기, 인게임이면 덮어쓰기
    public void ShowLoadPopup(int slotIndex)
    {
        selectedSlot = slotIndex;

        if (FromMainMenu)
            ShowPopup(loadPopupPanel, loadMessageText, $"슬롯 {slotIndex}을 불러오시겠습니까?");
        else
            ShowPopup(overwritePopupPanel, overwriteMessageText, $"슬롯 {slotIndex}에 덮어쓰시겠습니까?");
    }

    // 빈 슬롯 클릭 — 메인메뉴에서 왔으면 새 게임, 인게임이면 저장
    public void ShowNewGamePopup(int slotIndex)
    {
        selectedSlot = slotIndex;

        if (FromMainMenu)
            ShowPopup(newGamePopupPanel, newGameMessageText, $"슬롯 {slotIndex}에 새 게임을 시작하시겠습니까?");
        else
            ShowPopup(savePopupPanel, saveMessageText, $"슬롯 {slotIndex}에 저장하시겠습니까?");
    }

    // ── 새 게임 팝업 ──────────────────────────

    public void OnNewGameConfirm()
    {
        newGamePopupPanel.SetActive(false);
        ConfirmSelectedSlot();
        SceneManager.LoadScene("StoryMode");
    }

    public void OnNewGameCancel()
    {
        CancelPopup(newGamePopupPanel);
    }

    // ── 로드 팝업 ─────────────────────────────

    public void OnLoadConfirm()
    {
        loadPopupPanel.SetActive(false);
        ConfirmSelectedSlot();
        loadCompletePanel.SetActive(true);
    }

    public void OnLoadCancel()
    {
        CancelPopup(loadPopupPanel);
    }

    public void OnLoadCompleteConfirm()
    {
        overlay.SetActive(false);
        loadCompletePanel.SetActive(false);
        SceneManager.LoadScene("StoryMode");
    }

    // ── 덮어쓰기 팝업 ─────────────────────────

    public void OnOverwriteConfirm()
    {
        overwritePopupPanel.SetActive(false);
        SaveToSelectedSlot();
        overwriteCompletePanel.SetActive(true);
    }

    public void OnOverwriteCancel()
    {
        CancelPopup(overwritePopupPanel);
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
        ShowPopup(savePopupPanel, saveMessageText, $"슬롯 {slotIndex}에 저장하시겠습니까?");
    }

    public void OnSaveConfirm()
    {
        savePopupPanel.SetActive(false);
        SaveToSelectedSlot();
        saveCompletePanel.SetActive(true);
    }

    public void OnSaveCancel()
    {
        CancelPopup(savePopupPanel);
    }

    public void OnSaveCompleteConfirm()
    {
        saveCompletePanel.SetActive(false);
        overlay.SetActive(false);
    }

    // ── 공통 처리 ──────────────────────────────

    // 선택 슬롯에 현재 게임 상태를 저장하고 슬롯 목록 갱신
    void SaveToSelectedSlot()
    {
        ConfirmSelectedSlot();
        GameManager.Instance.SaveGame();
        RefreshAllSlots();
    }

    // 팝업 취소 — 팝업과 배경을 닫고 선택을 해제
    void CancelPopup(GameObject popup)
    {
        overlay.SetActive(false);
        popup.SetActive(false);
        selectedSlot = -1;
    }

    public void OnBackClick()
    {
        // 메인메뉴에서 왔으면 메인메뉴로, 인게임에서 왔으면 게임으로
        SceneManager.LoadScene(FromMainMenu ? "MainMenu" : "StoryMode");
    }
}