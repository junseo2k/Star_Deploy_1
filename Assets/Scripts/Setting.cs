using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TextMeshProUGUI VolumeMessage; //외부에서 접근할 이유가 없다면 public보단 이게 나음
    [SerializeField] private Slider volumeSlider; //음량 키 조절 UI 슬라이더
    public TMP_Dropdown resolutionDropdown;//해상도 드롭다운
    public TMP_Dropdown screenModeDropdown;//전체화면/전체 창 화면/창 모드 전환 드롭다운
    private int modeindex;//화면 모드를 이 값에 따라 조정
    private List<Resolution> resolutions; //유니티에서 지원하는 해상도들을 이 리스트에 담음
    public void ApplyOnClick() //적용 버튼 누르면 일어나는 일. 음량 설정, 화면 모드 전환, 해상도 설정
    {
        //음량 설정
        float applyvolume = volumeSlider.value;
        float applysound = applyvolume / 100f;
        AudioListener.volume = applysound;


        //조정된 음량을 PlayerPrefs에 저장
        PlayerPrefs.SetFloat("Volume", applyvolume);//저장 범위를 0~100으로 통일. 
        Debug.Log("현재 볼륨 :" + applyvolume);
    
        //전체 화면 설정
        modeindex = screenModeDropdown.value;
        PlayerPrefs.SetInt("ScreenMode", modeindex);
        switch(modeindex) //0, 1, 2에 따라 화면 모드 전환
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
        //초기 음량 설정
        float saved = PlayerPrefs.GetFloat("Volume", 50f); 
        volumeSlider.value = saved;
        AudioListener.volume = saved / 100f; //슬라이더 value의 범위는 0~100, AudioListener의 범위는 0~1이기 때문에, 100을 나눠줘야 함. //지원 해상도 목록 가져오기
        //초기 해상도 설정
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
        resolutionDropdown.ClearOptions(); //기존 옵션이 남아있으면 중복으로 쌓이기 때문에 먼저 비워줌
        resolutionDropdown.AddOptions(options);
        //초기 전체 화면 설정
        modeindex = PlayerPrefs.GetInt("ScreenMode", 1);
        screenModeDropdown.value = modeindex;
        screenModeDropdown.RefreshShownValue(); //지금 value에 맞는 텍스트로 화면 갱신
        int w = PlayerPrefs.GetInt("ResW", Screen.width); // 저장했던 해상도 불러오기
        int h = PlayerPrefs.GetInt("ResH", Screen.height);
        bool fullscreen = modeindex != 2; // 창모드면 false
        Screen.SetResolution(w, h, fullscreen);
        // 저장된 해상도와 일치하는 값을 드롭다운에 표시
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

    public void OnChangeSlider()//슬라이더의 값 표시
    {
        int volumeText = Mathf.RoundToInt(volumeSlider.value);//float를 정수로 변환
        VolumeMessage.text = $"{volumeText}";

    }

}
