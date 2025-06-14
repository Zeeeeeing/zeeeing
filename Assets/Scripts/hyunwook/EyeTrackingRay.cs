using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ZeeeingGaze;

namespace ZeeeingGaze
{
    [RequireComponent(typeof(LineRenderer))]
    public class EyeTrackingRay : MonoBehaviour
    {
        [Header("Ray Settings")]
        [SerializeField] private float rayDistance = 8.0f;
        [SerializeField] private LayerMask layersToInclude;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float rayUpdateInterval = 0.033f; // 30fps
        
        [Header("Gaze Detection")]
        [SerializeField] private float minDetectionDistance = 0.1f;
        [SerializeField] private float maxDetectionDistance = 6.0f;
        
        // 🔥 모든 안정성 관련 변수 제거! 즉시 반응하도록 변경
        
        [Header("Rendering - Contact Only")]
        [SerializeField] private bool useVFXLaser = true;
        [SerializeField] private bool showDefaultRay = false;
        [SerializeField] private float laserDuration = 0.1f;
        [SerializeField] private Color defaultLaserColor = Color.white;
        
        [Header("Basic Raycast Settings")]
        [SerializeField] private Color basicRaycastColor = Color.red;
        [SerializeField] private float basicRaycastWidth = 0.01f;
        [SerializeField] private float basicRaycastDuration = 0.1f;
        
        [Header("VFX Cooldown Settings")]
        [SerializeField] private float vfxCooldownTime = 0.1f;
        
        [Header("Performance")]
        [SerializeField] private bool useRayCastAll = false;
        [SerializeField] private bool autoCleanupInteractables = true;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool showGazeGizmos = true;
        
        private LineRenderer lineRenderer;
        private HashSet<EyeInteractable> eyeInteractables = new HashSet<EyeInteractable>();
        private float nextRaycastTime = 0f;
        private RaycastHit[] hitResults = new RaycastHit[10];
        private Dictionary<GameObject, float> lastVFXRequestTime = new Dictionary<GameObject, float>();
        
        // 🔥 안정화 관련 변수들 대부분 제거, 최소한만 유지
        private EyeInteractable currentActiveInteractable; // 현재 활성화된 인터랙터블
        
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        private void Start()
        {
            SetupRay();
        }
        
        private void SetupRay()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = basicRaycastWidth;
            lineRenderer.endWidth = basicRaycastWidth * 0.5f;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.forward * rayDistance);
            
            // 기본적으로 비활성화
            lineRenderer.enabled = false;
            nextRaycastTime = Time.time;
            
            if (debugMode)
            {
                Debug.Log("[EyeTrackingRay] LineRenderer 설정 완료 - 즉시 반응 버전!");
            }
        }
        
        private void Update()
        {
            // 성능 최적화: 낮은 빈도로 레이캐스트 수행
            if (Time.time >= nextRaycastTime)
            {
                PerformImmediateRaycast();
                nextRaycastTime = Time.time + rayUpdateInterval;
            }
            
            // 정리 작업
            if (Time.frameCount % 60 == 0) // 2초마다
            {
                CleanUpInactiveInteractables();
            }
        }
        
        // 🔥 즉시 레이캐스트 (안정성 시간 없음)
        private void PerformImmediateRaycast()
        {
            if (useRayCastAll)
            {
                PerformMultiRaycast();
            }
            else
            {
                PerformSingleRaycast();
            }
        }
        
        private void PerformSingleRaycast()
        {
            // 레이캐스트 계산
            OVREyeGaze eyeGaze = GetComponent<OVREyeGaze>();
            Ray ray = eyeGaze != null ? 
                new Ray(transform.position, eyeGaze.transform.forward) : 
                new Ray(transform.position, transform.forward);
            
            RaycastHit hit;
            EyeInteractable detectedInteractable = null;
            
            // 레이캐스트 수행
            if (Physics.Raycast(ray, out hit, rayDistance, layersToInclude, triggerInteraction))
            {
                if (debugMode)
                {
                    Debug.Log($"🎯 [레이캐스트 성공] {hit.transform.name}, 거리: {hit.distance:F2}m");
                }
                
                // 1단계: 거리 체크
                if (IsValidDetectionDistance(hit.distance))
                {
                    if (debugMode)
                    {
                        Debug.Log($"✅ [거리 체크 통과] {hit.distance:F2}m");
                    }
                    
                    // 2단계: EyeInteractable 체크
                    var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                    if (eyeInteractable != null)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"✅ [EyeInteractable 발견] {eyeInteractable.gameObject.name}");
                        }
                        
                        // 3단계: NPCEmotionController 체크
                        NPCEmotionController npcController = eyeInteractable.GetComponent<NPCEmotionController>();
                        if (npcController == null)
                        {
                            npcController = eyeInteractable.GetComponentInParent<NPCEmotionController>();
                        }
                        
                        if (npcController != null)
                        {
                            detectedInteractable = eyeInteractable;
                            
                            if (useVFXLaser)
                            {
                                if (debugMode)
                                {
                                    Debug.Log($"🚀 [HandleImmediateEyeContact 호출] {eyeInteractable.gameObject.name}");
                                }
                                HandleImmediateEyeContact(detectedInteractable, hit.point);
                            }
                        }
                    }
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log("❌ [레이캐스트 미스]");
                }
            }
            
            // 🔥 즉시 인터랙터블 변경 처리
            SetActiveInteractableImmediate(detectedInteractable);
        }
        
        private void PerformMultiRaycast()
        {
            OVREyeGaze eyeGaze = GetComponent<OVREyeGaze>();
            Ray ray = eyeGaze != null ? 
                new Ray(transform.position, eyeGaze.transform.forward) : 
                new Ray(transform.position, transform.forward);
            
            int hitCount = Physics.RaycastNonAlloc(ray, hitResults, rayDistance, layersToInclude, triggerInteraction);
            EyeInteractable detectedInteractable = null;
            Vector3 closestHitPoint = Vector3.zero;
            
            if (debugMode)
            {
                Debug.Log($"[EyeTrackingRay] 멀티 레이캐스트: {hitCount}개 객체 감지");
            }
            
            if (hitCount > 0)
            {
                float closestDistance = float.MaxValue;
                
                for (int i = 0; i < hitCount; i++)
                {
                    float distance = hitResults[i].distance;
                    
                    if (!IsValidDetectionDistance(distance))
                        continue;
                    
                    var eyeInteractable = hitResults[i].transform.GetComponent<EyeInteractable>();
                    if (eyeInteractable != null && distance < closestDistance)
                    {
                        // NPCEmotionController 찾기
                        NPCEmotionController npcController = eyeInteractable.GetComponent<NPCEmotionController>();
                        if (npcController == null)
                        {
                            npcController = eyeInteractable.GetComponentInParent<NPCEmotionController>();
                        }
                        
                        if (npcController != null)
                        {
                            closestDistance = distance;
                            detectedInteractable = eyeInteractable;
                            closestHitPoint = hitResults[i].point;
                        }
                    }
                }
                
                if (detectedInteractable != null && useVFXLaser)
                {
                    HandleImmediateEyeContact(detectedInteractable, closestHitPoint);
                }
            }
            
            // 🔥 즉시 인터랙터블 변경 처리
            SetActiveInteractableImmediate(detectedInteractable);
        }
        
        // 🔥 즉시 인터랙터블 설정 (안정성 시간 없음)
        private void SetActiveInteractableImmediate(EyeInteractable newInteractable)
        {
            // 같은 인터랙터블이면 아무것도 하지 않음
            if (currentActiveInteractable == newInteractable)
            {
                return;
            }
            
            // 이전 인터랙터블 비활성화
            if (currentActiveInteractable != null)
            {
                currentActiveInteractable.IsHovered = false;
                eyeInteractables.Remove(currentActiveInteractable);
                
                if (debugMode)
                {
                    Debug.Log($"[EyeTrackingRay] 즉시 비활성화: {currentActiveInteractable.gameObject.name}");
                }
            }
            
            // 새 인터랙터블 활성화
            if (newInteractable != null)
            {
                newInteractable.IsHovered = true;
                eyeInteractables.Add(newInteractable);
                
                if (debugMode)
                {
                    float distance = Vector3.Distance(transform.position, newInteractable.transform.position);
                    Debug.Log($"[EyeTrackingRay] 즉시 활성화: {newInteractable.gameObject.name} (거리: {distance:F2}m)");
                }
            }
            
            currentActiveInteractable = newInteractable;
        }
        
        // VFX 처리 (기존 유지)
        private void HandleImmediateEyeContact(EyeInteractable targetInteractable, Vector3 hitPoint)
        {
            if (debugMode)
            {
                Debug.Log($"🎯🎯🎯 [HandleImmediateEyeContact 진입!] {targetInteractable?.gameObject.name ?? "null"}");
            }
            
            if (targetInteractable == null) 
            {
                Debug.LogError("❌ targetInteractable이 null!");
                return;
            }
            
            // NPCEmotionController 찾기
            NPCEmotionController npcController = targetInteractable.GetComponent<NPCEmotionController>();
            if (npcController == null)
            {
                npcController = targetInteractable.GetComponentInParent<NPCEmotionController>();
            }
            
            if (npcController == null) 
            {
                Debug.LogError($"❌ NPCEmotionController 없음: {targetInteractable.gameObject.name}");
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"✅ NPC 발견: {npcController.gameObject.name}");
            }
            
            // 로컬 쿨다운 체크
            if (lastVFXRequestTime.TryGetValue(npcController.gameObject, out float lastTime))
            {
                float timeSinceLastRequest = Time.time - lastTime;
                if (timeSinceLastRequest < vfxCooldownTime)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"⏰ VFX 쿨다운 중: {timeSinceLastRequest:F2}초 < {vfxCooldownTime}초");
                    }
                    return;
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"✅ 쿨다운 통과");
            }
            
            // 쿨다운 시간 기록
            lastVFXRequestTime[npcController.gameObject] = Time.time;
            
            // EmotionGazeManager 확인
            if (EmotionGazeManager.Instance == null)
            {
                Debug.LogError("❌ EmotionGazeManager.Instance가 null!");
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"✅ EmotionGazeManager 존재");
            }
            
            // PlayerEmotionController 확인
            PlayerEmotionController playerController = EmotionGazeManager.Instance.GetPlayerEmotionController();
            if (playerController == null)
            {
                Debug.LogError("❌ PlayerEmotionController가 null!");
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"✅ PlayerEmotionController 존재");
            }
            
            // 감정 비교
            EmotionState playerEmotion = playerController.GetCurrentEmotion();
            EmotionState npcEmotion = npcController.GetCurrentEmotion();
            bool emotionMatched = playerEmotion == npcEmotion;
            
            if (debugMode)
            {
                Debug.Log($"🔍 감정 비교: 플레이어({playerEmotion}) vs NPC({npcEmotion}) = {emotionMatched}");
            }
            
            if (emotionMatched)
            {
                if (debugMode)
                {
                    Debug.Log($"💖 감정 일치! VFX 요청 전송...");
                }
                
                EmotionEventData eventData = new EmotionEventData(
                    npcController.GetCurrentEmotion(),
                    1.0f,
                    npcController.gameObject
                );
                
                EmotionGazeManager.Instance.HandleEyeGazeEvent(eventData);
                
                if (debugMode)
                {
                    Debug.Log($"🚀 VFX 요청 완료!");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log($"😔 감정 불일치! 기본 raycast 표시...");
                }
                ShowBasicRaycast(hitPoint);
                if (debugMode)
                {
                    Debug.Log($"📍 기본 raycast 표시 완료!");
                }
            }
        }
        
        // 기본 raycast 표시
        private void ShowBasicRaycast(Vector3 targetPoint)
        {
            if (lineRenderer == null) return;
            
            // LineRenderer 활성화 및 설정
            lineRenderer.enabled = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            
            // 시작점과 끝점 설정
            Vector3 startPoint = transform.position;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, targetPoint);
            
            // 스타일 설정
            lineRenderer.startWidth = basicRaycastWidth;
            lineRenderer.endWidth = basicRaycastWidth * 0.5f;
            
            // 머티리얼 설정
            if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
            {
                lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
                lineRenderer.material.color = basicRaycastColor;
            }
            
            // 일정 시간 후 자동으로 숨기기
            StopCoroutine(nameof(HideBasicRaycastAfterDelay));
            StartCoroutine(HideBasicRaycastAfterDelay(basicRaycastDuration));
            
            if (debugMode)
            {
                Debug.Log($"[EyeTrackingRay] 기본 raycast 표시: {startPoint} → {targetPoint}");
            }
        }
        
        private IEnumerator HideBasicRaycastAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        
        private bool IsValidDetectionDistance(float distance)
        {
            bool valid = distance >= minDetectionDistance && distance <= maxDetectionDistance;
            
            if (debugMode)
            {
                if (valid)
                {
                    Debug.Log($"✅ [거리 유효] {distance:F2}m (범위: {minDetectionDistance:F2}~{maxDetectionDistance:F2})");
                }
                else
                {
                    Debug.LogWarning($"❌ [거리 무효] {distance:F2}m (범위: {minDetectionDistance:F2}~{maxDetectionDistance:F2})");
                }
            }
            
            return valid;
        }
        
        private void CleanUpInactiveInteractables()
        {
            eyeInteractables.RemoveWhere(interactable => 
                interactable == null || !interactable.gameObject.activeInHierarchy);
        }
        
        private void OnDisable()
        {
            if (currentActiveInteractable != null)
            {
                currentActiveInteractable.IsHovered = false;
            }
            
            eyeInteractables.Clear();
            StopAllCoroutines();
        }
        
        private void OnDestroy()
        {
            OnDisable();
        }
        
        private void OnDrawGizmos()
        {
            if (!showGazeGizmos) return;
            
            // 시선 레이 표시
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
            
            // 감지 범위 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * minDetectionDistance, 0.05f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * maxDetectionDistance, 0.1f);
            
            // 현재 활성화된 인터랙터블 표시
            if (currentActiveInteractable != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentActiveInteractable.transform.position);
                Gizmos.DrawWireSphere(currentActiveInteractable.transform.position, 0.2f);
            }
        }
        
        #region Public Methods
        public EyeInteractable GetCurrentActiveInteractable()
        {
            return currentActiveInteractable;
        }
        
        // 🔥 안정성 체크 제거 - 항상 true 반환
        public bool IsInteractableStable()
        {
            return currentActiveInteractable != null;
        }
        
        public void SetDetectionRange(float minDistance, float maxDistance)
        {
            minDetectionDistance = Mathf.Max(0.05f, minDistance);
            maxDetectionDistance = Mathf.Max(minDetectionDistance + 0.1f, maxDistance);
        }
        
        [ContextMenu("Test: Show Basic Raycast")]
        public void TestShowBasicRaycast()
        {
            Vector3 testTarget = transform.position + transform.forward * 3f;
            ShowBasicRaycast(testTarget);
            Debug.Log("기본 raycast 테스트 - 3m 앞으로 raycast 표시");
        }
        
        [ContextMenu("Debug: Check Current Settings")]
        public void DebugCheckCurrentSettings()
        {
            Debug.Log("=== EyeTrackingRay 설정 확인 (즉시 반응 버전) ===");
            Debug.Log($"VFX 쿨다운: {vfxCooldownTime}초");
            Debug.Log($"기본 raycast 지속시간: {basicRaycastDuration}초");
            Debug.Log($"거리 범위: {minDetectionDistance}m ~ {maxDetectionDistance}m");
            Debug.Log($"안정성 시간: 완전 제거됨! ✅");
            Debug.Log($"디버그 모드: {debugMode}");
            Debug.Log($"VFX 레이저 사용: {useVFXLaser}");
            Debug.Log($"현재 활성 인터랙터블: {(currentActiveInteractable?.gameObject.name ?? "none")}");
        }
        
        [ContextMenu("Debug: Force Clear All Interactables")]
        public void ForceClearAllInteractables()
        {
            if (currentActiveInteractable != null)
            {
                currentActiveInteractable.IsHovered = false;
                Debug.Log($"강제 비활성화: {currentActiveInteractable.gameObject.name}");
            }
            
            foreach (var interactable in eyeInteractables)
            {
                if (interactable != null)
                {
                    interactable.IsHovered = false;
                }
            }
            
            currentActiveInteractable = null;
            eyeInteractables.Clear();
            
            Debug.Log("모든 EyeInteractable 강제 초기화 완료!");
        }
        #endregion
    }
}