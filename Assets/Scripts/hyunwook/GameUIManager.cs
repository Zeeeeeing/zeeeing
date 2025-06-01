using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using ZeeeingGaze;

public class GameUIManager : MonoBehaviour
{
    [Header("Player Emotion UI")]
    [SerializeField] private Image playerEmotionIcon;
    [SerializeField] private TextMeshProUGUI playerEmotionText;
    [SerializeField] private Image emotionIconBG;
    
    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Image interactionProgressBar;
    [SerializeField] private TextMeshProUGUI interactionNPCNameText;
    [SerializeField] private Image npcEmotionIcon;
    
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI npcCountText;
    
    [Header("Control Hints")]
    [SerializeField] private GameObject controlHintsPanel;
    [SerializeField] private TextMeshProUGUI controlHintsText;
    [SerializeField] private float hintShowDuration = 3f; // 시간 단축
    
    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Toggle debugToggle;
    [SerializeField] private Button happyButton;
    [SerializeField] private Button sadButton;
    [SerializeField] private Button angryButton;
    [SerializeField] private Button neutralButton;
    [SerializeField] private Button colorGameButton;
    [SerializeField] private Button heartGameButton;
    // Chase 게임 버튼 제거
    [SerializeField] private TextMeshProUGUI debugInfoText;
    
    [Header("References")]
    [SerializeField] private PlayerEmotionController playerEmotionController;
    [SerializeField] private NPCInteractionManager interactionManager;
    [SerializeField] private MiniGameManager miniGameManager;
    
    // 감정 아이콘 및 색상
    [SerializeField] private Sprite happyEmotionSprite;
    [SerializeField] private Sprite sadEmotionSprite;
    [SerializeField] private Sprite angryEmotionSprite;
    [SerializeField] private Sprite neutralEmotionSprite;
    
    private Dictionary<EmotionState, Sprite> emotionSprites = new Dictionary<EmotionState, Sprite>();
    private Coroutine hintCoroutine;
    
    private void Awake()
    {
        // 감정 스프라이트 딕셔너리 초기화
        emotionSprites[EmotionState.Happy] = happyEmotionSprite;
        emotionSprites[EmotionState.Sad] = sadEmotionSprite;
        emotionSprites[EmotionState.Angry] = angryEmotionSprite;
        emotionSprites[EmotionState.Neutral] = neutralEmotionSprite;
        
        // 필요 컴포넌트 자동 찾기
        if (playerEmotionController == null)
            playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
            
        if (interactionManager == null)
            interactionManager = FindAnyObjectByType<NPCInteractionManager>();

        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
    }
    
    private void Start()
    {
        // 이벤트 구독
        if (playerEmotionController != null)
            playerEmotionController.OnEmotionChanged += UpdatePlayerEmotionUI;
            
        // 초기 UI 세팅
        UpdatePlayerEmotionUI(playerEmotionController?.GetCurrentEmotion() ?? EmotionState.Neutral);
        UpdateScoreUI(0, 0);
        HideInteractionUI();
        
        // 조작 도움말 표시 (시간 단축)
        ShowControlHints();
        
        // FollowerManager 이벤트 구독
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            ZeeeingGaze.FollowerManager.Instance.OnScoreUpdated += UpdateScoreUI;
        }

        // Debug 패널 초기 설정
        if (debugPanel != null)
            debugPanel.SetActive(false);
            
        // Debug 토글 이벤트 연결
        if (debugToggle != null)
            debugToggle.onValueChanged.AddListener(ToggleDebugPanel);
            
        // 감정 버튼 이벤트 연결
        SetupDebugButtons();
        
        Debug.Log("GameUIManager 초기화 완료 (2개 미니게임 모드)");
    }
        
    private void Update()
    {
        // 상호작용 UI 업데이트
        UpdateInteractionUI();
        
        // 디버그 모드 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (debugToggle != null)
                debugToggle.isOn = !debugToggle.isOn;
        }
    }
    
    // 플레이어 감정 UI 업데이트
    private void UpdatePlayerEmotionUI(EmotionState emotion)
    {
        if (playerEmotionIcon != null && emotionSprites.TryGetValue(emotion, out Sprite sprite))
        {
            playerEmotionIcon.sprite = sprite;
        }
        
        if (playerEmotionText != null)
        {
            playerEmotionText.text = emotion.GetEmotionName();
            playerEmotionText.color = emotion.GetEmotionColor();
        }
        
        if (emotionIconBG != null)
        {
            emotionIconBG.color = Color.Lerp(Color.white, emotion.GetEmotionColor(), 0.3f);
        }
    }
    
    // 상호작용 UI 업데이트
    private void UpdateInteractionUI()
    {
        if (interactionManager == null) return;
        
        NPCController currentNPC = interactionManager.GetCurrentInteractingNPC();
        
        if (currentNPC != null)
        {
            // 거리 기반 UI 표시 여부 결정
            float distance = Vector3.Distance(Camera.main.transform.position, currentNPC.transform.position);
            bool shouldShowUI = ShouldShowUIAtDistance(distance);
            
            if (shouldShowUI)
            {
                // 상호작용 중인 NPC가 있으면 UI 표시
                ShowInteractionUI();
                
                // NPC 이름 표시
                if (interactionNPCNameText != null)
                {
                    interactionNPCNameText.text = currentNPC.GetName();
                }
                
                // 진행률 업데이트 - 빠른 템포를 위해 조정
                if (interactionProgressBar != null)
                {
                    // 미니게임 시작 시간을 2초로 단축
                    float fillAmount = Mathf.Clamp01(Time.time % 2f / 2f);
                    interactionProgressBar.fillAmount = fillAmount;
                }
                
                // NPC 감정 표시
                UpdateNPCEmotionIcon(currentNPC);
            }
            else
            {
                // 거리가 맞지 않으면 UI 숨기기
                HideInteractionUI();
            }
        }
        else
        {
            // 상호작용 중인 NPC가 없으면 UI 숨기기
            HideInteractionUI();
        }
    }
    
    // 거리에 따른 UI 표시 여부 결정
    private bool ShouldShowUIAtDistance(float distance)
    {
        // 설정에 따라 다른 로직 적용
        if (interactionManager == null) return true;
        
        // NPCInteractionManager의 설정 가져오기 (리플렉션 사용)
        var showUIAtAllDistancesField = interactionManager.GetType().GetField("showUIAtAllDistances", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var uiShowDistanceField = interactionManager.GetType().GetField("uiShowDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var uiHideDistanceField = interactionManager.GetType().GetField("uiHideDistance", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        bool showUIAtAllDistances = showUIAtAllDistancesField != null ? (bool)showUIAtAllDistancesField.GetValue(interactionManager) : true;
        float uiShowDistance = uiShowDistanceField != null ? (float)uiShowDistanceField.GetValue(interactionManager) : 6.0f;
        float uiHideDistance = uiHideDistanceField != null ? (float)uiHideDistanceField.GetValue(interactionManager) : 0.3f;
        
        // 모든 거리에서 표시하도록 설정되어 있으면 항상 true
        if (showUIAtAllDistances)
        {
            return true;
        }
        
        // 너무 가까우면 숨김 (UI가 겹치지 않도록)
        if (distance < uiHideDistance)
        {
            return false;
        }
        
        // 적정 거리에서 표시
        return distance <= uiShowDistance;
    }
    
    // NPC 감정 아이콘 업데이트
    private void UpdateNPCEmotionIcon(NPCController npc)
    {
        if (npcEmotionIcon == null || npc == null) return;
        
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            EmotionState npcEmotion = emotionController.GetCurrentEmotion();
            
            // 감정 아이콘 설정
            if (emotionSprites.TryGetValue(npcEmotion, out Sprite sprite))
            {
                npcEmotionIcon.sprite = sprite;
            }
            
            // 감정 강도에 따라 색상 투명도 조절
            Color emotionColor = npcEmotion.GetEmotionColor();
            float intensity = emotionController.GetCurrentEmotionIntensity();
            emotionColor.a = Mathf.Lerp(0.5f, 1f, intensity);
            npcEmotionIcon.color = emotionColor;
        }
    }
    
    // 점수 UI 업데이트
    public void UpdateScoreUI(int score, int npcCount)
    {
        if (scoreText != null)
        {
            scoreText.text = $"점수: {score}";
        }
        
        if (npcCountText != null)
        {
            npcCountText.text = $"꼬셔진 NPC: {npcCount}";
        }
    }
    
    // 상호작용 UI 표시
    private void ShowInteractionUI()
    {
        if (interactionPanel != null && !interactionPanel.activeSelf)
        {
            interactionPanel.SetActive(true);
        }
    }
    
    // 상호작용 UI 숨기기
    private void HideInteractionUI()
    {
        if (interactionPanel != null && interactionPanel.activeSelf)
        {
            interactionPanel.SetActive(false);
        }
    }
    
    // 조작 도움말 표시 (간소화)
    public void ShowControlHints()
    {
        if (controlHintsPanel == null) return;
        
        controlHintsPanel.SetActive(true);
        controlHintsText.text = 
            "감정 조작:\n" +
            "● 오른쪽 트리거: 행복\n" +
            "● 왼쪽 트리거: 슬픔\n" +
            "● 오른쪽 그립: 분노\n" +
            "● B 버튼: 중립\n\n" +
            "NPC와 시선을 맞추고 감정을 표현하세요!";
        
        // 이전 코루틴 중지
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
        }
        
        // 일정 시간 후 자동 숨김 (시간 단축)
        hintCoroutine = StartCoroutine(AutoHideHints());
    }
    
    // 도움말 자동 숨김 (시간 단축)
    private IEnumerator AutoHideHints()
    {
        yield return new WaitForSeconds(hintShowDuration);
        HideControlHints();
    }
    
    // 조작 도움말 숨기기
    public void HideControlHints()
    {
        if (controlHintsPanel != null)
        {
            controlHintsPanel.SetActive(false);
        }
    }
    
    // 디버그 패널 토글
    private void ToggleDebugPanel(bool show)
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(show);
            
            // Debug 패널이 활성화되면 정보 업데이트 시작
            if (show)
                InvokeRepeating("UpdateDebugInfo", 0f, 0.5f);
            else
                CancelInvoke("UpdateDebugInfo");
        }
    }
        
    // 감정 변경 버튼 (디버그용, 버튼에 연결)
    public void SetEmotionHappy() => playerEmotionController?.ForceSetEmotion(EmotionState.Happy);
    public void SetEmotionSad() => playerEmotionController?.ForceSetEmotion(EmotionState.Sad);
    public void SetEmotionAngry() => playerEmotionController?.ForceSetEmotion(EmotionState.Angry);
    public void SetEmotionNeutral() => playerEmotionController?.ForceSetEmotion(EmotionState.Neutral);
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (playerEmotionController != null)
            playerEmotionController.OnEmotionChanged -= UpdatePlayerEmotionUI;

        // FollowerManager 이벤트 구독 해제
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            ZeeeingGaze.FollowerManager.Instance.OnScoreUpdated -= UpdateScoreUI;
        }
            
        if (debugToggle != null)
            debugToggle.onValueChanged.RemoveListener(ToggleDebugPanel);

        CancelInvoke("UpdateDebugInfo");
    }

    // 디버그 버튼 설정 (Chase 버튼 제거)
    private void SetupDebugButtons()
    {
        // 감정 버튼 이벤트 연결
        if (happyButton != null && playerEmotionController != null)
            happyButton.onClick.AddListener(() => playerEmotionController.ForceSetEmotion(EmotionState.Happy));
            
        if (sadButton != null && playerEmotionController != null)
            sadButton.onClick.AddListener(() => playerEmotionController.ForceSetEmotion(EmotionState.Sad));
            
        if (angryButton != null && playerEmotionController != null)
            angryButton.onClick.AddListener(() => playerEmotionController.ForceSetEmotion(EmotionState.Angry));
            
        if (neutralButton != null && playerEmotionController != null)
            neutralButton.onClick.AddListener(() => playerEmotionController.ForceSetEmotion(EmotionState.Neutral));
            
        // 미니게임 버튼 이벤트 연결 (Chase 버튼 제거)
        if (colorGameButton != null && miniGameManager != null)
            colorGameButton.onClick.AddListener(() => miniGameManager.StartMiniGame(MiniGameManager.MiniGameType.ColorGaze));
            
        if (heartGameButton != null && miniGameManager != null)
            heartGameButton.onClick.AddListener(() => miniGameManager.StartMiniGame(MiniGameManager.MiniGameType.HeartGaze));
        
        Debug.Log("디버그 버튼 설정 완료 (2개 미니게임 모드)");
    }

    // 디버그 정보 업데이트 (간소화)
    private void UpdateDebugInfo()
    {
        if (debugInfoText == null) return;
        
        StringBuilder sb = new StringBuilder();
        
        // 현재 플레이어 감정 상태
        if (playerEmotionController != null)
        {
            sb.AppendLine($"감정: {playerEmotionController.GetCurrentEmotion().GetEmotionName()}");
        }
        
        // 현재 상호작용 NPC 정보
        if (interactionManager != null)
        {
            NPCController npc = interactionManager.GetCurrentInteractingNPC();
            if (npc != null)
            {
                sb.AppendLine($"NPC: {npc.GetName()}");
                
                NPCEmotionController npcEmotion = npc.GetComponent<NPCEmotionController>();
                if (npcEmotion != null)
                {
                    sb.AppendLine($"NPC 감정: {npcEmotion.GetCurrentEmotion().GetEmotionName()}");
                    sb.AppendLine($"강도: {npcEmotion.GetCurrentEmotionIntensity():F2}");
                }
            }
            else
            {
                sb.AppendLine("NPC: 없음");
            }
        }
        
        // 점수 정보
        if (miniGameManager != null)
        {
            sb.AppendLine($"점수: {miniGameManager.GetCurrentScore()}");
            sb.AppendLine($"NPC 수: {miniGameManager.GetFollowingNPCCount()}");
        }
        
        // 게임 모드 정보 추가
        sb.AppendLine("게임 모드: 2개 미니게임");
        sb.AppendLine("- 감정 매칭 (ColorGaze)");
        sb.AppendLine("- 하트 수집 (HeartGaze)");
        
        debugInfoText.text = sb.ToString();
    }
}