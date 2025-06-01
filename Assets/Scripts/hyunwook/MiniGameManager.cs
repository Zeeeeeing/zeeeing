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
    [SerializeField] private float feverGaugeIncreasePerSuccess = 0.2f;
    
    [Header("Fever Mode")]
    [SerializeField] private float feverGaugeMax = 1.0f;
    [SerializeField] private float feverModeTime = 15f;
    [SerializeField] private int feverModeScoreMultiplier = 3;
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI totalScoreText;
    [SerializeField] private FeverUI feverUI; // 새로운 FeverUI 컴포넌트
    
    [Header("NPC References")] 
    [SerializeField] private Transform playerTransform;
    
    // UI/이벤트 통합을 위한 이벤트 추가
    public event System.Action<MiniGameType> OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted;
    
    // 게임 상태 변수
    private int totalScore = 0;
    private float currentFeverGauge = 0f;
    private bool isFeverModeActive = false;
    
    // 현재 진행 중인 미니게임
    private enum ActiveMiniGame { None, ColorGaze, HeartGaze }
    private ActiveMiniGame currentMiniGame = ActiveMiniGame.None;
    
    private void Awake()
    {
        // FeverUI 자동 찾기 (Inspector에서 할당되지 않은 경우)
        if (feverUI == null)
        {
            feverUI = FindFeverUIComponent();
        }
        
        // UI 초기화
        UpdateScoreUI();
        UpdateFeverGaugeUI();
        
        // 미니게임 이벤트 구독
        SubscribeToMiniGameEvents();
        
        // 플레이어 참조 찾기
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
        
        Debug.Log("MiniGameManager Awake 완료 (Fever UI 통합 버전)");
    }
    
    /// <summary>
    /// FeverUI 컴포넌트를 찾는 메서드 (Unity 버전 호환성 고려)
    /// </summary>
    private FeverUI FindFeverUIComponent()
    {
        FeverUI foundFeverUI = null;
        
        // Unity 버전에 따른 호환성 처리
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
            Debug.LogWarning("FeverUI 컴포넌트를 찾을 수 없습니다! 다음을 확인하세요:\n" +
                           "1. FeverUI 스크립트가 씬의 어떤 오브젝트에 붙어있는지\n" +
                           "2. 해당 오브젝트가 활성화되어 있는지\n" +
                           "3. Inspector에서 직접 할당했는지");
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
            ZeeeingGaze.FollowerManager.Instance.OnPointsAdded += (points) => {
                totalScore += points;
                UpdateScoreUI();
            };
            Debug.Log("FollowerManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("FollowerManager 인스턴스가 없습니다!");
        }
    }
    
    private void Start()
    {
        SubscribeToFollowerManager();
        
        // MiniGameUI 초기화
        InitializeMiniGameUI();
        
        // UI 상태 디버깅
        Debug.Log("MiniGameManager 초기화 완료. UI 참조 상태:");
        Debug.Log($"totalScoreText: {(totalScoreText != null ? "있음" : "없음")}");
        Debug.Log($"feverUI (새로운): {(feverUI != null ? "있음" : "없음")}");
    }
    
    // MiniGameUI 초기화 메서드
    private void InitializeMiniGameUI()
    {
        MiniGameUI miniGameUI = FindAnyObjectByType<MiniGameUI>();
        
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
    
    // 색상-감정 매칭 미니게임 완료 처리
    private void OnColorGameCompleted(bool success, int matchCount)
    {
        Debug.Log($"ColorGame 완료: 성공={success}, 매치 수={matchCount}");
        currentMiniGame = ActiveMiniGame.None;
        
        if (success)
        {
            int scoreGain = CalculateScore(baseScorePerSuccess * matchCount);
            totalScore += scoreGain;
            
            // 피버 게이지 증가
            IncreaseFeverGauge(feverGaugeIncreasePerSuccess);
            
            // 성공 보상 처리
            HandleMiniGameSuccess(MiniGameType.ColorGaze);
        }
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 게임 완료 이벤트 발생
        OnGameCompleted?.Invoke(success, matchCount);
    }
    
    // 하트 수집 미니게임 완료 처리
    private void OnHeartGameCompleted(bool success, int heartsCount)
    {
        Debug.Log($"HeartGame 완료: 성공={success}, 하트 수={heartsCount}");
        currentMiniGame = ActiveMiniGame.None;
        
        if (success)
        {
            int scoreGain = CalculateScore(baseScorePerSuccess * heartsCount);
            totalScore += scoreGain;
            
            // 피버 게이지 증가
            IncreaseFeverGauge(feverGaugeIncreasePerSuccess);
            
            // 성공 보상 처리
            HandleMiniGameSuccess(MiniGameType.HeartGaze);
        }
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 게임 완료 이벤트 발생
        OnGameCompleted?.Invoke(success, heartsCount);
    }
    
    // 현재 적용되는 점수 계산 (피버 모드 등 고려)
    private int CalculateScore(int baseScore)
    {
        return baseScore * gamePointsMultiplier * (isFeverModeActive ? feverModeScoreMultiplier : 1);
    }
    
    // 피버 게이지 증가 (수정된 버전)
    private void IncreaseFeverGauge(float amount)
    {
        if (isFeverModeActive) return; // 이미 피버 모드면 게이지 증가 무시
        
        float previousGauge = currentFeverGauge;
        currentFeverGauge += amount;
        currentFeverGauge = Mathf.Clamp(currentFeverGauge, 0f, feverGaugeMax);
        
        Debug.Log($"피버 게이지 증가: {previousGauge:F2} → {currentFeverGauge:F2} (+{amount:F2})");
        
        // 피버 게이지가 최대에 도달하면 피버 모드 활성화
        if (currentFeverGauge >= feverGaugeMax)
        {
            ActivateFeverMode();
        }
        
        // UI 업데이트
        UpdateFeverGaugeUI();
    }
    
    // 피버 모드 활성화 (수정된 버전)
    private void ActivateFeverMode()
    {
        if (isFeverModeActive) return;
        
        isFeverModeActive = true;
        currentFeverGauge = feverGaugeMax;
        
        Debug.Log("🔥 피버 모드 활성화!");
        
        // 새로운 FeverUI 알림
        if (feverUI != null)
        {
            feverUI.OnFeverModeStart();
            Debug.Log("FeverUI에 피버 모드 시작 알림");
        }
        
        // 피버 모드 타이머 시작
        StartCoroutine(FeverModeTimer());
    }
    
    // 피버 모드 타이머 (수정된 버전)
    private IEnumerator FeverModeTimer()
    {
        float remainingTime = feverModeTime;
        
        Debug.Log($"피버 모드 타이머 시작: {feverModeTime}초");
        
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            
            // 피버 게이지를 시간에 따라 감소
            currentFeverGauge = (remainingTime / feverModeTime) * feverGaugeMax;
            
            // 새로운 FeverUI 타이머 업데이트
            if (feverUI != null)
            {
                feverUI.OnFeverTimerUpdate(remainingTime, feverModeTime);
            }
            
            // 기존 UI도 업데이트
            UpdateFeverGaugeUI();
            
            yield return null;
        }
        
        // 피버 모드 종료
        DeactivateFeverMode();
    }
    
    // 피버 모드 비활성화 (수정된 버전)
    private void DeactivateFeverMode()
    {
        if (!isFeverModeActive) return;
        
        isFeverModeActive = false;
        currentFeverGauge = 0f;
        
        Debug.Log("🔥 피버 모드 종료!");
        
        // 새로운 FeverUI 알림
        if (feverUI != null)
        {
            feverUI.OnFeverModeEnd();
            Debug.Log("FeverUI에 피버 모드 종료 알림");
        }
        
        // UI 업데이트
        UpdateFeverGaugeUI();
    }
    
    // 점수 UI 업데이트
    private void UpdateScoreUI()
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {totalScore}";
        }
        else
        {
            Debug.LogWarning("totalScoreText 참조가 없습니다!");
        }
    }
    
    // 피버 게이지 UI 업데이트 (기존 메서드 개선)
    private void UpdateFeverGaugeUI()
    {
        float normalizedValue = currentFeverGauge / feverGaugeMax;
        
        // 새로운 FeverUI 업데이트 (메인)
        if (feverUI != null)
        {
            feverUI.OnFeverGaugeChanged(normalizedValue);
        }
        else
        {
            // FeverUI가 없으면 다시 찾기 시도
            feverUI = FindFeverUIComponent();
            if (feverUI != null)
            {
                feverUI.OnFeverGaugeChanged(normalizedValue);
            }
        }
        
        // 디버그 로그 (개발 중에만)
        if (Application.isEditor && normalizedValue > 0)
        {
            Debug.Log($"피버 게이지 UI 업데이트: {normalizedValue:P1} ({currentFeverGauge:F2}/{feverGaugeMax:F2})");
        }
    }
    
    // 미니게임 성공 보상 처리
    private void HandleMiniGameSuccess(MiniGameType gameType)
    {
        NPCController npc = GetCurrentInteractingNPC();
        
        if (npc != null)
        {
            Debug.Log($"미니게임 성공 보상: {npc.GetName()} NPC 감정 상태 변경 및 유령 모드 설정");
            
            NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
            if (emotionController != null)
            {
                emotionController.ChangeEmotionState(EmotionState.Happy);
            }
            
            npc.SetSeduced();
            npc.SetGhostMode(true);
        }
        else
        {
            Debug.LogWarning("현재 상호작용 중인 NPC가 없습니다!");
        }
    }
    
    // 현재 상호작용 중인 NPC 가져오기
    private NPCController GetCurrentInteractingNPC()
    {
        NPCInteractionManager interactionManager = FindAnyObjectByType<NPCInteractionManager>();
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

    // NPC를 따라오게 만드는 처리
    public void MakeNPCFollow(NPCController npc)
    {
        if (npc == null) return;
        
        if (!npc.gameObject.activeInHierarchy) return;
        
        Debug.Log($"{npc.GetName()} NPC를 따라오게 설정");
        
        npc.SetSeduced();
        npc.SetGhostMode(true);
        
        AddScore(npc.GetPointValue());
    }
    
    public int GetFollowingNPCCount()
    {
        if (ZeeeingGaze.FollowerManager.Instance == null) return 0;
        return ZeeeingGaze.FollowerManager.Instance.GetFollowingCount();
    }

    // 점수 추가 메서드
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreUI();
    }
    
    // 미니게임 시작 메서드
    public bool StartMiniGame(MiniGameType gameType, int difficulty = 1)
    {
        if (currentMiniGame != ActiveMiniGame.None)
        {
            Debug.LogWarning("이미 진행 중인 미니게임이 있습니다!");
            return false;
        }
        
        bool started = false;
        
        switch (gameType)
        {
            case MiniGameType.ColorGaze:
                if (colorGazeGame != null)
                {
                    Debug.Log($"ColorGazeGame 시작 (난이도: {difficulty})");
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
                    Debug.Log($"HeartGazeGame 시작 (난이도: {difficulty})");
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
            Debug.Log($"미니게임 시작 이벤트 발생: {gameType}");
            OnGameStarted?.Invoke(gameType);
            
            MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
            if (ui != null)
            {
                Debug.Log("MiniGameUI 직접 호출 시도");
                ui.ShowMiniGameUI(gameType);
            }
            else
            {
                Debug.LogError("MiniGameUI 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        return started;
    }
    
    // 현재 실행 중인 미니게임 중지
    public void StopCurrentMiniGame()
    {
        Debug.Log("현재 미니게임 중지 요청");
        
        switch (currentMiniGame)
        {
            case ActiveMiniGame.ColorGaze:
                if (colorGazeGame != null)
                {
                    colorGazeGame.StopGame();
                    Debug.Log("ColorGazeGame 중지됨");
                }
                break;
                
            case ActiveMiniGame.HeartGaze:
                if (heartGazeGame != null)
                {
                    heartGazeGame.StopGame();
                    Debug.Log("HeartGazeGame 중지됨");
                }
                break;
                
            case ActiveMiniGame.None:
                Debug.Log("현재 실행 중인 미니게임이 없습니다");
                break;
        }
        
        currentMiniGame = ActiveMiniGame.None;
    }
    
    // 현재 점수 반환
    public int GetCurrentScore()
    {
        return totalScore;
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
        
        MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
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
        
    // 디버그용 피버 모드 수동 활성화 (수정된 버전)
    [ContextMenu("Debug: Activate Fever Mode")]
    public void DebugActivateFeverMode()
    {
        Debug.Log("🔥 피버 모드 수동 활성화 요청");
        currentFeverGauge = feverGaugeMax;
        ActivateFeverMode();
    }
    
    // 디버그용 피버 게이지 설정
    [ContextMenu("Debug: Set Fever Gauge 50%")]
    public void DebugSetFeverGauge50()
    {
        currentFeverGauge = feverGaugeMax * 0.5f;
        UpdateFeverGaugeUI();
        Debug.Log("피버 게이지를 50%로 설정");
    }
    
    [ContextMenu("Debug: Set Fever Gauge 90%")]
    public void DebugSetFeverGauge90()
    {
        currentFeverGauge = feverGaugeMax * 0.9f;
        UpdateFeverGaugeUI();
        Debug.Log("피버 게이지를 90%로 설정");
    }
    
    // 사용 중인 리소스 정리
    private void OnDestroy()
    {
        Debug.Log("MiniGameManager OnDestroy - 리소스 정리");
        
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
        MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
        if (ui != null)
        {
            OnGameStarted -= ui.ShowMiniGameUI;
            OnGameCompleted -= ui.ShowResultUI;
        }
        
        // 코루틴 정리
        StopAllCoroutines();
    }
}