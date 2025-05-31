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
        [SerializeField] private float gazeAngleTolerance = 5.0f;
        [SerializeField] private float minDetectionDistance = 0.1f;
        [SerializeField] private float maxDetectionDistance = 6.0f;
        [SerializeField] private float gazeStabilityTime = 0.2f;
        
        [Header("Close Range Settings")]
        [SerializeField] private bool allowCloseRangeDetection = true;
        [SerializeField] private float closeRangeThreshold = 1.0f;
        [SerializeField] private float closeRangeAngleTolerance = 12.0f;
        
        [Header("Stability Settings - Anti-Flicker")]
        [SerializeField] private float hoverStateChangeDelay = 0.1f;
        [SerializeField] private float consistentDetectionTime = 0.15f;
        [SerializeField] private int requiredConsistentFrames = 3;
        [SerializeField] private float hysteresisAngle = 1.0f;
        
        [Header("Rendering - Contact Only")]
        [SerializeField] private bool useVFXLaser = true;
        [SerializeField] private bool showDefaultRay = false;
        [SerializeField] private float laserDuration = 0.1f; // 매우 짧은 레이저 지속시간
        [SerializeField] private Color defaultLaserColor = Color.white;
        
        [Header("Performance")]
        [SerializeField] private bool useRayCastAll = false;
        [SerializeField] private bool autoCleanupInteractables = true;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showGazeGizmos = true;
        
        private LineRenderer lineRenderer;
        private HashSet<EyeInteractable> eyeInteractables = new HashSet<EyeInteractable>();
        private float nextRaycastTime = 0f;
        private RaycastHit[] hitResults = new RaycastHit[10];
        
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
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.003f;
            lineRenderer.endWidth = 0.001f;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, rayDistance));
            
            lineRenderer.enabled = showDefaultRay;
            nextRaycastTime = Time.time;
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
                if (IsValidDetectionDistance(hit.distance))
                {
                    var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
                    if (eyeInteractable != null && IsWithinStableGazeAngle(hit.point, hit.distance))
                    {
                        detectedInteractable = eyeInteractable;
                        
                        // 핵심: 실제로 EyeInteractable과 접촉하고 있을 때만 레이저 생성
                        if (useVFXLaser)
                        {
                            CreateInstantLaser(hit.point);
                        }
                    }
                }
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
            Vector3 hitPoint = Vector3.zero;
            
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
                        if (IsWithinStableGazeAngle(hitResults[i].point, distance))
                        {
                            closestDistance = distance;
                            detectedInteractable = eyeInteractable;
                            hitPoint = hitResults[i].point;
                        }
                    }
                }
                
                // 핵심: 실제로 EyeInteractable과 접촉하고 있을 때만 레이저 생성
                if (detectedInteractable != null && useVFXLaser)
                {
                    CreateInstantLaser(hitPoint);
                }
            }
            
            ProcessDetectedInteractable(detectedInteractable);
        }
        
        // 새로운 메서드: 즉시 레이저 생성 (접촉 시에만)
        private void CreateInstantLaser(Vector3 targetPosition)
        {
            if (EmotionGazeManager.Instance == null || !EmotionGazeManager.Instance.HasDefaultVFXAsset())
                return;
                
            Camera playerCamera = Camera.main;
            if (playerCamera == null) return;
            
            Vector3 startPos = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
            
            try
            {
                // 매우 짧은 지속시간의 레이저 생성
                GameObject laserVFX = EmotionGazeManager.Instance.CreateGazeLaser(startPos, targetPosition, defaultLaserColor);
                
                if (laserVFX != null)
                {
                    // 매우 짧은 시간 후 자동 제거
                    Destroy(laserVFX, laserDuration);
                    
                    if (debugMode)
                    {
                        Debug.Log($"즉시 레이저 생성됨 - 지속시간: {laserDuration}초");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (debugMode) Debug.LogError($"즉시 레이저 생성 중 오류: {e.Message}");
            }
        }
        
        private void ProcessDetectedInteractable(EyeInteractable detected)
        {
            // 현재 감지된 것이 이전과 같으면 안정성 증가
            if (detected == currentTargetInteractable)
            {
                if (detected != null)
                {
                    // 일관된 감지 카운트 증가
                    if (!consistentDetectionCounts.ContainsKey(detected))
                        consistentDetectionCounts[detected] = 0;
                    
                    consistentDetectionCounts[detected]++;
                    
                    // 충분히 일관되게 감지되면 안정된 것으로 간주
                    if (consistentDetectionCounts[detected] >= requiredConsistentFrames)
                    {
                        SetPendingInteractable(detected);
                    }
                }
            }
            else
            {
                // 다른 것이 감지되면 카운트 리셋
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
                    Debug.Log($"대기 인터랙터블 설정: {(interactable?.gameObject.name ?? "None")}");
                }
            }
        }
        
        private void ProcessStabilityLogic()
        {
            // 대기 중인 인터랙터블이 있고 충분한 시간이 지났으면 활성화
            if (pendingInteractable != null && 
                Time.time - pendingStartTime >= consistentDetectionTime)
            {
                if (lastStableInteractable != pendingInteractable)
                {
                    // 상태 변경 지연 체크
                    if (Time.time - lastInteractableChangeTime >= hoverStateChangeDelay)
                    {
                        ChangeActiveInteractable(pendingInteractable);
                        lastInteractableChangeTime = Time.time;
                    }
                }
            }
            
            // 아무것도 감지되지 않으면 현재 활성화된 것 해제
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
            // 이전 인터랙터블 비활성화
            if (lastStableInteractable != null)
            {
                lastStableInteractable.IsHovered = false;
                eyeInteractables.Remove(lastStableInteractable);
                
                if (debugMode)
                {
                    Debug.Log($"인터랙터블 비활성화: {lastStableInteractable.gameObject.name}");
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
                    Debug.Log($"인터랙터블 활성화: {newInteractable.gameObject.name} (거리: {distance:F2}m)");
                }
            }
            
            lastStableInteractable = newInteractable;
        }
        
        private bool IsValidDetectionDistance(float distance)
        {
            return distance >= minDetectionDistance && distance <= maxDetectionDistance;
        }
        
        private bool IsWithinStableGazeAngle(Vector3 hitPoint, float distance)
        {
            Vector3 directionToHit = (hitPoint - transform.position).normalized;
            Vector3 gazeDirection = transform.forward;
            float angle = Vector3.Angle(gazeDirection, directionToHit);
            
            // 기본 허용 각도 계산
            float baseAllowedAngle = distance <= closeRangeThreshold ? 
                closeRangeAngleTolerance : gazeAngleTolerance;
            
            // 히스테리시스 적용 (현재 활성화된 인터랙터블에 더 관대)
            float allowedAngle = baseAllowedAngle;
            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, 0.1f, layersToInclude);
            EyeInteractable hitInteractable = null;
            
            if (hitColliders.Length > 0)
            {
                hitInteractable = hitColliders[0].GetComponent<EyeInteractable>();
            }
            
            // 현재 활성화된 인터랙터블이면 히스테리시스 적용
            if (hitInteractable == lastStableInteractable)
            {
                allowedAngle += hysteresisAngle;
            }
            
            bool withinAngle = angle <= allowedAngle;
            
            if (debugMode && !withinAngle)
            {
                Debug.Log($"각도 초과: {angle:F2}도 > {allowedAngle:F2}도 (거리: {distance:F2}m)");
            }
            
            return withinAngle;
        }
        
        private void CleanUpInactiveInteractables()
        {
            // 비활성화된 인터랙터블 제거
            eyeInteractables.RemoveWhere(interactable => 
                interactable == null || !interactable.gameObject.activeInHierarchy);
            
            // 오래된 감지 시간 정보 정리
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
            // 모든 상태 초기화
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
        
        // 디버그용 기즈모
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
            
            // 근거리 범위 표시
            if (allowCloseRangeDetection)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + transform.forward * closeRangeThreshold, 0.05f);
            }
            
            // 현재 활성화된 인터랙터블 표시
            if (lastStableInteractable != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastStableInteractable.transform.position);
                Gizmos.DrawWireSphere(lastStableInteractable.transform.position, 0.2f);
            }
        }
        
        #region Public Methods
        public void SetStabilitySettings(float changeDelay, float detectionTime, int requiredFrames)
        {
            hoverStateChangeDelay = Mathf.Max(0.05f, changeDelay);
            consistentDetectionTime = Mathf.Max(0.1f, detectionTime);
            requiredConsistentFrames = Mathf.Max(1, requiredFrames);
        }
        
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
        
        public void SetGazeAngleTolerance(float angle)
        {
            gazeAngleTolerance = Mathf.Clamp(angle, 0.1f, 45f);
        }
        
        public void SetCloseRangeSettings(bool allowCloseRange, float threshold, float angleTolerance)
        {
            allowCloseRangeDetection = allowCloseRange;
            closeRangeThreshold = Mathf.Max(0.1f, threshold);
            closeRangeAngleTolerance = Mathf.Clamp(angleTolerance, 0.1f, 45f);
        }
        
        public void SetLaserDuration(float duration)
        {
            laserDuration = Mathf.Max(0.05f, duration);
            Debug.Log($"레이저 지속시간 설정: {laserDuration}초");
        }
        #endregion
    }
}