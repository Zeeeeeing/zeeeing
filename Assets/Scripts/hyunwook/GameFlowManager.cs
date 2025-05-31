using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeeeingGaze;

public class GameFlowManager : MonoBehaviour
{
    [Header("Demo Settings - 4 Minute Experience")]
    [SerializeField] private float totalDemoTime = 240f; // 4분
    [SerializeField] private float tutorialTime = 30f; // 30초 튜토리얼
    [SerializeField] private float gameplayTime = 180f; // 3분 게임플레이
    [SerializeField] private float endingTime = 30f; // 30초 엔딩
    
    [Header("Game Phases")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Tutorial;
    [SerializeField] private float phaseTimer = 0f;
    
    [Header("NPC Management - Manual Setup")]
    [SerializeField] private int targetNPCCount = 5; // 목표 NPC 수 (수동 설정된 NPC 개수에 따라 조정)
    [SerializeField] private List<NPCController> sceneNPCs = new List<NPCController>(); // 씬에 미리 배치된 NPC들
    
    [Header("UI References")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private GameObject gameplayUI;
    [SerializeField] private GameObject endingUI;
    [SerializeField] private TextMeshProUGUI phaseTimerText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;
    
    [Header("Audio")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip tutorialBGM;
    [SerializeField] private AudioClip gameplayBGM;
    [SerializeField] private AudioClip endingBGM;
    
    [Header("References")]
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private GameUIManager gameUIManager;
    [SerializeField] private PlayerEmotionController playerEmotionController;
    
    [Header("UI Controllers")]
    [SerializeField] private TutorialUIController tutorialController;
    [SerializeField] private EndingUIController endingController;
    
    // 게임 상태 추적
    private int seducedNPCCount = 0;
    private float gameStartTime;
    private bool isGameActive = false;
    
    // 이벤트
    public event System.Action<GamePhase> OnPhaseChanged;
    public event System.Action<int, int> OnProgressUpdated; // 현재/목표
    public event System.Action OnDemoCompleted;
    
    public enum GamePhase
    {
        Tutorial,    // 튜토리얼 (30초)
        Gameplay,    // 메인 게임플레이 (3분)
        Ending       // 엔딩 (30초)
    }
    
    private void Awake()
    {
        // 필요한 컴포넌트 자동 찾기
        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
            
        if (gameUIManager == null)
            gameUIManager = FindAnyObjectByType<GameUIManager>();
            
        if (playerEmotionController == null)
            playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
            
        if (bgmAudioSource == null)
            bgmAudioSource = GetComponent<AudioSource>();
            
        // UI 컨트롤러들 자동 찾기
        if (tutorialController == null)
            tutorialController = FindAnyObjectByType<TutorialUIController>();
            
        if (endingController == null)
            endingController = FindAnyObjectByType<EndingUIController>();
    }
    
    private void Start()
    {
        gameStartTime = Time.time;
        isGameActive = true;
        
        // 씬의 기존 NPC들 찾기 및 설정
        FindAndSetupSceneNPCs();
        
        // 초기 페이즈 설정
        StartTutorialPhase();
        
        // 이벤트 구독
        SubscribeToEvents();
        
        Debug.Log($"GameFlowManager 시작 - 4분 데모 모드, 씬 NPC 수: {sceneNPCs.Count}");
    }
    
    private void FindAndSetupSceneNPCs()
    {
        // 씬에 있는 모든 NPC 찾기
        NPCController[] foundNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        
        // sceneNPCs 리스트가 비어있으면 자동으로 찾은 NPC들로 채우기
        if (sceneNPCs.Count == 0)
        {
            sceneNPCs.AddRange(foundNPCs);
        }
        
        // 목표 NPC 수를 실제 씬의 NPC 수에 맞게 조정
        if (sceneNPCs.Count > 0)
        {
            targetNPCCount = Mathf.Min(targetNPCCount, sceneNPCs.Count);
        }
        
        // 각 NPC에 이벤트 구독
        foreach (NPCController npc in sceneNPCs)
        {
            if (npc != null)
            {
                npc.OnNPCSeduced += OnNPCSeducedEvent;
                Debug.Log($"NPC 설정됨: {npc.GetName()}");
            }
        }
        
        Debug.Log($"씬 NPC 설정 완료 - 총 {sceneNPCs.Count}개, 목표: {targetNPCCount}개");
    }
    
    private void Update()
    {
        if (!isGameActive) return;
        
        // 페이즈 타이머 업데이트
        phaseTimer += Time.deltaTime;
        
        // 전체 진행률 업데이트
        UpdateProgress();
        
        // 현재 페이즈에 따른 처리
        switch (currentPhase)
        {
            case GamePhase.Tutorial:
                UpdateTutorialPhase();
                break;
            case GamePhase.Gameplay:
                UpdateGameplayPhase();
                break;
            case GamePhase.Ending:
                UpdateEndingPhase();
                break;
        }
        
        // UI 업데이트
        UpdateUI();
    }
    
    private void SubscribeToEvents()
    {
        // FollowerManager 이벤트 구독
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            ZeeeingGaze.FollowerManager.Instance.OnPointsAdded += OnNPCSeduced;
        }
        
        // MiniGameManager 이벤트 구독
        if (miniGameManager != null)
        {
            miniGameManager.OnGameCompleted += OnMiniGameCompleted;
        }
    }
    
    #region Tutorial Phase
    private void StartTutorialPhase()
    {
        currentPhase = GamePhase.Tutorial;
        phaseTimer = 0f;
        
        // 튜토리얼 UI 활성화
        if (tutorialController != null)
        {
            tutorialController.ShowTutorial(tutorialTime);
        }
        else
        {
            SetActiveUI(tutorialUI);
        }
        
        // 튜토리얼 BGM 재생
        PlayBGM(tutorialBGM);
        
        // 컨트롤 힌트 표시
        if (gameUIManager != null)
        {
            gameUIManager.ShowControlHints();
        }
        
        Debug.Log("튜토리얼 페이즈 시작 (30초)");
        OnPhaseChanged?.Invoke(currentPhase);
    }
    
    private void UpdateTutorialPhase()
    {
        // 튜토리얼 컨트롤러가 있으면 진행률 업데이트
        if (tutorialController != null)
        {
            float progress = phaseTimer / tutorialTime;
            tutorialController.UpdateProgress(progress);
        }
        
        // 튜토리얼 시간 종료 또는 조건 만족시 다음 페이즈
        if (phaseTimer >= tutorialTime || seducedNPCCount >= 1)
        {
            StartGameplayPhase();
        }
    }
    
    // 튜토리얼 완료 메서드 (TutorialUIController에서 호출)
    public void CompleteTutorial()
    {
        if (currentPhase == GamePhase.Tutorial)
        {
            StartGameplayPhase();
        }
    }
    #endregion
    
    #region Gameplay Phase
    private void StartGameplayPhase()
    {
        currentPhase = GamePhase.Gameplay;
        phaseTimer = 0f;
        
        // 게임플레이 UI 활성화
        SetActiveUI(gameplayUI);
        
        // 게임플레이 BGM 재생
        PlayBGM(gameplayBGM);
        
        // 컨트롤 힌트 숨기기
        if (gameUIManager != null)
        {
            gameUIManager.HideControlHints();
        }
        
        Debug.Log("게임플레이 페이즈 시작 (3분) - 수동 배치된 NPC들과 상호작용");
        OnPhaseChanged?.Invoke(currentPhase);
    }
    
    private void UpdateGameplayPhase()
    {
        // 게임플레이 시간 종료 또는 목표 달성시 엔딩
        if (phaseTimer >= gameplayTime || seducedNPCCount >= targetNPCCount)
        {
            StartEndingPhase();
        }
        
        // 중간 진행 상황 체크 (격려 메시지 등)
        float progress = (float)seducedNPCCount / targetNPCCount;
        if (progress < 0.3f && phaseTimer > gameplayTime * 0.5f)
        {
            // 진행이 느린 경우 힌트 메시지 표시 등
            Debug.Log("진행이 느립니다. NPC들과 더 적극적으로 상호작용해보세요!");
        }
    }
    #endregion
    
    #region Ending Phase
    private void StartEndingPhase()
    {
        currentPhase = GamePhase.Ending;
        phaseTimer = 0f;
        
        // 엔딩 UI 활성화
        if (endingController != null)
        {
            endingController.ShowEndingScreen();
        }
        else
        {
            SetActiveUI(endingUI);
            ShowFinalResults();
        }
        
        // 엔딩 BGM 재생
        PlayBGM(endingBGM);
        
        Debug.Log("엔딩 페이즈 시작 (30초)");
        OnPhaseChanged?.Invoke(currentPhase);
    }
    
    private void UpdateEndingPhase()
    {
        // 엔딩 시간 종료시 데모 완료
        if (phaseTimer >= endingTime)
        {
            CompletDemo();
        }
    }
    
    private void ShowFinalResults()
    {
        // 기존 방식의 결과 표시 (백업용)
        if (endingUI == null) return;
        
        // 최종 점수 및 통계 표시
        int finalScore = miniGameManager != null ? miniGameManager.GetCurrentScore() : 0;
        int followingCount = ZeeeingGaze.FollowerManager.Instance != null ? ZeeeingGaze.FollowerManager.Instance.GetFollowingCount() : 0;
        
        float completionRate = (float)seducedNPCCount / targetNPCCount * 100f;

        TextMeshProUGUI resultText = endingUI.GetComponentInChildren<TextMeshProUGUI>();
        if (resultText != null)
        {
            string grade = GetPerformanceGrade(completionRate);
            
            resultText.text = $"데모 완료!\n\n" +
                            $"최종 점수: {finalScore}\n" +
                            $"꼬셔진 NPC: {followingCount}명\n" +
                            $"완료율: {completionRate:F1}%\n" +
                            $"등급: {grade}\n\n" +
                            $"플레이해주셔서 감사합니다!";
        }
        
        Debug.Log($"최종 결과 - 점수: {finalScore}, NPC: {followingCount}명, 완료율: {completionRate:F1}%");
    }
    
    private string GetPerformanceGrade(float completionRate)
    {
        if (completionRate >= 100f) return "S";
        if (completionRate >= 80f) return "A";
        if (completionRate >= 60f) return "B";
        if (completionRate >= 40f) return "C";
        return "D";
    }
    #endregion
    
    #region Event Handlers
    private void OnNPCSeducedEvent(NPCController npc)
    {
        seducedNPCCount++;
        OnProgressUpdated?.Invoke(seducedNPCCount, targetNPCCount);
        
        Debug.Log($"NPC 꼬시기 성공: {npc.GetName()} ({seducedNPCCount}/{targetNPCCount})");
    }
    
    private void OnNPCSeduced(int points)
    {
        // FollowerManager에서 호출되는 이벤트
        // 별도 처리 필요시 여기에 추가
    }
    
    private void OnMiniGameCompleted(bool success, int score)
    {
        if (success)
        {
            Debug.Log($"미니게임 성공! 점수: {score}");
            // 성공시 추가 보상이나 처리 가능
        }
    }
    #endregion
    
    #region UI Management
    private void SetActiveUI(GameObject targetUI)
    {
        // 모든 UI 비활성화
        if (tutorialUI != null) tutorialUI.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (endingUI != null) endingUI.SetActive(false);
        
        // 타겟 UI 활성화
        if (targetUI != null) targetUI.SetActive(true);
    }
    
    private void UpdateUI()
    {
        // 페이즈 타이머 표시
        if (phaseTimerText != null)
        {
            float remainingTime = GetPhaseRemainingTime();
            phaseTimerText.text = $"{currentPhase}: {Mathf.CeilToInt(remainingTime)}초";
        }
        
        // 진행률 표시
        if (progressSlider != null)
        {
            float totalProgress = (Time.time - gameStartTime) / totalDemoTime;
            progressSlider.value = Mathf.Clamp01(totalProgress);
        }
        
        if (progressText != null)
        {
            progressText.text = $"진행률: {seducedNPCCount}/{targetNPCCount}";
        }
    }
    
    private float GetPhaseRemainingTime()
    {
        switch (currentPhase)
        {
            case GamePhase.Tutorial:
                return tutorialTime - phaseTimer;
            case GamePhase.Gameplay:
                return gameplayTime - phaseTimer;
            case GamePhase.Ending:
                return endingTime - phaseTimer;
            default:
                return 0f;
        }
    }
    #endregion
    
    #region Audio Management
    private void PlayBGM(AudioClip clip)
    {
        if (bgmAudioSource != null && clip != null)
        {
            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }
    #endregion
    
    #region Game Control
    private void UpdateProgress()
    {
        OnProgressUpdated?.Invoke(seducedNPCCount, targetNPCCount);
    }
    
    private void CompletDemo()
    {
        isGameActive = false;
        Debug.Log("4분 데모 완료!");
        
        OnDemoCompleted?.Invoke();
        
        // 데모 완료 후 처리 (재시작 버튼 표시 등)
        StartCoroutine(ShowRestartOption());
    }
    
    private IEnumerator ShowRestartOption()
    {
        yield return new WaitForSeconds(3f);
        
        // 재시작 옵션 표시
        if (endingUI != null)
        {
            GameObject restartButton = new GameObject("RestartButton");
            restartButton.transform.SetParent(endingUI.transform);
            // 버튼 설정 로직...
        }
    }
    
    public void RestartDemo()
    {
        // 데모 재시작
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    public void QuitDemo()
    {
        // 데모 종료
        Application.Quit();
    }
    #endregion
    
    #region Public Methods
    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }
    
    public float GetPhaseProgress()
    {
        float maxTime = 0f;
        switch (currentPhase)
        {
            case GamePhase.Tutorial: maxTime = tutorialTime; break;
            case GamePhase.Gameplay: maxTime = gameplayTime; break;
            case GamePhase.Ending: maxTime = endingTime; break;
        }
        
        return maxTime > 0 ? phaseTimer / maxTime : 0f;
    }
    
    public float GetOverallProgress()
    {
        return (Time.time - gameStartTime) / totalDemoTime;
    }
    
    public int GetSeducedNPCCount()
    {
        return seducedNPCCount;
    }
    
    public int GetTargetNPCCount()
    {
        return targetNPCCount;
    }
    
    // 씬 NPC 수동 추가 메서드
    public void AddSceneNPC(NPCController npc)
    {
        if (npc != null && !sceneNPCs.Contains(npc))
        {
            sceneNPCs.Add(npc);
            npc.OnNPCSeduced += OnNPCSeducedEvent;
            Debug.Log($"씬 NPC 수동 추가: {npc.GetName()}");
        }
    }
    
    // 씬 NPC 제거 메서드
    public void RemoveSceneNPC(NPCController npc)
    {
        if (npc != null && sceneNPCs.Contains(npc))
        {
            sceneNPCs.Remove(npc);
            npc.OnNPCSeduced -= OnNPCSeducedEvent;
            Debug.Log($"씬 NPC 제거: {npc.GetName()}");
        }
    }
    #endregion
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            ZeeeingGaze.FollowerManager.Instance.OnPointsAdded -= OnNPCSeduced;
        }
        
        if (miniGameManager != null)
        {
            miniGameManager.OnGameCompleted -= OnMiniGameCompleted;
        }
        
        // NPC 이벤트 구독 해제
        foreach (var npc in sceneNPCs)
        {
            if (npc != null)
            {
                npc.OnNPCSeduced -= OnNPCSeducedEvent;
            }
        }
    }
}