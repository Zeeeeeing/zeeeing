using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Canvas hudCanvas; // VR 카메라에 고정되는 HUD 캔버스
    
    [Header("LOVE Gauge")]
    [SerializeField] GameObject loveGaugePanel;         // LOVE 게이지 전체 패널
    [SerializeField] Image[] heartImages;               // 하트 이미지 배열
    [SerializeField] Sprite fullHeartSprite;            // 꽉 찬 하트 스프라이트
    [SerializeField] Sprite emptyHeartSprite;           // 빈 하트 스프라이트
    [SerializeField] int maxHearts = 9;                 // 최대 하트 개수
    [SerializeField] int currentHearts = 0;             // 현재 하트 개수 (처음에는 0으로 시작)
    [SerializeField] int scorePerHeart = 10;            // 하트 1개를 채우는데 필요한 점수

    [Header("Score Panel")]
    [SerializeField] GameObject scorePanel;             // 스코어 패널
    [SerializeField] TextMeshProUGUI scoreDigitsText;   // 숫자로 된 스코어 텍스트
    
    [Header("Timer")]
    [SerializeField] GameObject timerPanel;             // 타이머 패널
    [SerializeField] Image timerArrow;                  // 타이머 화살표 이미지
    
    [Header("Gameplay")]
    public int   score          = 0;
    public float maxTimeSeconds = 300f;
    private float timeRemaining;
    private bool running = false;
    private bool initialized = false;
    private int lastHeartScore = 0;                     // 마지막으로 하트가 채워진 점수

    [Header("Test Settings")]
    [SerializeField] bool autoIncreaseScore = false;   // 점수 자동 증가 활성화
    [SerializeField] float scoreIncreaseInterval = 1f; // 점수 증가 간격 (초)
    [SerializeField] int scoreIncreaseAmount = 5;      // 한 번에 증가할 점수량
    private float nextScoreIncreaseTime = 0f;          // 다음 점수 증가 시간

    void Awake()
    {
        // Awake에서 타이머 값만 초기화 (한 번만 실행되도록)
        if (!initialized)
        {
            Debug.Log("[HUD] Awake() 호출 - 초기 값 설정");
            timeRemaining = maxTimeSeconds;
            running = false;
            score = 0;
            currentHearts = 0;  // 시작 시 하트 0개
            lastHeartScore = 0; // 마지막 하트 점수 초기화
            initialized = true;
        }
    }

    void Start()
    {
        Debug.Log("[HUD] Start() 호출 - UI 초기화");
        
        // UI 초기 설정
        InitializeUI();
        
        // VR 카메라에 HUD 캔버스 연결 (월드 스페이스 캔버스)
        SetupHUDCanvas();
        
        // 이벤트 초기화 및 연결
        if (OnTimeUp == null)
            OnTimeUp = new UnityEvent();
            
        OnTimeUp.AddListener(LoadEndScene);
        
        Debug.Log("[HUD] UI 초기화 완료");
    }

    private void InitializeUI()
    {
        // 점수 초기화 및 표시
        UpdateScore(0);
        
        // 하트 게이지 초기화 (모두 빈 하트로 시작)
        UpdateHeartDisplay();
        
        // 타이머 초기 표시
        UpdateTimerDisplay();
    }
    
    private void SetupHUDCanvas()
    {
        if (hudCanvas != null && Camera.main != null)
        {
            hudCanvas.renderMode = RenderMode.WorldSpace;
            // VR 카메라 위에 위치 설정 (카메라로부터 약간 위쪽으로)
            hudCanvas.transform.position = Camera.main.transform.position + Camera.main.transform.up * 0.5f + Camera.main.transform.forward * 2f;
            // 캔버스가 항상 카메라를 향하도록 설정
            hudCanvas.transform.rotation = Quaternion.LookRotation(
                hudCanvas.transform.position - Camera.main.transform.position);
            
            // 크기 조정 (월드 스페이스에서는 크기를 적절히 조정해야 함)
            hudCanvas.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            
            Debug.Log("[HUD] 캔버스 위치 설정 완료");
        }
        else
        {
            if (hudCanvas == null)
                Debug.LogWarning("[HUD] hudCanvas가 null입니다!");
            if (Camera.main == null)
                Debug.LogWarning("[HUD] Camera.main이 null입니다!");
        }
    }

    void Update()
    {
        // HUD 캔버스 위치 업데이트
        UpdateHUDCanvasPosition();

        // 타이머 업데이트
        UpdateTimer();
        
        // 자동 점수 증가 테스트 코드 추가
        if (autoIncreaseScore && running)
        {
            // 설정한 간격마다 점수 증가
            if (Time.time >= nextScoreIncreaseTime)
            {
                // 점수 증가
                UpdateScore(scoreIncreaseAmount);

                // 다음 증가 시간 설정
                nextScoreIncreaseTime = Time.time + scoreIncreaseInterval;

                // 로그 출력
                Debug.Log($"[테스트] 점수 자동 증가: +{scoreIncreaseAmount}, 현재 점수: {score}");
            }
        }
    }
    
    private void UpdateHUDCanvasPosition()
    {
        if (hudCanvas != null && Camera.main != null)
        {
            // 카메라의 약간 위쪽에 위치
            Vector3 targetPosition = Camera.main.transform.position + 
                                    Camera.main.transform.up * 0.5f + 
                                    Camera.main.transform.forward * 2f;
            
            // 부드럽게 움직이도록 Lerp 사용
            hudCanvas.transform.position = Vector3.Lerp(
                hudCanvas.transform.position, 
                targetPosition, 
                Time.deltaTime * 5f);
            
            // 항상 카메라 방향 바라보기
            hudCanvas.transform.rotation = Quaternion.Lerp(
                hudCanvas.transform.rotation,
                Quaternion.LookRotation(hudCanvas.transform.position - Camera.main.transform.position),
                Time.deltaTime * 5f);
        }
    }
    
    private void UpdateTimer()
    {
        if (!running) return;
        
        // 타이머 감소
        timeRemaining -= Time.deltaTime;
        
        // 타이머 종료 체크
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            running = false;
            Debug.Log("[HUD] 타이머 종료!");
            if (OnTimeUp != null)
                OnTimeUp.Invoke();
        }
        
        // 타이머 표시 업데이트
        UpdateTimerDisplay();
    }
    
    // 하트 게이지 업데이트
    private void UpdateHeartDisplay()
    {
        // 하트 이미지 배열이 할당되어 있지 않으면 무시
        if (heartImages == null || heartImages.Length == 0) 
        {
            Debug.LogWarning("[HUD] 하트 이미지가 할당되지 않았습니다!");
            return;
        }
        
        // 각 하트 이미지 업데이트
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < currentHearts)
                heartImages[i].sprite = fullHeartSprite;
            else
                heartImages[i].sprite = emptyHeartSprite;
        }
    }
    
    // 타이머 표시 업데이트
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
        }
        
        // 타이머 화살표 회전
        if (timerArrow != null)
        {
            // 0~360도 범위로 회전 (시계 방향)
            float rotationAngle = 360f * (1f - (timeRemaining / maxTimeSeconds));
            timerArrow.transform.rotation = Quaternion.Euler(0, 0, -rotationAngle);
        }
    }

    // 점수 업데이트
    public void UpdateScore(int delta)
    {
        // 점수 증가
        score += delta;
        
        // UI 텍스트 업데이트
        if (scoreText != null)
            scoreText.text = $"{score}";
            
        if (scoreDigitsText != null)
        {
            // 8자리 숫자로 표시 (앞을 0으로 채움)
            scoreDigitsText.text = score.ToString("D8");
        }
        
        // 하트 게이지 업데이트 체크 (점수가 증가했을 때만)
        if (delta > 0)
        {
            // 현재 점수가 다음 하트를 채우기에 충분한지 확인
            int heartsToAdd = (score - lastHeartScore) / scorePerHeart;
            
            if (heartsToAdd > 0)
            {
                // 하트 추가 (최대 개수 제한)
                AddHeart(heartsToAdd);
                // 마지막 하트 점수 업데이트
                lastHeartScore = score - (score % scorePerHeart);
                
                Debug.Log($"[HUD] 하트 {heartsToAdd}개 추가됨! 현재 하트: {currentHearts}, 다음 하트까지 필요 점수: {scorePerHeart - (score % scorePerHeart)}");
            }
        }
    }
    
    // LOVE 게이지 업데이트
    public void UpdateLoveGauge(int hearts)
    {
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);
        UpdateHeartDisplay();
    }
    
    // LOVE 게이지 증가
    public void AddHeart(int amount = 1)
    {
        currentHearts = Mathf.Clamp(currentHearts + amount, 0, maxHearts);
        UpdateHeartDisplay();
    }
    
    // LOVE 게이지 감소
    public void RemoveHeart(int amount = 1)
    {
        currentHearts = Mathf.Clamp(currentHearts - amount, 0, maxHearts);
        UpdateHeartDisplay();
    }

    // 타이머 시작 함수
    public void StartTimer()
    {
        Debug.Log("[HUD] StartTimer() 호출됨");
        
        // 타이머가 이미 실행 중이면 무시
        if (running) return;
        
        // 타이머가 0이면 재설정
        if (timeRemaining <= 0f)
        {
            timeRemaining = maxTimeSeconds;
        }
        
        running = true;
        Debug.Log("[HUD] 타이머 시작됨! - 남은 시간: " + timeRemaining);
        
        // UI 업데이트
        UpdateTimerDisplay();
    }

    // 타이머 강제 시작
    public void ForceStartTimer()
    {
        timeRemaining = maxTimeSeconds;
        running = true;
        Debug.Log("[HUD] 타이머 강제 시작! - 남은 시간: " + timeRemaining);
        
        // UI 업데이트
        UpdateTimerDisplay();
    }

    // 타이머 실행 상태 확인
    public bool IsTimerRunning()
    {
        return running;
    }

    // 타임업 이벤트
    public UnityEvent OnTimeUp;

    void LoadEndScene()
    {
        Debug.Log("[HUD] 시간 종료! EndScene으로 이동");
        SceneManager.LoadScene("EndScene");
    }
}