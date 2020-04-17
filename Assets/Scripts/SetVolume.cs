using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SetVolume : MonoBehaviour
{
    public AudioMixer mixer;
    private Slider slider;
    public void Start()
    {
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        slider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        slider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

    }
    public void SetMasterVol(float sliderValue)
    {
        mixer.SetFloat("MasterVol", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }

    public void SetMusicVol(float sValue)
    {
        mixer.SetFloat("MusicVol", Mathf.Log10(sValue) * 20);
        PlayerPrefs.SetFloat("MusicVolume", sValue);

    }

    public void SetSFXVol(float sVal)
    {
        mixer.SetFloat("SFXVol", Mathf.Log10(sVal) * 20);
        PlayerPrefs.SetFloat("SFXVolume", sVal);

    }

}