using UnityEngine;
using TMPro;
using System.IO;

// 개별 슬롯 UI를 관리하는 클래스
public class SlotUI : MonoBehaviour
{
    public TextMeshProUGUI slotNameText; // 슬롯 이름 / 빈 슬롯 표시
    public TextMeshProUGUI dayText;      // 날짜 표시
    public TextMeshProUGUI goldText;     // 골드 표시

    private int slotIndex;   // 슬롯 번호
    private string savePath; // 저장 파일 경로

    // 슬롯 번호 설정 및 경로 초기화
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        savePath = Application.persistentDataPath + $"/save{slotIndex}.json";
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

    // 슬롯 클릭 시 저장 유무에 따라 팝업 분기
    public void OnSlotClick()
    {
        if (File.Exists(savePath))
            SlotManager.Instance.ShowLoadPopup(slotIndex);   // 저장된 슬롯 팝업
        else
            SlotManager.Instance.ShowNewGamePopup(slotIndex); // 빈 슬롯 팝업
    }

    // 저장 슬롯으로 선택됐을 때 호출
    public void OnSaveToThisSlot()
    {
        SlotManager.Instance.ShowSavePopup(slotIndex);
    }
}