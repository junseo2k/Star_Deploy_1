using UnityEngine;
using UnityEngine.SceneManagement;

// 세이브/로드 씬의 슬롯 목록을 관리하는 클래스
public class SlotManager : MonoBehaviour
{
    public SlotUI[] slots; // 슬롯 UI 배열 (Inspector에서 5개 연결)

    void Start()
    {
        RefreshAllSlots();
    }

    // 모든 슬롯 UI를 저장 파일 기준으로 갱신
    public void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetSlotIndex(i + 1); // 슬롯 번호 1~5
            slots[i].Refresh();
        }
    }

    // 메인 메뉴로 돌아가기
    public void OnBackClick()
    {
        SceneManager.LoadScene("MainMenu");
    }
}