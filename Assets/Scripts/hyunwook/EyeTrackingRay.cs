using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ZeeeingGaze; // EmotionGazeManager 접근을 위한 네임스페이스 추가

namespace ZeeeingGaze
{
    [RequireComponent(typeof(LineRenderer))]
    public class EyeTrackingRay : MonoBehaviour
    {
        [Header("Ray Settings")]
        [SerializeField] private float rayDistance = 10.0f;
        [SerializeField] private LayerMask layersToInclude;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float rayUpdateInterval = 0.02f; // 레이캐스트 업데이트 간격 (성능 최적화)
        
        [Header("Rendering")]
        [SerializeField] private bool useVFXLaser = true; // VFX 레이저 사용 여부 설정
        [SerializeField] private bool showDefaultRay = false; // 기본 레이 표시 여부 (디버깅용)
        [SerializeField] private float laserDuration = 0.3f; // 레이저 표시 지속 시간
        [SerializeField] private Color defaultLaserColor = Color.white;
        
        [Header("Options")]
        [SerializeField] private bool useRayCastAll = false; // 모든 대상에 레이캐스트 수행 여부
        [SerializeField] private bool autoCleanupInteractables = true; // 자동으로 비활성화된 인터랙터블 정리
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        private LineRenderer lineRenderer;
        private HashSet<EyeInteractable> eyeInteractables = new HashSet<EyeInteractable>(); // List 대신 HashSet 사용
        private GameObject currentLaserVFX; // 현재 활성화된 레이저 VFX 오브젝트
        private float nextRaycastTime = 0f; // 다음 레이캐스트 시간
        private RaycastHit[] hitResults = new RaycastHit[5]; // 레이캐스트 결과 저장 배열 (재사용)
        
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
            // 기본 레이 설정은 유지하되, 표시 여부는 설정에 따라 결정
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.005f; // 좀 더 얇게 설정
            lineRenderer.endWidth = 0.001f; // 끝으로 갈수록 얇아짐
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, rayDistance));
            
            // 기본 레이 표시 여부 설정
            lineRenderer.enabled = showDefaultRay;
            
            // 초기 레이캐스트 타이밍 설정
            nextRaycastTime = Time.time;
        }
        
        private void Update()
        {
            // 성능 최적화: 일정 간격으로만 레이캐스트 수행
            if (Time.time >= nextRaycastTime)
            {
                PerformRaycast();
                nextRaycastTime = Time.time + rayUpdateInterval;
            }
            
            // 정리 작업
            if (autoCleanupInteractables && Time.frameCount % 30 == 0) // 30프레임마다 정리
            {
                CleanUpInactiveInteractables();
            }
        }
        
        private void PerformRaycast()
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
            // 현재 호버 상태 해제
            UnSelectAll();
            
            // 레이캐스트 계산
            OVREyeGaze eyeGaze = GetComponent<OVREyeGaze>();
            Ray ray = new Ray(transform.position, eyeGaze.transform.forward);
            RaycastHit hit;
            
            // 레이캐스트 수행
            if (Physics.Raycast(ray, out hit, rayDistance, layersToInclude, triggerInteraction))
            {
                HandleRaycastHit(hit);
            }
            else
            {
                // 아무것도 닿지 않은 경우 레이저 제거
                DestroyLaserVFX();
            }
        }
        
        private void PerformMultiRaycast()
        {
            // 현재 호버 상태 해제
            UnSelectAll();
            
            // 레이캐스트 계산
            Ray ray = new Ray(transform.position, transform.forward);
            int hitCount = Physics.RaycastNonAlloc(ray, hitResults, rayDistance, layersToInclude, triggerInteraction);
            
            if (hitCount > 0)
            {
                // 가장 가까운 유효한 인터랙터블 찾기
                float closestDistance = float.MaxValue;
                EyeInteractable closestInteractable = null;
                
                for (int i = 0; i < hitCount; i++)
                {
                    var eyeInteractable = hitResults[i].transform.GetComponent<EyeInteractable>();
                    if (eyeInteractable != null && hitResults[i].distance < closestDistance)
                    {
                        closestDistance = hitResults[i].distance;
                        closestInteractable = eyeInteractable;
                    }
                }
                
                // 가장 가까운 인터랙터블 처리
                if (closestInteractable != null)
                {
                    HandleInteractableHit(closestInteractable);
                }
                else
                {
                    // 유효한 인터랙터블이 없는 경우 레이저 제거
                    DestroyLaserVFX();
                }
            }
            else
            {
                // 아무것도 닿지 않은 경우 레이저 제거
                DestroyLaserVFX();
            }
        }
        
        private void HandleRaycastHit(RaycastHit hit)
        {
            var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
            if (eyeInteractable != null)
            {
                HandleInteractableHit(eyeInteractable);
            }
            else
            {
                // EyeInteractable이 아닌 객체에 닿은 경우 레이저 제거
                DestroyLaserVFX();
            }
        }
        
        private void HandleInteractableHit(EyeInteractable eyeInteractable)
        {
            // 상호작용 가능한 객체 등록 및 호버 상태 설정
            eyeInteractables.Add(eyeInteractable);
            eyeInteractable.IsHovered = true;
            
            // VFX 레이저 생성 로직
            if (useVFXLaser)
            {
                Vector3 targetPos = eyeInteractable.GetTargetPosition();
                CreateOrUpdateLaserVFX(targetPos);
            }
            
            if (debugMode)
            {
                Debug.Log($"시선 감지: {eyeInteractable.gameObject.name}");
            }
        }
        
        // VFX 레이저 생성 또는 업데이트
        private void CreateOrUpdateLaserVFX(Vector3 targetPosition)
        {
            // EmotionGazeManager가 존재하는지 확인
            if (EmotionGazeManager.Instance == null)
            {
                if (debugMode) Debug.LogWarning("EmotionGazeManager가 존재하지 않습니다.");
                return;
            }
            
            // EmotionGazeManager에 VFX 에셋이 설정되었는지 확인
            if (!EmotionGazeManager.Instance.HasDefaultVFXAsset())
            {
                if (debugMode) Debug.LogWarning("EmotionGazeManager에 VFX 에셋이 설정되지 않았습니다.");
                return;
            }
            
            // 레이저 VFX가 이미 생성되어 있다면 위치만 업데이트
            if (currentLaserVFX != null)
            {
                try
                {
                    // 방향 계산 및 회전 업데이트
                    Vector3 direction = targetPosition - transform.position;
                    currentLaserVFX.transform.rotation = Quaternion.LookRotation(direction);
                    
                    // VFX 길이 업데이트
                    UnityEngine.VFX.VisualEffect vfxComponent = currentLaserVFX.GetComponent<UnityEngine.VFX.VisualEffect>();
                    if (vfxComponent != null && vfxComponent.HasFloat("LaserLength"))
                    {
                        vfxComponent.SetFloat("LaserLength", direction.magnitude);
                    }
                }
                catch (System.Exception e)
                {
                    if (debugMode) Debug.LogError($"레이저 VFX 업데이트 중 오류 발생: {e.Message}");
                    DestroyLaserVFX();
                }
            }
            else
            {
                // 새 레이저 VFX 생성
                try
                {
                    currentLaserVFX = EmotionGazeManager.Instance.CreateGazeLaser(transform.position, targetPosition, defaultLaserColor);
                    
                    // 일정 시간 후 자동 파괴 (오래된 VFX가 남지 않도록)
                    if (currentLaserVFX != null)
                    {
                        Destroy(currentLaserVFX, laserDuration);
                    }
                }
                catch (System.Exception e)
                {
                    if (debugMode) Debug.LogError($"레이저 VFX 생성 중 오류 발생: {e.Message}");
                }
            }
        }
        
        // 레이저 VFX 제거
        private void DestroyLaserVFX()
        {
            if (currentLaserVFX != null)
            {
                Destroy(currentLaserVFX);
                currentLaserVFX = null;
            }
        }
        
        // 특정 인터랙터블의 선택 상태 해제
        private void UnSelect(EyeInteractable interactable)
        {
            if (interactable != null)
            {
                interactable.IsHovered = false;
            }
        }
        
        // 모든 인터랙터블의 선택 상태 해제
        private void UnSelectAll()
        {
            foreach (var interactable in eyeInteractables)
            {
                if (interactable != null)
                {
                    interactable.IsHovered = false;
                }
            }
        }
        
        // 비활성화된 인터랙터블 제거
        private void CleanUpInactiveInteractables()
        {
            eyeInteractables.RemoveWhere(interactable => interactable == null || !interactable.gameObject.activeInHierarchy);
        }
        
        private void OnDisable()
        {
            // 비활성화될 때 모든 인터랙터블 선택 해제
            UnSelectAll();
            DestroyLaserVFX();
            StopAllCoroutines();
        }
        
        private void OnDestroy()
        {
            // 클린업
            UnSelectAll();
            eyeInteractables.Clear();
            DestroyLaserVFX();
        }
        
        // 시선 레이 설정 변경 메소드 (런타임 설정 가능)
        public void SetRayDistance(float distance)
        {
            rayDistance = Mathf.Max(0.1f, distance);
            lineRenderer.SetPosition(1, new Vector3(0, 0, rayDistance));
        }
        
        public void SetLayerMask(LayerMask layers)
        {
            layersToInclude = layers;
        }
        
        public void SetUseVFXLaser(bool useVFX)
        {
            useVFXLaser = useVFX;
        }
        
        public void SetShowDefaultRay(bool show)
        {
            showDefaultRay = show;
            lineRenderer.enabled = show;
        }
        
        // 현재 호버 중인 인터랙터블 가져오기
        public IReadOnlyCollection<EyeInteractable> GetHoveredInteractables()
        {
            return eyeInteractables;
        }
        
        // 디버그 모드 설정
        public void SetDebugMode(bool enable)
        {
            debugMode = enable;
        }
    }
}