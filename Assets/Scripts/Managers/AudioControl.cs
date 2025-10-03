using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioControl : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider slider;

    private void Start()
    {
        // Load saved volume or set to default (1f)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        slider.value = savedVolume;
        SetVolume(savedVolume);

        // Add listener
        slider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        // AudioMixers use decibels, so we convert [0–1] to [-80, 0] dB
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);

        // Save setting
        PlayerPrefs.SetFloat("MasterVolume", value);
    }
}

