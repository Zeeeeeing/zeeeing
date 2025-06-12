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
        [SerializeField] private float gazeStabilityTime = 0.2f;
        
        // 🔥 Angle 관련 모든 변수 제거!
        // [SerializeField] private float gazeAngleTolerance = 25.0f; // 삭제
        // [SerializeField] private float closeRangeAngleTolerance = 35.0f; // 삭제
        // [SerializeField] private bool allowCloseRangeDetection = true; // 삭제
        // [SerializeField] private float closeRangeThreshold = 1.0f; // 삭제
        // [SerializeField] private float hysteresisAngle = 1.0f; // 삭제
        
        [Header("Stability Settings - Anti-Flicker")]
        [SerializeField] private float hoverStateChangeDelay = 0.1f;
        [SerializeField] private float consistentDetectionTime = 0.15f;
        [SerializeField] private int requiredConsistentFrames = 3;
        
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
        [SerializeField] private float vfxCooldownTime = 0.1f; // 👈 쿨다운 조정 가능!
        
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
        private Dictionary<GameObject, float> lastVFXRequestTime = new Dictionary<GameObject, float>(); // 👈 로컬 쿨다운
        
        // 안정화를 위한 변수들
        private EyeInteractable currentTargetInteractable;
        private EyeInteractable lastStableInteractable;
        private float lastInteractableChangeTime;
        private Vector3 lastGazeDirection;
        private bool isGazeStable = false;
        
        // 깜빡거림 방지를 위한 변수들
        private Dictionary<EyeInteractable, float> interactableDetectionTimes = new Dictionary<EyeInteractable, float>();
        private Dictionary<EyeInteractable, int> consistentDetectionCounts = new Dictionary<EyeInteractable, int>();
        private EyeInteractable pendingInteractable;
        private float pendingStartTime;
        
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
                Debug.Log("[EyeTrackingRay] LineRenderer 설정 완료 - Angle 체크 없음!");
            }
        }
        
        private void Update()
        {
            // 성능 최적화: 낮은 빈도로 레이캐스트 수행
            if (Time.time >= nextRaycastTime)
            {
                PerformStableRaycast();
                nextRaycastTime = Time.time + rayUpdateInterval;
            }
            
            // 안정화 처리
            ProcessStabilityLogic();
            
            // 정리 작업
            if (Time.frameCount % 60 == 0) // 2초마다
            {
                CleanUpInactiveInteractables();
            }
        }
        
        private void PerformStableRaycast()
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
                Debug.Log($"🎯 [레이캐스트 성공] {hit.transform.name}, 거리: {hit.distance:F2}m");
                
                // 1단계: 거리 체크
                if (IsValidDetectionDistance(hit.distance))
                {
                    Debug.Log($"✅ [거리 체크 통과] {hit.distance:F2}m");
                    
                    // 2단계: EyeInteractable 체크
                    var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                    if (eyeInteractable != null)
                    {
                        Debug.Log($"✅ [EyeInteractable 발견] {eyeInteractable.gameObject.name}");
                        
                        // 3단계: NPCEmotionController 체크
                        NPCEmotionController npcController = eyeInteractable.GetComponent<NPCEmotionController>();
                        if (npcController == null)
                        {
                            npcController = eyeInteractable.GetComponentInParent<NPCEmotionController>();
                            if (npcController != null)
                            {
                                Debug.Log($"✅ [상위에서 NPC 발견] {npcController.gameObject.name}");
                            }
                        }
                        else
                        {
                            Debug.Log($"✅ [직접 NPC 발견] {npcController.gameObject.name}");
                        }
                        
                        if (npcController != null)
                        {
                            detectedInteractable = eyeInteractable;
                            
                            // 4단계: useVFXLaser 체크
                            Debug.Log($"🔍 [VFX 설정 체크] useVFXLaser: {useVFXLaser}");
                            
                            if (useVFXLaser)
                            {
                                Debug.Log($"🚀 [HandleImmediateEyeContact 호출] {eyeInteractable.gameObject.name}");
                                HandleImmediateEyeContact(detectedInteractable, hit.point);
                            }
                            else
                            {
                                Debug.LogWarning($"❌ [VFX 비활성화] useVFXLaser가 false입니다!");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"❌ [NPC 없음] {eyeInteractable.gameObject.name}에 NPCEmotionController가 없습니다!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"❌ [EyeInteractable 없음] {hit.transform.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"❌ [거리 체크 실패] {hit.distance:F2}m (허용: {minDetectionDistance}~{maxDetectionDistance})");
                }
            }
            else
            {
                Debug.Log("❌ [레이캐스트 미스]");
            }
            
            // 감지된 인터랙터블 처리
            ProcessDetectedInteractable(detectedInteractable);
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
                
                // 🔥 Angle 체크 없이 바로 VFX 처리
                if (detectedInteractable != null && useVFXLaser)
                {
                    HandleImmediateEyeContact(detectedInteractable, closestHitPoint);
                }
            }
            
            ProcessDetectedInteractable(detectedInteractable);
        }
        
        // 🔥 완전 디버깅 + 로컬 쿨다운 적용 버전
        private void HandleImmediateEyeContact(EyeInteractable targetInteractable, Vector3 hitPoint)
        {
            Debug.Log($"🎯🎯🎯 [HandleImmediateEyeContact 진입!] {targetInteractable?.gameObject.name ?? "null"}");
            
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
            
            Debug.Log($"✅ NPC 발견: {npcController.gameObject.name}");
            
            // 로컬 쿨다운 체크
            if (lastVFXRequestTime.TryGetValue(npcController.gameObject, out float lastTime))
            {
                float timeSinceLastRequest = Time.time - lastTime;
                if (timeSinceLastRequest < vfxCooldownTime)
                {
                    Debug.LogWarning($"⏰ VFX 쿨다운 중: {timeSinceLastRequest:F2}초 < {vfxCooldownTime}초");
                    return;
                }
            }
            
            Debug.Log($"✅ 쿨다운 통과");
            
            // 쿨다운 시간 기록
            lastVFXRequestTime[npcController.gameObject] = Time.time;
            
            // EmotionGazeManager 확인
            if (EmotionGazeManager.Instance == null)
            {
                Debug.LogError("❌ EmotionGazeManager.Instance가 null!");
                return;
            }
            
            Debug.Log($"✅ EmotionGazeManager 존재");
            
            // PlayerEmotionController 확인
            PlayerEmotionController playerController = EmotionGazeManager.Instance.GetPlayerEmotionController();
            if (playerController == null)
            {
                Debug.LogError("❌ PlayerEmotionController가 null!");
                return;
            }
            
            Debug.Log($"✅ PlayerEmotionController 존재");
            
            // 감정 비교
            EmotionState playerEmotion = playerController.GetCurrentEmotion();
            EmotionState npcEmotion = npcController.GetCurrentEmotion();
            bool emotionMatched = playerEmotion == npcEmotion;
            
            Debug.Log($"🔍 감정 비교: 플레이어({playerEmotion}) vs NPC({npcEmotion}) = {emotionMatched}");
            
            if (emotionMatched)
            {
                Debug.Log($"💖 감정 일치! VFX 요청 전송...");
                
                EmotionEventData eventData = new EmotionEventData(
                    npcController.GetCurrentEmotion(),
                    1.0f,
                    npcController.gameObject
                );
                
                EmotionGazeManager.Instance.HandleEyeGazeEvent(eventData);
                
                Debug.Log($"🚀 VFX 요청 완료!");
            }
            else
            {
                Debug.Log($"😔 감정 불일치! 기본 raycast 표시...");
                ShowBasicRaycast(hitPoint);
                Debug.Log($"📍 기본 raycast 표시 완료!");
            }
        }
        
        // 🔥 기본 raycast 표시
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
        
        private void ProcessDetectedInteractable(EyeInteractable detected)
        {
            if (detected == currentTargetInteractable)
            {
                if (detected != null)
                {
                    if (!consistentDetectionCounts.ContainsKey(detected))
                        consistentDetectionCounts[detected] = 0;
                    
                    consistentDetectionCounts[detected]++;
                    
                    if (consistentDetectionCounts[detected] >= requiredConsistentFrames)
                    {
                        SetPendingInteractable(detected);
                    }
                }
            }
            else
            {
                currentTargetInteractable = detected;
                consistentDetectionCounts.Clear();
                
                if (detected != null)
                {
                    consistentDetectionCounts[detected] = 1;
                }
            }
        }
        
        private void SetPendingInteractable(EyeInteractable interactable)
        {
            if (pendingInteractable != interactable)
            {
                pendingInteractable = interactable;
                pendingStartTime = Time.time;
                
                if (debugMode)
                {
                    Debug.Log($"[EyeTrackingRay] 대기 인터랙터블 설정: {(interactable?.gameObject.name ?? "None")}");
                }
            }
        }
        
        private void ProcessStabilityLogic()
        {
            if (pendingInteractable != null && 
                Time.time - pendingStartTime >= consistentDetectionTime)
            {
                if (lastStableInteractable != pendingInteractable)
                {
                    if (Time.time - lastInteractableChangeTime >= hoverStateChangeDelay)
                    {
                        ChangeActiveInteractable(pendingInteractable);
                        lastInteractableChangeTime = Time.time;
                    }
                }
            }
            
            if (currentTargetInteractable == null && 
                Time.time - lastInteractableChangeTime >= hoverStateChangeDelay)
            {
                if (lastStableInteractable != null)
                {
                    ChangeActiveInteractable(null);
                    lastInteractableChangeTime = Time.time;
                }
            }
        }
        
        private void ChangeActiveInteractable(EyeInteractable newInteractable)
        {
            if (lastStableInteractable != null)
            {
                lastStableInteractable.IsHovered = false;
                eyeInteractables.Remove(lastStableInteractable);
                
                if (debugMode)
                {
                    Debug.Log($"[EyeTrackingRay] 인터랙터블 비활성화: {lastStableInteractable.gameObject.name}");
                }
            }
            
            if (newInteractable != null)
            {
                newInteractable.IsHovered = true;
                eyeInteractables.Add(newInteractable);
                
                if (debugMode)
                {
                    float distance = Vector3.Distance(transform.position, newInteractable.transform.position);
                    Debug.Log($"[EyeTrackingRay] 인터랙터블 활성화: {newInteractable.gameObject.name} (거리: {distance:F2}m)");
                }
            }
            
            lastStableInteractable = newInteractable;
        }
        
        private void CleanUpInactiveInteractables()
        {
            eyeInteractables.RemoveWhere(interactable => 
                interactable == null || !interactable.gameObject.activeInHierarchy);
            
            var keysToRemove = new List<EyeInteractable>();
            foreach (var kvp in interactableDetectionTimes)
            {
                if (kvp.Key == null || Time.time - kvp.Value > 5f)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                interactableDetectionTimes.Remove(key);
                consistentDetectionCounts.Remove(key);
            }
        }
        
        private void OnDisable()
        {
            if (lastStableInteractable != null)
            {
                lastStableInteractable.IsHovered = false;
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
            if (lastStableInteractable != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastStableInteractable.transform.position);
                Gizmos.DrawWireSphere(lastStableInteractable.transform.position, 0.2f);
            }
        }
        
        #region Public Methods
        public EyeInteractable GetCurrentActiveInteractable()
        {
            return lastStableInteractable;
        }
        
        public bool IsInteractableStable()
        {
            return lastStableInteractable != null && 
                   Time.time - lastInteractableChangeTime >= consistentDetectionTime;
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
            Debug.Log("=== EyeTrackingRay 설정 확인 ===");
            Debug.Log($"VFX 쿨다운: {vfxCooldownTime}초");
            Debug.Log($"기본 raycast 지속시간: {basicRaycastDuration}초");
            Debug.Log($"거리 범위: {minDetectionDistance}m ~ {maxDetectionDistance}m");
            Debug.Log($"Angle 체크: 완전 제거됨! ✅");
            Debug.Log($"디버그 모드: {debugMode}");
            Debug.Log($"VFX 레이저 사용: {useVFXLaser}");
        }
        #endregion
    }
}