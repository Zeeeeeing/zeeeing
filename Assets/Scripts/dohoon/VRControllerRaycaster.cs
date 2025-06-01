using UnityEngine;

public class VRControllerRaycaster : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float rayLength = 10f; // 레이 길이 증가
    [SerializeField] private LayerMask raycastLayerMask; // 레이캐스트할 레이어
    [SerializeField] private Transform rayOrigin; // 레이 시작점
    [SerializeField] private bool showRay = true; // 디버그용 레이 표시
    [SerializeField] private float hitDetectionRadius = 0.5f; // 히트 감지 반경 추가

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode triggerButton = KeyCode.JoystickButton0; // VR 컨트롤러 트리거 버튼
    [SerializeField] private KeyCode altTriggerButton = KeyCode.A; // 대체 키보드 버튼
    
    private LineRenderer lineRenderer; // 시각적 레이 표시용
    private VRButtonInteractable currentHitObject; // 현재 레이가 닿은 상호작용 가능한 오브젝트
    
    private void Start()
    {
        // 레이 시작점이 설정되지 않았으면 현재 오브젝트 사용
        if (rayOrigin == null)
            rayOrigin = transform;
            
        // 레이 시각화를 위한 LineRenderer 추가
        if (showRay)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.001f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.blue;
            lineRenderer.endColor = Color.cyan;
            lineRenderer.positionCount = 2;
        }

        // 레이어마스크가 없으면 모든 레이어로 설정
        if (raycastLayerMask.value == 0)
            raycastLayerMask = ~0; // 모든 레이어 활성화
    }
    
    private void Update()
    {
        // 개선된 레이캐스트 - SphereCast 사용 (더 넓은 영역 감지)
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(
            rayOrigin.position, // 시작 위치
            hitDetectionRadius, // 구의 반지름 (감지 영역 확대)
            rayOrigin.forward,  // 방향
            out hit,            // 히트 정보
            rayLength,          // 레이 길이
            raycastLayerMask    // 레이어 마스크
        );
        
        // 표준 레이캐스트도 시각화를 위해 수행
        RaycastHit standardHit;
        bool standardHitSomething = Physics.Raycast(
            rayOrigin.position, 
            rayOrigin.forward, 
            out standardHit, 
            rayLength, 
            raycastLayerMask
        );
        
        // 레이 시각화
        if (showRay && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, rayOrigin.position);
            
            if (standardHitSomething)
                lineRenderer.SetPosition(1, standardHit.point);
            else
                lineRenderer.SetPosition(1, rayOrigin.position + rayOrigin.forward * rayLength);
                
            // 레이가 상호작용 가능한 오브젝트에 닿으면 색상 변경
            if (hitSomething && hit.collider.GetComponent<VRButtonInteractable>() != null)
            {
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
                lineRenderer.startWidth = 0.02f; // 레이 두께 증가
            }
            else
            {
                lineRenderer.startColor = Color.blue;
                lineRenderer.endColor = Color.cyan;
                lineRenderer.startWidth = 0.01f;
            }
        }
        
        // 히트 결과 로깅 (디버깅용)
        if (hitSomething)
        {
            Debug.Log("레이가 " + hit.collider.gameObject.name + "에 닿았습니다. 거리: " + hit.distance);
            
            // 버튼 객체인지 로깅
            VRButtonInteractable interactable = hit.collider.GetComponent<VRButtonInteractable>();
            if (interactable != null)
            {
                Debug.Log("상호작용 가능한 버튼을 감지했습니다: " + interactable.gameObject.name);
            }
        }
        
        // 이전 상호작용 오브젝트 참조 제거
        if (currentHitObject != null && (!hitSomething || hit.collider.GetComponent<VRButtonInteractable>() != currentHitObject))
        {
            currentHitObject.OnRayExit();
            currentHitObject = null;
        }
        
        // 새로운 상호작용 오브젝트 처리
        if (hitSomething)
        {
            VRButtonInteractable interactable = hit.collider.GetComponent<VRButtonInteractable>();
            if (interactable != null)
            {
                // 레이가 새로운 오브젝트에 닿았을 때
                if (currentHitObject != interactable)
                {
                    currentHitObject = interactable;
                    currentHitObject.OnRayEnter();
                }
                
                // 트리거 버튼 눌림 감지 (여러 입력 방식 지원)
                if (Input.GetKeyDown(triggerButton) || 
                    Input.GetKeyDown(altTriggerButton) || 
                    Input.GetKeyDown(KeyCode.Space) || 
                    Input.GetMouseButtonDown(0))
                {
                    Debug.Log("트리거 버튼이 눌렸습니다!");
                    currentHitObject.OnRayTrigger();
                }
            }
        }
        
        // 모든 입력 로깅 (뭐가 눌리는지 확인용)
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown((KeyCode)i))
            {
                Debug.Log("키 감지됨: " + ((KeyCode)i).ToString());
            }
        }
        
        // 조이스틱 버튼 로깅
        for (int i = 350; i < 370; i++)
        {
            if (Input.GetKeyDown((KeyCode)i))
            {
                Debug.Log("조이스틱 버튼 감지됨: " + ((KeyCode)i).ToString());
            }
        }
    }
    
    // SphereCast 시각화 (디버그용)
    private void OnDrawGizmos()
    {
        if (rayOrigin != null && hitDetectionRadius > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(rayOrigin.position, hitDetectionRadius);
            Gizmos.DrawWireSphere(rayOrigin.position + rayOrigin.forward * rayLength, hitDetectionRadius);
            Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * rayLength);
        }
    }
}