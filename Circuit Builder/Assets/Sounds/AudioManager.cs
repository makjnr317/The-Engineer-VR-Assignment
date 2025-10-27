using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("----------- Ambience Settings ----------")]
    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 0.2f;
    [SerializeField] private float ambientFadeIn = 3f;

    [Header("---------- Audio CLip ----------")]
    public AudioClip successChime;
    public AudioClip batteryDrop;
    public AudioClip batteryFall;
    public AudioClip buttons;
    public AudioClip buzzerWrong;
    public AudioClip electricMeterbuzz;
    public AudioClip electricSparks;
    public AudioClip fluorescentLights;
    public AudioClip lightSwitch;
    public AudioClip lightSwitchonoff;
    public AudioClip paperHandling;
    public AudioClip papers;
    public AudioClip successChimes;
    public AudioClip switchclick;
    public AudioClip flipPage;
    public AudioClip snap;

    private void Start()
    {
        //Setting up the musicSource for the ambient audio
        musicSource.clip = fluorescentLights;
        musicSource.loop = true; //Ambient sounds should loop
        musicSource.volume = 0f; //Start at zero volume for the fade-in
        musicSource.Play(); //Start playing

        //Start the fade-in coroutine
        StartCoroutine(FadeInMusic(ambientFadeIn, ambientVolume));
    }

    private IEnumerator FadeInMusic(float duration, float targetVolume)
    {
        float currentTime = 0f;
        while (currentTime < duration)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, currentTime / duration);
            currentTime += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
    public void PlayFlipSound()
    {
        SFXSource.PlayOneShot(flipPage);
    }
}
