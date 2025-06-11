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
    [SerializeField] private float collectionRadius = 0.1f; // 하트 콜라이더 크기 (시선 감지용)
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
        // 하트 크기 조정 (0.25배로 고정)
        heart.transform.localScale = Vector3.one * 0.05f;
        
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
        
        // 기존 concave MeshCollider 제거 후 SphereCollider로 교체
        Collider[] existingColliders = heart.GetComponents<Collider>();
        for (int i = 0; i < existingColliders.Length; i++)
        {
            if (existingColliders[i] is MeshCollider meshCol && !meshCol.convex)
            {
                Debug.Log($"Concave MeshCollider 발견, 제거 중: {heart.name}");
                DestroyImmediate(existingColliders[i]);
            }
        }
        
        // SphereCollider 확인 및 추가 (EyeTracking 감지를 위해 필수)
        SphereCollider sphereCollider = heart.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = heart.AddComponent<SphereCollider>();
            Debug.Log($"하트에 SphereCollider 추가됨: {heart.name}");
        }
        
        // ⭐ EyeTracking을 위한 SphereCollider 설정
        sphereCollider.radius = collectionRadius * 1.0f; // EyeTracking 감지 영역
        sphereCollider.isTrigger = true; // EyeTracking을 위해 true로 설정
        
        // Rigidbody 확인 및 추가 (EyeInteractable 필요)
        Rigidbody rb = heart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = heart.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // 물리 영향 안받도록
        rb.useGravity = false;
        
        // ⭐ EyeInteractable 컴포넌트 반드시 추가 (EyeTracking을 위해 필수)
        EyeInteractable eyeInteractable = heart.GetComponent<EyeInteractable>();
        if (eyeInteractable == null)
        {
            eyeInteractable = heart.AddComponent<EyeInteractable>();
            Debug.Log($"하트에 EyeInteractable 추가됨 (EyeTracking용): {heart.name}");
        }
        
        // 하트 수집 추적 초기화
        heartGazeTimes[heart] = 0f;
        
        Debug.Log($"하트 설정 완료 (EyeTracking 지원): 크기={heart.transform.localScale}, 레이어={heart.layer}, 콜라이더 반경={sphereCollider.radius}");
    }
    
    private void DetectHeartCollection()
    {
        if (cameraTransform == null) return;
        
        GameObject gazedHeart = null;
        float closestDistance = float.MaxValue;
        
        // ⭐ EyeTracking 기반 감지 (1순위)
        foreach (GameObject heart in activeHearts)
        {
            if (heart == null) continue;
            
            // EyeInteractable 컴포넌트로 실제 시선 추적 확인
            EyeInteractable eyeInteractable = heart.GetComponent<EyeInteractable>();
            if (eyeInteractable != null && eyeInteractable.IsHovered)
            {
                float distance = Vector3.Distance(cameraTransform.position, heart.transform.position);
                if (distance < closestDistance)
                {
                    gazedHeart = heart;
                    closestDistance = distance;
                }
                
                Debug.Log($"👁️ EyeTracking으로 하트 감지: {heart.name}, 거리: {distance:F2}m");
            }
        }
        
        // ⭐ 백업: 화면 중앙 기반 감지 (EyeTracking 실패 시)
        if (gazedHeart == null)
        {
            // 물리 레이캐스트로 직접 하트 감지
            Ray gazeRay = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit[] hits = Physics.RaycastAll(gazeRay, maxSpawnDistance + 1f);
            
            // 레이캐스트로 감지된 모든 오브젝트 확인
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // 활성 하트 목록에 있는지 확인
                if (activeHearts.Contains(hitObject))
                {
                    float distance = hit.distance;
                    if (distance < closestDistance)
                    {
                        gazedHeart = hitObject;
                        closestDistance = distance;
                    }
                    
                    Debug.Log($"🎯 레이캐스트로 하트 감지 (백업): {hitObject.name}, 거리: {distance:F2}m");
                }
            }
            
            // 3차 백업: 각도 기반 감지
            if (gazedHeart == null)
            {
                foreach (GameObject heart in activeHearts)
                {
                    if (heart == null) continue;
                    
                    // 시선 방향 계산
                    Vector3 directionToHeart = (heart.transform.position - cameraTransform.position).normalized;
                    float dotProduct = Vector3.Dot(cameraTransform.forward, directionToHeart);
                    
                    // 거리 계산
                    float distance = Vector3.Distance(cameraTransform.position, heart.transform.position);
                    
                    // 더 엄격한 각도 조건
                    if (dotProduct > 0.85f && distance <= maxSpawnDistance && distance < closestDistance)
                    {
                        gazedHeart = heart;
                        closestDistance = distance;
                        Debug.Log($"🔄 각도 기반으로 하트 감지 (3차 백업): {heart.name}, 각도: {dotProduct:F2}, 거리: {distance:F2}m");
                    }
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
                Debug.Log($"⏰ 이전 하트 응시 시간 리셋: {currentGazedHeart.name}");
            }
            
            currentGazedHeart = gazedHeart;
            
            if (currentGazedHeart != null)
            {
                // EyeTracking 여부 표시
                EyeInteractable eyeInteractable = currentGazedHeart.GetComponent<EyeInteractable>();
                bool isEyeTracking = eyeInteractable != null && eyeInteractable.IsHovered;
                Debug.Log($"👁️ 새로운 하트에 시선 고정: {currentGazedHeart.name} (EyeTracking: {isEyeTracking})");
            }
        }
        
        // 현재 바라보고 있는 하트의 응시 시간 증가
        if (currentGazedHeart != null && heartGazeTimes.ContainsKey(currentGazedHeart))
        {
            heartGazeTimes[currentGazedHeart] += Time.deltaTime;
            float currentGazeTime = heartGazeTimes[currentGazedHeart];
            
            // 응시 진행률 표시 (디버깅용)
            float progress = currentGazeTime / gazeHoldTime;
            if (progress % 0.2f < Time.deltaTime) // 20%마다 로그
            {
                // EyeTracking 상태 표시
                EyeInteractable eyeInteractable = currentGazedHeart.GetComponent<EyeInteractable>();
                bool isEyeTracking = eyeInteractable != null && eyeInteractable.IsHovered;
                string trackingMethod = isEyeTracking ? "👁️EyeTracking" : "🎯화면중앙";
                
                Debug.Log($"{trackingMethod} 하트 응시 중: {currentGazedHeart.name} - {progress:P0} ({currentGazeTime:F1}s/{gazeHoldTime:F1}s)");
            }
            
            // 충분히 오래 바라봤으면 수집
            if (currentGazeTime >= gazeHoldTime)
            {
                Debug.Log($"✅ 하트 수집 조건 달성! {currentGazedHeart.name} - {currentGazeTime:F2}초");
                CollectHeart(currentGazedHeart);
            }
        }
        
        // 시각적 피드백: 응시 중인 하트 강조
        HighlightGazedHeart();
    }

    private void HighlightGazedHeart()
    {
        foreach (GameObject heart in activeHearts)
        {
            if (heart == null) continue;
            
            Renderer heartRenderer = heart.GetComponent<Renderer>();
            if (heartRenderer == null) continue;
            
            if (heart == currentGazedHeart)
            {
                // 응시 중인 하트는 밝게
                float progress = heartGazeTimes.ContainsKey(heart) ? heartGazeTimes[heart] / gazeHoldTime : 0f;
                float intensity = 1.0f + Mathf.Sin(Time.time * 8f) * 0.3f; // 깜빡임 효과
                Color highlightColor = Color.Lerp(Color.white, Color.yellow, progress) * intensity;
                
                if (heartRenderer.material.HasProperty("_Color"))
                {
                    heartRenderer.material.color = highlightColor;
                }
                else if (heartRenderer.material.HasProperty("_BaseColor"))
                {
                    heartRenderer.material.SetColor("_BaseColor", highlightColor);
                }
            }
            else
            {
                // 일반 하트는 기본 색상
                if (heartRenderer.material.HasProperty("_Color"))
                {
                    heartRenderer.material.color = Color.red; // 하트 기본 색상
                }
                else if (heartRenderer.material.HasProperty("_BaseColor"))
                {
                    heartRenderer.material.SetColor("_BaseColor", Color.red);
                }
            }
        }
    }
    
    private IEnumerator HeartLifetimeRoutine(GameObject heart)
    {
        if (heart == null) 
        {
            Debug.LogWarning("HeartLifetimeRoutine: heart가 null입니다");
            yield break;
        }
        
        float elapsed = 0f;
        
        // 하트가 자동으로 사라지는 시간까지 대기
        while (elapsed < heartLifetime && heart != null && activeHearts.Contains(heart))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // ⭐ 수명이 다한 하트 안전하게 제거
        if (heart != null && activeHearts.Contains(heart))
        {
            Debug.Log($"하트 수명 만료로 제거: {heart.name}. 남은 활성 하트: {activeHearts.Count - 1}");
            RemoveHeart(heart);
            
            // 오브젝트 파괴
            if (heart != null)
            {
                Destroy(heart);
            }
        }
        else
        {
            Debug.Log($"하트가 이미 제거됨 또는 null: {heart?.name ?? "null"}");
        }
    }

    private void CollectHeart(GameObject heart)
    {
        if (!isGameActive || heart == null || !activeHearts.Contains(heart)) 
        {
            Debug.LogWarning($"하트 수집 조건 불만족: 게임활성={isGameActive}, 하트null={heart == null}, 목록포함={activeHearts.Contains(heart)}");
            return;
        }
        
        Debug.Log($"하트 수집! 응시 시간: {heartGazeTimes[heart]:F2}초");
        
        // ⭐ 먼저 목록에서 제거 (중복 처리 방지)
        RemoveHeart(heart);
        
        // 하트 수집 카운트 증가
        heartsCollected++;
        
        // UI 업데이트
        UpdateUI();
        
        // 성공 피드백
        PlaySuccessFeedback();
        
        // ⭐ 즉시 하트 제거 (코루틴 사용하지 않음)
        if (heart != null)
        {
            // 간단한 효과만 적용 후 즉시 제거
            heart.transform.localScale *= 1.2f; // 약간 커지는 효과
            Destroy(heart);
            Debug.Log($"하트 즉시 제거됨: {heart.name}");
        }
        
        // ⭐ 목표 달성 확인 (하트가 확실히 제거된 후)
        Debug.Log($"현재 수집된 하트: {heartsCollected}/{totalHeartsToCollect}");
        if (heartsCollected >= totalHeartsToCollect)
        {
            Debug.Log("목표 달성! 게임 성공으로 종료");
            GameOver(true);
        }
    }
    
    private void RemoveHeart(GameObject heart)
    {
        if (heart == null) 
        {
            Debug.LogWarning("RemoveHeart: heart가 null입니다");
            return;
        }
        
        // 활성 하트 목록에서 제거
        bool removed = activeHearts.Remove(heart);
        Debug.Log($"활성 하트 목록에서 제거: {removed}, 남은 하트 수: {activeHearts.Count}");
        
        // 응시 시간 추적에서 제거
        if (heartGazeTimes.ContainsKey(heart))
        {
            heartGazeTimes.Remove(heart);
            Debug.Log($"응시 시간 추적에서 제거: {heart.name}");
        }
        
        // 현재 응시 중인 하트였다면 초기화
        if (currentGazedHeart == heart)
        {
            currentGazedHeart = null;
            Debug.Log("현재 응시 중인 하트 초기화됨");
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
        Debug.Log($"모든 하트 정리 시작. 현재 활성 하트 수: {activeHearts.Count}");
        
        // ⭐ 역순으로 제거 (안전성 향상)
        for (int i = activeHearts.Count - 1; i >= 0; i--)
        {
            GameObject heart = activeHearts[i];
            if (heart != null)
            {
                Debug.Log($"하트 제거 중: {heart.name}");
                Destroy(heart);
            }
        }
        
        // 모든 목록 초기화
        activeHearts.Clear();
        heartGazeTimes.Clear();
        currentGazedHeart = null;
        
        Debug.Log("모든 하트 정리 완료");
    }
    
    private void GameOver(bool success)
    {
        if (!isGameActive)
        {
            Debug.LogWarning("[HeartGazeMiniGame] 이미 게임이 비활성화된 상태에서 GameOver 호출됨");
            return;
        }
        
        Debug.Log($"[HeartGazeMiniGame] GameOver 호출: 성공={success}, 수집된 하트={heartsCollected}");
        
        // ⭐ 게임 상태를 먼저 비활성화 (중복 호출 방지)
        isGameActive = false;
        
        // 모든 코루틴 정지 (하트 수명 코루틴 포함)
        StopAllCoroutines();
        
        // 게임 UI 비활성화
        if (gameUI != null)
            gameUI.SetActive(false);
        
        // ⭐ 모든 하트 강제 정리 (마지막 하트 포함)
        Debug.Log("게임 종료로 인한 모든 하트 강제 정리");
        ClearAllHearts();
        
        // ⭐ MiniGameUI 파괴 여부 확인 후 이벤트 호출
        try
        {
            if (OnGameCompleted != null) 
            {
                Debug.Log($"[HeartGazeMiniGame] 게임 완료 이벤트 발생: 성공={success}, 점수={heartsCollected}");
                OnGameCompleted.Invoke(success, heartsCollected);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HeartGazeMiniGame] 게임 완료 이벤트 호출 중 에러: {e.Message}");
        }
        
        Debug.Log("[HeartGazeMiniGame] GameOver 처리 완료");
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
        Debug.Log("[HeartGazeMiniGame] OnDisable 호출됨");
        
        // ⭐ 게임이 활성화된 상태에서만 정리 작업 수행
        if (isGameActive)
        {
            Debug.Log("[HeartGazeMiniGame] 게임 활성화 상태에서 강제 종료");
            
            // 이벤트 호출 전에 게임 상태를 먼저 비활성화
            isGameActive = false;
            
            // ⭐ MiniGameUI가 파괴되기 전에 직접 정리
            try
            {
                if (OnGameCompleted != null) 
                {
                    Debug.Log("[HeartGazeMiniGame] 게임 완료 이벤트 발생 (OnDisable)");
                    OnGameCompleted.Invoke(false, heartsCollected);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HeartGazeMiniGame] OnDisable에서 이벤트 호출 중 에러 (무시됨): {e.Message}");
            }
        }
        
        // 코루틴 중지 (모든 하트 수명 코루틴 포함)
        StopAllCoroutines();
        
        // ⭐ 모든 하트 오브젝트 강제 정리
        ClearAllHearts();
        
        Debug.Log("[HeartGazeMiniGame] OnDisable 정리 완료");
    }
    
    // 외부 이벤트
    public event System.Action OnGameStarted;
    public event System.Action<bool, int> OnGameCompleted; // 성공 여부, 수집한 하트 수
}