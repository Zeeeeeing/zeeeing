using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class NPCChaseMiniGame : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private NPCEmotionController npcEmotionController;
    [SerializeField] private float gameTime = 45f;
    [SerializeField] private int requiredEmotionTriggers = 3;
    [SerializeField] private EmotionState targetEmotion = EmotionState.Happy;
    [SerializeField] private float requiredEmotionIntensity = 0.7f;
    
    [Header("Difficulty Settings")]
    [SerializeField] private int easyRequiredTriggers = 2;
    [SerializeField] private int normalRequiredTriggers = 3;
    [SerializeField] private int hardRequiredTriggers = 5;
    
    [SerializeField] private float easyGameTime = 60f;
    [SerializeField] private float normalGameTime = 45f;
    [SerializeField] private float hardGameTime = 30f;
    
    [Header("NPC Movement Settings")]
    [SerializeField] private float normalMoveSpeed = 1.0f;
    [SerializeField] private float chaseMoveSpeed = 2.0f; // 미니게임 중 증가된 속도
    // [SerializeField] private float rotationSpeed = 180f;
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI triggerCountText;
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameUI;
    
    // NavMesh 컴포넌트 참조
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private MonoBehaviour autonomousDriver; // 도훈의 스크립트
    
    // 게임 상태 변수
    private int emotionTriggerCount = 0;
    private float remainingTime;
    private bool isGameActive = false;
    private float originalMoveSpeed;
    
    private void Awake()
    {
        gameUI.SetActive(false);
        
        // NavMesh 에이전트 컴포넌트 찾기
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // 자율 이동 스크립트 찾기 (도훈의 스크립트)
        autonomousDriver = GetComponent<MonoBehaviour>(); // 정확한 타입명으로 교체 필요
    }
    
    public void StartMiniGame(int difficulty = 1)
    {
        // 필요한 참조 확인
        if (gameUI == null)
        {
            Debug.LogError("gameUI가 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        if (npcEmotionController == null)
        {
            Debug.LogError("npcEmotionController가 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        if (triggerCountText == null || timerText == null)
        {
            Debug.LogError("UI 텍스트 참조가 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        try
        {
            // 난이도에 따른 설정
            switch(difficulty)
            {
                case 0: // Easy
                    requiredEmotionTriggers = easyRequiredTriggers;
                    gameTime = easyGameTime;
                    break;
                case 1: // Normal
                    requiredEmotionTriggers = normalRequiredTriggers;
                    gameTime = normalGameTime;
                    break;
                case 2: // Hard
                    requiredEmotionTriggers = hardRequiredTriggers;
                    gameTime = hardGameTime;
                    break;
            }
            
            // 게임 초기화
            emotionTriggerCount = 0;
            remainingTime = gameTime;
            isGameActive = true;

            if (gameUI != null)
            {
                gameUI.SetActive(true);
                Debug.Log("ColorGazeMiniGame UI 활성화됨");
            }
            
            // 원래 이동 속도 저장 및 속도 증가
            if (navMeshAgent != null)
            {
                originalMoveSpeed = navMeshAgent.speed;
                navMeshAgent.speed = chaseMoveSpeed;
            }
            else
            {
                Debug.LogWarning("navMeshAgent가 null입니다! 속도 조정 불가");
                // 기본값 설정
                originalMoveSpeed = normalMoveSpeed;
            }
            
            // 이벤트 구독
            npcEmotionController.EmotionTriggered += OnEmotionTriggered;
            
            // UI 업데이트
            UpdateUI();
            
            // 게임 시작 알림
            if (OnGameStarted != null) OnGameStarted.Invoke();
            
            Debug.Log($"NPCChase 미니게임 시작 성공 (난이도: {difficulty})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NPCChase 미니게임 시작 중 오류 발생: {e.Message}\n{e.StackTrace}");
            
            // 오류 발생 시 게임 정리
            if (npcEmotionController != null)
            {
                npcEmotionController.EmotionTriggered -= OnEmotionTriggered;
            }
            
            isGameActive = false;
            if (gameUI != null)
                gameUI.SetActive(false);
        }
    }
    
    private void OnEmotionTriggered(EmotionEventData eventData)
    {
        if (!isGameActive) return;
        
        // 목표 감정에 도달했는지 확인
        if (eventData.Emotion == targetEmotion && eventData.Intensity >= requiredEmotionIntensity)
        {
            emotionTriggerCount++;
            
            // 성공 피드백
            PlaySuccessFeedback();
            
            // UI 업데이트
            UpdateUI();
            
            // 목표 달성 확인
            if (emotionTriggerCount >= requiredEmotionTriggers)
            {
                GameOver(true);
            }
        }
    }
    
    private void PlaySuccessFeedback()
    {
        // 컨트롤러 진동
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand) is var device && device.TryGetHapticCapabilities(out var capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, 0.7f, 0.3f);
            }
        }
        
        // 오디오 재생 (필요시)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        // 잠시 멈춤 (선택적) - NPC가 감정 표현을 위해 잠시 멈추는 효과
        if (navMeshAgent != null)
        {
            StartCoroutine(PauseMovementBriefly());
        }
    }
    
    private IEnumerator PauseMovementBriefly()
    {
        if (navMeshAgent != null)
        {
            float originalSpeed = navMeshAgent.speed; // 현재 속도 저장
            bool wasEnabled = navMeshAgent.enabled;
            navMeshAgent.enabled = false;
            
            // 1초 정도 멈춤
            yield return new WaitForSeconds(1.0f);
            
            // 다시 이동 시작 (원래 속도로 복원)
            navMeshAgent.enabled = wasEnabled;
            if (navMeshAgent.enabled)
            {
                navMeshAgent.speed = originalSpeed;
            }
        }
    }
    
    private void Update()
    {
        if (!isGameActive) return;
        
        // 타이머 업데이트
        remainingTime -= Time.deltaTime;
        
        // UI 업데이트
        UpdateUI();
        
        // 시간 종료 체크
        if (remainingTime <= 0)
        {
            GameOver(emotionTriggerCount >= requiredEmotionTriggers);
        }
    }
    
    private void UpdateUI()
    {
        // 타이머 텍스트 업데이트
        timerText.text = $"시간: {Mathf.CeilToInt(remainingTime)}초";
        
        // 감정 트리거 카운트 업데이트
        triggerCountText.text = $"감정 유발: {emotionTriggerCount}/{requiredEmotionTriggers}";
    }
        
    private void GameOver(bool success)
    {
        isGameActive = false;
        
        // 이벤트 구독 해제
        npcEmotionController.EmotionTriggered -= OnEmotionTriggered;
        
        // 속도 원래대로 복구
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = normalMoveSpeed; // normalMoveSpeed 사용
        }
        
        // 게임 UI 비활성화
        gameUI.SetActive(false);
        
        // 게임 완료 이벤트 호출
        if (OnGameCompleted != null) OnGameCompleted.Invoke(success, emotionTriggerCount);
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
    public event System.Action<bool, int> OnGameCompleted; // 성공 여부, 감정 트리거 횟수
}