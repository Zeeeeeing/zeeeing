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
    public AudioSource bgmSource; // BGM ����
    public AudioSource sfxSource; // ȿ���� ����
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
    public float crossFadeDuration = 2f; // BGM ��ȯ �ð�

    [Header("Haptic Settings")]
    [Range(0f, 1f)] public float hapticIntensity = 0.8f;
    public float hapticDuration = 0.3f;

    // ���� ��� ���� BGM ����
    private AudioClip currentBGM;
    private Coroutine crossFadeCoroutine;

    private void Awake()
    {
        // �̱��� ����
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
        // BGM AudioSource ����
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.spatialBlend = 0f; // 2D ����

        // SFX AudioSource ����
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.spatialBlend = 0f; // 2D ����

        // Debug.Log("[AudioHapticManager] ����� �ҽ� �ʱ�ȭ �Ϸ�");
    }

    #region BGM Control
    /// <summary>
    /// BGM�� ��� ��� (���̵� ����)
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

        // Debug.Log($"[AudioHapticManager] BGM ���: {clip.name}");
    }

    /// <summary>
    /// BGM�� ũ�ν����̵�� ��ȯ (�ڿ������� ��ȯ)
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

        // ���̵� �ƿ�
        float timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / (duration / 2f));
            yield return null;
        }

        // �� Ŭ������ ��ü
        bgmSource.clip = newClip;
        bgmSource.Play();
        currentBGM = newClip;

        // ���̵� ��
        timer = 0f;
        while (timer < duration / 2f)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, timer / (duration / 2f));
            yield return null;
        }

        bgmSource.volume = bgmVolume;
        // Debug.Log($"[AudioHapticManager] BGM ũ�ν����̵� �Ϸ�: {newClip.name}");
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
            // Debug.Log($"[AudioHapticManager] SFX ���: {clip.name}");
        }
    }

    // Ư�� ȿ���� ��� �޼����
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
    /// �⺻ ��ƽ �ǵ��
    /// </summary>
    public void PlayHapticFeedback(float intensity = -1f, float duration = -1f)
    {
        if (intensity < 0) intensity = hapticIntensity;
        if (duration < 0) duration = hapticDuration;

        // Meta Quest Pro ��Ʈ�ѷ� ��ƽ (���)
        PlayControllerHaptic(OVRInput.Controller.LTouch, intensity, duration);
        PlayControllerHaptic(OVRInput.Controller.RTouch, intensity, duration);
    }

    /// <summary>
    /// Ư�� ��Ʈ�ѷ����� ��ƽ �ǵ��
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

        // ��ƽ ����
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    /// <summary>
    /// ��Ȥ ���� �� Ư���� ��ƽ ����
    /// </summary>
    public void PlaySeductionSuccessHaptic()
    {
        StartCoroutine(SeductionSuccessHapticPattern());
    }

    private IEnumerator SeductionSuccessHapticPattern()
    {
        // ª�� ���� 3��
        for (int i = 0; i < 3; i++)
        {
            PlayHapticFeedback(0.8f, 0.1f);
            yield return new WaitForSeconds(0.15f);
        }

        // �� ���� 1��
        yield return new WaitForSeconds(0.1f);
        PlayHapticFeedback(1.0f, 0.5f);
    }
    #endregion

    #region Combined Audio + Haptic Effects
    /// <summary>
    /// ��Ȥ ���� �� ������� ��ƽ�� �Բ� ���
    /// </summary>
    public void PlaySeductionSuccess()
    {
        PlaySeductionSuccessSFX();
        PlaySeductionSuccessHaptic();
        // Debug.Log("[AudioHapticManager] ��Ȥ ���� ȿ�� ���");
    }

    /// <summary>
    /// �ǹ� ��� ���� �� ������� ��ƽ�� �Բ� ���
    /// </summary>
    public void PlayFeverModeEnter()
    {
        PlayFeverModeEnterSFX();
        TransitionToFeverModeBGM();
        PlayHapticFeedback(1.0f, 0.6f);
        // Debug.Log("[AudioHapticManager] �ǹ� ��� ���� ȿ�� ���");
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