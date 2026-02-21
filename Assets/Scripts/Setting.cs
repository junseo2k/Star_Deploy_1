using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshPro VolumeMessage;
    [SerializeField] private Slider volumeSlider;
    void Start()
    {
        float saved = PlayerPrefs.GetFloat("Volume", 50f); // 초기 음량 설정
        volumeSlider.value = saved;
        AudioListener.volume = saved / 100f; //슬라이더 value의 범위는 0~100, AudioListener의 범위는 0~1이기 때문에, 100을 나눠줘야 함.      
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ApplyOnClick()
    {
        float applyvolume = volumeSlider.value;
        float applysound = applyvolume / 100f;
        AudioListener.volume = applysound;


        PlayerPrefs.SetFloat("Volume", applysound);
        PlayerPrefs.Save(); //변경된 값을 기록함

    }
    public void OnChangeSlider()
    {
        int volumeText = Mathf.RoundToInt(volumeSlider.value);//float를 정수로 변환
        VolumeMessage.text = "volumeText";

    }
}
