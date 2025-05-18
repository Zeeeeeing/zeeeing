using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class HeartGazeMiniGame : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private GameObject heartPrefab; // 하트 오브젝트 프리팹
    [SerializeField] private int totalHeartsToCollect = 10;
    [SerializeField] private float gameTime = 30f;
    [SerializeField] private float heartLifetime = 3f; // 하트가 자동으로 사라지는 시간
    
    [Header("Difficulty Settings")]
    [SerializeField] private int easyTargetHearts = 8;
    [SerializeField] private int normalTargetHearts = 12;
    [SerializeField] private int hardTargetHearts = 15;
    
    [SerializeField] private float easyGameTime = 40f;
    [SerializeField] private float normalGameTime = 30f;
    [SerializeField] private float hardGameTime = 20f;
    
    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnDistance = 0.5f; // 최소 생성 거리
    [SerializeField] private float maxSpawnDistance = 2.0f; // 최대 생성 거리
    // [SerializeField] private float spawnHeight = 1.7f; // 플레이어 눈높이 기준
    [SerializeField] private float spawnAngleRange = 45f; // 시야각 범위 (좌우)
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI heartsCollectedText;
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameUI;
    
    // 게임 상태 변수
    private int heartsCollected = 0;
    private float remainingTime;
    private bool isGameActive = false;
    private List<GameObject> activeHearts = new List<GameObject>();
    
    // 카메라 참조 (플레이어 시점)
    private Transform cameraTransform;
    
    private void Awake()
    {
        gameUI.SetActive(false);
        cameraTransform = Camera.main.transform;
    }
    
    public void StartMiniGame(int difficulty = 1)
    {
        // 필요한 참조 확인
        if (gameUI == null)
        {
            Debug.LogWarning("gameUI가 null입니다! Unity Inspector에서 할당해주세요.");
            gameUI = GameObject.Find("ColorGamePanel");
            if (gameUI == null)
            {
                Debug.LogError("ColorGamePanel을 찾을 수 없습니다! UI가 표시되지 않을 수 있습니다.");
            }
        }
    
        // 난이도에 따른 설정
        switch(difficulty)
        {
            case 0: // Easy
                totalHeartsToCollect = easyTargetHearts;
                gameTime = easyGameTime;
                break;
            case 1: // Normal
                totalHeartsToCollect = normalTargetHearts;
                gameTime = normalGameTime;
                break;
            case 2: // Hard
                totalHeartsToCollect = hardTargetHearts;
                gameTime = hardGameTime;
                break;
        }
        
        // 게임 초기화
        heartsCollected = 0;
        remainingTime = gameTime;
        isGameActive = true;

        if (gameUI != null)
        {
            gameUI.SetActive(true);
            Debug.Log("ColorGazeMiniGame UI 활성화됨");
        }
        
        // 기존 하트 정리
        ClearAllHearts();
        
        // UI 업데이트
        UpdateUI();
        
        // 하트 생성 시작
        StartCoroutine(SpawnHeartsRoutine());
        
        // 게임 시작 알림
        if (OnGameStarted != null) OnGameStarted.Invoke();
    }
    
    private IEnumerator SpawnHeartsRoutine()
    {
        // 총 생성할 하트 수 (목표 하트 수의 150% 정도)
        int totalHeartsToSpawn = Mathf.CeilToInt(totalHeartsToCollect * 1.5f);
        int spawnedHearts = 0;
        
        while (isGameActive && spawnedHearts < totalHeartsToSpawn)
        {
            // 한 번에 최대 3개까지만 화면에 표시
            if (activeHearts.Count < 3)
            {
                SpawnHeart();
                spawnedHearts++;
            }
            
            // 랜덤 간격으로 하트 생성
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 2.0f));
        }
    }
    
    private void SpawnHeart()
    {
        if (cameraTransform == null) 
        {
            Debug.LogError("카메라 트랜스폼이 null입니다!");
            return;
        }
        
        // heartPrefab이 null인지 확인
        if (heartPrefab == null)
        {
            Debug.LogError("heartPrefab이 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        try
        {
            // 플레이어 시야 내 랜덤 위치 계산
            float angle = UnityEngine.Random.Range(-spawnAngleRange, spawnAngleRange);
            float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
            
            // 위치 계산 (플레이어 기준)
            Vector3 direction = Quaternion.Euler(0, angle, 0) * cameraTransform.forward;
            Vector3 position = cameraTransform.position + direction * distance;
            
            // 높이 설정 (눈높이 기준)
            position.y = cameraTransform.position.y + UnityEngine.Random.Range(-0.3f, 0.3f);
            
            // 하트가 항상 플레이어를 향하도록 회전 설정
            Quaternion rotation = Quaternion.LookRotation(position - cameraTransform.position);
            
            // 하트 생성
            GameObject heart = Instantiate(heartPrefab, position, rotation);
            
            if (heart == null)
            {
                Debug.LogError("하트 인스턴스 생성 실패!");
                return;
            }
            
            // 콜라이더가 없다면 추가 (필수)
            Collider heartCollider = heart.GetComponent<Collider>();
            if (heartCollider == null)
            {
                SphereCollider sphereCollider = heart.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.2f;
                sphereCollider.isTrigger = true;
            }
            
            activeHearts.Add(heart);
            
            // EyeInteractable 컴포넌트 생성 처리
            EyeInteractable eyeInteractable = heart.GetComponent<EyeInteractable>();
            if (eyeInteractable == null)
            {
                // Collider가 있는지 한 번 더 확인
                if (heart.GetComponent<Collider>() != null)
                {
                    eyeInteractable = heart.AddComponent<EyeInteractable>();
                }
                else
                {
                    Debug.LogWarning("Collider가 없어 EyeInteractable을 추가할 수 없습니다.");
                }
            }
            
            // eyeInteractable이 null이 아닌지 확인 후 이벤트 구독
            if (eyeInteractable != null)
            {
                // 이벤트 리스너 참조 캡처를 위한 로컬 참조 저장
                GameObject heartRef = heart;
                eyeInteractable.OnObjectHover.AddListener((go) => {
                    if (heartRef != null)
                    {
                        CollectHeart(heartRef);
                    }
                });
            }
            
            // 일정 시간 후 하트 자동 제거
            StartCoroutine(HeartLifetimeRoutine(heart));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"하트 생성 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private IEnumerator HeartLifetimeRoutine(GameObject heart)
    {
        if (heart == null) yield break;
        
        // 하트가 자동으로 사라지는 시간
        yield return new WaitForSeconds(heartLifetime);
        
        // 아직 수집되지 않은 하트라면 제거
        if (heart != null && activeHearts.Contains(heart))
        {
            activeHearts.Remove(heart);
            Destroy(heart);
        }
    }
    
    private void CollectHeart(GameObject heart)
    {
        if (!isGameActive || heart == null || !activeHearts.Contains(heart)) return;
        
        // 목록에서 제거
        activeHearts.Remove(heart);
        
        // 하트 수집 카운트 증가
        heartsCollected++;
        
        // UI 업데이트
        UpdateUI();
        
        // 성공 피드백
        PlaySuccessFeedback();
        
        // 하트 제거 (VFX와 함께)
        StartCoroutine(DestroyHeartWithEffect(heart));
        
        // 목표 달성 확인
        if (heartsCollected >= totalHeartsToCollect)
        {
            GameOver(true);
        }
    }
    
    private IEnumerator DestroyHeartWithEffect(GameObject heart)
    {
        if (heart == null) yield break;
        
        // 여기에 하트 수집 효과 추가 (파티클, 애니메이션 등)
        // 예: 하트 크기 커지고 사라지는 효과
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = heart.transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 스케일 증가 후 감소
            float scale = Mathf.Sin(t * Mathf.PI) * 0.5f + 1.0f;
            heart.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        Destroy(heart);
    }
    
    private void PlaySuccessFeedback()
    {
        // 컨트롤러 진동
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand) is var device && device.TryGetHapticCapabilities(out var capabilities))
        {
            if (capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, 0.3f, 0.1f);
            }
        }
        
        // 오디오 재생 (필요시)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
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
            GameOver(heartsCollected >= totalHeartsToCollect);
        }
    }
    
    private void UpdateUI()
    {
        // 타이머 텍스트 업데이트
        timerText.text = $"시간: {Mathf.CeilToInt(remainingTime)}초";
        
        // 하트 수집 텍스트 업데이트
        heartsCollectedText.text = $"하트: {heartsCollected}/{totalHeartsToCollect}";
    }
    
    private void ClearAllHearts()
    {
        foreach (GameObject heart in activeHearts)
        {
            if (heart != null)
            {
                Destroy(heart);
            }
        }
        
        activeHearts.Clear();
    }
    
    private void GameOver(bool success)
    {
        isGameActive = false;
        
        // 모든 코루틴 정지
        StopAllCoroutines();
        
        // 게임 UI 비활성화
        gameUI.SetActive(false);
        
        // 모든 하트 정리
        ClearAllHearts();
        
        // 게임 완료 이벤트 호출
        if (OnGameCompleted != null) OnGameCompleted.Invoke(success, heartsCollected);
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

    private void OnDisable()
    {
        // 게임 활성화 상태라면 중지
        if (isGameActive)
        {
            GameOver(false);
        }
        
        // 코루틴 중지
        StopAllCoroutines();
        
        // 모든 하트 오브젝트 정리
        ClearAllHearts();
    }
    
    // 외부 이벤트
    public event System.Action OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted; // 성공 여부, 수집한 하트 수
}