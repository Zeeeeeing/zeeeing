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
    // gameplayUI 제거 - HUDController의 TopHUDPanel로 대체
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
    [SerializeField] private HUDController hudController; // HUDController 참조 (TopHUDPanel 포함)

    [Header("UI Controllers")]
    [SerializeField] private TutorialUIController tutorialController;
    [SerializeField] private EndingUIController endingController;

    [Header("Scene Transition")]
    [SerializeField] private bool enableSceneTransition = true; // 씬 전환 활성화 여부
    [SerializeField] private float resultDisplayTime = 5f; // 결과 표시 시간

    // 게임 상태 추적
    private int seducedNPCCount = 0;
    private float gameStartTime;
    private bool isGameActive = false;
    private float lastStateCheck = 0f; // ⭐ 주기적 상태 체크용

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

        // HUDController 자동 찾기
        if (hudController == null)
            hudController = FindAnyObjectByType<HUDController>();

        // EndingUI 자동 찾기
        if (endingUI == null)
            endingUI = GameObject.Find("EndingUI");
    }

    private void Start()
    {
        gameStartTime = Time.time;
        isGameActive = true;

        // ⭐ HUD 초기화를 더 안전하게 처리
        InitializeHUD();

        // 씬의 기존 NPC들 찾기 및 설정
        FindAndSetupSceneNPCs();

        // 초기 페이즈 설정
        StartTutorialPhase();

        // 이벤트 구독
        SubscribeToEvents();

        Debug.Log($"GameFlowManager 시작 - 4분 데모 모드, 씬 NPC 수: {sceneNPCs.Count}");
    }

    // ⭐ HUD 초기화 메서드 추가
    private void InitializeHUD()
    {
        if (hudController == null)
        {
            hudController = FindAnyObjectByType<HUDController>();
            if (hudController == null)
            {
                Debug.LogError("[GameFlow] HUDController를 찾을 수 없습니다!");
                return;
            }
        }

        Debug.Log("[GameFlow] HUD 초기화 시작");

        // ⭐ HUD가 초기화될 때까지 대기 후 제어
        StartCoroutine(WaitForHUDInitialization());
    }

    // ⭐ HUD 초기화 대기 코루틴
    private IEnumerator WaitForHUDInitialization()
    {
        // HUDController의 Start()가 완료될 때까지 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        // 현재 페이즈에 따라 HUD 상태 설정
        bool shouldHUDBeActive = (currentPhase == GamePhase.Gameplay);

        if (hudController != null)
        {
            Debug.Log($"[GameFlow] HUD 초기 상태 설정: {shouldHUDBeActive} (페이즈: {currentPhase})");
            hudController.SetHUDActive(shouldHUDBeActive);

            // ⭐ 설정 후 상태 확인
            yield return new WaitForSeconds(0.1f);
            bool actualState = hudController.IsHUDActive();
            if (actualState != shouldHUDBeActive)
            {
                Debug.LogWarning($"[GameFlow] HUD 상태 설정 실패! 재시도... 예상: {shouldHUDBeActive}, 실제: {actualState}");
                hudController.SetHUDActive(shouldHUDBeActive);
            }
        }
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

        // ⭐ 주기적 상태 체크 (5초마다)
        if (Time.time - lastStateCheck > 5f)
        {
            lastStateCheck = Time.time;
            CheckGameState();
        }

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

    // ⭐ 게임 상태 체크
    private void CheckGameState()
    {
        if (hudController == null) return;

        bool expectedHUDState = (currentPhase == GamePhase.Gameplay);
        bool actualHUDState = hudController.IsHUDActive();

        if (expectedHUDState != actualHUDState)
        {
            Debug.LogWarning($"[GameFlow] HUD 상태 불일치 감지! 페이즈: {currentPhase}, 예상HUD: {expectedHUDState}, 실제HUD: {actualHUDState}");

            // 자동 수정
            SetHUDSafely(expectedHUDState, $"자동 수정 (페이즈: {currentPhase})");
        }
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

        // ⭐ HUD 이벤트 구독 수정 - OnLoveGaugeFull 제거!
        if (hudController != null)
        {
            // LOVE 게이지 풀 이벤트는 MiniGameManager에서만 처리하도록 함
            // hudController.OnLoveGaugeFull.AddListener(OnLoveGaugeFull); // 이 줄 제거!

            // 진행률 업데이트 이벤트만 구독 (필요한 경우)
            hudController.OnLoveGaugeChanged += OnHUDProgressChanged;

            Debug.Log("[GameFlow] HUD 이벤트 구독 완료 (OnLoveGaugeFull 제외 - MiniGameManager에서 처리)");
        }
    }

    #region Tutorial Phase
    // ⭐ 튜토리얼 페이즈 시작 수정
    private void StartTutorialPhase()
    {
        currentPhase = GamePhase.Tutorial;
        phaseTimer = 0f;

        // ⭐ HUD 제어를 더 안전하게
        SetHUDSafely(false, "튜토리얼 시작");

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
    // ⭐ 게임플레이 페이즈 시작 수정
    private void StartGameplayPhase()
    {
        currentPhase = GamePhase.Gameplay;
        phaseTimer = 0f;

        // ⭐ HUD 제어를 더 안전하게
        SetHUDSafely(true, "게임플레이 시작");

        // 튜토리얼 UI 비활성화
        SetActiveUI(null);

        // 게임플레이 BGM 재생
        PlayBGM(gameplayBGM);

        // 컨트롤 힌트 숨기기
        if (gameUIManager != null)
        {
            gameUIManager.HideControlHints();
        }

        // ⭐ HUD 타이머 시작을 안전하게
        StartCoroutine(SafeStartHUDTimer());

        Debug.Log("게임플레이 페이즈 시작 (3분)");
        OnPhaseChanged?.Invoke(currentPhase);
    }

    // ⭐ 안전한 HUD 타이머 시작
    private IEnumerator SafeStartHUDTimer()
    {
        yield return new WaitForSeconds(0.2f); // HUD가 완전히 활성화될 때까지 대기

        if (hudController != null && hudController.IsHUDActive())
        {
            if (!hudController.IsTimerRunning())
            {
                hudController.StartTimer();
                Debug.Log("[GameFlow] HUD 타이머 시작 완료");
            }
            else
            {
                Debug.Log("[GameFlow] HUD 타이머 이미 실행 중");
            }
        }
        else
        {
            Debug.LogWarning("[GameFlow] HUD가 비활성화 상태이거나 null이어서 타이머 시작 실패");
        }
    }

    private void UpdateGameplayPhase()
    {
        // 게임플레이 시간 종료 또는 목표 달성시 엔딩
        if (phaseTimer >= gameplayTime || seducedNPCCount >= targetNPCCount)
        {
            StartEndingPhase();
        }
    }
    #endregion

    #region Ending Phase
    private void StartEndingPhase()
    {
        currentPhase = GamePhase.Ending;
        phaseTimer = 0f;

        Debug.Log("=== 엔딩 페이즈 시작 ===");

        // HUD 제어를 더 안전하게
        SetHUDSafely(false, "엔딩 시작");

        // 엔딩 UI 활성화
        Debug.Log($"EndingController: {(endingController != null ? "존재" : "null")}");
        Debug.Log($"EndingUI: {(endingUI != null ? endingUI.name : "null")}");

        // EndingController와 직접 활성화 둘 다 시도
        StartCoroutine(HandleEndingUIActivation());

        // 엔딩 BGM 재생
        PlayBGM(endingBGM);

        Debug.Log("엔딩 페이즈 시작 (30초)");
        OnPhaseChanged?.Invoke(currentPhase);
    }

    // 엔딩 UI 활성화 처리 코루틴
    private IEnumerator HandleEndingUIActivation()
    {
        bool endingUIActivated = false;

        // 1단계: EndingController 시도
        if (endingController != null)
        {
            Debug.Log("EndingController를 통한 엔딩 화면 표시 시도");
            endingController.ShowEndingScreen();

            // EndingController가 UI를 제대로 활성화했는지 확인
            yield return new WaitForSeconds(0.2f);
            if (endingUI != null && endingUI.activeInHierarchy)
            {
                Debug.Log("EndingController가 UI를 성공적으로 활성화함");
                // ShowFinalResults();
                endingUIActivated = true;
            }
            else
            {
                Debug.LogWarning("EndingController가 UI를 활성화하지 않음, 직접 활성화 시도");
            }
        }

        // 2단계: 직접 활성화 시도
        if (!endingUIActivated)
        {
            Debug.Log("직접 EndingUI 활성화");

            // EndingUI 찾기 재시도
            if (endingUI == null)
            {
                Debug.Log("EndingUI가 null이어서 다시 찾는 중...");
                endingUI = GameObject.Find("EndingUI");
                Debug.Log($"재검색 결과: {(endingUI != null ? endingUI.name : "여전히 null")}");
            }

            if (endingUI != null)
            {
                Debug.Log($"EndingUI 직접 활성화 시도: {endingUI.name}");
                SetActiveUI(endingUI);

                // 활성화 확인
                yield return new WaitForEndOfFrame();
                if (endingUI.activeInHierarchy)
                {
                    Debug.Log("EndingUI 직접 활성화 성공!");
                    ShowFinalResults();
                    endingUIActivated = true;
                }
                else
                {
                    Debug.LogError("EndingUI 직접 활성화도 실패!");
                }
            }
            else
            {
                Debug.LogError("EndingUI를 찾을 수 없습니다!");
            }
        }

        Debug.Log($"엔딩 UI 활성화 최종 결과: {endingUIActivated}");
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
        if (endingUI == null) return;

        // HUD에서 최종 점수 가져오기
        int finalScore = hudController != null ? hudController.score : 0;
        int followingCount = ZeeeingGaze.FollowerManager.Instance != null ? ZeeeingGaze.FollowerManager.Instance.GetFollowingCount() : seducedNPCCount;

        float completionRate = (float)seducedNPCCount / targetNPCCount * 100f;
        string grade = GetPerformanceGrade(completionRate);
        string targetScene = GetTargetSceneName(completionRate);

        // ResultPanel에서 텍스트 찾기
        TextMeshProUGUI resultText = null;
        Transform resultPanel = endingUI.transform.Find("ResultPanel");
        if (resultPanel != null)
            resultText = resultPanel.GetComponentInChildren<TextMeshProUGUI>();
        else
            resultText = endingUI.GetComponentInChildren<TextMeshProUGUI>();

        if (resultText != null)
        {
            //resultText.text = $"데모 완료!\n\n" +
            //                $"최종 점수: {finalScore}\n" +
            //                $"꼬셔진 NPC: {followingCount}명\n" +
            //                $"완료율: {completionRate:F1}%\n" +
            //                $"등급: {grade}\n\n" +
            //                (enableSceneTransition ? $"'{targetScene}' 씬으로 이동합니다..." : "플레이해주셔서 감사합니다!");
        }

        Debug.Log($"최종 결과 - 점수: {finalScore}, NPC: {followingCount}명, 완료율: {completionRate:F1}%, 등급: {grade}, 목표씬: {targetScene}");
    }

    private string GetTargetSceneName(float completionRate)
    {
        if (completionRate >= 100f) return "S score";
        if (completionRate >= 80f) return "A score";
        if (completionRate >= 60f) return "B score";
        if (completionRate >= 40f) return "C score";
        return "D score";
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

    // ⭐ 안전한 HUD 제어 메서드
    private void SetHUDSafely(bool active, string reason)
    {
        if (hudController == null)
        {
            Debug.LogWarning($"[GameFlow] HUD 제어 실패 - HUDController가 null ({reason})");
            return;
        }

        Debug.Log($"[GameFlow] HUD {(active ? "활성화" : "비활성화")} 요청: {reason}");

        // 즉시 설정
        hudController.SetHUDActive(active);

        // 설정 확인 및 재시도
        StartCoroutine(VerifyHUDState(active, reason));
    }

    // ⭐ HUD 상태 확인 및 재시도
    private IEnumerator VerifyHUDState(bool expectedState, string reason)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        if (hudController != null)
        {
            bool actualState = hudController.IsHUDActive();
            Debug.Log($"[GameFlow] HUD 상태 확인 ({reason}): 예상={expectedState}, 실제={actualState}");

            if (actualState != expectedState)
            {
                Debug.LogWarning($"[GameFlow] HUD 상태 불일치! 재시도 ({reason})");
                hudController.SetHUDActive(expectedState);

                // 한 번 더 확인
                yield return new WaitForSeconds(0.1f);
                actualState = hudController.IsHUDActive();
                Debug.Log($"[GameFlow] HUD 재시도 결과 ({reason}): {actualState}");
            }
        }
    }

    #region Event Handlers
    // NPC 꼬시기 이벤트 핸들러 (수정된 버전)
    private void OnNPCSeducedEvent(NPCController npc)
    {
        seducedNPCCount++;
        OnProgressUpdated?.Invoke(seducedNPCCount, targetNPCCount);

        // ⚠️ 점수는 이미 NPCController에서 처리되므로 여기서는 추가하지 않음!
        // 기존 코드 제거:
        // if (MiniGameManager.Instance != null)
        // {
        //     MiniGameManager.Instance.AddScore(npcScore, $"NPC_Seduced_{npc.GetName()}");
        // }

        string npcType = npc.IsEliteNPC() ? "Elite" : "Regular";
        string completionMethod = npc.IsMiniGameCompleted() ? "미니게임" : "일반 꼬시기";
        int seductionScore = npc.GetPointValue();
        int bonusScore = npc.IsEliteNPC() && npc.IsMiniGameCompleted() ? npc.GetMiniGameBonusScore() : 0;

        Debug.Log($"NPC 꼬시기 성공: {npc.GetName()} ({npcType}, {completionMethod}) " +
                 $"꼬시기점수: {seductionScore}" +
                 (bonusScore > 0 ? $", 미니게임보너스: {bonusScore}" : "") +
                 $" ({seducedNPCCount}/{targetNPCCount}) - 점수는 NPCController에서 이미 처리됨");
    }

    // FollowerManager 이벤트는 점수 추가 없이 로그만
    private void OnNPCSeduced(int points)
    {
        Debug.Log($"FollowerManager 이벤트 수신: {points}점 (점수는 NPCController에서 이미 처리됨)");
    }

    private void OnMiniGameCompleted(bool success, int score)
    {
        if (success)
        {
            Debug.Log($"미니게임 성공! 퍼포먼스 점수: {score} (NPC 꼬시기 점수는 별도 처리됨)");
            // 미니게임 퍼포먼스 점수는 MiniGameManager에서 자동 처리됨
            // NPC 꼬시기 점수는 NPCController.SetSeducedByMiniGame()에서 처리됨
        }
    }

    private void OnHUDProgressChanged(int current, int max)
    {
        // HUD의 진행률 변화를 처리 (필요시)
        Debug.Log($"HUD 진행률 변화: {current}/{max}");
    }
    #endregion

    #region UI Management
    private void SetActiveUI(GameObject targetUI)
    {
        Debug.Log($"SetActiveUI 호출됨 - 타겟: {(targetUI != null ? targetUI.name : "null")}");

        // 모든 UI 비활성화
        if (tutorialUI != null)
        {
            tutorialUI.SetActive(false);
            Debug.Log("TutorialUI 비활성화");
        }
        if (endingUI != null)
        {
            endingUI.SetActive(false);
            Debug.Log("EndingUI 일단 비활성화");
        }

        // 타겟 UI 활성화
        if (targetUI != null)
        {
            Debug.Log($"{targetUI.name} 활성화 시도");
            targetUI.SetActive(true);

            // 활성화 확인
            if (targetUI.activeInHierarchy)
            {
                Debug.Log($"{targetUI.name} 활성화 성공!");
            }
            else
            {
                Debug.LogError($"{targetUI.name} 활성화 실패!");
            }
        }
        else
        {
            Debug.Log("모든 UI 비활성화 (타겟이 null)");
        }
    }

    private void UpdateUI()
    {
        // 페이즈 타이머 표시 (HUD가 아닌 다른 UI 요소에서)
        if (phaseTimerText != null)
        {
            float remainingTime = GetPhaseRemainingTime();
            phaseTimerText.text = $"{currentPhase}: {Mathf.CeilToInt(remainingTime)}초";
        }

        // 전체 진행률 표시
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

        // 최종 점수 계산
        float completionRate = (float)seducedNPCCount / targetNPCCount * 100f;

        // 데모 완료 후 처리
        StartCoroutine(HandleDemoCompletion(completionRate));
    }

    private IEnumerator HandleDemoCompletion(float completionRate)
    {
        // 결과 화면 표시
        ShowFinalResults();

        // 결과를 일정 시간 표시
        yield return new WaitForSeconds(resultDisplayTime);

        // 씬 전환이 활성화되어 있으면 성적별 씬으로 이동
        if (enableSceneTransition)
        {
            // VRSceneTransition이 있는지 확인
            VRSceneTransition sceneTransition = VRSceneTransition.Instance;
            if (sceneTransition == null)
            {
                // VRSceneTransition 컴포넌트가 없으면 생성
                GameObject transitionObj = new GameObject("VR_Scene_Transition");
                sceneTransition = transitionObj.AddComponent<VRSceneTransition>();
                DontDestroyOnLoad(transitionObj);
            }

            Debug.Log($"성적별 씬 전환 시작 - 완료율: {completionRate:F1}%");
            sceneTransition.TransitionToScoreScene(completionRate);
        }
        else
        {
            // 씬 전환이 비활성화되어 있으면 기존 재시작 옵션 표시
            StartCoroutine(ShowRestartOption());
        }
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

    // HUD 관련 공용 메서드들 (레거시 호환성)
    public void AddScore(int points)
    {
        // 직접 점수 추가 대신 MiniGameManager를 통해 처리
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.AddScore(points, "GameFlowManager_Legacy");
        }
        else if (hudController != null)
        {
            hudController.UpdateScore(points);
        }
    }

    public int GetCurrentScore()
    {
        if (MiniGameManager.Instance != null)
        {
            return MiniGameManager.Instance.GetCurrentScore();
        }
        return hudController != null ? hudController.score : 0;
    }

    public void AddLoveGauge(int hearts)
    {
        if (hudController != null)
        {
            hudController.AddHeart(hearts);
        }
    }

    public bool IsLoveGaugeFull()
    {
        return hudController != null ? hudController.IsLoveGaugeFull() : false;
    }

    // ⭐ 디버깅용 메서드들 추가
    [ContextMenu("Debug: Force Enable HUD")]
    public void DebugForceEnableHUD()
    {
        SetHUDSafely(true, "디버그 강제 활성화");
    }

    [ContextMenu("Debug: Force Disable HUD")]
    public void DebugForceDisableHUD()
    {
        SetHUDSafely(false, "디버그 강제 비활성화");
    }

    [ContextMenu("Debug: Check Current State")]
    public void DebugCheckCurrentState()
    {
        Debug.Log($"=== GameFlowManager 상태 ===");
        Debug.Log($"Current Phase: {currentPhase}");
        Debug.Log($"Phase Timer: {phaseTimer:F1}s");
        Debug.Log($"HUDController: {(hudController != null ? "존재" : "null")}");
        if (hudController != null)
        {
            Debug.Log($"HUD Active: {hudController.IsHUDActive()}");
            Debug.Log($"Timer Running: {hudController.IsTimerRunning()}");
        }
    }

    [ContextMenu("Debug: Force Start Gameplay")]
    public void DebugForceStartGameplay()
    {
        StartGameplayPhase();
        Debug.Log("디버그: 게임플레이 페이즈 강제 시작");
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

        // ⭐ HUD 이벤트 구독 해제 수정
        if (hudController != null)
        {
            // OnLoveGaugeFull 구독하지 않았으므로 해제도 하지 않음
            // hudController.OnLoveGaugeFull.RemoveListener(OnLoveGaugeFull); // 이 줄 제거!

            hudController.OnLoveGaugeChanged -= OnHUDProgressChanged;
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
    
    // 디버그용 메서드들 추가
    [ContextMenu("Debug: Test Scene Transition (S Grade)")]
    public void DebugTestSGradeTransition()
    {
        TestSceneTransition(100f);
    }

    [ContextMenu("Debug: Test Scene Transition (A Grade)")]
    public void DebugTestAGradeTransition()
    {
        TestSceneTransition(85f);
    }

    [ContextMenu("Debug: Test Scene Transition (B Grade)")]
    public void DebugTestBGradeTransition()
    {
        TestSceneTransition(65f);
    }

    [ContextMenu("Debug: Test Scene Transition (C Grade)")]
    public void DebugTestCGradeTransition()
    {
        TestSceneTransition(45f);
    }

    [ContextMenu("Debug: Test Scene Transition (D Grade)")]
    public void DebugTestDGradeTransition()
    {
        TestSceneTransition(25f);
    }

    private void TestSceneTransition(float testCompletionRate)
    {
        Debug.Log($"테스트 씬 전환 시작 - 완료율: {testCompletionRate}%");
        
        VRSceneTransition sceneTransition = VRSceneTransition.Instance;
        if (sceneTransition == null)
        {
            GameObject transitionObj = new GameObject("VR_Scene_Transition");
            sceneTransition = transitionObj.AddComponent<VRSceneTransition>();
        }
        
        sceneTransition.TransitionToScoreScene(testCompletionRate);
    }

    // 씬 전환 설정 메서드들
    public void EnableSceneTransition(bool enable)
    {
        enableSceneTransition = enable;
        Debug.Log($"씬 전환 {(enable ? "활성화" : "비활성화")}");
    }

    public void SetResultDisplayTime(float time)
    {
        resultDisplayTime = Mathf.Max(0f, time);
        Debug.Log($"결과 표시 시간 설정: {resultDisplayTime}초");
    }
}