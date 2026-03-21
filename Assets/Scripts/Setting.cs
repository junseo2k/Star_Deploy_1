using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 게임 설정(음량, 해상도, 화면 모드)을 관리하는 클래스
public class Setting : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI VolumeMessage; // 현재 음량 수치를 표시하는 텍스트 (외부 접근 불필요 → SerializeField 사용)
    [SerializeField] private Slider volumeSlider; // 음량 조절 UI 슬라이더
    public TMP_Dropdown resolutionDropdown; // 해상도 선택 드롭다운
    public TMP_Dropdown screenModeDropdown; // 전체화면/전체 창/창 모드 전환 드롭다운
    private int modeindex; // 현재 선택된 화면 모드 인덱스 (0: 전체화면, 1: 전체 창, 2: 창 모드)
    private List<Resolution> resolutions; // 유니티에서 지원하는 해상도 목록

    // 적용 버튼 클릭 시 호출 — 음량, 화면 모드, 해상도를 PlayerPrefs에 저장하고 즉시 적용
    public void ApplyOnClick()
    {
        // 음량 설정 및 저장 (슬라이더 범위 0~100 → AudioListener 범위 0~1로 변환)
        float applyvolume = volumeSlider.value;
        float applysound = applyvolume / 100f;
        AudioListener.volume = applysound;
        PlayerPrefs.SetFloat("Volume", applyvolume);
        Debug.Log("현재 볼륨 :" + applyvolume);

        // 화면 모드 설정 및 저장
        modeindex = screenModeDropdown.value;
        PlayerPrefs.SetInt("ScreenMode", modeindex);
        switch (modeindex) // 0, 1, 2에 따라 화면 모드 전환
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

        // 해상도 설정 및 저장
        int index = resolutionDropdown.value;
        Resolution selected = resolutions[index];
        bool fullscreen = modeindex != 2; // 창 모드가 아닐 경우 fullscreen 처리
        Screen.SetResolution(selected.width, selected.height, fullscreen);
        PlayerPrefs.SetInt("ResW", selected.width);
        PlayerPrefs.SetInt("ResH", selected.height);
#if UNITY_EDITOR // 에디터에서만 출력 (빌드 시 제거됨)
        Debug.Log($"선택한 해상도: {selected.width}x{selected.height}");
        Debug.Log($"Screen.width/height: {Screen.width}x{Screen.height}");
        Debug.Log($"currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        Debug.Log("현재 해상도 :" + Screen.width + "x" + Screen.height);
#endif

        PlayerPrefs.Save(); // 변경된 설정을 디스크에 저장
    }

    void Start()
    {
        // 저장된 음량 불러와서 슬라이더 및 AudioListener에 반영
        float saved = PlayerPrefs.GetFloat("Volume", 50f);
        volumeSlider.value = saved;
        AudioListener.volume = saved / 100f; // 슬라이더 0~100 → AudioListener 0~1 변환

        // 지원 해상도 목록 구성 (주사율만 다른 중복 항목 제거)
        List<string> options = new List<string>();
        resolutions = new List<Resolution>();
        string last = "";
        foreach (Resolution res in Screen.resolutions)
        {
            string entry = res.width + "x" + res.height;
            if (entry == last) continue; // 주사율만 다른 중복 해상도 건너뜀
            resolutions.Add(res);
            options.Add(entry);
            last = entry;
        }
        resolutionDropdown.ClearOptions(); // 기존 옵션 제거 후 새 목록 추가 (중복 방지)
        resolutionDropdown.AddOptions(options);

        // 저장된 화면 모드 불러와서 드롭다운에 반영
        modeindex = PlayerPrefs.GetInt("ScreenMode", 1);
        screenModeDropdown.value = modeindex;
        screenModeDropdown.RefreshShownValue(); // 현재 value에 맞는 텍스트로 UI 갱신

        // 저장된 해상도 불러와서 실제 해상도와 드롭다운에 반영
        int w = PlayerPrefs.GetInt("ResW", Screen.width);
        int h = PlayerPrefs.GetInt("ResH", Screen.height);
        bool fullscreen = modeindex != 2; // 창 모드면 false
        Screen.SetResolution(w, h, fullscreen);

        // 저장된 해상도와 일치하는 항목을 드롭다운에서 선택 상태로 설정
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == w && resolutions[i].height == h)
            {
                resolutionDropdown.value = i;
                break;
            }
        }
        resolutionDropdown.RefreshShownValue();
    }

    void Update()
    {

    }

    // 슬라이더 값이 변경될 때 호출 — 현재 음량 수치를 정수로 변환하여 텍스트에 표시
    public void OnChangeSlider()
    {
        int volumeText = Mathf.RoundToInt(volumeSlider.value); // float → 정수 변환
        VolumeMessage.text = $"{volumeText}";
    }
}
