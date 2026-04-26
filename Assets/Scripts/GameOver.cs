using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 오버 씬을 관리하는 클래스
public class GameOver : MonoBehaviour
{
    // 메인 메뉴로 돌아가기 버튼 클릭 시 호출
    public void OnMainMenuClick()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
