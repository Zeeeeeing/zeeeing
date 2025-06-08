using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeeeingGaze;

public class EndingUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI npcCountText;
    [SerializeField] private TextMeshProUGUI completionRateText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI emotionStatsText;
    [SerializeField] private TextMeshProUGUI thankYouText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float textAnimationDelay = 0.3f;
    [SerializeField] private float numberCountDuration = 2f;
    
    [Header("Grade Colors")]
    [SerializeField] private Color gradeS = new Color(1f, 0.84f, 0f); // 금색
    [SerializeField] private Color gradeA = new Color(0.75f, 0.75f, 0.75f); // 은색
    [SerializeField] private Color gradeB = new Color(0.8f, 0.5f, 0.2f); // 동색
    [SerializeField] private Color gradeC = new Color(0.5f, 0.5f, 0.5f); // 회색
    [SerializeField] private Color gradeD = new Color(0.6f, 0.3f, 0.3f); // 적갈색
    
    private GameFlowManager gameFlowManager;
    private MiniGameManager miniGameManager;
    private PlayerEmotionController playerEmotionController;
    private bool isShowing = false;
    
    private void Awake()
    {
        // 필요한 매니저들 찾기
        gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        miniGameManager = FindAnyObjectByType<MiniGameManager>();
        playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
        
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
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    public void ShowEndingScreen()
    {
        if (isShowing) return;
        
        isShowing = true;
        gameObject.SetActive(true);
        
        // 데이터 수집 및 표시
        StartCoroutine(ShowEndingAnimation());
    }
    
    private IEnumerator ShowEndingAnimation()
    {
        // 페이드 인
        yield return StartCoroutine(FadeIn());
        
        // 최종 데이터 수집
        GameStats stats = CollectGameStats();
        
        // 모든 UI를 즉시 최종 값으로 표시
        ShowScoreInstant(stats.finalScore);
        ShowNPCCountInstant(stats.npcCount);
        ShowCompletionRateInstant(stats.completionRate);
        ShowGradeInstant(stats.grade);
        ShowEmotionStatsInstant(stats.emotionStats, stats.mostUsedEmotion);
        
        // 감사 메시지 표시
        ShowThankYouMessage();
        
        // 버튼 활성화
        EnableButtons();
    }
    
    // 새로운 즉시 표시 메서드들
    private void ShowScoreInstant(int score)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"<color=#FFD700>최종 점수</color> <size=60>{score:N0}</size>";
    }
    
    private void ShowNPCCountInstant(int count)
    {
        if (npcCountText != null)
            npcCountText.text = $"<color=#FF69B4>꼬셔진 NPC</color> <size=50>{count}명</size>";
    }
    
    private void ShowCompletionRateInstant(float rate)
    {
        if (completionRateText != null)
            completionRateText.text = $"<color=#00FFFF>완료율</color> <size=50>{rate:F1}%</size>";
    }
    
    private void ShowGradeInstant(string grade)
    {
        if (gradeText != null)
        {
            Color gradeColor = GetGradeColor(grade);
            gradeText.color = gradeColor;
            gradeText.text = $"<size=80>{grade}</size> 등급";
            
            // 펄스 효과는 유지
            StartCoroutine(GradePulseEffect());
        }
    }
    
    // 등급 펄스 효과 별도 메서드
    private IEnumerator GradePulseEffect()
    {
        for (int i = 0; i < 3; i++)
        {
            gradeText.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.2f);
            gradeText.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    private void ShowEmotionStatsInstant(Dictionary<EmotionState, int> stats, EmotionState mostUsed)
    {
        if (emotionStatsText == null || stats == null) return;
        
        string statsText = "<color=#FFFF90>감정 사용 통계</color>\n\n";
        
        foreach (var kvp in stats)
        {
            Color emotionColor = kvp.Key.GetEmotionColor();
            string colorHex = ColorUtility.ToHtmlStringRGB(emotionColor);
            
            string highlight = kvp.Key == mostUsed ? " !!!" : "";
            statsText += $"<color=#{colorHex}>{kvp.Key.GetEmotionName()}</color>: {kvp.Value}회{highlight}\n";
        }
        
        statsText += $"\n<color=#90EE90>가장 선호하는 감정: {mostUsed.GetEmotionName()}</color>";
        
        emotionStatsText.text = statsText;
    }
    
    private GameStats CollectGameStats()
    {
        GameStats stats = new GameStats();
        
        // 점수 수집
        stats.finalScore = miniGameManager != null ? miniGameManager.GetCurrentScore() : 0;
        
        // NPC 수 수집
        stats.npcCount = ZeeeingGaze.FollowerManager.Instance != null ? 
            ZeeeingGaze.FollowerManager.Instance.GetFollowingCount() : 0;
        
        // 완료율 계산
        int targetCount = gameFlowManager != null ? gameFlowManager.GetTargetNPCCount() : 5;
        stats.completionRate = targetCount > 0 ? (float)stats.npcCount / targetCount * 100f : 0f;
        
        // 등급 계산
        stats.grade = CalculateGrade(stats.completionRate);
        
        // 감정 통계 수집
        if (playerEmotionController != null)
        {
            stats.emotionStats = playerEmotionController.GetEmotionUsageStats();
            stats.mostUsedEmotion = playerEmotionController.GetMostUsedEmotion();
        }
        
        return stats;
    }
    
    private string CalculateGrade(float completionRate)
    {
        if (completionRate >= 100f) return "S";
        if (completionRate >= 80f) return "A";
        if (completionRate >= 60f) return "B";
        if (completionRate >= 40f) return "C";
        return "D";
    }
    
    private Color GetGradeColor(string grade)
    {
        switch (grade)
        {
            case "S": return gradeS;
            case "A": return gradeA;
            case "B": return gradeB;
            case "C": return gradeC;
            case "D": return gradeD;
            default: return Color.white;
        }
    }
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    private void ShowThankYouMessage()
    {
        if (thankYouText == null) return;
        
        thankYouText.text = 
            "<color=#FFB6C1>데모를 플레이해주셔서 감사합니다!</color>\n\n" +
            "<size=18>눈빛 보내기 VR의 정식 버전을 기대해주세요!</size>";
    }
    
    private void EnableButtons()
    {
        if (restartButton != null)
            restartButton.interactable = true;
            
        if (quitButton != null)
            quitButton.interactable = true;
    }
    
    private void OnRestartClicked()
    {
        if (gameFlowManager != null)
        {
            gameFlowManager.RestartDemo();
        }
    }
    
    private void OnQuitClicked()
    {
        if (gameFlowManager != null)
        {
            gameFlowManager.QuitDemo();
        }
    }
    
    public void HideEndingScreen()
    {
        isShowing = false;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        gameObject.SetActive(false);
    }
    
    // 게임 통계를 담는 구조체
    private struct GameStats
    {
        public int finalScore;
        public int npcCount;
        public float completionRate;
        public string grade;
        public Dictionary<EmotionState, int> emotionStats;
        public EmotionState mostUsedEmotion;
    }
}