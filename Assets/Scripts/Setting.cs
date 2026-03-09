using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TextMeshProUGUI VolumeMessage; //외부에서 접근할 이유가 없다면 public보단 이게 나음
    [SerializeField] private Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;
    private int modeindex;
    private List<Resolution> resolutions;
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
        bool fullscreen = modeindex != 2;
        Screen.SetResolution(selected.width, selected.height, fullscreen);
        PlayerPrefs.SetInt("ResW", selected.width);
        PlayerPrefs.SetInt("ResH", selected.height);
        #if UNITY_EDITOR // 빌드하면 제거되도록 수정
        Debug.Log($"선택한 해상도: {selected.width}x{selected.height}");
        Debug.Log($"Screen.width/height: {Screen.width}x{Screen.height}");
        Debug.Log($"currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        Debug.Log("현재 해상도 :" + Screen.width + "x" + Screen.height);
        #endif

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
        resolutions = new List<Resolution>();
        string last = "";
        foreach (Resolution res in Screen.resolutions) //해상도 불러온 후 리스트에 목록 추가 (중복 제거)
        {
            string entry = res.width + "x" + res.height;
            if (entry == last) continue; // 주사율만 다른 중복 해상도 건너뜀
            resolutions.Add(res);
            options.Add(entry);
            last = entry;
        }
        resolutionDropdown.ClearOptions(); //옵션 비워두기. 그래야 깔끔하게 적용값이 들어갈 수 있음
        resolutionDropdown.AddOptions(options);
        //전체 화면 설정
        modeindex = PlayerPrefs.GetInt("ScreenMode", 1);
        screenModeDropdown.value = modeindex;
        screenModeDropdown.RefreshShownValue(); //지금 value에 맞는 텍스트로 화면 갱신
        int w = PlayerPrefs.GetInt("ResW", Screen.width); // 저장했던 해상도 불러오기
        int h = PlayerPrefs.GetInt("ResH", Screen.height);
        bool fullscreen = modeindex != 2; // 창모드면 false
        Screen.SetResolution(w, h, fullscreen);
        // 저장된 해상도와 일치하는 인덱스를 찾아 드롭다운에 반영
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == w && resolutions[i].height == h) //그리고
            {
                resolutionDropdown.value = i;
                break;
            }
        }
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
