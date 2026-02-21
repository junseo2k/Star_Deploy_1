using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Slider volumeSlider;
    void Start()
    {
        float saved = PlayerPrefs.GetFloat("Volume", 50f);
        volumeSlider.value = saved;
        AudioListener.volume = saved / 100f;       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SoundControl(float value)
    {
        float SoundSaved = volumeSlider.value;
        AudioListener.volume = SoundSaved / 100f;

        PlayerPrefs.SetFloat("Volume", SoundSaved);
        PlayerPrefs.Save();

    }
}
