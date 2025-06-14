using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class MiniGameManager : MonoBehaviour
{
    [Header("MiniGame References")]
    [SerializeField] private ColorGazeMiniGame colorGazeGame;
    [SerializeField] private HeartGazeMiniGame heartGazeGame;

    [Header("Game Settings")]
    [SerializeField] private int baseScorePerSuccess = 100;
    [SerializeField] private int gamePointsMultiplier = 1;

    [Header("Fever Mode (LOVE Gauge 기반)")]
    [SerializeField] private float feverModeTime = 15f;
    [SerializeField] private int feverModeScoreMultiplier = 3; // ⭐ 피버모드 점수 배율
    [SerializeField] private HUDController hudController;
    [SerializeField] private bool autoFindHUD = true;

    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI totalScoreText;
    [SerializeField] private FeverUI feverUI;

    [Header("NPC References")]
    [SerializeField] private Transform playerTransform;

    // 통합 점수 시스템
    public static MiniGameManager Instance { get; private set; }

    // UI/이벤트 통합을 위한 이벤트 추가
    public event System.Action<MiniGameType> OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted;
    public event System.Action<int> OnScoreChanged; // 점수 변화 이벤트 추가

    // 게임 상태 변수
    private int totalScore = 0;
    private bool isFeverModeActive = false;
    private NPCController currentTargetNPC;

    // 현재 진행 중인 미니게임
    private enum ActiveMiniGame { None, ColorGaze, HeartGaze }
    private ActiveMiniGame currentMiniGame = ActiveMiniGame.None;

    private void Awake()
    {
        // 싱글톤 패턴 적용 (점수 통합을 위해)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // FeverUI 자동 찾기
        if (feverUI == null)
        {
            feverUI = FindFeverUIComponent();
        }

        // HUD Controller 자동 찾기
        if (autoFindHUD && hudController == null)
        {
            hudController = FindHUDController();
        }

        // UI 초기화
        UpdateAllScoreUIs();

        // 미니게임 이벤트 구독
        SubscribeToMiniGameEvents();

        // 플레이어 참조 찾기
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

        Debug.Log("MiniGameManager Awake 완료 (통합 점수 시스템)");
    }

    /// <summary>
    /// 통합 점수 추가 메서드 (모든 점수는 여기로!) - 피버모드 배율 수정
    /// </summary>
    public void AddScore(int points, string source = "")
    {
        if (points <= 0) return;

        int previousTotal = totalScore;

        // ⭐ 피버 모드 배율 적용 - 여기서만 적용!
        int finalPoints = CalculateScore(points);

        // 총점에 누적 추가
        totalScore += finalPoints;

        // 상세 디버그 로그 - 피버모드 상태 정확히 표시
        Debug.Log($"💰 점수 누적: [{source}] " +
                 $"원본: {points} → 최종: {finalPoints} " +
                 $"(이전 총점: {previousTotal} → 새 총점: {totalScore}) " +
                 $"피버모드: {isFeverModeActive}"); // ⭐ 수정: isFeverModeActive 직접 사용

        // 모든 UI 즉시 업데이트
        UpdateAllScoreUIs();

        // 점수 변화 이벤트 발생
        OnScoreChanged?.Invoke(totalScore);

        // 추가 검증 로그
        if (hudController != null)
        {
            Debug.Log($"🔄 HUD 점수 확인: HUD.score = {hudController.score}, MiniGame.totalScore = {totalScore}");
        }
    }

    /// <summary>
    /// 피버모드를 포함한 최종 점수 계산 (디버깅 강화)
    /// </summary>
    private int CalculateScore(int baseScore)
    {
        int finalScore = baseScore * gamePointsMultiplier;

        // ⭐ 피버 모드일 때 배율 적용
        if (isFeverModeActive)
        {
            finalScore *= feverModeScoreMultiplier;
            Debug.Log($"🔥 피버모드 배율 적용: {baseScore} × {gamePointsMultiplier} × {feverModeScoreMultiplier} = {finalScore}");
        }
        else
        {
            Debug.Log($"⚪ 일반모드 점수: {baseScore} × {gamePointsMultiplier} = {finalScore}");
        }

        return finalScore;
    }


    /// <summary>
    /// 모든 점수 UI를 동기화하는 메서드 - 디버깅 강화
    /// </summary>
    private void UpdateAllScoreUIs()
    {
        Debug.Log($"📊 UI 동기화 시작: MiniGame.totalScore = {totalScore}");

        // MiniGameManager UI 업데이트
        UpdateScoreUI();

        // HUD UI 업데이트 (점수만 동기화, 증가 로직은 건드리지 않음)
        if (hudController != null)
        {
            int hudScoreBefore = hudController.score;
            hudController.SyncScore(totalScore);
            Debug.Log($"📊 HUD 동기화: {hudScoreBefore} → {totalScore}");
        }
        else
        {
            Debug.LogWarning("📊 HUD 컨트롤러가 null입니다!");
        }
    }

    /// <summary>
    /// ⭐ 수정된 HUD Controller 찾기 및 이벤트 구독 - 강화된 버전
    /// </summary>
    private HUDController FindHUDController()
    {
        HUDController foundHUD = null;

    #if UNITY_2023_1_OR_NEWER
        foundHUD = FindAnyObjectByType<HUDController>();
    #else
        foundHUD = FindObjectOfType<HUDController>();
    #endif

        if (foundHUD != null)
        {
            Debug.Log($"[MiniGameManager] HUDController 자동 발견: {foundHUD.gameObject.name}");

            // ⭐ 이벤트 구독 전에 기존 구독 해제 (중복 방지)
            foundHUD.OnLoveGaugeFull.RemoveListener(OnLoveGaugeFullHandler);
            foundHUD.OnLoveGaugeChanged -= OnLoveGaugeChangedHandler;

            // LOVE 게이지 이벤트 구독
            foundHUD.OnLoveGaugeFull.AddListener(OnLoveGaugeFullHandler);
            foundHUD.OnLoveGaugeChanged += OnLoveGaugeChangedHandler;

            Debug.Log("[MiniGameManager] LOVE 게이지 이벤트 구독 완료!");

            // ⭐ 이벤트 구독 확인 및 테스트
            Debug.Log($"[MiniGameManager] OnLoveGaugeFull 리스너 수: {foundHUD.OnLoveGaugeFull.GetPersistentEventCount()}");
            
            // ⭐ 즉시 테스트 - LOVE 게이지가 이미 가득 찬 상태인지 확인
            if (foundHUD.IsLoveGaugeFull())
            {
                Debug.Log("[MiniGameManager] 이미 LOVE 게이지가 가득 찬 상태 - 즉시 피버모드 활성화!");
                OnLoveGaugeFullHandler();
            }
        }
        else
        {
            Debug.LogError("[MiniGameManager] HUDController를 찾을 수 없습니다!");
        }

        return foundHUD;
    }

    /// <summary>
    /// ⭐ 수정된 LOVE 게이지 가득참 이벤트 핸들러
    /// </summary>
    private void OnLoveGaugeFullHandler()
    {
        Debug.Log($"💖 [MiniGameManager] OnLoveGaugeFullHandler 호출됨!");
        Debug.Log($"💖 [MiniGameManager] 현재 피버모드 상태: {isFeverModeActive}");
        
        // ⭐ 호출 위치 추적
        Debug.Log($"💖 [MiniGameManager] 호출 스택 정보:\n{System.Environment.StackTrace}");

        // 피버모드가 이미 활성화되어 있는지 확인
        if (!isFeverModeActive)
        {
            Debug.Log("💖 [MiniGameManager] LOVE 게이지가 가득참! 피버 모드 활성화 시도!");
            ActivateFeverMode();
        }
        else
        {
            Debug.Log("💖 [MiniGameManager] 이미 피버 모드가 활성화되어 있음 - 활성화 건너뜀");
        }
        
        Debug.Log("💖 [MiniGameManager] OnLoveGaugeFullHandler 처리 완료");
    }

    /// <summary>
    /// LOVE 게이지 변화 이벤트 핸들러 (수정된 버전!)
    /// </summary>
    private void OnLoveGaugeChangedHandler(int currentHearts, int maxHearts)
    {
        // LOVE 게이지 비율을 FeverUI에 반영
        float loveRatio = (float)currentHearts / maxHearts;

        if (feverUI != null)
        {
            // 피버 모드가 아닐 때만 LOVE 게이지 비율을 FeverUI에 반영
            if (!isFeverModeActive)
            {
                feverUI.OnFeverGaugeChanged(loveRatio);
                Debug.Log($"💖 LOVE 게이지 → FeverUI 동기화: {currentHearts}/{maxHearts} ({loveRatio:P1})");
            }
            else
            {
                Debug.Log($"🔥 피버 모드 중이므로 LOVE 게이지 → FeverUI 동기화 건너뜀");
            }
        }
        else
        {
            Debug.LogWarning("FeverUI가 null입니다! LOVE 게이지 동기화 불가");
        }

        Debug.Log($"LOVE 게이지 변화: {currentHearts}/{maxHearts} ({loveRatio:P1})");
    }

    /// <summary>
    /// FeverUI 컴포넌트를 찾는 메서드
    /// </summary>
    private FeverUI FindFeverUIComponent()
    {
        FeverUI foundFeverUI = null;

#if UNITY_2023_1_OR_NEWER
        foundFeverUI = FindAnyObjectByType<FeverUI>();
#else
            foundFeverUI = FindObjectOfType<FeverUI>();
#endif

        if (foundFeverUI != null)
        {
            Debug.Log($"FeverUI 자동 발견: {foundFeverUI.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("FeverUI 컴포넌트를 찾을 수 없습니다!");
        }

        return foundFeverUI;
    }

    private void SubscribeToMiniGameEvents()
    {
        if (colorGazeGame != null)
        {
            colorGazeGame.OnGameCompleted += OnColorGameCompleted;
            Debug.Log("ColorGazeGame 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("colorGazeGame 참조가 없습니다!");
        }

        if (heartGazeGame != null)
        {
            heartGazeGame.OnGameCompleted += OnHeartGameCompleted;
            Debug.Log("HeartGazeGame 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("heartGazeGame 참조가 없습니다!");
        }
    }

    private void SubscribeToFollowerManager()
    {
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            ZeeeingGaze.FollowerManager.Instance.OnPointsAdded += (points) =>
            {
                // ⚠️ FollowerManager에서 오는 점수는 이미 NPCController에서 처리되므로 
                // 여기서는 점수 추가하지 않고 로그만 출력
                Debug.Log($"FollowerManager 이벤트 수신: {points}점 (NPCController에서 이미 처리됨 - 중복 방지)");

                // 기존 코드 제거:
                // AddScore(points, "FollowerManager");
            };
            Debug.Log("FollowerManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("FollowerManager 인스턴스가 없습니다!");
        }
    }

    // ⭐ Start 메서드에서도 재시도 로직 강화
    private void Start()
    {
        SubscribeToFollowerManager();

        // MiniGameUI 초기화
        InitializeMiniGameUI();

        // ⭐ HUD 연동 재시도 (Start에서 한 번 더) - 강화된 버전
        if (hudController == null && autoFindHUD)
        {
            Debug.Log("[MiniGameManager] Start에서 HUD 재검색 시도...");
            hudController = FindHUDController();
        }
        else if (hudController != null)
        {
            Debug.Log("[MiniGameManager] 기존 HUD 연결 상태 재확인...");
            
            // ⭐ 기존 HUD가 있어도 이벤트 구독 상태 재확인
            hudController.OnLoveGaugeFull.RemoveListener(OnLoveGaugeFullHandler);
            hudController.OnLoveGaugeFull.AddListener(OnLoveGaugeFullHandler);
            
            if (hudController.OnLoveGaugeChanged != null)
            {
                hudController.OnLoveGaugeChanged -= OnLoveGaugeChangedHandler;
            }
            hudController.OnLoveGaugeChanged += OnLoveGaugeChangedHandler;
            
            Debug.Log("[MiniGameManager] HUD 이벤트 재구독 완료");
            
            // ⭐ 현재 LOVE 게이지 상태 확인
            if (hudController.IsLoveGaugeFull())
            {
                Debug.Log("[MiniGameManager] Start 시점에 LOVE 게이지가 이미 가득참 - 피버모드 활성화!");
                OnLoveGaugeFullHandler();
            }
        }

        // 초기 점수 동기화
        UpdateAllScoreUIs();

        // ⭐ 초기화 완료 후 상태 체크
        StartCoroutine(DelayedStatusCheck());

        // UI 상태 디버깅
        Debug.Log("[MiniGameManager] 초기화 완료. UI 참조 상태:");
        Debug.Log($"totalScoreText: {(totalScoreText != null ? "있음" : "없음")}");
        Debug.Log($"feverUI: {(feverUI != null ? "있음" : "없음")}");
        Debug.Log($"hudController: {(hudController != null ? "있음" : "없음")}");
    }

    // ⭐ 지연된 상태 체크 코루틴 추가
    private IEnumerator DelayedStatusCheck()
    {
        yield return new WaitForSeconds(1f); // 1초 후 체크
        
        Debug.Log("=== [MiniGameManager] 지연된 상태 체크 ===");
        Debug.Log($"HUD 연결 상태: {(hudController != null ? "연결됨" : "null")}");
        Debug.Log($"현재 피버모드 상태: {isFeverModeActive}");
        
        if (hudController != null)
        {
            bool isLoveFull = hudController.IsLoveGaugeFull();
            int currentHearts = hudController.GetCurrentHearts();
            int maxHearts = hudController.GetMaxHearts();
            
            Debug.Log($"LOVE 게이지 상태: {currentHearts}/{maxHearts} (가득참: {isLoveFull})");
            
            // ⭐ LOVE 게이지가 가득 찬데 피버모드가 비활성화라면 강제 활성화
            if (isLoveFull && !isFeverModeActive)
            {
                Debug.LogWarning("[MiniGameManager] LOVE 게이지가 가득 찬데 피버모드가 비활성화됨! 강제 활성화!");
                OnLoveGaugeFullHandler();
            }
        }
    }

    // MiniGameUI 초기화 메서드
    private void InitializeMiniGameUI()
    {
#if UNITY_2023_1_OR_NEWER
        MiniGameUI miniGameUI = FindAnyObjectByType<MiniGameUI>();
#else
            MiniGameUI miniGameUI = FindObjectOfType<MiniGameUI>();
#endif

        if (miniGameUI == null)
        {
            GameObject miniGameCanvasObj = GameObject.Find("MiniGameCanvas");
            if (miniGameCanvasObj != null)
            {
                miniGameUI = miniGameCanvasObj.GetComponent<MiniGameUI>();
                if (miniGameUI == null)
                {
                    miniGameUI = miniGameCanvasObj.AddComponent<MiniGameUI>();
                    Debug.Log("MiniGameCanvas에 MiniGameUI 컴포넌트 추가됨");
                }
            }
            else
            {
                Debug.LogError("MiniGameCanvas 게임 오브젝트를 찾을 수 없습니다!");
            }
        }

        if (miniGameUI != null)
        {
            Debug.Log("MiniGameUI 컴포넌트 찾음");
            miniGameUI.SetMiniGameManager(this);

            OnGameStarted -= miniGameUI.ShowMiniGameUI;
            OnGameStarted += miniGameUI.ShowMiniGameUI;

            OnGameCompleted -= miniGameUI.ShowResultUI;
            OnGameCompleted += miniGameUI.ShowResultUI;
        }
        else
        {
            Debug.LogError("MiniGameUI 컴포넌트를 찾을 수 없습니다! Canvas 구조를 확인하세요.");
        }
    }

    // ⭐ 색상-감정 매칭 미니게임 완료 처리 (피버모드 배율 적용!)
    private void OnColorGameCompleted(bool success, int matchCount)
    {
        Debug.Log($"ColorGame 완료: 성공={success}, 매치 수={matchCount}");
        currentMiniGame = ActiveMiniGame.None;

        if (success)
        {
            int gameScore = baseScorePerSuccess * matchCount;
            AddScore(gameScore, "ColorGame_Performance");
            HandleMiniGameSuccess();
        }
        
        // 성공/실패와 관계없이 타겟 NPC 초기화
        currentTargetNPC = null; 

        OnGameCompleted?.Invoke(success, matchCount);
    }

    // ⭐ 하트 수집 미니게임 완료 처리 (피버모드 배율 적용!)
    private void OnHeartGameCompleted(bool success, int heartsCount)
    {
        Debug.Log($"HeartGame 완료: 성공={success}, 하트 수={heartsCount}");
        currentMiniGame = ActiveMiniGame.None;

        if (success)
        {
            int gameScore = baseScorePerSuccess * heartsCount;
            AddScore(gameScore, "HeartGame_Performance");
            HandleMiniGameSuccess();
        }

        // 성공/실패와 관계없이 타겟 NPC 초기화
        currentTargetNPC = null;
        
        OnGameCompleted?.Invoke(success, heartsCount);
    }

    // 현재 실행 중인 미니게임 중지 (수정됨)
    public void StopCurrentMiniGame()
    {
        Debug.Log("현재 미니게임 중지 요청");

        switch (currentMiniGame)
        {
            case ActiveMiniGame.ColorGaze:
                if (colorGazeGame != null) colorGazeGame.StopGame();
                break;
            case ActiveMiniGame.HeartGaze:
                if (heartGazeGame != null) heartGazeGame.StopGame();
                break;
        }

        currentMiniGame = ActiveMiniGame.None;
        currentTargetNPC = null; // 중지 시에도 타겟 NPC 초기화
    }

    // MiniGameManager.cs의 ActivateFeverMode 메서드를 다음과 같이 수정:

/// <summary>
/// ⭐ 수정된 피버 모드 활성화 - 디버깅 강화 및 안전성 개선
/// </summary>
private void ActivateFeverMode()
{
    // 중복 활성화 방지
    if (isFeverModeActive)
    {
        Debug.LogWarning("🔥 [MiniGameManager] 피버 모드가 이미 활성화되어 있습니다!");
        return;
    }

    // ⭐ 피버모드 상태 설정
    isFeverModeActive = true;

    // ⭐ 상세 디버그 로그 추가
    Debug.Log($"🔥💖 [MiniGameManager] 피버 모드 활성화 성공!");
    Debug.Log($"🔥 [MiniGameManager] 점수 배율: {feverModeScoreMultiplier}x");
    Debug.Log($"🔥 [MiniGameManager] 지속 시간: {feverModeTime}초");
    
    // ⭐ 호출 위치 추적 (디버깅용)
    Debug.Log($"🔥 [MiniGameManager] 호출 스택:\n{System.Environment.StackTrace}");

    // HUD에 피버 모드 알림
    if (hudController != null)
    {
        hudController.SetFeverMode(true);
        Debug.Log("🔥 [MiniGameManager] HUD에 피버 모드 알림 전송 완료");
    }
    else
    {
        Debug.LogWarning("🔥 [MiniGameManager] HUD 컨트롤러가 null - 피버 모드 알림 실패");
    }

    // FeverUI 알림
    if (feverUI != null)
    {
        feverUI.OnFeverModeStart();
        Debug.Log("🔥 [MiniGameManager] FeverUI에 피버 모드 시작 알림 완료");
    }
    else
    {
        Debug.LogWarning("🔥 [MiniGameManager] FeverUI가 null - 피버 모드 알림 실패");
    }

    // ⭐ 기존 피버모드 타이머가 실행 중이면 중지 (안전성 강화)
    StopAllCoroutines(); // 모든 코루틴 중지 후 새로 시작
    
    // 피버 모드 타이머 시작
    StartCoroutine(FeverModeTimer());
    
    Debug.Log("🔥 [MiniGameManager] 피버 모드 활성화 처리 완료!");
}

    /// <summary>
    /// ⭐ 수정된 피버 모드 타이머 - 안전성 강화
    /// </summary>
    private IEnumerator FeverModeTimer()
    {
        float remainingTime = feverModeTime;

        Debug.Log($"🔥 [MiniGameManager] 피버 모드 타이머 시작: {feverModeTime}초");
        Debug.Log($"🔥 [MiniGameManager] 피버모드 상태 확인: {isFeverModeActive}");

        // ⭐ 피버모드가 활성화되어 있는 동안만 타이머 실행
        while (remainingTime > 0 && isFeverModeActive)
        {
            remainingTime -= Time.deltaTime;

            // FeverUI 타이머 업데이트
            if (feverUI != null)
            {
                feverUI.OnFeverTimerUpdate(remainingTime, feverModeTime);
            }

            // ⭐ 매 5초마다 상태 로그 (디버깅용)
            if (Mathf.FloorToInt(remainingTime) % 5 == 0 && 
                Mathf.FloorToInt(remainingTime) != Mathf.FloorToInt(remainingTime + Time.deltaTime))
            {
                Debug.Log($"🔥 [MiniGameManager] 피버모드 남은시간: {Mathf.CeilToInt(remainingTime)}초 (상태: {isFeverModeActive})");
            }

            yield return null;
        }

        // ⭐ 타이머 종료 이유 로그
        if (remainingTime <= 0)
        {
            Debug.Log("🔥 [MiniGameManager] 피버 모드 시간 만료로 종료");
        }
        else if (!isFeverModeActive)
        {
            Debug.Log("🔥 [MiniGameManager] 피버 모드가 외부에서 비활성화됨");
        }

        // 피버 모드 종료 처리
        DeactivateFeverMode();
    }

    /// <summary>
    /// ⭐ 수정된 피버 모드 비활성화 - 안전성 강화
    /// </summary>
    private void DeactivateFeverMode()
    {
        // 이미 비활성화되어 있으면 중복 처리 방지
        if (!isFeverModeActive) 
        {
            Debug.LogWarning("🔥 [MiniGameManager] 이미 피버 모드가 비활성화되어 있습니다!");
            return;
        }

        // ⭐ 피버모드 상태 해제
        isFeverModeActive = false;

        Debug.Log("🔥💖 [MiniGameManager] 피버 모드 종료 시작!");

        // HUD에 피버 모드 종료 알림
        if (hudController != null)
        {
            hudController.SetFeverMode(false);
            Debug.Log("🔥 [MiniGameManager] HUD에 피버 모드 종료 알림 전송 완료");
        }

        // FeverUI 알림
        if (feverUI != null)
        {
            feverUI.OnFeverModeEnd();
            Debug.Log("🔥 [MiniGameManager] FeverUI에 피버 모드 종료 알림 완료");
        }

        // ⭐ 남은 코루틴들 정리 (안전성 강화)
        StopAllCoroutines();
        
        Debug.Log("🔥 [MiniGameManager] 피버 모드 종료 처리 완료!");
    }

    // 점수 UI 업데이트
    private void UpdateScoreUI()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"{totalScore}";
        }
        else
        {
            Debug.LogWarning("totalScoreText 참조가 없습니다!");
        }
    }

    // 미니게임 성공 보상 처리
    private void HandleMiniGameSuccess()
    {
        if (currentTargetNPC != null)
        {
            Debug.Log($"미니게임 성공 보상: {currentTargetNPC.GetName()} NPC - 저장된 NPC를 꼬십니다.");

            // NPC 감정 상태 변경
            NPCEmotionController emotionController = currentTargetNPC.GetComponent<NPCEmotionController>();
            if (emotionController != null)
            {
                emotionController.ChangeEmotionState(EmotionState.Happy);
            }

            // 미니게임 성공으로 NPC 꼬시기
            currentTargetNPC.SetSeducedByMiniGame();
            currentTargetNPC.SetGhostMode(true);
        }
        else
        {
            Debug.LogWarning("미니게임 보상을 전달할 대상 NPC가 지정되지 않았습니다!");
        }
    }

    // 현재 상호작용 중인 NPC 가져오기
    private NPCController GetCurrentInteractingNPC()
    {
#if UNITY_2023_1_OR_NEWER
        NPCInteractionManager interactionManager = FindAnyObjectByType<NPCInteractionManager>();
#else
            NPCInteractionManager interactionManager = FindObjectOfType<NPCInteractionManager>();
#endif

        if (interactionManager != null)
        {
            NPCController npc = interactionManager.GetCurrentInteractingNPC();
            if (npc != null)
            {
                return npc;
            }
            else
            {
                Debug.LogWarning("현재 상호작용 중인 NPC가 없습니다");
            }
        }
        else
        {
            Debug.LogError("NPCInteractionManager를 찾을 수 없습니다!");
        }

        return null;
    }

    // NPC가 이미 따라오고 있는지 확인
    public bool IsNPCFollowing(NPCController npc)
    {
        return ZeeeingGaze.FollowerManager.Instance != null && ZeeeingGaze.FollowerManager.Instance.IsNPCFollowing(npc);
    }

    // NPC를 따라오게 만드는 처리 (레거시 메서드 - 사용하지 않음)
    public void MakeNPCFollow(NPCController npc)
    {
        if (npc == null) return;

        if (!npc.gameObject.activeInHierarchy) return;

        Debug.Log($"{npc.GetName()} NPC를 따라오게 설정 (레거시 메서드 - 점수는 NPCController에서 처리됨)");

        npc.SetSeduced();
        npc.SetGhostMode(true);
    }

    public int GetFollowingNPCCount()
    {
        if (ZeeeingGaze.FollowerManager.Instance == null) return 0;
        return ZeeeingGaze.FollowerManager.Instance.GetFollowingCount();
    }

    // 미니게임 시작 메서드
    public bool StartMiniGame(MiniGameType gameType, int difficulty, NPCController targetNPC)
    {
        if (currentMiniGame != ActiveMiniGame.None)
        {
            Debug.LogWarning("이미 진행 중인 미니게임이 있습니다!");
            return false;
        }

        if (targetNPC == null)
        {
            Debug.LogError("미니게임을 시작할 대상 NPC가 null입니다!");
            return false;
        }

        // 대상 NPC 저장
        this.currentTargetNPC = targetNPC;
        bool started = false;

        switch (gameType)
        {
            case MiniGameType.ColorGaze:
                if (colorGazeGame != null)
                {
                    Debug.Log($"ColorGazeGame 시작 (대상: {targetNPC.GetName()}, 난이도: {difficulty})");
                    colorGazeGame.StartMiniGame(difficulty);
                    currentMiniGame = ActiveMiniGame.ColorGaze;
                    started = true;
                }
                else
                {
                    Debug.LogError("colorGazeGame 참조가 null입니다!");
                }
                break;

            case MiniGameType.HeartGaze:
                if (heartGazeGame != null)
                {
                    Debug.Log($"HeartGazeGame 시작 (대상: {targetNPC.GetName()}, 난이도: {difficulty})");
                    heartGazeGame.StartMiniGame(difficulty);
                    currentMiniGame = ActiveMiniGame.HeartGaze;
                    started = true;
                }
                else
                {
                    Debug.LogError("heartGazeGame 참조가 null입니다!");
                }
                break;
        }

        if (started)
        {
            OnGameStarted?.Invoke(gameType);
        }
        else
        {
            // 시작에 실패하면 저장했던 NPC를 초기화
            this.currentTargetNPC = null;
        }

        return started;
    }

    // 현재 점수 반환 (통합!)
    public int GetCurrentScore()
    {
        return totalScore;
    }

    // 현재 피버 모드 상태 반환
    public bool IsFeverModeActive()
    {
        return isFeverModeActive;
    }

    // 미니게임 타입 열거형
    public enum MiniGameType
    {
        ColorGaze,
        HeartGaze
    }

    public void ForceCleanupAllUI()
    {
        Debug.Log("모든 미니게임 UI 강제 정리");

#if UNITY_2023_1_OR_NEWER
        MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
#else
            MiniGameUI ui = FindObjectOfType<MiniGameUI>();
#endif

        if (ui != null)
        {
            ui.HideAllPanels();
        }

        if (colorGazeGame != null)
            colorGazeGame.HideGameUI();

        if (heartGazeGame != null)
            heartGazeGame.HideGameUI();

        LogGameUIStates();
    }

    // 디버깅용 UI 상태 로깅
    private void LogGameUIStates()
    {
        Debug.Log($"미니게임 UI 상태: " +
                $"ColorGame UI: {(colorGazeGame != null ? colorGazeGame.IsGameUIActive() : "null")}, " +
                $"HeartGame UI: {(heartGazeGame != null ? heartGazeGame.IsGameUIActive() : "null")}, " +
                $"FeverUI: {(feverUI != null ? "활성" : "null")}");
    }

    // ⭐ 디버그 메서드들 - 피버모드 점수 테스트 추가!
    [ContextMenu("Debug: Add 100 Score")]
    public void DebugAdd100Score()
    {
        AddScore(100, "Debug");
        Debug.Log("💰 디버그: 100점 추가");
    }

    [ContextMenu("Debug: Check Current Scores")]
    public void DebugCheckCurrentScores()
    {
        int hudScore = hudController != null ? hudController.score : -1;
        Debug.Log($"📊 현재 점수 상태 체크:\n" +
                 $"- MiniGameManager.totalScore: {totalScore}\n" +
                 $"- HUDController.score: {hudScore}\n" +
                 $"- 피버 모드: {isFeverModeActive}\n" +
                 $"- UI 참조: totalScoreText={totalScoreText != null}, HUD연결={hudController != null}");
    }

    [ContextMenu("Debug: Test Fever Mode Score Multiplier")]
    public void DebugTestFeverModeScore()
    {
        Debug.Log("=== 피버모드 점수 배율 테스트 ===");

        // 기본 점수 테스트
        int baseScore = 100;
        Debug.Log($"기본 점수: {baseScore}");

        // 피버모드 OFF 상태
        if (isFeverModeActive)
        {
            DeactivateFeverMode();
        }

        int normalScore = CalculateScore(baseScore);
        Debug.Log($"일반 모드 최종 점수: {normalScore}");

        // 피버모드 ON 상태
        ActivateFeverMode();
        int feverScore = CalculateScore(baseScore);
        Debug.Log($"피버 모드 최종 점수: {feverScore} (배율: {feverModeScoreMultiplier})");

        Debug.Log($"점수 차이: {feverScore - normalScore} (+{((float)feverScore / normalScore - 1) * 100:F0}%)");
    }

    [ContextMenu("Debug: Add Score in Current Mode")]
    public void DebugAddScoreInCurrentMode()
    {
        int testScore = 100;
        string mode = isFeverModeActive ? "피버모드" : "일반모드";
        Debug.Log($"💰 {mode}에서 {testScore}점 추가 테스트");

        AddScore(testScore, $"Debug_{mode}");
    }

    [ContextMenu("Debug: Fill Love Gauge (Activate Fever)")]
    public void DebugFillLoveGaugeAndActivateFever()
    {
        if (hudController != null)
        {
            hudController.FillLoveGauge();
            Debug.Log("💖 LOVE 게이지를 가득 채워서 피버 모드 활성화!");
        }
        else
        {
            Debug.LogError("HUDController가 연결되지 않았습니다!");
        }
    }

    [ContextMenu("Debug: Reset Love Gauge")]
    public void DebugResetLoveGauge()
    {
        if (hudController != null)
        {
            hudController.ResetLoveGauge();
            Debug.Log("💔 LOVE 게이지 리셋");
        }
    }

    [ContextMenu("Debug: Force Activate Fever Mode")]
    public void DebugForceActivateFeverMode()
    {
        Debug.Log("🔥 피버 모드 강제 활성화");
        ActivateFeverMode();
    }

    [ContextMenu("Debug: Force Deactivate Fever Mode")]
    public void DebugForceDeactivateFeverMode()
    {
        Debug.Log("🔥 피버 모드 강제 종료");
        DeactivateFeverMode();
    }

    // 사용 중인 리소스 정리
    private void OnDestroy()
    {
        Debug.Log("MiniGameManager OnDestroy - 리소스 정리");

        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }

        // HUD 이벤트 구독 해제
        if (hudController != null)
        {
            hudController.OnLoveGaugeFull.RemoveListener(OnLoveGaugeFullHandler);
            hudController.OnLoveGaugeChanged -= OnLoveGaugeChangedHandler;
        }

        // 이벤트 구독 해제
        if (colorGazeGame != null)
        {
            colorGazeGame.OnGameCompleted -= OnColorGameCompleted;
        }

        if (heartGazeGame != null)
        {
            heartGazeGame.OnGameCompleted -= OnHeartGameCompleted;
        }

        // MiniGameUI 이벤트 구독 해제
#if UNITY_2023_1_OR_NEWER
        MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
#else
            MiniGameUI ui = FindObjectOfType<MiniGameUI>();
#endif

        if (ui != null)
        {
            OnGameStarted -= ui.ShowMiniGameUI;
            OnGameCompleted -= ui.ShowResultUI;
        }

        // 코루틴 정리
        StopAllCoroutines();
    }

    /// <summary>
    /// 외부에서 피버모드를 활성화하는 공개 메서드
    /// </summary>
    public void TriggerFeverMode()
    {
        Debug.Log("🔥💖 외부에서 피버모드 활성화 요청!");
        ActivateFeverMode();
    }

    /// <summary>
    /// 디버그용 메서드들 - 피버모드 연결 상태 확인
    /// </summary>
    [ContextMenu("Debug: Check Fever Mode Connection")]
public void DebugCheckFeverModeConnection()
{
    Debug.Log("=== 피버모드 연결 상태 점검 ===");
    Debug.Log($"현재 피버모드 상태: {isFeverModeActive}");
    Debug.Log($"HUD 컨트롤러 연결: {(hudController != null ? "연결됨" : "연결 안됨")}");
    Debug.Log($"FeverUI 연결: {(feverUI != null ? "연결됨" : "연결 안됨")}");
    
    if (hudController != null)
    {
        Debug.Log($"OnLoveGaugeFull 이벤트 리스너 수: {hudController.OnLoveGaugeFull.GetPersistentEventCount()}");
        
        // 수동으로 피버모드 활성화 테스트
        Debug.Log("수동 피버모드 활성화 테스트 시작");
        OnLoveGaugeFullHandler();
    }
}

[ContextMenu("Debug: Manual Activate Fever Mode")]
public void DebugManualActivateFeverMode()
{
    Debug.Log("🔥 수동 피버모드 활성화 시도");
    ActivateFeverMode();
}

    [ContextMenu("Debug: Test Score with Manual Fever")]
    public void DebugTestScoreWithManualFever()
    {
        Debug.Log("=== 수동 피버모드 점수 테스트 ===");

        // 현재 상태 확인
        Debug.Log($"테스트 전 피버모드 상태: {isFeverModeActive}");

        // 수동으로 피버모드 활성화
        if (!isFeverModeActive)
        {
            ActivateFeverMode();
        }

        // 테스트 점수 추가
        int testScore = 100;
        Debug.Log($"피버모드에서 {testScore}점 추가 테스트");
        AddScore(testScore, "Manual_Fever_Test");

        Debug.Log($"테스트 후 총점: {totalScore}");
    }
[ContextMenu("Debug: Force Check Love Gauge and Activate Fever")]
public void DebugForceCheckLoveGaugeAndActivateFever()
{
    Debug.Log("=== 강제 LOVE 게이지 체크 및 피버모드 활성화 ===");
    
    if (hudController == null)
    {
        Debug.LogError("HUDController가 null입니다!");
        hudController = FindHUDController();
        if (hudController == null)
        {
            Debug.LogError("HUDController를 찾을 수 없습니다!");
            return;
        }
    }
    
    // 현재 상태 출력
    bool isLoveFull = hudController.IsLoveGaugeFull();
    int currentHearts = hudController.GetCurrentHearts();
    int maxHearts = hudController.GetMaxHearts();
    
    Debug.Log($"현재 LOVE 게이지: {currentHearts}/{maxHearts} (가득참: {isLoveFull})");
    Debug.Log($"현재 피버모드: {isFeverModeActive}");
    
    // LOVE 게이지를 가득 채우고 피버모드 활성화
    if (!isLoveFull)
    {
        Debug.Log("LOVE 게이지를 가득 채웁니다...");
        hudController.FillLoveGauge();
    }
    
    // 피버모드 강제 활성화
    Debug.Log("피버모드 강제 활성화 시도...");
    OnLoveGaugeFullHandler();
}

[ContextMenu("Debug: Test Event Subscription")]
public void DebugTestEventSubscription()
{
    Debug.Log("=== 이벤트 구독 상태 테스트 ===");
    
    if (hudController == null)
    {
        Debug.LogError("HUDController가 null입니다!");
        return;
    }
    
    Debug.Log($"OnLoveGaugeFull 이벤트 리스너 수: {hudController.OnLoveGaugeFull.GetPersistentEventCount()}");
    Debug.Log($"OnLoveGaugeChanged 이벤트 등록 여부: {(hudController.OnLoveGaugeChanged != null)}");
    
    // 수동으로 이벤트 발생시켜 테스트
    Debug.Log("수동으로 OnLoveGaugeFull 이벤트 발생...");
    hudController.OnLoveGaugeFull?.Invoke();
}

[ContextMenu("Debug: Manual Fever Activation Test")]
public void DebugManualFeverActivationTest()
{
    Debug.Log("=== 수동 피버모드 활성화 테스트 ===");
    
    Debug.Log($"활성화 전 피버모드 상태: {isFeverModeActive}");
    
    // 직접 OnLoveGaugeFullHandler 호출
    OnLoveGaugeFullHandler();
    
    Debug.Log($"활성화 후 피버모드 상태: {isFeverModeActive}");
    
    // 점수 테스트
    if (isFeverModeActive)
    {
        Debug.Log("피버모드에서 100점 추가 테스트...");
        AddScore(100, "Manual_Fever_Test");
    }
}

[ContextMenu("Debug: Check All Connections")]
public void DebugCheckAllConnections()
{
    Debug.Log("=== 모든 연결 상태 체크 ===");
    Debug.Log($"MiniGameManager.Instance: {(Instance != null ? "존재" : "null")}");
    Debug.Log($"hudController: {(hudController != null ? "연결됨" : "null")}");
    Debug.Log($"feverUI: {(feverUI != null ? "연결됨" : "null")}");
    Debug.Log($"현재 피버모드: {isFeverModeActive}");
    
    if (hudController != null)
    {
        Debug.Log($"HUD GameObject: {hudController.gameObject.name}");
        Debug.Log($"HUD IsLoveGaugeFull: {hudController.IsLoveGaugeFull()}");
        Debug.Log($"HUD Hearts: {hudController.GetCurrentHearts()}/{hudController.GetMaxHearts()}");
    }
    
    // HUD 재검색 시도
    if (hudController == null)
    {
        Debug.Log("HUD 재검색 시도...");
        hudController = FindHUDController();
    }
}
}