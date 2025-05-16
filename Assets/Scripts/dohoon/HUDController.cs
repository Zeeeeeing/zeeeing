using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI timerText;

    [Header("Gameplay")]
    public int   score          = 0;
    public float maxTimeSeconds = 300f;

    float timeRemaining;
    bool  running = false;

    void Start()
    {
        // 게임 시작 시 타이머 초기화만 하고 실행은 하지 않음
        timeRemaining = maxTimeSeconds;
        running = false; // 시작 시에는 실행하지 않음
        UpdateScore(0);
        
        // 타이머 초기 표시
        timerText.text = $"TIME  {maxTimeSeconds:0}";

        // 코드에서 바로 이벤트 연결
        OnTimeUp.AddListener(LoadEndScene);
    }

    void Update()
    {
        if (!running) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            running = false;
            OnTimeUp.Invoke();
        }
        timerText.text = $"TIME  {timeRemaining:0}";
    }

    public void UpdateScore(int delta)
    {
        score += delta;
        scoreText.text = $"SCORE  {score}";
    }

    // 타이머 시작 함수 - 외부에서 호출 가능하도록 public으로 설정
    public void StartTimer()
    {
        running = true;
    }

    // ------------------------------
    // 타임업 이벤트
    // ------------------------------
    public UnityEvent OnTimeUp;

    void LoadEndScene()
    {
        SceneManager.LoadScene("EndScene");
    }
}