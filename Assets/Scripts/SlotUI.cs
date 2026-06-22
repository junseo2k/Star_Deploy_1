using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

// 개별 슬롯 UI를 관리하는 클래스
public class SlotUI : MonoBehaviour
{
    public TextMeshProUGUI slotNameText;  // "슬롯 1" 등 슬롯 이름
    public TextMeshProUGUI dayText;       // "날짜: 5일" 표시
    public TextMeshProUGUI goldText;      // "골드: 5000G" 표시

    private int slotIndex;  // 이 슬롯의 번호 (1~5)
    private string savePath; // 저장 파일 경로

    // 슬롯 번호 설정 및 경로 초기화
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        savePath = Application.persistentDataPath + $"/save{slotIndex}.json";
        slotNameText.text = $"슬롯 {slotIndex}";
    }

    // 저장 파일 유무에 따라 슬롯 UI 갱신
    public void Refresh()
{
    if (File.Exists(savePath))
    {
        string json = File.ReadAllText(savePath);
        GameManager.GameSaveData data = JsonUtility.FromJson<GameManager.GameSaveData>(json);
        slotNameText.text = $"슬롯 {slotIndex}";
        dayText.text = $"날짜: {data.currentday}일";
        goldText.text = $"골드: {data.currentgold} G";
        dayText.gameObject.SetActive(true);
        goldText.gameObject.SetActive(true);
    }
    else
    {
        slotNameText.text = "빈 슬롯";
        dayText.gameObject.SetActive(false);
        goldText.gameObject.SetActive(false);
    }
}

    // 슬롯 클릭 시 선택한 슬롯 번호 저장 후 게임 씬으로 이동
    public void OnSlotClick()
    {
        PlayerPrefs.SetInt("SelectedSlot", slotIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("StoryMode");
    }
}