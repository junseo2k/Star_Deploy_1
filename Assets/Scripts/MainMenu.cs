using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 화면을 관리하는 클래스
public class MainMenu : MonoBehaviour
{
    // 시작 버튼 클릭 시 세이브/로드 씬으로 이동
    public void OnClickStart()
    {
        PlayerPrefs.SetInt("FromMainMenu", 1); // 메인 메뉴에서 왔음을 표시
        PlayerPrefs.Save();
        SceneManager.LoadScene("SaveLoad");
    }

    // 설정 버튼 클릭 시 설정 씬으로 이동
    public void OnClickSetting()
    {
        SceneManager.LoadScene("Setting");
    }
    public void OnClickExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 플레이 모드 종료
        #else
            Application.Quit(); // 빌드에서 게임 종료
        #endif
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
