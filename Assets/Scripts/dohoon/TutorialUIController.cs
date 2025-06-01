using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI tutorialTitle;
    [SerializeField] private TextMeshProUGUI tutorialInstructions;
    [SerializeField] private Slider tutorialProgress;
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float typewriterSpeed = 0.03f;
    
    [Header("Content")]
    [SerializeField] private string titleText = "눈빛 보내기 VR";
    [SerializeField] private string instructionsText = 
        "눈빛 보내기 VR에 오신 것을 환영합니다!\n\n" +
        "🎯 <color=#FFD700>목표</color>: NPC들과 시선을 맞추고 감정을 표현하여 호감도를 쌓으세요\n\n" +
        "🎮 <color=#00FFFF>조작법</color>:\n" +
        "• <color=#FFFF00>오른쪽 트리거</color>: 행복 감정 💛\n" +
        "• <color=#0080FF>왼쪽 트리거</color>: 슬픔 감정 💙\n" +
        "• <color=#FF4040>오른쪽 그립</color>: 분노 감정 ❤️\n" +
        "• <color=#CCCCCC>왼쪽 그립</color>: 중립 감정 🤍\n\n" +
        "👀 NPC를 바라보고 적절한 감정을 표현하세요!\n" +
        "일부 NPC는 미니게임을 통해 꼬셔야 합니다.\n\n" +
        "<color=#90EE90>준비되셨나요?</color>";
    
    private GameFlowManager gameFlowManager;
    private bool isActive = false;
    private Coroutine currentAnimation;
    
    private void Awake()
    {
        gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        
        // 초기 상태 설정
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // 버튼 이벤트 연결
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }
    
    public void ShowTutorial(float duration)
    {
        if (isActive) return;
        
        isActive = true;
        gameObject.SetActive(true);
        
        // 애니메이션 시작
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(ShowTutorialAnimation(duration));
    }
    
    public void HideTutorial()
    {
        if (!isActive) return;
        
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(HideTutorialAnimation());
    }
    
    private IEnumerator ShowTutorialAnimation(float duration)
    {
        // 페이드 인
        yield return StartCoroutine(FadeIn());
        
        // 타이틀 설정
        if (tutorialTitle != null)
        {
            tutorialTitle.text = titleText;
        }
        
        // 타이프라이터 효과로 설명 텍스트 표시
        if (tutorialInstructions != null)
        {
            yield return StartCoroutine(TypewriterEffect(instructionsText));
        }
        
        // 진행률 바 업데이트
        float elapsed = 0f;
        while (elapsed < duration && isActive)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            if (tutorialProgress != null)
            {
                tutorialProgress.value = progress;
            }
            
            yield return null;
        }
        
        // 자동으로 숨기기
        if (isActive)
        {
            OnTutorialComplete();
        }
    }
    
    private IEnumerator HideTutorialAnimation()
    {
        yield return StartCoroutine(FadeOut());
        
        isActive = false;
        gameObject.SetActive(false);
    }
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    private IEnumerator TypewriterEffect(string text)
    {
        if (tutorialInstructions == null) yield break;
        
        tutorialInstructions.text = "";
        
        for (int i = 0; i <= text.Length; i++)
        {
            tutorialInstructions.text = text.Substring(0, i);
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
    
    private void OnSkipClicked()
    {
        OnTutorialComplete();
    }
    
    private void OnTutorialComplete()
    {
        if (gameFlowManager != null)
        {
            // GameFlowManager에게 튜토리얼 완료 알림
            gameFlowManager.GetType().GetMethod("CompleteTutorial")?.Invoke(gameFlowManager, null);
        }
        
        HideTutorial();
    }
    
    public void UpdateProgress(float progress)
    {
        if (tutorialProgress != null)
        {
            tutorialProgress.value = Mathf.Clamp01(progress);
        }
    }
    
    public bool IsActive()
    {
        return isActive;
    }
}