using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class ColorGazeMiniGame : MonoBehaviour
{
    [Header("Game Settings - Fast Tempo")]
    [SerializeField] private int requiredMatches = 3; // 기본값 감소 (5 -> 3)
    [SerializeField] private float timeLimit = 15f; // 기본값 감소 (20 -> 15)
    [SerializeField] private NPCEmotionController npcController;
    [SerializeField] private float requiredMatchingTime = 1.5f; // 감소 (2.0 -> 1.5)
    
    [Header("Player References")]
    [SerializeField] private PlayerEmotionController playerEmotionController;
    
    [Header("Difficulty Settings - Optimized for Demo")]
    [SerializeField] private int easyRequiredMatches = 2; // 감소 (3 -> 2)
    [SerializeField] private int normalRequiredMatches = 3; // 감소 (5 -> 3)
    [SerializeField] private int hardRequiredMatches = 4; // 감소 (7 -> 4)
    
    [SerializeField] private float easyTimeLimit = 20f; // 감소 (30 -> 20)
    [SerializeField] private float normalTimeLimit = 15f; // 감소 (20 -> 15)
    [SerializeField] private float hardTimeLimit = 12f; // 감소 (15 -> 12)
    
    [Header("UI References")]
    [SerializeField] private Image targetColorDisplay;
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    [SerializeField] private TMPro.TextMeshProUGUI matchCountText;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private Image emotionMatchProgressBar;
    
    // 게임 상태 변수
    private EmotionState targetEmotion;
    private int matchesCompleted = 0;
    private float remainingTime;
    private bool isGameActive = false;
    private float matchingTime = 0f;
    
    // 색상-감정 매핑용 캐시
    private Dictionary<EmotionState, Color> emotionColors = new Dictionary<EmotionState, Color>();
    
    // 가능한 감정 상태 목록 (간소화)
    [SerializeField] private List<EmotionState> availableEmotions = new List<EmotionState>();
    
    private void Awake()
    {
        // 게임 시작 시 UI 비활성화
        if (gameUI != null)
            gameUI.SetActive(false);
        
        // 사용 가능한 감정이 없으면 기본값 설정 (4가지로 간소화)
        if (availableEmotions.Count == 0)
        {
            availableEmotions.Add(EmotionState.Happy);
            availableEmotions.Add(EmotionState.Angry);
            availableEmotions.Add(EmotionState.Sad);
            availableEmotions.Add(EmotionState.Neutral);
        }
        
        // 플레이어 감정 컨트롤러가 없으면 찾기
        if (playerEmotionController == null)
        {
            playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
            if (playerEmotionController == null)
            {
                Debug.LogWarning("PlayerEmotionController를 찾을 수 없습니다. 게임이 제대로 작동하지 않을 수 있습니다.");
            }
        }
    }
    
    public void StartMiniGame(int difficulty = 1)
    {
        // 필요한 참조 확인
        if (gameUI == null)
        {
            Debug.LogWarning("gameUI가 null입니다! Unity Inspector에서 할당해주세요.");
            gameUI = GameObject.Find("ColorGamePanel");
            if (gameUI == null)
            {
                Debug.LogError("ColorGamePanel을 찾을 수 없습니다! UI가 표시되지 않을 수 있습니다.");
            }
        }

        // 난이도에 따른 설정
        switch(difficulty)
        {
            case 0: // Easy
                requiredMatches = easyRequiredMatches;
                timeLimit = easyTimeLimit;
                break;
            case 1: // Normal
                requiredMatches = normalRequiredMatches;
                timeLimit = normalTimeLimit;
                break;
            case 2: // Hard
                requiredMatches = hardRequiredMatches;
                timeLimit = hardTimeLimit;
                break;
        }
        
        // 게임 초기화
        matchesCompleted = 0;
        remainingTime = timeLimit;
        isGameActive = true;
        matchingTime = 0f;
        
        if (gameUI != null)
        {
            gameUI.SetActive(true);
            Debug.Log("ColorGazeMiniGame UI 활성화됨 (빠른 템포 모드)");
        }

        // 색상-감정 매핑 캐시 초기화
        CacheEmotionColors();
        
        // 첫 번째 타겟 감정 선택
        SelectRandomEmotion();
        
        // UI 업데이트
        UpdateUI();
        
        // 이벤트 구독 - NPC 감정 변화 대신 플레이어 감정 변화 이벤트 사용
        if (playerEmotionController != null)
        {
            playerEmotionController.OnEmotionChanged += OnPlayerEmotionChanged;
        }
        
        // 게임 시작 알림
        if (OnGameStarted != null) OnGameStarted.Invoke();
    }
    
    private void CacheEmotionColors()
    {
        emotionColors.Clear();
        
        // 4가지 감정 상태만 사용 (명확한 색상 구분)
        emotionColors[EmotionState.Happy] = new Color(1f, 0.9f, 0f); // 밝은 노란색
        emotionColors[EmotionState.Sad] = new Color(0.2f, 0.4f, 0.9f); // 진한 파란색
        emotionColors[EmotionState.Angry] = new Color(1f, 0.2f, 0.2f); // 진한 빨간색
        emotionColors[EmotionState.Neutral] = new Color(0.8f, 0.8f, 0.8f); // 회색
        
        // 사용 가능한 감정 상태 목록 업데이트
        availableEmotions.Clear();
        availableEmotions.Add(EmotionState.Happy);
        availableEmotions.Add(EmotionState.Sad);
        availableEmotions.Add(EmotionState.Angry);
        availableEmotions.Add(EmotionState.Neutral);
    }
    
    private void SelectRandomEmotion()
    {
        if (availableEmotions.Count == 0) return;
        
        // 이전과 다른 감정 선택 (연속 방지)
        EmotionState newEmotion;
        do {
            int randomIndex = Random.Range(0, availableEmotions.Count);
            newEmotion = availableEmotions[randomIndex];
        } while (newEmotion == targetEmotion && availableEmotions.Count > 1);
        
        targetEmotion = newEmotion;
        
        // 타겟 색상 표시 업데이트
        if (targetColorDisplay != null && emotionColors.TryGetValue(targetEmotion, out Color color))
        {
            targetColorDisplay.color = color;
        }
        
        Debug.Log($"새로운 타겟 감정: {targetEmotion} ({targetEmotion.GetEmotionName()})");
    }
    
    private void Update()
    {
        if (!isGameActive) return;
        
        // 타이머 업데이트
        remainingTime -= Time.deltaTime;
        
        // 현재 플레이어 감정 확인 및 처리
        CheckPlayerEmotion();
        
        // UI 업데이트
        UpdateUI();
        
        // 시간 종료 체크
        if (remainingTime <= 0)
        {
            GameOver(false);
        }
    }
    
    // 플레이어 감정 확인 메서드
    private void CheckPlayerEmotion()
    {
        if (playerEmotionController == null) return;
        
        // 현재 플레이어 감정 가져오기
        EmotionState currentPlayerEmotion = playerEmotionController.GetCurrentEmotion();
        
        // 타겟 감정과 일치하는지 확인
        if (currentPlayerEmotion == targetEmotion)
        {
            // 일치 시간 증가
            matchingTime += Time.deltaTime;
            
            // 일정 시간 이상 유지되면 매치 성공
            if (matchingTime >= requiredMatchingTime)
            {
                // 매치 성공
                matchesCompleted++;
                matchingTime = 0f;
                
                // 성공 피드백
                PlaySuccessFeedback();
                
                // 모든 매치를 완료했는지 확인
                if (matchesCompleted >= requiredMatches)
                {
                    GameOver(true);
                }
                else
                {
                    // 다음 감정 선택
                    SelectRandomEmotion();
                }
            }
        }
        else
        {
            // 감정이 일치하지 않으면 타이머 리셋
            matchingTime = 0f;
        }
        
        // 매칭 진행도 UI 업데이트
        if (emotionMatchProgressBar != null)
        {
            emotionMatchProgressBar.fillAmount = matchingTime / requiredMatchingTime;
        }
    }
    
    // 플레이어 감정 변경 이벤트 핸들러
    private void OnPlayerEmotionChanged(EmotionState newEmotion)
    {
        // 감정 변경 시 즉각 체크 - 이벤트 기반으로도 추가 확인
        if (newEmotion == targetEmotion)
        {
            // 이미 매칭 진행 중이면 그대로 유지, Update에서 처리됨
        }
        else
        {
            // 타겟 감정과 다르면 매칭 타이머 리셋
            matchingTime = 0f;
        }
    }
    
    private void UpdateUI()
    {
        // 타이머 텍스트 업데이트
        if (timerText != null)
        {
            timerText.text = $"시간: {Mathf.CeilToInt(remainingTime)}초";
        }
        
        // 매치 카운트 업데이트
        if (matchCountText != null)
        {
            matchCountText.text = $"매치: {matchesCompleted}/{requiredMatches}";
        }
    }
    
    private void PlaySuccessFeedback()
    {
        // 성공 피드백 (간단하게)
        Debug.Log($"매치 성공! ({matchesCompleted}/{requiredMatches})");
        
        // 컨트롤러 진동 (간단하게)
        try
        {
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand) is var device && device.TryGetHapticCapabilities(out var capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, 0.5f, 0.1f);
                }
            }
        }
        catch (System.Exception)
        {
            // VR 컨트롤러가 없을 경우 무시
        }
    }
    
    private void GameOver(bool success)
    {
        try
        {
            isGameActive = false;
            
            // 이벤트 구독 해제
            if (playerEmotionController != null)
            {
                playerEmotionController.OnEmotionChanged -= OnPlayerEmotionChanged;
            }
            
            // 게임 UI 비활성화 (중요: 여기서 명시적으로 비활성화)
            if (gameUI != null)
                gameUI.SetActive(false);
            
            // 게임 완료 이벤트 호출
            if (OnGameCompleted != null) 
            {
                Debug.Log($"ColorGame 완료 이벤트 발생: 성공={success}, 점수={matchesCompleted} (빠른 템포 모드)");
                OnGameCompleted.Invoke(success, matchesCompleted);
            }
            else
            {
                Debug.LogWarning("OnGameCompleted 이벤트가 null입니다. 이벤트 구독자가 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameOver 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public void StopGame()
    {
        if (isGameActive)
        {
            GameOver(false);
        }
    }

    // UI 접근을 위한 public 메서드
    public void HideGameUI()
    {
        if (gameUI != null)
            gameUI.SetActive(false);
    }

    // gameUI 상태 확인용 메서드
    public bool IsGameUIActive()
    {
        return gameUI != null && gameUI.activeSelf;
    }
    
    // 외부 이벤트
    public event System.Action OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted; // 성공 여부, 매치 수
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제 (안전장치)
        if (playerEmotionController != null)
        {
            playerEmotionController.OnEmotionChanged -= OnPlayerEmotionChanged;
        }
    }
}