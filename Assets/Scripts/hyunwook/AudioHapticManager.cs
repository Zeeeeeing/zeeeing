using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioHapticManager : MonoBehaviour
{
    #region Singleton
    private static AudioHapticManager _instance;
    public static AudioHapticManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioHapticManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioHapticManager");
                    _instance = go.AddComponent<AudioHapticManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("Audio Sources")]
    public AudioSource bgmSource; // BGM 전용
    public AudioSource sfxSource; // 효과음 전용
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    [Header("BGM Clips")]
    public AudioClip tutorialBGM;
    public AudioClip gameplayBGM;
    public AudioClip feverModeBGM;
    public AudioClip endingBGM;

    [Header("SFX Clips")]
    public AudioClip startButtonSFX;
    public AudioClip feverModeEnterSFX;
    public AudioClip seductionSuccessSFX;
    public AudioClip eyeVFXSFX;
    public AudioClip raycastFailSFX;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public float crossFadeDuration = 2f; // BGM 전환 시간

    [Header("Haptic Settings")]
    [Range(0f, 1f)] public float hapticIntensity = 0.8f;
    public float hapticDuration = 0.3f;

    // 현재 재생 중인 BGM 추적
    private AudioClip currentBGM;
    private Coroutine crossFadeCoroutine;

    private void Awake()
    {
        // 싱글톤 패턴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        // BGM AudioSource 설정
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.spatialBlend = 0f; // 2D 사운드

        // SFX AudioSource 설정
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.spatialBlend = 0f; // 2D 사운드

        // Debug.Log("[AudioHapticManager] 오디오 소스 초기화 완료");
    }

    #region BGM Control
    /// <summary>
    /// BGM을 즉시 재생 (페이드 없이)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (crossFadeCoroutine != null)
        {
            StopCoroutine(crossFadeCoroutine);
        }

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
        currentBGM = clip;

        // Debug.Log($"[AudioHapticManager] BGM 재생: {clip.name}");
    }

    /// <summary>
    /// BGM을 크로스페이드로 전환 (자연스러운 전환)
    /// </summary>
    public void CrossFadeToBGM(AudioClip newClip, float fadeDuration = -1f)
    {
        if (newClip == null || newClip == currentBGM) return;

        if (fadeDuration < 0) fadeDuration = crossFadeDuration;

        if (crossFadeCoroutine != null)
        {
            StopCoroutine(crossFadeCoroutine);
        }

        crossFadeCoroutine = StartCoroutine(CrossFadeCoroutine(newClip, fadeDuration));
    }

    private IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
    {
        float startVolume = bgmSource.volume;

        // 페이드 아웃
        float timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2f));
            yield return null;
        }

        // 새 클립으로 교체
        bgmSource.clip = newClip;
        bgmSource.Play();
        currentBGM = newClip;

        // 페이드 인
        timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, timer / (duration / 2f));
            yield return null;
        }

        bgmSource.volume = bgmVolume;
        // Debug.Log($"[AudioHapticManager] BGM 크로스페이드 완료: {newClip.name}");
    }

    public void StopBGM(bool fadeOut = true)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutBGM());
        }
        else
        {
            bgmSource.Stop();
            currentBGM = null;
        }
    }

    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < crossFadeDuration / 2f)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / (crossFadeDuration / 2f));
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
        currentBGM = null;
    }
    #endregion

    #region SFX Control
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            // Debug.Log($"[AudioHapticManager] SFX 재생: {clip.name}");
        }
    }

    // 특정 효과음 재생 메서드들
    public void PlayStartButtonSFX() => PlaySFX(startButtonSFX);
    public void PlayFeverModeEnterSFX() => PlaySFX(feverModeEnterSFX);
    public void PlaySeductionSuccessSFX() => PlaySFX(seductionSuccessSFX);
    public void PlayEyeVFXSFX() => PlaySFX(eyeVFXSFX);
    public void PlayRaycastFailSFX() => PlaySFX(raycastFailSFX);
    #endregion

    #region BGM Transition Methods
    public void PlayTutorialBGM() => PlayBGM(tutorialBGM);
    public void TransitionToGameplayBGM() => CrossFadeToBGM(gameplayBGM);
    public void TransitionToFeverModeBGM() => CrossFadeToBGM(feverModeBGM);
    public void TransitionBackToGameplayBGM() => CrossFadeToBGM(gameplayBGM);
    public void TransitionToEndingBGM() => CrossFadeToBGM(endingBGM);
    #endregion

    #region Haptic Control
    /// <summary>
    /// 기본 햅틱 피드백
    /// </summary>
    public void PlayHapticFeedback(float intensity = -1f, float duration = -1f)
    {
        if (intensity < 0) intensity = hapticIntensity;
        if (duration < 0) duration = hapticDuration;

        // Meta Quest Pro 컨트롤러 햅틱 (양손)
        PlayControllerHaptic(OVRInput.Controller.LTouch, intensity, duration);
        PlayControllerHaptic(OVRInput.Controller.RTouch, intensity, duration);
    }

    /// <summary>
    /// 특정 컨트롤러에만 햅틱 피드백
    /// </summary>
    public void PlayControllerHaptic(OVRInput.Controller controller, float intensity = -1f, float duration = -1f)
    {
        if (intensity < 0) intensity = hapticIntensity;
        if (duration < 0) duration = hapticDuration;

        StartCoroutine(HapticCoroutine(controller, intensity, duration));
    }

    private IEnumerator HapticCoroutine(OVRInput.Controller controller, float intensity, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            OVRInput.SetControllerVibration(intensity, intensity, controller);
            timer += Time.deltaTime;
            yield return null;
        }

        // 햅틱 중지
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    /// <summary>
    /// 유혹 성공 시 특별한 햅틱 패턴
    /// </summary>
    public void PlaySeductionSuccessHaptic()
    {
        StartCoroutine(SeductionSuccessHapticPattern());
    }

    private IEnumerator SeductionSuccessHapticPattern()
    {
        // 짧은 진동 3번
        for (int i = 0; i < 3; i++)
        {
            PlayHapticFeedback(0.8f, 0.1f);
            yield return new WaitForSeconds(0.15f);
        }

        // 긴 진동 1번
        yield return new WaitForSeconds(0.1f);
        PlayHapticFeedback(1.0f, 0.5f);
    }
    #endregion

    #region Combined Audio + Haptic Effects
    /// <summary>
    /// 유혹 성공 시 오디오와 햅틱을 함께 재생
    /// </summary>
    public void PlaySeductionSuccess()
    {
        PlaySeductionSuccessSFX();
        PlaySeductionSuccessHaptic();
        // Debug.Log("[AudioHapticManager] 유혹 성공 효과 재생");
    }

    /// <summary>
    /// 피버 모드 진입 시 오디오와 햅틱을 함께 재생
    /// </summary>
    public void PlayFeverModeEnter()
    {
        PlayFeverModeEnterSFX();
        TransitionToFeverModeBGM();
        PlayHapticFeedback(1.0f, 0.6f);
        // Debug.Log("[AudioHapticManager] 피버 모드 진입 효과 재생");
    }
    #endregion

    #region Volume Control
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetHapticIntensity(float intensity)
    {
        hapticIntensity = Mathf.Clamp01(intensity);
    }
    #endregion
}