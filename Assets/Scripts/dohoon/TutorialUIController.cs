using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

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
        "<color=#FFD700>목표</color>: NPC들과 시선을 맞추고 감정을 표현하여 호감도를 쌓으세요\n\n" +
        "<color=#00FFFF>조작법</color>:\n" +
        "• <color=#FFFF00>오른쪽 트리거</color>: 행복 감정 \n" +
        "• <color=#0080FF>왼쪽 트리거</color>: 슬픔 감정 \n" +
        "• <color=#FF4040>오른쪽 그립</color>: 분노 감정 \n" +
        "• <color=#CCCCCC>왼쪽 그립</color>: 중립 감정 \n\n" +
        "NPC를 바라보고 적절한 감정을 표현하세요!\n" +
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
            yield return StartCoroutine(TypewriterEffectWithRichText(instructionsText));
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
    
    private IEnumerator TypewriterEffectWithRichText(string text)
    {
        if (tutorialInstructions == null) yield break;
        
        tutorialInstructions.text = "";
        
        // 리치 텍스트 태그와 일반 텍스트를 분리
        var parsedText = ParseRichText(text);
        
        string displayText = "";
        int visibleCharCount = 0;
        
        for (int i = 0; i < parsedText.Count; i++)
        {
            var segment = parsedText[i];
            
            if (segment.isTag)
            {
                // 태그는 즉시 추가 (화면에 표시되지 않음)
                displayText += segment.text;
            }
            else
            {
                // 일반 텍스트는 한 글자씩 추가
                for (int j = 0; j < segment.text.Length; j++)
                {
                    displayText += segment.text[j];
                    tutorialInstructions.text = displayText;
                    
                    // 공백이나 줄바꿈이 아닌 경우에만 딜레이
                    if (segment.text[j] != ' ' && segment.text[j] != '\n')
                    {
                        yield return new WaitForSeconds(typewriterSpeed);
                    }
                }
            }
            
            tutorialInstructions.text = displayText;
        }
    }
    
    // 리치 텍스트를 파싱하여 태그와 일반 텍스트를 분리
    private System.Collections.Generic.List<TextSegment> ParseRichText(string text)
    {
        var segments = new System.Collections.Generic.List<TextSegment>();
        
        // 태그를 찾는 정규식 (열기/닫기 태그 모두)
        Regex tagRegex = new Regex(@"<[^>]+>");
        
        int lastIndex = 0;
        MatchCollection matches = tagRegex.Matches(text);
        
        foreach (Match match in matches)
        {
            // 태그 이전의 일반 텍스트 추가
            if (match.Index > lastIndex)
            {
                string normalText = text.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(normalText))
                {
                    segments.Add(new TextSegment { text = normalText, isTag = false });
                }
            }
            
            // 태그 추가
            segments.Add(new TextSegment { text = match.Value, isTag = true });
            
            lastIndex = match.Index + match.Length;
        }
        
        // 마지막 남은 일반 텍스트 추가
        if (lastIndex < text.Length)
        {
            string remainingText = text.Substring(lastIndex);
            if (!string.IsNullOrEmpty(remainingText))
            {
                segments.Add(new TextSegment { text = remainingText, isTag = false });
            }
        }
        
        return segments;
    }
    
    // 텍스트 세그먼트 구조체
    private struct TextSegment
    {
        public string text;
        public bool isTag;
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