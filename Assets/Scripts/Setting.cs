using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI VolumeMessage;
    [SerializeField] private Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;
    private int modeindex;
    private Resolution[] resolutions;
    public void ApplyOnClick()
    {
        //음량 설정
        float applyvolume = volumeSlider.value;
        float applysound = applyvolume / 100f;
        AudioListener.volume = applysound;


        PlayerPrefs.SetFloat("Volume", applyvolume);//저장 범위를 0~100으로 통일. 
        Debug.Log("현재 볼륨 :" + applyvolume);

        //전체 화면 설정
        modeindex = screenModeDropdown.value;
        PlayerPrefs.SetInt("ScreenMode", modeindex);
        switch(modeindex)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Debug.Log("현재 화면 : 전체 화면");
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Debug.Log("현재 화면 : 전체 창 화면");
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Debug.Log("현재 화면 : 창 모드");
                break;
        }
        //해상도 설정
        int index = resolutionDropdown.value;
        Resolution selected = resolutions[index];
        bool fullscreen = (modeindex != 2);
        Screen.SetResolution(selected.width, selected.height, fullscreen);
        PlayerPrefs.SetInt("ResW", selected.width);
        PlayerPrefs.SetInt("ResH", selected.height);
        Debug.Log($"선택한 해상도: {selected.width}x{selected.height}");
        Debug.Log($"Screen.width/height: {Screen.width}x{Screen.height}");
        Debug.Log($"currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        Debug.Log("현재 해상도 :" + Screen.width + "x" + Screen.height);

        PlayerPrefs.SetInt("ScreenMode", modeindex);
        PlayerPrefs.Save(); //변경된 값을 기록함
    }
    void Start()
    {
        //음량 설정
        float saved = PlayerPrefs.GetFloat("Volume", 50f); // 초기 음량 설정
        volumeSlider.value = saved;
        AudioListener.volume = saved / 100f; //슬라이더 value의 범위는 0~100, AudioListener의 범위는 0~1이기 때문에, 100을 나눠줘야 함. //지원 해상도 목록 가져오기
        //해상도 설정
        List<string> options = new List<string>(); // 리스트 선언
        resolutions = Screen.resolutions;
        foreach (Resolution res in resolutions) //해상도 불러온 후 리스트에 목록 추가
        {
            options.Add(res.width + "x" + res.height);
        }
        resolutionDropdown.ClearOptions(); //옵션 비워두기. 그래야 깔끔하게 적용값이 들어갈 수 있음
        resolutionDropdown.AddOptions(options);
        //전체 화면 설정
        modeindex = PlayerPrefs.GetInt("ScreenMode", 1);
        screenModeDropdown.value = modeindex;
        screenModeDropdown.RefreshShownValue(); //지금 value에 맞는 텍스트로 화면 갱신
        int w = PlayerPrefs.GetInt("ResW", Screen.width); // 저장했던 해상도 불러오기
        int h = PlayerPrefs.GetInt("ResH", Screen.height);
        bool fullscreen = (modeindex != 2); // 창모드면 false
        Screen.SetResolution(w, h, fullscreen);
        resolutionDropdown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnChangeSlider()
    {
        int volumeText = Mathf.RoundToInt(volumeSlider.value);//float를 정수로 변환
        VolumeMessage.text = $"{volumeText}";

    }

}
