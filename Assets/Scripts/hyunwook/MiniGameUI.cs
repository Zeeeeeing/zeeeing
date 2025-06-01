using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeeeingGaze;

public class MiniGameUI : MonoBehaviour
{
    [Header("Common UI")]
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private TextMeshProUGUI gameTitle;
    [SerializeField] private TextMeshProUGUI gameInstructions;
    [SerializeField] private Image gameProgressBar;
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Color Game UI")]
    [SerializeField] private GameObject colorGamePanel;
    [SerializeField] private Image targetColorDisplay;
    [SerializeField] private Image emotionMatchProgressBar;
    [SerializeField] private TextMeshProUGUI matchCountText;
    
    [Header("Heart Game UI")]
    [SerializeField] private GameObject heartGamePanel;
    [SerializeField] private TextMeshProUGUI heartsCollectedText;
    
    [Header("Game Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button continueButton;
    
    [Header("References")]
    [SerializeField] private MiniGameManager miniGameManager;
    
    // 자동 숨김 코루틴 참조 (취소 가능하도록)
    private Coroutine autoHideCoroutine;
    
    private void Awake()
    {
        // MiniGameManager 자동 찾기
        if (miniGameManager == null)
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
        
        // 초기 상태 설정
        HideAllPanels();
        
        // 디버그 로그
        Debug.Log("[MiniGameUI] Awake 호출됨, 모든 패널 숨김 (2개 게임 모드)");
    }
    
    private void Start()
    {
        // 버튼 이벤트 연결
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            continueButton.onClick.AddListener(HideResultPanel);
            Debug.Log("[MiniGameUI] 계속 버튼 이벤트 연결됨");
        }
        else
        {
            Debug.LogWarning("[MiniGameUI] 계속 버튼 참조 없음");
        }
        
        // 초기화 완료 로그
        Debug.Log("[MiniGameUI] Start 완료, UI 준비됨 (ColorGaze, HeartGaze 전용)");
    }
    
    // 모든 패널 숨기기 (Chase 관련 제거)
    public void HideAllPanels()
    {
        try
        {
            // 모든 코루틴 중지 (중요: 자동 숨김 코루틴 취소)
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
            
            // 모든 패널 비활성화 (Chase 패널 제거)
            if (miniGamePanel != null) miniGamePanel.SetActive(false);
            if (colorGamePanel != null) colorGamePanel.SetActive(false);
            if (heartGamePanel != null) heartGamePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            
            // 로그
            Debug.Log("[MiniGameUI] 모든 패널 숨김 완료");
            
            // 현재 패널 활성화 상태 로깅 (디버깅용)
            LogPanelStates();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MiniGameUI] 패널 숨기기 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // 디버깅용: 패널 상태 로깅 (Chase 관련 제거)
    private void LogPanelStates()
    {
        Debug.Log($"[MiniGameUI] 패널 상태 - " +
                  $"miniGamePanel: {(miniGamePanel != null ? miniGamePanel.activeSelf.ToString() : "null")}, " +
                  $"colorGamePanel: {(colorGamePanel != null ? colorGamePanel.activeSelf.ToString() : "null")}, " +
                  $"heartGamePanel: {(heartGamePanel != null ? heartGamePanel.activeSelf.ToString() : "null")}, " +
                  $"resultPanel: {(resultPanel != null ? resultPanel.activeSelf.ToString() : "null")}");
    }
    
    // 미니게임 UI 표시 (Chase 케이스 제거)
    public void ShowMiniGameUI(MiniGameManager.MiniGameType gameType)
    {
        try
        {
            Debug.Log($"[MiniGameUI] {gameType} 게임 UI 표시 요청");
            
            // UI 참조 확인 및 자동 찾기
            EnsureUIReferences();
            
            // 모든 패널 숨기기 (기존 패널 정리)
            HideAllPanels();
            
            // 메인 미니게임 패널 활성화
            if (miniGamePanel != null)
            {
                miniGamePanel.SetActive(true);
                Debug.Log("메인 미니게임 패널 활성화됨");
            }
            else
            {
                Debug.LogError("miniGamePanel이 null입니다!");
            }
            
            // 게임 타입에 따른 UI 표시 (Chase 케이스 제거)
            switch (gameType)
            {
                case MiniGameManager.MiniGameType.ColorGaze:
                    ShowColorGameUI();
                    break;
                    
                case MiniGameManager.MiniGameType.HeartGaze:
                    ShowHeartGameUI();
                    break;
                    
                default:
                    Debug.LogWarning($"[MiniGameUI] 지원하지 않는 게임 타입: {gameType}");
                    break;
            }
            
            // 현재 패널 활성화 상태 로깅 (디버깅용)
            LogPanelStates();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MiniGameUI] 미니게임 UI 표시 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    // UI 참조 확인 및 자동 찾기 (Chase 관련 제거)
    private void EnsureUIReferences()
    {
        // 메인 미니게임 패널 참조 확인
        if (miniGamePanel == null)
        {
            miniGamePanel = GameObject.Find("MiniGamePanel");
            if (miniGamePanel == null)
            {
                Debug.LogError("MiniGamePanel을 찾을 수 없습니다!");
            }
        }
        
        // 각 게임별 패널 참조 확인 (Chase 제거)
        if (colorGamePanel == null)
        {
            colorGamePanel = GameObject.Find("ColorGamePanel");
        }
        
        if (heartGamePanel == null)
        {
            heartGamePanel = GameObject.Find("HeartGamePanel");
        }
        
        if (resultPanel == null)
        {
            resultPanel = GameObject.Find("ResultPanel");
        }
        
        // 패널 참조 상태 로깅 (Chase 제거)
        Debug.Log($"UI 패널 참조 상태: miniGamePanel={miniGamePanel != null}, " +
                $"colorGamePanel={colorGamePanel != null}, " +
                $"heartGamePanel={heartGamePanel != null}, " +
                $"resultPanel={resultPanel != null}");
    }
    
    // 색상게임 UI 표시
    private void ShowColorGameUI()
    {
        if (colorGamePanel != null)
            colorGamePanel.SetActive(true);
            
        if (gameTitle != null)
            gameTitle.text = "감정 매칭 게임";
            
        if (gameInstructions != null)
            gameInstructions.text = "화면에 표시된 색상에 해당하는 감정을 표현하세요!";
            
        Debug.Log("[MiniGameUI] 색상 게임 UI 표시됨");
    }
    
    // 하트게임 UI 표시
    private void ShowHeartGameUI()
    {
        if (heartGamePanel != null)
            heartGamePanel.SetActive(true);
            
        if (gameTitle != null)
            gameTitle.text = "하트 수집 게임";
            
        if (gameInstructions != null)
            gameInstructions.text = "시야에 나타나는 하트를 시선으로 수집하세요!";
            
        Debug.Log("[MiniGameUI] 하트 게임 UI 표시됨");
    }
    
    // 타이머 업데이트
    public void UpdateTimer(float remainingTime)
    {
        if (timerText != null)
        {
            timerText.text = $"시간: {Mathf.CeilToInt(remainingTime)}초";
        }
    }
    
    // 진행 상황 업데이트
    public void UpdateProgress(float progress)
    {
        if (gameProgressBar != null)
        {
            gameProgressBar.fillAmount = progress;
        }
    }
    
    // 색상 게임 매치 카운트 업데이트
    public void UpdateMatchCount(int current, int target)
    {
        if (matchCountText != null)
        {
            matchCountText.text = $"매치: {current}/{target}";
        }
    }
    
    // 하트 게임 수집 카운트 업데이트
    public void UpdateHeartsCollected(int current, int target)
    {
        if (heartsCollectedText != null)
        {
            heartsCollectedText.text = $"하트: {current}/{target}";
        }
    }
    
    // 감정 매칭 진행도 업데이트
    public void UpdateEmotionMatchProgress(float progress)
    {
        if (emotionMatchProgressBar != null)
        {
            emotionMatchProgressBar.fillAmount = progress;
        }
    }
    
    // 타겟 색상 업데이트
    public void UpdateTargetColor(Color color)
    {
        if (targetColorDisplay != null)
        {
            targetColorDisplay.color = color;
        }
    }
    
    // 게임 결과 UI 표시
    public void ShowResultUI(bool success, int score)
    {
        try
        {
            Debug.Log($"[MiniGameUI] 결과 UI 표시 - 성공: {success}, 점수: {score}");
            
            // 기존 자동 숨김 코루틴 취소
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
            
            // 결과 패널 활성화
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                
                if (resultText != null)
                {
                    if (success)
                    {
                        resultText.text = $"성공!\n점수: {score}";
                        resultText.color = Color.green;
                    }
                    else
                    {
                        resultText.text = $"실패!\n점수: {score}";
                        resultText.color = Color.red;
                    }
                }
                
                // 자동으로 결과 화면을 숨기는 코루틴 시작 (3초 후로 단축)
                autoHideCoroutine = StartCoroutine(AutoHideResultPanel(3.0f));
                
                // 현재 패널 활성화 상태 로깅 (디버깅용)
                LogPanelStates();
            }
            else
            {
                Debug.LogWarning("[MiniGameUI] 결과 패널이 null입니다");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MiniGameUI] 결과 UI 표시 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // 일정 시간 후 결과 패널 자동 숨기기 (시간 단축)
    private IEnumerator AutoHideResultPanel(float delay)
    {
        Debug.Log($"[MiniGameUI] {delay}초 후 결과 패널 자동 숨김 예약됨");
        yield return new WaitForSeconds(delay);
        
        Debug.Log("[MiniGameUI] 결과 패널 자동 숨김 실행");
        HideResultPanel();
        autoHideCoroutine = null;
    }
    
    // 결과 패널 숨기기
    public void HideResultPanel()
    {
        try
        {
            Debug.Log("[MiniGameUI] 결과 패널 숨김 요청");
            
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
                
                // 미니게임 UI도 함께 숨기기
                HideAllPanels();
                
                // 미니게임 매니저에 게임 종료 알림 (필요한 경우)
                if (miniGameManager != null)
                {
                    miniGameManager.StopCurrentMiniGame();
                }
            }
            else
            {
                Debug.LogWarning("[MiniGameUI] 결과 패널이 null입니다");
            }
            
            // 현재 패널 활성화 상태 로깅 (디버깅용)
            LogPanelStates();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MiniGameUI] 결과 패널 숨기기 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // 색상 게임 UI만 숨기기
    public void HideColorGameUI()
    {
        if (colorGamePanel != null)
            colorGamePanel.SetActive(false);
    }
    
    // 하트 게임 UI만 숨기기
    public void HideHeartGameUI()
    {
        if (heartGamePanel != null)
            heartGamePanel.SetActive(false);
    }
    
    // UI 강제 숨기기 (외부에서 호출 가능)
    public void ForceHideAllUI()
    {
        Debug.Log("[MiniGameUI] 모든 UI 강제 숨김 요청");
        
        // 모든 코루틴 중지
        StopAllCoroutines();
        autoHideCoroutine = null;
        
        // 모든 패널 비활성화
        HideAllPanels();
        
        // 미니게임 매니저 알림
        if (miniGameManager != null)
        {
            miniGameManager.StopCurrentMiniGame();
        }
    }
    
    private void OnDisable()
    {
        // 컴포넌트 비활성화 시 모든 코루틴 중지
        StopAllCoroutines();
        autoHideCoroutine = null;
        
        Debug.Log("[MiniGameUI] OnDisable 호출됨, 모든 코루틴 중지");
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (continueButton != null)
            continueButton.onClick.RemoveListener(HideResultPanel);
            
        Debug.Log("[MiniGameUI] OnDestroy 호출됨, 이벤트 구독 해제");
    }
    
    // 미니게임 매니저 참조 설정 (필요시)
    public void SetMiniGameManager(MiniGameManager manager)
    {
        miniGameManager = manager;
    }
}