using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 화면을 관리하는 클래스
public class MainMenu : MonoBehaviour
{
    // 시작 버튼 클릭 시 세이브/로드 씬으로 이동
    public void OnClickStart()
    {
        SceneManager.LoadScene("SaveLoad");
    }

    // 설정 버튼 클릭 시 설정 씬으로 이동
    public void OnClickSetting()
    {
        SceneManager.LoadScene("Setting");
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
