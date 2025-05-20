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
    [SerializeField] private NPCChaseMiniGame chaseGame;
    
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
    [SerializeField] private Image feverGaugeImage;
    [SerializeField] private GameObject feverModeIndicator;
    
    [Header("NPC References")] 
    [SerializeField] private Transform playerTransform; // 플레이어 위치 (NPC가 따라갈 대상)
    
    // UI/이벤트 통합을 위한 이벤트 추가
    public event System.Action<MiniGameType> OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted;
    
    // 게임 상태 변수
    private int totalScore = 0;
    private float currentFeverGauge = 0f;
    private bool isFeverModeActive = false;
    
    // 현재 진행 중인 미니게임
    private enum ActiveMiniGame { None, ColorGaze, HeartGaze, NPCChase }
    private ActiveMiniGame currentMiniGame = ActiveMiniGame.None;
    
    private void Awake()
    {
        // UI 초기화
        UpdateScoreUI();
        UpdateFeverGaugeUI();
        if (feverModeIndicator != null)
            feverModeIndicator.SetActive(false);
        
        // 미니게임 이벤트 구독
        SubscribeToMiniGameEvents();
        
        // 플레이어 참조 찾기
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
        
        Debug.Log("MiniGameManager Awake 완료");
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
        
        if (chaseGame != null)
        {
            chaseGame.OnGameCompleted += OnChaseGameCompleted;
            Debug.Log("ChaseGame 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("chaseGame 참조가 없습니다!");
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
        Debug.Log($"feverGaugeImage: {(feverGaugeImage != null ? "있음" : "없음")}");
        Debug.Log($"feverModeIndicator: {(feverModeIndicator != null ? "있음" : "없음")}");
    }
    
    // MiniGameUI 초기화 메서드 (새로 추가)
    private void InitializeMiniGameUI()
    {
        // 여러 방법으로 MiniGameUI 컴포넌트 찾기 시도
        MiniGameUI miniGameUI = FindAnyObjectByType<MiniGameUI>();
        
        // 못 찾았다면 씬에서 이름으로 찾기 시도
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
            
            // miniGameUI에 MiniGameManager 참조 설정
            miniGameUI.SetMiniGameManager(this);
            
            // 이벤트 구독
            OnGameStarted -= miniGameUI.ShowMiniGameUI;
            OnGameStarted += miniGameUI.ShowMiniGameUI;
            
            OnGameCompleted -= miniGameUI.ShowResultUI;
            OnGameCompleted += miniGameUI.ShowResultUI;
            
            // 테스트 코드 제거하거나 디버그 플래그로 제어
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
            
            // 성공 보상 처리 (NPC를 따라오게 하는 등)
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
    
    // NPC 추적 미니게임 완료 처리
    private void OnChaseGameCompleted(bool success, int triggerCount)
    {
        Debug.Log($"ChaseGame 완료: 성공={success}, 트리거 수={triggerCount}");
        currentMiniGame = ActiveMiniGame.None;
        
        if (success)
        {
            int scoreGain = CalculateScore(baseScorePerSuccess * triggerCount);
            totalScore += scoreGain;
            
            // 피버 게이지 증가
            IncreaseFeverGauge(feverGaugeIncreasePerSuccess);
            
            // 성공 보상 처리
            HandleMiniGameSuccess(MiniGameType.NPCChase);
        }
        
        // UI 업데이트
        UpdateScoreUI();
        
        // 게임 완료 이벤트 발생
        OnGameCompleted?.Invoke(success, triggerCount);
    }
    
    // 현재 적용되는 점수 계산 (피버 모드 등 고려)
    private int CalculateScore(int baseScore)
    {
        return baseScore * gamePointsMultiplier * (isFeverModeActive ? feverModeScoreMultiplier : 1);
    }
    
    // 피버 게이지 증가
    private void IncreaseFeverGauge(float amount)
    {
        if (isFeverModeActive) return; // 이미 피버 모드면 게이지 증가 무시
        
        currentFeverGauge += amount;
        
        // 피버 게이지가 최대에 도달하면 피버 모드 활성화
        if (currentFeverGauge >= feverGaugeMax)
        {
            ActivateFeverMode();
        }
        
        // UI 업데이트
        UpdateFeverGaugeUI();
    }
    
    // 피버 모드 활성화
    private void ActivateFeverMode()
    {
        if (isFeverModeActive) return;
        
        isFeverModeActive = true;
        currentFeverGauge = feverGaugeMax;
        
        // 피버 모드 시각 효과 활성화
        if (feverModeIndicator != null)
            feverModeIndicator.SetActive(true);
        
        // 피버 모드 오디오/효과음 재생
        // TODO: 오디오 재생 코드 추가
        
        // 피버 모드 타이머 시작
        StartCoroutine(FeverModeTimer());
        Debug.Log("피버 모드 활성화!");
    }
    
    // 피버 모드 타이머
    private IEnumerator FeverModeTimer()
    {
        float remainingTime = feverModeTime;
        
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            
            // 피버 게이지 UI 업데이트 (시간 경과에 따라 감소)
            currentFeverGauge = (remainingTime / feverModeTime) * feverGaugeMax;
            UpdateFeverGaugeUI();
            
            yield return null;
        }
        
        // 피버 모드 종료
        DeactivateFeverMode();
    }
    
    // 피버 모드 비활성화
    private void DeactivateFeverMode()
    {
        isFeverModeActive = false;
        currentFeverGauge = 0f;
        
        // 피버 모드 시각 효과 비활성화
        if (feverModeIndicator != null)
            feverModeIndicator.SetActive(false);
        
        // UI 업데이트
        UpdateFeverGaugeUI();
        Debug.Log("피버 모드 종료");
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
    
    // 피버 게이지 UI 업데이트
    private void UpdateFeverGaugeUI()
    {
        if (feverGaugeImage != null)
        {
            feverGaugeImage.fillAmount = currentFeverGauge / feverGaugeMax;
        }
        else
        {
            Debug.LogWarning("feverGaugeImage 참조가 없습니다!");
        }
    }
    
    // 미니게임 성공 보상 처리
    private void HandleMiniGameSuccess(MiniGameType gameType)
    {
        // 현재 상호작용 중인 NPC를 가져옴
        NPCController npc = GetCurrentInteractingNPC();
        
        if (npc != null)
        {
            Debug.Log($"미니게임 성공 보상: {npc.GetName()} NPC 감정 상태 변경 및 유령 모드 설정");
            
            // NPC 감정 상태 업데이트
            NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
            if (emotionController != null)
            {
                emotionController.ChangeEmotionState(EmotionState.Happy);
            }
            
            // NPC를 유령 모드로 설정 (자동으로 FollowerManager에 등록됨)
            npc.SetSeduced();
            npc.SetGhostMode(true);
            
            // 점수는 FollowerManager에서 자동으로 처리됨
        }
        else
        {
            Debug.LogWarning("현재 상호작용 중인 NPC가 없습니다!");
        }
    }
    
    // 현재 상호작용 중인 NPC 가져오기
    private NPCController GetCurrentInteractingNPC()
    {
        // NPCInteractionManager에서 현재 상호작용 중인 NPC 가져오기
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

    // NPC를 따라오게 만드는 처리 (public으로 변경)
    public void MakeNPCFollow(NPCController npc)
    {
        if (npc == null) return;
        
        // NPC 컴포넌트 확인
        if (!npc.gameObject.activeInHierarchy) return;
        
        Debug.Log($"{npc.GetName()} NPC를 따라오게 설정");
        
        // NPC를 유령 모드로 설정 (자동으로 FollowerManager에 등록됨)
        npc.SetSeduced();
        npc.SetGhostMode(true);
        
        // 점수 추가
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
    
    // 미니게임 시작 메서드 (외부에서 호출)
    public bool StartMiniGame(MiniGameType gameType, int difficulty = 1)
    {
        // 이미 진행 중인 미니게임이 있으면 시작 거부
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
                
            case MiniGameType.NPCChase:
                if (chaseGame != null)
                {
                    Debug.Log($"ChaseGame 시작 (난이도: {difficulty})");
                    chaseGame.StartMiniGame(difficulty);
                    currentMiniGame = ActiveMiniGame.NPCChase;
                    started = true;
                }
                else
                {
                    Debug.LogError("chaseGame 참조가 null입니다!");
                }
                break;
        }
        
        // 이벤트 발생
        if (started)
        {
            Debug.Log($"미니게임 시작 이벤트 발생: {gameType}");
            OnGameStarted?.Invoke(gameType);
            
            // MiniGameUI 직접 참조 시도
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
                
            case ActiveMiniGame.NPCChase:
                if (chaseGame != null)
                {
                    chaseGame.StopGame();
                    Debug.Log("ChaseGame 중지됨");
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
        HeartGaze,
        NPCChase
    }

    public void ForceCleanupAllUI()
    {
        Debug.Log("모든 미니게임 UI 강제 정리");
        
        // MiniGameUI 찾기
        MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
        if (ui != null)
        {
            ui.HideAllPanels();
        }
        
        // 각 미니게임의 public 메서드 호출
        if (colorGazeGame != null)
            colorGazeGame.HideGameUI();
            
        if (heartGazeGame != null)
            heartGazeGame.HideGameUI();
            
        if (chaseGame != null)
            chaseGame.HideGameUI();
            
        // UI 상태 로깅
        LogGameUIStates();
    }

    // 디버깅용 UI 상태 로깅
    private void LogGameUIStates()
    {
        Debug.Log($"미니게임 UI 상태: " +
                $"ColorGame UI: {(colorGazeGame != null ? colorGazeGame.IsGameUIActive() : "null")}, " +
                $"HeartGame UI: {(heartGazeGame != null ? heartGazeGame.IsGameUIActive() : "null")}, " +
                $"ChaseGame UI: {(chaseGame != null ? chaseGame.IsGameUIActive() : "null")}");
    }
        
    // (선택적) 피버 모드 수동 활성화 (테스트용)
    public void DebugActivateFeverMode()
    {
        Debug.Log("피버 모드 수동 활성화 요청");
        ActivateFeverMode();
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
        
        if (chaseGame != null)
        {
            chaseGame.OnGameCompleted -= OnChaseGameCompleted;
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