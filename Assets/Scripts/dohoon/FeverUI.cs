using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private ParticleSystem feverModeParticles;
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.3f;
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
        // 기본 설정
        InitializeUI();
        
        // 오디오 소스 설정
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // 초기 상태 설정
        SetFeverGauge(0f);
        SetFeverModeActive(false);
    }
    
    private void Start()
    {
        // MiniGameManager 이벤트 구독
        MiniGameManager miniGameManager = FindAnyObjectByType<MiniGameManager>();
        if (miniGameManager != null)
        {
            // 피버 게이지 업데이트 이벤트 구독 (리플렉션으로 내부 이벤트 접근)
            // 실제로는 MiniGameManager에서 직접 이 UI를 참조하도록 설정해야 함
        }
    }
    
    private void InitializeUI()
    {
        // 기본 색상 저장
        if (feverModeText != null)
            originalTextColor = feverModeText.color;
            
        if (feverModeBackground != null)
            originalBackgroundColor = feverModeBackground.color;
        
        // 그라디언트 설정 (없으면 기본값)
        if (feverGaugeGradient.colorKeys.Length == 0)
        {
            feverGaugeGradient = new Gradient();
            feverGaugeGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.red, 0.0f), 
                    new GradientColorKey(Color.yellow, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 1.0f) 
                }
            );
        }
    }
    
    // 피버 게이지 설정 (0.0 ~ 1.0)
    public void SetFeverGauge(float value)
    {
        targetGaugeValue = Mathf.Clamp01(value);
        
        // 부드러운 애니메이션으로 게이지 업데이트
        if (gaugeAnimationCoroutine != null)
            StopCoroutine(gaugeAnimationCoroutine);
            
        gaugeAnimationCoroutine = StartCoroutine(AnimateGaugeToTarget());
        
        // 퍼센티지 텍스트 업데이트
        if (feverPercentageText != null)
        {
            feverPercentageText.text = $"{Mathf.RoundToInt(targetGaugeValue * 100)}%";
        }
    }
    
    // 게이지 애니메이션 코루틴
    private IEnumerator AnimateGaugeToTarget()
    {
        float animationDuration = 0.5f;
        float startValue = currentGaugeValue;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            currentGaugeValue = Mathf.Lerp(startValue, targetGaugeValue, pulseCurve.Evaluate(t));
            
            // UI 업데이트
            UpdateGaugeVisuals();
            
            yield return null;
        }
        
        currentGaugeValue = targetGaugeValue;
        UpdateGaugeVisuals();
        
        gaugeAnimationCoroutine = null;
    }
    
    // 게이지 시각적 업데이트
    private void UpdateGaugeVisuals()
    {
        if (feverGaugeFill != null)
        {
            feverGaugeFill.fillAmount = currentGaugeValue;
            feverGaugeFill.color = feverGaugeGradient.Evaluate(currentGaugeValue);
        }
        
        // 게이지가 거의 찰 때 펄스 효과
        if (currentGaugeValue > 0.8f && !isFeverModeActive)
        {
            float pulseValue = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity + 1f;
            if (feverGaugeFill != null)
            {
                feverGaugeFill.transform.localScale = Vector3.one * pulseValue;
            }
        }
        else
        {
            if (feverGaugeFill != null)
            {
                feverGaugeFill.transform.localScale = Vector3.one;
            }
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
            PlayFeverSound(feverActivationSound);
        }
        else
        {
            StopFeverModeEffects();
            PlayFeverSound(feverEndSound);
        }
    }
    
    // 피버 모드 시각 효과 시작
    private void StartFeverModeEffects()
    {
        // 기존 애니메이션 중지
        if (feverAnimationCoroutine != null)
            StopCoroutine(feverAnimationCoroutine);
        
        // 새 애니메이션 시작
        feverAnimationCoroutine = StartCoroutine(FeverModeAnimation());
        
        // 파티클 효과 시작
        if (feverModeParticles != null)
        {
            feverModeParticles.Play();
        }
        
        // 사운드 루프 시작
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
        // 애니메이션 중지
        if (feverAnimationCoroutine != null)
        {
            StopCoroutine(feverAnimationCoroutine);
            feverAnimationCoroutine = null;
        }
        
        // 파티클 효과 중지
        if (feverModeParticles != null)
        {
            feverModeParticles.Stop();
        }
        
        // 사운드 중지
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
        
        // UI 원래 상태로 복구
        RestoreOriginalColors();
    }
    
    // 피버 모드 애니메이션 코루틴
    private IEnumerator FeverModeAnimation()
    {
        while (isFeverModeActive)
        {
            float time = Time.time * pulseSpeed;
            
            // 텍스트 펄스 효과
            if (feverModeText != null)
            {
                float alpha = Mathf.Sin(time) * 0.3f + 0.7f;
                Color textColor = originalTextColor;
                textColor.a = alpha;
                feverModeText.color = textColor;
                
                // 스케일 효과
                float scale = Mathf.Sin(time * 0.5f) * 0.1f + 1f;
                feverModeText.transform.localScale = Vector3.one * scale;
            }
            
            // 배경 펄스 효과
            if (feverModeBackground != null)
            {
                float intensity = Mathf.Sin(time * 1.5f) * 0.2f + 0.8f;
                Color bgColor = Color.yellow * intensity;
                bgColor.a = originalBackgroundColor.a;
                feverModeBackground.color = bgColor;
            }
            
            // 게이지 특수 효과
            if (feverGaugeFill != null)
            {
                float glowIntensity = Mathf.Sin(time * 2f) * 0.3f + 1f;
                feverGaugeFill.color = Color.white * glowIntensity;
            }
            
            yield return null;
        }
    }
    
    // 원래 색상으로 복구
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
    
    // 피버 타이머 표시 (남은 시간)
    public void SetFeverTimer(float remainingTime, float totalTime)
    {
        if (!isFeverModeActive) return;
        
        if (feverModeText != null)
        {
            feverModeText.text = $"FEVER!\n{Mathf.CeilToInt(remainingTime)}초";
        }
        
        // 타이머에 따른 게이지 업데이트
        float timerProgress = remainingTime / totalTime;
        SetFeverGauge(timerProgress);
    }
    
    // 오디오 재생
    private void PlayFeverSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // 외부에서 호출 가능한 메서드들
    public void OnFeverGaugeChanged(float newValue)
    {
        SetFeverGauge(newValue);
    }
    
    public void OnFeverModeStart()
    {
        SetFeverModeActive(true);
    }
    
    public void OnFeverModeEnd()
    {
        SetFeverModeActive(false);
    }
    
    public void OnFeverTimerUpdate(float remainingTime, float totalTime)
    {
        SetFeverTimer(remainingTime, totalTime);
    }
    
    // 디버그용 메서드들
    [ContextMenu("Test Fever Gauge 50%")]
    private void TestFeverGauge50()
    {
        SetFeverGauge(0.5f);
    }
    
    [ContextMenu("Test Fever Gauge 100%")]
    private void TestFeverGauge100()
    {
        SetFeverGauge(1.0f);
    }
    
    [ContextMenu("Test Fever Mode On")]
    private void TestFeverModeOn()
    {
        SetFeverModeActive(true);
    }
    
    [ContextMenu("Test Fever Mode Off")]
    private void TestFeverModeOff()
    {
        SetFeverModeActive(false);
    }
    
    private void OnDestroy()
    {
        // 코루틴 정리
        StopAllCoroutines();
        
        // 오디오 정리
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    private void Update()
    {
        // 피버 모드가 아닐 때만 일반 게이지 업데이트
        if (!isFeverModeActive && gaugeAnimationCoroutine == null)
        {
            UpdateGaugeVisuals();
        }
    }
}