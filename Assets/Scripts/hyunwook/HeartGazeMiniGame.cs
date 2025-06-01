using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class HeartGazeMiniGame : MonoBehaviour
{
    [Header("Game Settings - Fast Tempo")]
    [SerializeField] private GameObject heartPrefab; // 하트 오브젝트 프리팹
    [SerializeField] private int totalHeartsToCollect = 6; // 감소 (10 -> 6)
    [SerializeField] private float gameTime = 20f; // 감소 (30 -> 20)
    [SerializeField] private float heartLifetime = 4f; // 증가 (3 -> 4, 수집 시간 여유)
    
    [Header("Difficulty Settings - Optimized for Demo")]
    [SerializeField] private int easyTargetHearts = 4; // 감소 (8 -> 4)
    [SerializeField] private int normalTargetHearts = 6; // 감소 (12 -> 6)
    [SerializeField] private int hardTargetHearts = 8; // 감소 (15 -> 8)
    
    [SerializeField] private float easyGameTime = 25f; // 감소 (40 -> 25)
    [SerializeField] private float normalGameTime = 20f; // 감소 (30 -> 20)
    [SerializeField] private float hardGameTime = 15f; // 감소 (20 -> 15)
    
    [Header("Spawn Settings - More Accessible")]
    [SerializeField] private float minSpawnDistance = 0.8f; // 증가 (0.5 -> 0.8)
    [SerializeField] private float maxSpawnDistance = 1.5f; // 감소 (2.0 -> 1.5)
    [SerializeField] private float spawnAngleRange = 60f; // 증가 (45 -> 60, 더 넓은 범위)
    [SerializeField] private int maxConcurrentHearts = 2; // 동시 하트 수 제한
    
    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI heartsCollectedText;
    [SerializeField] private TMPro.TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameUI;
    
    [Header("Heart Collection Settings")]
    [SerializeField] private float gazeHoldTime = 0.8f; // 하트를 바라봐야 하는 시간 (감소)
    [SerializeField] private float collectionRadius = 0.3f; // 하트 콜라이더 크기 (시선 감지용)
    [SerializeField] private int heartLayer = 0; // 하트가 배치될 레이어 (0-31 범위)
    
    // 게임 상태 변수
    private int heartsCollected = 0;
    private float remainingTime;
    private bool isGameActive = false;
    private List<GameObject> activeHearts = new List<GameObject>();
    
    // 카메라 참조 (플레이어 시점)
    private Transform cameraTransform;
    
    // 하트 생성 간격 조정
    private float heartSpawnInterval = 1.2f; // 감소 (기존 랜덤 0.8-2.0 -> 고정 1.2)
    private float lastHeartSpawnTime = 0f;
    
    // 하트 수집 추적
    private Dictionary<GameObject, float> heartGazeTimes = new Dictionary<GameObject, float>();
    private GameObject currentGazedHeart = null;
    
    private void Awake()
    {
        if (gameUI != null)
            gameUI.SetActive(false);
            
        // 카메라 참조 확인 및 설정
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("메인 카메라 참조 설정 완료");
        }
        else
        {
            Debug.LogError("메인 카메라를 찾을 수 없습니다!");
        }
    }
    
    public void StartMiniGame(int difficulty = 1)
    {
        // 필수 참조 확인
        if (cameraTransform == null)
        {
            Debug.LogError("카메라 참조가 null입니다! 게임을 시작할 수 없습니다.");
            return;
        }
        
        if (heartPrefab == null)
        {
            Debug.LogError("heartPrefab이 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        // UI 참조 확인
        if (gameUI == null)
        {
            Debug.LogWarning("gameUI가 null입니다! Unity Inspector에서 할당해주세요.");
            gameUI = GameObject.Find("HeartGamePanel");
            if (gameUI == null)
            {
                Debug.LogError("HeartGamePanel을 찾을 수 없습니다! UI가 표시되지 않을 수 있습니다.");
            }
        }
    
        // 난이도에 따른 설정
        switch(difficulty)
        {
            case 0: // Easy
                totalHeartsToCollect = easyTargetHearts;
                gameTime = easyGameTime;
                heartSpawnInterval = 1.5f; // 더 여유롭게
                maxConcurrentHearts = 2;
                gazeHoldTime = 0.6f; // 더 쉽게
                break;
            case 1: // Normal
                totalHeartsToCollect = normalTargetHearts;
                gameTime = normalGameTime;
                heartSpawnInterval = 1.2f;
                maxConcurrentHearts = 2;
                gazeHoldTime = 0.8f;
                break;
            case 2: // Hard
                totalHeartsToCollect = hardTargetHearts;
                gameTime = hardGameTime;
                heartSpawnInterval = 1.0f; // 더 빠르게
                maxConcurrentHearts = 3;
                gazeHoldTime = 1.0f; // 더 어렵게
                break;
        }
        
        // 게임 초기화
        heartsCollected = 0;
        remainingTime = gameTime;
        isGameActive = true;
        lastHeartSpawnTime = 0f;
        
        // 하트 수집 추적 초기화
        heartGazeTimes.Clear();
        currentGazedHeart = null;

        if (gameUI != null)
        {
            gameUI.SetActive(true);
            Debug.Log("HeartGazeMiniGame UI 활성화됨 (빠른 템포 모드)");
        }
        
        // 기존 하트 정리
        ClearAllHearts();
        
        // UI 업데이트
        UpdateUI();
        
        // 게임 시작 알림
        if (OnGameStarted != null) OnGameStarted.Invoke();
        
        Debug.Log($"하트 수집 게임 시작 - 목표: {totalHeartsToCollect}개, 시간: {gameTime}초, 응시시간: {gazeHoldTime}초");
    }
    
    private void Update()
    {
        if (!isGameActive) return;
        
        // 타이머 업데이트
        remainingTime -= Time.deltaTime;
        
        // 하트 생성 로직 (maxConcurrentHearts 체크 강화)
        if (Time.time - lastHeartSpawnTime >= heartSpawnInterval)
        {
            // 실제 활성화된 하트 수 체크 (null 제거)
            activeHearts.RemoveAll(heart => heart == null);
            
            if (activeHearts.Count < maxConcurrentHearts)
            {
                SpawnHeart();
                lastHeartSpawnTime = Time.time;
                Debug.Log($"하트 생성됨. 현재 활성 하트 수: {activeHearts.Count}/{maxConcurrentHearts}");
            }
            else
            {
                Debug.Log($"최대 하트 수 도달: {activeHearts.Count}/{maxConcurrentHearts}");
            }
        }
        
        // 하트 수집 감지 (시선 기반)
        DetectHeartCollection();
        
        // UI 업데이트
        UpdateUI();
        
        // 시간 종료 체크
        if (remainingTime <= 0)
        {
            GameOver(heartsCollected >= totalHeartsToCollect);
        }
    }
    
    private void SpawnHeart()
    {
        // 안전성 검사
        if (cameraTransform == null)
        {
            Debug.LogError("카메라 트랜스폼이 null입니다!");
            return;
        }
        
        if (heartPrefab == null)
        {
            Debug.LogError("heartPrefab이 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        try
        {
            // 플레이어 시야 내 랜덤 위치 계산 (더 접근하기 쉽게)
            float angle = Random.Range(-spawnAngleRange, spawnAngleRange);
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            
            // 위치 계산 (플레이어 기준)
            Vector3 direction = Quaternion.Euler(0, angle, 0) * cameraTransform.forward;
            Vector3 position = cameraTransform.position + direction * distance;
            
            // 높이 설정 (눈높이 기준, 범위 확대)
            position.y = cameraTransform.position.y + Random.Range(-0.4f, 0.4f);
            
            // 하트가 항상 플레이어를 향하도록 회전 설정
            Vector3 toPlayer = cameraTransform.position - position;
            Quaternion rotation = Quaternion.LookRotation(toPlayer);
            
            // 하트 생성
            GameObject heart = Instantiate(heartPrefab, position, rotation);
            
            if (heart == null)
            {
                Debug.LogError("하트 인스턴스 생성 실패!");
                return;
            }
            
            // 하트 설정
            SetupHeart(heart);
            
            activeHearts.Add(heart);
            
            // 일정 시간 후 하트 자동 제거
            StartCoroutine(HeartLifetimeRoutine(heart));
            
            Debug.Log($"하트 생성됨: 위치={position}, 각도={angle}도, 거리={distance}m");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"하트 생성 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void SetupHeart(GameObject heart)
    {
        // 하트 크기 조정 (더 크게, 보기 쉽게)
        heart.transform.localScale = Vector3.one * Random.Range(0.15f, 0.25f);
        
        // 레이어 설정 (0-31 범위 체크)
        if (heartLayer >= 0 && heartLayer <= 31)
        {
            heart.layer = heartLayer;
        }
        else
        {
            Debug.LogWarning($"잘못된 레이어 값: {heartLayer}. 기본 레이어(0) 사용");
            heart.layer = 0;
        }
        
        // 콜라이더 확인 및 추가
        Collider heartCollider = heart.GetComponent<Collider>();
        if (heartCollider == null)
        {
            SphereCollider sphereCollider = heart.AddComponent<SphereCollider>();
            sphereCollider.radius = collectionRadius; // 시선 감지를 위한 콜라이더 크기
            sphereCollider.isTrigger = true;
            Debug.Log($"하트에 SphereCollider 추가됨 (반경: {collectionRadius})");
        }
        else
        {
            heartCollider.isTrigger = true;
            // 기존 콜라이더가 SphereCollider라면 크기 조정
            if (heartCollider is SphereCollider sphere)
            {
                sphere.radius = collectionRadius;
            }
        }
        
        // Rigidbody 확인 및 추가 (EyeInteractable 필요)
        Rigidbody rb = heart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = heart.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // EyeInteractable 컴포넌트 추가
        EyeInteractable eyeInteractable = heart.GetComponent<EyeInteractable>();
        if (eyeInteractable == null)
        {
            eyeInteractable = heart.AddComponent<EyeInteractable>();
            Debug.Log("하트에 EyeInteractable 추가됨");
        }
        
        // 하트 수집 추적 초기화
        heartGazeTimes[heart] = 0f;
        
        Debug.Log($"하트 설정 완료: 크기={heart.transform.localScale}, 레이어={heart.layer}, 콜라이더 반경={collectionRadius}");
    }
    
    private void DetectHeartCollection()
    {
        if (cameraTransform == null) return;
        
        // 현재 바라보고 있는 하트 찾기
        GameObject gazedHeart = null;
        float closestDistance = float.MaxValue;
        
        foreach (GameObject heart in activeHearts)
        {
            if (heart == null) continue;
            
            // 시선 방향 계산
            Vector3 directionToHeart = (heart.transform.position - cameraTransform.position).normalized;
            float dotProduct = Vector3.Dot(cameraTransform.forward, directionToHeart);
            
            // 거리 계산
            float distance = Vector3.Distance(cameraTransform.position, heart.transform.position);
            
            // 시선 각도 및 거리 조건 확인 (더 관대하게)
            if (dotProduct > 0.8f && distance <= maxSpawnDistance && distance < closestDistance)
            {
                // EyeInteractable로 호버 상태 확인
                EyeInteractable eyeInteractable = heart.GetComponent<EyeInteractable>();
                if (eyeInteractable != null && eyeInteractable.IsHovered)
                {
                    gazedHeart = heart;
                    closestDistance = distance;
                }
            }
        }
        
        // 시선 대상 변경 처리
        if (currentGazedHeart != gazedHeart)
        {
            // 이전 하트의 응시 시간 리셋
            if (currentGazedHeart != null && heartGazeTimes.ContainsKey(currentGazedHeart))
            {
                heartGazeTimes[currentGazedHeart] = 0f;
            }
            
            currentGazedHeart = gazedHeart;
        }
        
        // 현재 바라보고 있는 하트의 응시 시간 증가
        if (currentGazedHeart != null && heartGazeTimes.ContainsKey(currentGazedHeart))
        {
            heartGazeTimes[currentGazedHeart] += Time.deltaTime;
            
            // 충분히 오래 바라봤으면 수집
            if (heartGazeTimes[currentGazedHeart] >= gazeHoldTime)
            {
                CollectHeart(currentGazedHeart);
            }
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
            Debug.Log($"하트 수명 만료로 제거. 남은 활성 하트: {activeHearts.Count - 1}");
            RemoveHeart(heart);
            Destroy(heart);
        }
    }
    
    private void CollectHeart(GameObject heart)
    {
        if (!isGameActive || heart == null || !activeHearts.Contains(heart)) return;
        
        Debug.Log($"하트 수집! 응시 시간: {heartGazeTimes[heart]:F2}초");
        
        // 목록에서 제거
        RemoveHeart(heart);
        
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
    
    private void RemoveHeart(GameObject heart)
    {
        if (heart == null) return;
        
        activeHearts.Remove(heart);
        
        if (heartGazeTimes.ContainsKey(heart))
        {
            heartGazeTimes.Remove(heart);
        }
        
        if (currentGazedHeart == heart)
        {
            currentGazedHeart = null;
        }
    }
    
    private IEnumerator DestroyHeartWithEffect(GameObject heart)
    {
        if (heart == null) yield break;
        
        // 여기에 하트 수집 효과 추가 (파티클, 애니메이션 등)
        // 예: 하트 크기 커지고 사라지는 효과 (더 빠르게)
        float duration = 0.3f; // 감소 (0.5 -> 0.3)
        float elapsed = 0f;
        Vector3 originalScale = heart.transform.localScale;
        
        while (elapsed < duration && heart != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 스케일 증가 후 감소
            float scale = Mathf.Sin(t * Mathf.PI) * 0.8f + 1.0f;
            heart.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        if (heart != null)
            Destroy(heart);
    }
    
    private void PlaySuccessFeedback()
    {
        // 컨트롤러 진동 (강화)
        try
        {
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand) is var device && device.TryGetHapticCapabilities(out var capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, 0.6f, 0.15f); // 더 강한 진동
                }
            }
        }
        catch (System.Exception)
        {
            // VR 컨트롤러가 없을 경우 무시
        }
        
        // 오디오 재생 (필요시)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        Debug.Log($"하트 수집 성공! ({heartsCollected}/{totalHeartsToCollect})");
    }
    
    private void UpdateUI()
    {
        // 타이머 텍스트 업데이트
        if (timerText != null)
        {
            timerText.text = $"시간: {Mathf.CeilToInt(remainingTime)}초";
        }
        
        // 하트 수집 텍스트 업데이트
        if (heartsCollectedText != null)
        {
            heartsCollectedText.text = $"하트: {heartsCollected}/{totalHeartsToCollect}";
        }
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
        heartGazeTimes.Clear();
        currentGazedHeart = null;
    }
    
    private void GameOver(bool success)
    {
        isGameActive = false;
        
        // 모든 코루틴 정지
        StopAllCoroutines();
        
        // 게임 UI 비활성화
        if (gameUI != null)
            gameUI.SetActive(false);
        
        // 모든 하트 정리
        ClearAllHearts();
        
        // 게임 완료 이벤트 호출
        if (OnGameCompleted != null) 
        {
            Debug.Log($"HeartGame 완료 이벤트 발생: 성공={success}, 점수={heartsCollected} (빠른 템포 모드)");
            OnGameCompleted.Invoke(success, heartsCollected);
        }
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