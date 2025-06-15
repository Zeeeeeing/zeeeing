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

    [Header("HUD Control")] // 새로 추가
    [SerializeField] GameObject topHUDPanel; // TopHUDPanel 직접 참조
    [SerializeField] bool isHUDActive = false; // HUD 활성화 상태

    [Header("LOVE Gauge")]
    [SerializeField] GameObject loveGaugePanel;         // LOVE 게이지 전체 패널
    [SerializeField] Image[] heartImages;               // 하트 이미지 배열
    [SerializeField] Sprite fullHeartSprite;            // 꽉 찬 하트 스프라이트
    [SerializeField] Sprite emptyHeartSprite;           // 빈 하트 스프라이트
    [SerializeField] int maxHearts = 9;                 // 최대 하트 개수
    [SerializeField] int currentHearts = 0;             // 현재 하트 개수 (처음에는 0으로 시작)
    [SerializeField] int scorePerHeart = 100;           // 하트 1개를 채우는데 필요한 점수

    [Header("Score Panel")]
    [SerializeField] GameObject scorePanel;             // 스코어 패널
    [SerializeField] TextMeshProUGUI scoreDigitsText;   // 숫자로 된 스코어 텍스트

    [Header("Timer")]
    [SerializeField] GameObject timerPanel;             // 타이머 패널

    [Header("Love Gauge Events")]
    [SerializeField] private UnityEvent onLoveGaugeFull = new UnityEvent();    // LOVE 게이지가 가득 찰 때
    public UnityEvent OnLoveGaugeFull => onLoveGaugeFull;                      // 외부 접근용 프로퍼티
    public System.Action<int, int> OnLoveGaugeChanged;                         // LOVE 게이지 변화 시 (현재, 최대)

    [Header("Fever Mode Control")]
    [SerializeField] private bool isFeverModeActive = false;                   // 피버 모드 활성화 상태
    [SerializeField] private bool blockHeartIncreaseInFever = true;            // 피버 모드 중 하트 증가 차단 여부
    public System.Action<bool> OnFeverModeChanged;                             // 피버 모드 상태 변화 이벤트

    [Header("Gameplay")]
    public int score = 0;
    public float maxTimeSeconds = 300f;
    private float timeRemaining;
    private bool running = false;
    private bool initialized = false;
    private int lastHeartScore = 0;                     // 마지막으로 하트가 채워진 점수

    void Awake()
    {
        // Awake에서 타이머 값만 초기화 (한 번만 실행되도록)
        if (!initialized)
        {
            // Debug.Log("[HUD] Awake() 호출 - 초기 값 설정");
            timeRemaining = maxTimeSeconds;
            running = false;
            score = 0;
            currentHearts = 0;  // 시작 시 하트 0개
            lastHeartScore = 0; // 마지막 하트 점수 초기화
            isFeverModeActive = false; // 피버 모드 초기화
            isHUDActive = false; // HUD 초기 비활성화
            initialized = true;
        }

        // TopHUDPanel 자동 찾기
        if (topHUDPanel == null)
        {
            topHUDPanel = GameObject.Find("TopHUDPanel");
            if (topHUDPanel == null)
            {
                // Debug.LogWarning("[HUD] TopHUDPanel을 찾을 수 없습니다!");
            }
        }
    }

    void Start()
    {
        // Debug.Log("[HUD] Start() 호출 - UI 초기화");

        // UI 초기 설정
        InitializeUI();

        // VR 카메라에 HUD 캔버스 연결 (월드 스페이스 캔버스)
        SetupHUDCanvas();

        // 이벤트 초기화 및 연결
        if (OnTimeUp == null)
            OnTimeUp = new UnityEvent();

        OnTimeUp.AddListener(LoadEndScene);

        // HUD 초기 상태 설정 (비활성화 상태로 시작)
        SetHUDActive(false);

        // Debug.Log("[HUD] UI 초기화 완료 (LOVE 게이지 이벤트 시스템 + 피버 모드 제어 + HUD 제어 포함)");
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
            // VR 카메라 위에 위치 설정 (카메라로부터 좀 더 멀리)
            hudCanvas.transform.position = Camera.main.transform.position + Camera.main.transform.up * 1.0f + Camera.main.transform.forward * 5f;
            // 캔버스가 항상 카메라를 향하도록 설정
            hudCanvas.transform.rotation = Quaternion.LookRotation(
                hudCanvas.transform.position - Camera.main.transform.position);

            // 크기 조정 (월드 스페이스에서는 크기를 적절히 조정해야 함)
            hudCanvas.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // Debug.Log("[HUD] VR용 월드 스페이스 캔버스 설정 완료");
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
    }

    private void UpdateHUDCanvasPosition()
    {
        if (hudCanvas != null && Camera.main != null)
        {
            // 카메라의 좀 더 멀리에 위치
            Vector3 targetPosition = Camera.main.transform.position +
                                    Camera.main.transform.up * 1.0f +
                                    Camera.main.transform.forward * 5f;

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
            // Debug.Log("[HUD] 타이머 종료!");
            if (OnTimeUp != null)
                OnTimeUp.Invoke();
        }

        // 타이머 표시 업데이트
        UpdateTimerDisplay();
    }

    #region HUD Control Methods (새로 추가)

    /// <summary>
    /// HUD 전체 활성화/비활성화 제어
    /// </summary>
    /// <param name="active">활성화 여부</param>
    public void SetHUDActive(bool active)
    {
        isHUDActive = active;

        // Debug.Log($"[HUD] SetHUDActive 호출됨: {active}");

        // TopHUDPanel 활성화/비활성화
        if (topHUDPanel != null)
        {
            topHUDPanel.SetActive(active);
            // Debug.Log($"[HUD] TopHUDPanel {(active ? "활성화" : "비활성화")} 완료");
        }
        else
        {
            // Debug.LogError("[HUD] TopHUDPanel이 null입니다! Inspector에서 할당을 확인하세요.");
        }

        // 개별 패널들도 제어 (필요시)
        if (loveGaugePanel != null)
        {
            loveGaugePanel.SetActive(active);
            // Debug.Log($"[HUD] LoveGaugePanel {(active ? "활성화" : "비활성화")}");
        }
        if (scorePanel != null)
        {
            scorePanel.SetActive(active);
            // Debug.Log($"[HUD] ScorePanel {(active ? "활성화" : "비활성화")}");
        }
        if (timerPanel != null)
        {
            timerPanel.SetActive(active);
            // Debug.Log($"[HUD] TimerPanel {(active ? "활성화" : "비활성화")}");
        }

        // Debug.Log($"[HUD] HUD 전체 {(active ? "활성화" : "비활성화")} 완료");
    }

    /// <summary>
    /// HUD 활성화 상태 반환
    /// </summary>
    public bool IsHUDActive()
    {
        return isHUDActive;
    }

    /// <summary>
    /// 개별 패널 제어
    /// </summary>
    public void SetPanelActive(string panelName, bool active)
    {
        switch (panelName.ToLower())
        {
            case "love":
            case "lovegauge":
                if (loveGaugePanel != null)
                    loveGaugePanel.SetActive(active);
                break;
            case "score":
                if (scorePanel != null)
                    scorePanel.SetActive(active);
                break;
            case "timer":
                if (timerPanel != null)
                    timerPanel.SetActive(active);
                break;
            default:
                // Debug.LogWarning($"[HUD] 알 수 없는 패널 이름: {panelName}");
                break;
        }
    }

    #endregion

    // 수정된 하트 게이지 업데이트 (피버 모드 상태 표시 추가)
   private void UpdateHeartDisplay()
{
    // 하트 이미지 배열이 할당되어 있지 않으면 무시
    if (heartImages == null || heartImages.Length == 0)
    {
        // Debug.LogWarning("[HUD] 하트 이미지가 할당되지 않았습니다!");
        return;
    }

    // 각 하트 이미지 업데이트
    for (int i = 0; i < heartImages.Length; i++)
    {
        if (i < currentHearts)
        {
            heartImages[i].sprite = fullHeartSprite;

            // 피버 모드 중에는 하트에 특수 효과 적용 (선택사항)
            if (isFeverModeActive)
            {
                heartImages[i].color = Color.yellow;
            }
            else
            {
                heartImages[i].color = Color.white;
            }
        }
        else
        {
            heartImages[i].sprite = emptyHeartSprite;
            heartImages[i].color = Color.white;
        }
    }

    // LOVE 게이지 변화 이벤트 발생
    OnLoveGaugeChanged?.Invoke(currentHearts, maxHearts);

    // ⭐ LOVE 게이지가 가득 찬 경우 이벤트 발생 (피버 모드가 아닐 때만) - 디버깅 강화
    if (currentHearts >= maxHearts && !isFeverModeActive)
    {
        // Debug.Log("💖 [HUD] LOVE 게이지가 가득 찬 상태에서 피버 모드 활성화 신호!");
        // Debug.Log($"💖 [HUD] OnLoveGaugeFull 이벤트 발생 시도... (리스너 수: {OnLoveGaugeFull.GetPersistentEventCount()})");
        
        // ⭐ 이벤트 발생 전후 로그 추가
        // Debug.Log("💖 [HUD] OnLoveGaugeFull.Invoke() 호출 전");
        OnLoveGaugeFull?.Invoke();
        // Debug.Log("💖 [HUD] OnLoveGaugeFull.Invoke() 호출 후");
    }
    else if (currentHearts >= maxHearts && isFeverModeActive)
    {
        // Debug.Log("💖 [HUD] LOVE 게이지가 가득 찬 상태 (피버 모드 중이므로 이벤트 발생 안함)");
    }
    else
    {
        // Debug.Log($"💖 [HUD] LOVE 게이지 상태: {currentHearts}/{maxHearts} (가득참 아님)");
    }
}

    // 타이머 표시 업데이트
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
        }
    }

    // 수정된 점수 업데이트 (피버 모드 중 하트 증가 차단 기능 추가)
    public void UpdateScore(int delta)
    {
        // 점수 증가
        score += delta;

        // Debug.Log($"[HUD] 점수 업데이트: +{delta} (총 점수: {score}, 피버 모드: {isFeverModeActive})");

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
            // 피버 모드 중에는 하트 증가 차단 (핵심 기능!)
            if (isFeverModeActive && blockHeartIncreaseInFever)
            {
                // Debug.Log($"🔥 [HUD] 피버 모드 중이므로 하트 증가가 차단되었습니다! (점수만 증가: +{delta})");
                return; // 하트 증가 로직을 건너뜀
            }

            // 현재 점수가 다음 하트를 채우기에 충분한지 확인
            int heartsToAdd = (score - lastHeartScore) / scorePerHeart;

            if (heartsToAdd > 0)
            {
                // 하트 추가 (최대 개수 제한)
                AddHeart(heartsToAdd);
                // 마지막 하트 점수 업데이트
                lastHeartScore = score - (score % scorePerHeart);

                // Debug.Log($"[HUD] ❤️ 하트 {heartsToAdd}개 추가됨! 현재 하트: {currentHearts}/{maxHearts}, 다음 하트까지 필요 점수: {scorePerHeart - (score % scorePerHeart)}");
            }
        }
    }

    /// <summary>
    /// 점수 동기화 메서드 (통합 점수 시스템용) - 디버깅 강화!
    /// </summary>
    /// <param name="newScore">새로운 총점</param>
    public void SyncScore(int newScore)
    {
        int previousScore = score;

        // 점수를 새로운 총점으로 설정 (덮어쓰기)
        score = newScore;

        // Debug.Log($"📊 [HUD] 점수 동기화: {previousScore} → {newScore}");

        // UI 텍스트 즉시 업데이트
        if (scoreText != null)
        {
            scoreText.text = $"{score}";
            // Debug.Log($"📊 [HUD] scoreText 업데이트: {score}");
        }
        else
        {
            // Debug.LogWarning("📊 [HUD] scoreText가 null입니다!");
        }

        if (scoreDigitsText != null)
        {
            scoreDigitsText.text = score.ToString("D8");
            // Debug.Log($"📊 [HUD] scoreDigitsText 업데이트: {score:D8}");
        }
        else
        {
            // Debug.LogWarning("📊 [HUD] scoreDigitsText가 null입니다!");
        }

        // 하트 게이지는 점수 증가분만큼만 처리
        int scoreDelta = newScore - previousScore;
        if (scoreDelta > 0)
        {
            // Debug.Log($"📊 [HUD] 점수 증가분: +{scoreDelta}");

            // 피버 모드 중에는 하트 증가 차단
            if (isFeverModeActive && blockHeartIncreaseInFever)
            {
                // Debug.Log($"🔥 [HUD] 피버 모드 중 하트 증가 차단됨 (+{scoreDelta}점)");
                return;
            }

            // 현재 점수가 다음 하트를 채우기에 충분한지 확인
            int heartsToAdd = (score - lastHeartScore) / scorePerHeart;

            if (heartsToAdd > 0)
            {
                // Debug.Log($"❤️ [HUD] 하트 추가 계산: 현재점수({score}) - 마지막하트점수({lastHeartScore}) = {score - lastHeartScore}, 하트당점수({scorePerHeart}) → {heartsToAdd}개 추가");

                AddHeart(heartsToAdd);
                lastHeartScore = score - (score % scorePerHeart);

                // Debug.Log($"❤️ [HUD] 하트 {heartsToAdd}개 추가됨! 새 마지막하트점수: {lastHeartScore}");
            }
        }
        else if (scoreDelta < 0)
        {
            // Debug.LogWarning($"📊 [HUD] 점수가 감소했습니다: {scoreDelta} (이상함!)");
        }
    }

    // 수정된 LOVE 게이지 업데이트 (피버 모드 제어 추가)
    public void UpdateLoveGauge(int hearts)
    {
        int previousHearts = currentHearts;
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);
        UpdateHeartDisplay();

        // Debug.Log($"[HUD] LOVE 게이지 직접 업데이트: {previousHearts} → {currentHearts} (피버 모드: {isFeverModeActive})");
    }

    // 수정된 하트 추가 (피버 모드 제어 추가)
    public void AddHeart(int amount = 1)
    {
        // 피버 모드 중에는 하트 증가 차단 (옵션)
        if (isFeverModeActive && blockHeartIncreaseInFever)
        {
            // Debug.Log($"🔥 [HUD] 피버 모드 중이므로 하트 추가가 차단되었습니다! (요청량: +{amount})");
            return;
        }

        int previousHearts = currentHearts;
        currentHearts = Mathf.Clamp(currentHearts + amount, 0, maxHearts);
        UpdateHeartDisplay();

        // Debug.Log($"[HUD] ❤️ 하트 추가: {previousHearts} → {currentHearts} (+{amount})");
    }

    // 수정된 하트 제거 (피버 모드 상관없이 항상 가능)
    public void RemoveHeart(int amount = 1)
    {
        int previousHearts = currentHearts;
        currentHearts = Mathf.Clamp(currentHearts - amount, 0, maxHearts);
        UpdateHeartDisplay();

        // Debug.Log($"[HUD] 💔 하트 제거: {previousHearts} → {currentHearts} (-{amount})");
    }

    // LOVE 게이지를 가득 채우는 메서드 (피버 모드 제어 추가)
    public void FillLoveGauge()
    {
        if (isFeverModeActive && blockHeartIncreaseInFever)
        {
            // Debug.Log($"🔥 [HUD] 피버 모드 중이므로 LOVE 게이지 강제 충전이 차단되었습니다!");
            return;
        }

        UpdateLoveGauge(maxHearts);
    }

    // LOVE 게이지를 모두 비우는 메서드 (피버 모드 상관없이 항상 가능)
    public void ResetLoveGauge()
    {
        UpdateLoveGauge(0);

        // 하트 점수 추적도 리셋
        lastHeartScore = score - (score % scorePerHeart);

        // Debug.Log($"[HUD] LOVE 게이지 완전 리셋 (점수 추적도 리셋: lastHeartScore = {lastHeartScore})");
    }

    // LOVE 게이지가 가득 찬 상태인지 확인
    public bool IsLoveGaugeFull()
    {
        return currentHearts >= maxHearts;
    }

    // LOVE 게이지 비율 반환 (0.0 ~ 1.0)
    public float GetLoveGaugeRatio()
    {
        return (float)currentHearts / maxHearts;
    }

    // 현재 하트 개수 반환
    public int GetCurrentHearts()
    {
        return currentHearts;
    }

    // 최대 하트 개수 반환
    public int GetMaxHearts()
    {
        return maxHearts;
    }

    // 피버 모드 제어 메서드들

    /// <summary>
    /// 피버 모드 상태를 설정합니다
    /// </summary>
    /// <param name="active">피버 모드 활성화 여부</param>
    public void SetFeverMode(bool active)
    {
        if (isFeverModeActive == active) return; // 중복 호출 방지

        bool previousState = isFeverModeActive;
        isFeverModeActive = active;

        // Debug.Log($"🔥 [HUD] 피버 모드 상태 변경: {previousState} → {isFeverModeActive}");

        // 피버 모드 비활성화 시 자동으로 LOVE 게이지 리셋
        if (!active && previousState)
        {
            // Debug.Log("🔥 [HUD] 피버 모드 종료로 인한 LOVE 게이지 자동 리셋");
            ResetLoveGauge();
        }

        // 하트 시각 효과 업데이트
        UpdateHeartDisplay();

        // 피버 모드 상태 변화 이벤트 발생
        OnFeverModeChanged?.Invoke(isFeverModeActive);
    }

    /// <summary>
    /// 현재 피버 모드 상태를 반환합니다
    /// </summary>
    public bool IsFeverModeActive()
    {
        return isFeverModeActive;
    }

    /// <summary>
    /// 피버 모드 중 하트 증가 차단 설정을 변경합니다
    /// </summary>
    /// <param name="block">차단 여부</param>
    public void SetBlockHeartIncreaseInFever(bool block)
    {
        blockHeartIncreaseInFever = block;
        // Debug.Log($"[HUD] 피버 모드 중 하트 증가 차단 설정: {blockHeartIncreaseInFever}");
    }

    /// <summary>
    /// 피버 모드 중 하트 증가 차단 설정을 반환합니다
    /// </summary>
    public bool IsHeartIncreaseBlockedInFever()
    {
        return blockHeartIncreaseInFever;
    }

    // 타이머 시작 함수
    public void StartTimer()
    {
        // Debug.Log("[HUD] StartTimer() 호출됨");

        // 타이머가 이미 실행 중이면 무시
        if (running) return;

        // 타이머가 0이면 재설정
        if (timeRemaining <= 0f)
        {
            timeRemaining = maxTimeSeconds;
        }

        running = true;
        // Debug.Log("[HUD] 타이머 시작됨! - 남은 시간: " + timeRemaining);

        // UI 업데이트
        UpdateTimerDisplay();
    }

    // 타이머 강제 시작
    public void ForceStartTimer()
    {
        timeRemaining = maxTimeSeconds;
        running = true;
        // Debug.Log("[HUD] 타이머 강제 시작! - 남은 시간: " + timeRemaining);

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
        // Debug.Log("[HUD] 시간 종료! EndScene으로 이동");
        SceneManager.LoadScene("EndScene");
    }
}