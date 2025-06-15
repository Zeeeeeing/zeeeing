using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeeeingGaze;

public class FeverUI : MonoBehaviour
{
    [Header("Fever Gauge UI")]
    [SerializeField] private Image feverGaugeBG;
    [SerializeField] private Image feverGaugeFill;
    [SerializeField] private TextMeshProUGUI feverPercentageText;
    [SerializeField] private Gradient feverGaugeGradient;

    [Header("Fever Mode UI")]
    [SerializeField] private GameObject feverModeIndicator;
    [SerializeField] private TextMeshProUGUI feverModeText;
    [SerializeField] private Image feverModeBackground;
    
    // [중요] 이제 프리팹이 아닌, 씬에 배치된 파티클 시스템을 직접 연결합니다.
    [SerializeField] private ParticleSystem feverModeParticles; 

    // 인스턴스화가 필요 없으므로 playerBody, offset 관련 변수 제거 가능
    // 단, 파티클이 플레이어를 따라다니지 않는 UI에 붙어있다면 위치 동기화 코드가 필요할 수 있습니다.
    // 여기서는 파티클이 플레이어의 자식이라고 가정하고 관련 코드를 제거합니다.

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 0.0f;
    [SerializeField] private float pulseIntensity = 0.0f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip feverActivationSound;
    [SerializeField] private AudioClip feverLoopSound;
    [SerializeField] private AudioClip feverEndSound;

    // 내부 상태
    private float currentGaugeValue = 0f;
    private float targetGaugeValue = 0f;
    private bool isFeverModeActive = false;
    private Coroutine feverAnimationCoroutine;
    private Coroutine gaugeAnimationCoroutine;

    // 기본 색상 저장
    private Color originalTextColor;
    private Color originalBackgroundColor;

    private void Awake()
    {
        InitializeUI();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 시작할 때 파티클이 꺼져 있는지 확인
        if (feverModeParticles != null)
        {
            feverModeParticles.gameObject.SetActive(false);
        }

        SetFeverGauge(0f);
        SetFeverModeActive(false);
    }
    
    private void InitializeUI()
    {
        if (feverModeText != null)
            originalTextColor = feverModeText.color;
            
        if (feverModeBackground != null)
            originalBackgroundColor = feverModeBackground.color;
        
        // (이하 그라디언트 설정 코드는 동일)
        if (feverGaugeGradient.colorKeys.Length == 0)
        {
            feverGaugeGradient = new Gradient();
            feverGaugeGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.green, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
        }
    }

    // 피버 모드 활성화/비활성화
    public void SetFeverModeActive(bool active)
    {
        if (isFeverModeActive == active) return;
        
        isFeverModeActive = active;
        
        if (feverModeIndicator != null)
            feverModeIndicator.SetActive(active);
        
        if (active)
        {
            StartFeverModeEffects();
            //PlayFeverSound(feverActivationSound);
            
            // 피버 모드 진입 오디오 + 햅틱
            if (AudioHapticManager.Instance != null)
            {
                AudioHapticManager.Instance.PlayFeverModeEnter();
            }
        }
        else
        {
            StopFeverModeEffects();
            //PlayFeverSound(feverEndSound);

            if (AudioHapticManager.Instance != null)
            {
                AudioHapticManager.Instance.TransitionBackToGameplayBGM();
            }
        }
    }

    // 피버 모드 시각 효과 시작
    private void StartFeverModeEffects()
    {
        if (feverAnimationCoroutine != null)
            StopCoroutine(feverAnimationCoroutine);
        
        feverAnimationCoroutine = StartCoroutine(FeverModeAnimation());
        
        // ✨ 파티클 게임 오브젝트를 활성화하여 켜기
        if (feverModeParticles != null)
        {
            feverModeParticles.gameObject.SetActive(true);
            // 필요시 Play()를 명시적으로 호출할 수도 있습니다. SetActive(true) 시 자동으로 재생되도록 설정된 경우가 많습니다.
            // feverModeParticles.Play(); 
            // Debug.Log("[FeverUI] 🔥 Fever 파티클 활성화!");
        }
        
        // (이하 오디오 관련 코드는 동일)
        if (audioSource != null && feverLoopSound != null)
        {
            audioSource.clip = feverLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // 피버 모드 시각 효과 중지
    private void StopFeverModeEffects()
    {
        if (feverAnimationCoroutine != null)
        {
            StopCoroutine(feverAnimationCoroutine);
            feverAnimationCoroutine = null;
        }
        
        // ✨ 파티클 게임 오브젝트를 비활성화하여 끄기
        if (feverModeParticles != null)
        {
            feverModeParticles.gameObject.SetActive(false);
            // Debug.Log("[FeverUI] ❄️ Fever 파티클 비활성화!");
        }
        
        // (이하 오디오 및 색상 복구 코드는 동일)
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
        
        RestoreOriginalColors();
    }
    
    private void OnDestroy()
    {
        // 이제 파괴할 인스턴스가 없으므로 관련 코드가 필요 없습니다.
        StopAllCoroutines();
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // --- 이하 나머지 코드는 이전과 거의 동일합니다 ---

    public void SetFeverGauge(float value)
    {
        targetGaugeValue = Mathf.Clamp01(value);

        // GameObject가 활성화되어 있을 때만 코루틴 시작
        if (gameObject.activeInHierarchy)
        {
            if (gaugeAnimationCoroutine != null) StopCoroutine(gaugeAnimationCoroutine);
            gaugeAnimationCoroutine = StartCoroutine(AnimateGaugeToTarget());
        }
        else
        {
            // 비활성화 상태라면 즉시 값 설정
            currentGaugeValue = targetGaugeValue;
            UpdateGaugeVisuals();
        }

        if (feverPercentageText != null)
            feverPercentageText.text = $"{Mathf.RoundToInt(targetGaugeValue * 100)}%";
    }

    private void OnEnable()
    {
        // GameObject가 활성화될 때 필요한 업데이트 수행
        if (Mathf.Abs(currentGaugeValue - targetGaugeValue) > 0.01f)
        {
            if (gaugeAnimationCoroutine != null) StopCoroutine(gaugeAnimationCoroutine);
            gaugeAnimationCoroutine = StartCoroutine(AnimateGaugeToTarget());
        }
    }

    private IEnumerator AnimateGaugeToTarget()
    {
        float animationDuration = 0.5f;
        float startValue = currentGaugeValue;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            currentGaugeValue = Mathf.Lerp(startValue, targetGaugeValue, pulseCurve.Evaluate(elapsed / animationDuration));
            UpdateGaugeVisuals();
            yield return null;
        }
        currentGaugeValue = targetGaugeValue;
        UpdateGaugeVisuals();
        gaugeAnimationCoroutine = null;
    }

    private void UpdateGaugeVisuals()
    {
        if (feverGaugeFill != null)
        {
            feverGaugeFill.fillAmount = currentGaugeValue;
            feverGaugeFill.color = feverGaugeGradient.Evaluate(currentGaugeValue);
        }
        if (currentGaugeValue > 0.8f && !isFeverModeActive)
        {
            float pulseValue = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity + 1f;
            if (feverGaugeFill != null) feverGaugeFill.transform.localScale = Vector3.one * pulseValue;
        }
        else
        {
            if (feverGaugeFill != null) feverGaugeFill.transform.localScale = Vector3.one;
        }
    }
    
    private IEnumerator FeverModeAnimation()
    {
        while (isFeverModeActive)
        {
            float time = Time.time * pulseSpeed;
            if (feverModeText != null)
            {
                float alpha = Mathf.Sin(time) * 0.3f + 0.7f;
                Color textColor = originalTextColor;
                textColor.a = alpha;
                feverModeText.color = textColor;
                float scale = Mathf.Sin(time * 0.5f) * 0.1f + 1f;
                feverModeText.transform.localScale = Vector3.one * scale;
            }
            if (feverModeBackground != null)
            {
                float intensity = Mathf.Sin(time * 1.5f) * 0.2f + 0.8f;
                Color bgColor = Color.yellow * intensity;
                bgColor.a = originalBackgroundColor.a;
                feverModeBackground.color = bgColor;
            }
            if (feverGaugeFill != null)
            {
                float glowIntensity = Mathf.Sin(time * 2f) * 0.3f + 1f;
                feverGaugeFill.color = Color.white * glowIntensity;
            }
            yield return null;
        }
    }

    private void RestoreOriginalColors()
    {
        if (feverModeText != null)
        {
            feverModeText.color = originalTextColor;
            feverModeText.transform.localScale = Vector3.one;
        }
        if (feverModeBackground != null)
        {
            feverModeBackground.color = originalBackgroundColor;
        }
        if (feverGaugeFill != null)
        {
            feverGaugeFill.color = feverGaugeGradient.Evaluate(currentGaugeValue);
        }
    }

    public void SetFeverTimer(float remainingTime, float totalTime)
    {
        if (!isFeverModeActive) return;
        if (feverModeText != null) feverModeText.text = $"{Mathf.CeilToInt(remainingTime)}초";

        // GameObject 활성화 상태 확인
        if (gameObject.activeInHierarchy)
        {
            SetFeverGauge(remainingTime / totalTime);
        }
        else
        {
            // 비활성화 상태라면 직접 값만 설정
            targetGaugeValue = Mathf.Clamp01(remainingTime / totalTime);
            currentGaugeValue = targetGaugeValue;
            UpdateGaugeVisuals();
        }
    }

    private void PlayFeverSound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
    
    public void OnFeverGaugeChanged(float newValue) => SetFeverGauge(newValue);
    public void OnFeverModeStart() => SetFeverModeActive(true);
    public void OnFeverModeEnd() => SetFeverModeActive(false);
    public void OnFeverTimerUpdate(float remainingTime, float totalTime) => SetFeverTimer(remainingTime, totalTime);
    
    private void Update()
    {
        // 파티클 오브젝트가 플레이어의 자식으로 설정되었다면, Update에서 위치를 계속 동기화할 필요가 없습니다.
        // 부모-자식 관계에 의해 자동으로 위치가 갱신됩니다.
        // 따라서 관련 코드를 제거하여 Update 함수를 더 가볍게 만듭니다.

        if (!isFeverModeActive && gaugeAnimationCoroutine == null)
        {
            UpdateGaugeVisuals();
        }
    }
}