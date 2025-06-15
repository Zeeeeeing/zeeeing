using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AutonomousDriver : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;           // 이동 속도 (unit/sec)
    public float rotationSpeed = 180f;     // 회전 속도 (deg/sec)
    public float arriveThreshold = 0.5f;   // 도착 판정 반경

    [Header("NavMesh Sampling")]
    public float edgeMargin = 1f;          // NavMesh 에지로부터 최소 거리
    public int maxSampleAttempts = 30;     // 샘플 재시도 횟수

    [Header("Interaction Settings")]
    public float lookAtRotationSpeed = 90f; // 플레이어를 바라볼 때의 회전 속도
    public float interactionDistance = 8f;   // 상호작용 중 플레이어와의 최대 거리
    
    // 🔧 거리 관련 설정 추가
    [Header("Distance Settings")]
    public float minLookAtDistance = 0.5f;   // 최소 바라보기 거리 (기존 1.5f에서 줄임)
    public float pushBackSpeed = 1.0f;       // 뒤로 밀리는 속도
    public bool enablePushBack = false;      // 뒤로 밀리기 활성화 (기본값: 비활성화)

    private NavMeshAgent agent;
    private NavMeshTriangulation navTri;
    private Vector3 destination;
    
    // 🆕 상호작용 모드 관련 변수
    private bool isInInteractionMode = false;
    private Transform playerTarget = null;
    private float originalMoveSpeed;
    private float originalRotationSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 경로·회전 자동 업데이트 끄기
        agent.updatePosition = false;
        agent.updateRotation = false;

        // 속도·회전·회피 품질 설정
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // 🆕 원본 속도 저장
        originalMoveSpeed = moveSpeed;
        originalRotationSpeed = rotationSpeed;

        // NavMesh 정보 캐시
        navTri = NavMesh.CalculateTriangulation();

        // 첫 목적지 설정
        PickNewDestination();
        
        Debug.Log($"[{gameObject.name}] AutonomousDriver 초기화 - 원본 속도: {originalMoveSpeed}, 회전: {originalRotationSpeed}");
    }

    void Update()
    {
        if (isInInteractionMode)
        {
            HandleInteractionMode();
        }
        else
        {
            HandleNormalMovement();
        }
    }

    // 🆕 상호작용 모드 설정/해제
    public void SetInteractionMode(bool enable, Transform playerTransform)
    {
        isInInteractionMode = enable;
        playerTarget = playerTransform;

        if (enable)
        {
            Debug.Log($"[{gameObject.name}] 상호작용 모드 활성화 - 플레이어를 바라보며 정지");
            Debug.Log($"  - 플레이어 위치: {playerTransform?.position}");
            Debug.Log($"  - 현재 NPC 위치: {transform.position}");
            Debug.Log($"  - 거리: {(playerTransform ? Vector3.Distance(transform.position, playerTransform.position) : 0):F2}m");
            
            // 속도를 0으로 설정
            moveSpeed = 0f;
            
            // 현재 이동 중단
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 상호작용 모드 해제 - 정상 이동 재개");
            
            // 🔧 원본 속도 정확히 복원
            moveSpeed = originalMoveSpeed;
            rotationSpeed = originalRotationSpeed;
            
            Debug.Log($"  - 복원된 속도: {moveSpeed} (원본: {originalMoveSpeed})");
            
            // 이동 재개
            agent.isStopped = false;
            agent.speed = moveSpeed; // Agent 속도도 동기화
            
            // 새로운 목적지 설정
            PickNewDestination();
        }
    }

    // 🔧 상호작용 모드 처리 (개선된 버전)
    private void HandleInteractionMode()
    {
        if (playerTarget == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] 상호작용 모드이지만 playerTarget이 null!");
            return;
        }

        // 플레이어 방향 계산 (Y축 무시)
        Vector3 directionToPlayer = (playerTarget.position - transform.position);
        directionToPlayer.y = 0f; // Y축 회전 무시
        
        float distanceToPlayer = directionToPlayer.magnitude;
        
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            // 플레이어를 바라보는 회전 계산
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
            
            // 🔧 더 부드럽고 빠른 회전
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lookAtRotationSpeed * Time.deltaTime
            );
            
            // 🔧 상세한 바라보기 디버그 정보
            float currentAngle = Quaternion.Angle(transform.rotation, targetRotation);
            if (currentAngle > 5f) // 5도 이상 차이날 때만 로그
            {
                Debug.Log($"[{gameObject.name}] 바라보기 중... 남은 각도: {currentAngle:F1}°, 거리: {distanceToPlayer:F2}m");
            }
        }

        // 🔧 거리 조건 개선: enablePushBack이 활성화되어야만 뒤로 밀기
        if (enablePushBack && distanceToPlayer < minLookAtDistance)
        {
            Vector3 awayFromPlayer = (transform.position - playerTarget.position).normalized;
            Vector3 newPosition = transform.position + awayFromPlayer * Time.deltaTime * pushBackSpeed;
            
            // NavMesh 유효성 검사
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"[{gameObject.name}] 너무 가까워서 뒤로 이동: {distanceToPlayer:F2}m → {minLookAtDistance:F2}m");
            }
        }
        else if (distanceToPlayer < minLookAtDistance)
        {
            // 🔧 너무 가까워도 뒤로 밀지 않고 그냥 바라보기만 함
            Debug.Log($"[{gameObject.name}] 가까운 거리에서 바라보기: {distanceToPlayer:F2}m (최소: {minLookAtDistance:F2}m)");
        }

        // Agent 위치 동기화
        agent.nextPosition = transform.position;
    }

    // 🆕 정상 이동 모드 처리
    private void HandleNormalMovement()
    {
        // 경로 계산 중이면 대기
        if (agent.pathPending) return;

        // Agent가 계산한 원하는 속도 벡터 (회피 + 경로 따라가기)
        Vector3 desired = agent.desiredVelocity;
        if (desired.sqrMagnitude < 0.01f) return;

        // 1) 회전: 항상 desired 방향 바라보기
        Quaternion targetRot = Quaternion.LookRotation(desired.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        // 2) 이동: 직선으로
        transform.position += desired.normalized * moveSpeed * Time.deltaTime;

        // 3) Agent 위치 동기화
        agent.nextPosition = transform.position;

        // 4) 도착 체크
        if (!agent.pathPending && agent.remainingDistance <= arriveThreshold)
            PickNewDestination();
    }

    void PickNewDestination()
    {
        // 상호작용 모드에서는 새 목적지를 설정하지 않음
        if (isInInteractionMode) return;

        Vector3 samplePoint = transform.position;
        bool found = false;

        // NavMesh 에지로부터 edgeMargin만큼 내부 지점 샘플링
        for (int i = 0; i < maxSampleAttempts; i++)
        {
            int triIndex = Random.Range(0, navTri.indices.Length / 3);
            Vector3 v0 = navTri.vertices[navTri.indices[triIndex * 3 + 0]];
            Vector3 v1 = navTri.vertices[navTri.indices[triIndex * 3 + 1]];
            Vector3 v2 = navTri.vertices[navTri.indices[triIndex * 3 + 2]];

            // 삼각형 내부 barycentric 샘플링
            float r1 = Random.value, r2 = Random.value;
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
            Vector3 pt = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);

            // 에지로부터 거리 검사
            if (NavMesh.FindClosestEdge(pt, out NavMeshHit hit, NavMesh.AllAreas)
                && hit.distance >= edgeMargin)
            {
                samplePoint = pt;
                found = true;
                break;
            }
        }

        // 유효 지점 미발견 시 중앙 fallback
        if (!found)
        {
            int midTri = (navTri.indices.Length / 3) / 2;
            samplePoint = navTri.vertices[navTri.indices[midTri * 3]];
        }

        destination = samplePoint;
        agent.SetDestination(destination);
    }

    // 🆕 현재 상태 확인 메서드들
    public bool IsInInteractionMode() => isInInteractionMode;
    public Transform GetPlayerTarget() => playerTarget;
    public float GetCurrentMoveSpeed() => moveSpeed;
    public float GetOriginalMoveSpeed() => originalMoveSpeed; // 🔧 원본 속도 반환 메서드 추가

    // 🔧 속도 직접 설정 (기존 호환성)
    public void SetMoveSpeed(float speed)
    {
        if (!isInInteractionMode)
        {
            moveSpeed = speed;
            agent.speed = speed;
            
            // 🔧 원본 속도도 업데이트 (런타임에서 변경된 경우)
            originalMoveSpeed = speed;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 상호작용 모드 중에는 속도를 변경할 수 없습니다.");
        }
    }

    // 🔧 거리 설정 메서드 추가
    public void SetLookAtDistanceSettings(float minDistance, float pushSpeed, bool enablePush)
    {
        minLookAtDistance = minDistance;
        pushBackSpeed = pushSpeed;
        enablePushBack = enablePush;
        
        Debug.Log($"[{gameObject.name}] 바라보기 거리 설정 - 최소: {minDistance}m, 밀기: {enablePush}, 속도: {pushSpeed}");
    }

    // 🔧 디버그용 상태 확인 메서드
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== {gameObject.name} AutonomousDriver 상태 ===");
        Debug.Log($"상호작용 모드: {isInInteractionMode}");
        Debug.Log($"현재 속도: {moveSpeed} (원본: {originalMoveSpeed})");
        Debug.Log($"현재 회전속도: {rotationSpeed} (원본: {originalRotationSpeed})");
        Debug.Log($"플레이어 타겟: {(playerTarget?.name ?? "null")}");
        Debug.Log($"뒤로 밀기: {enablePushBack} (최소거리: {minLookAtDistance}m)");
        
        if (playerTarget != null)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, direction);
            
            Debug.Log($"플레이어 거리: {distance:F2}m");
            Debug.Log($"바라보기 각도 차이: {angle:F1}°");
        }
        
        if (agent != null)
        {
            Debug.Log($"NavMeshAgent - isStopped: {agent.isStopped}, velocity: {agent.velocity}");
        }
    }

    // 디버그용 목적지 시각화
    void OnDrawGizmosSelected()
    {
        // 일반 목적지
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(destination, 0.2f);
        Gizmos.DrawLine(transform.position, destination);

        // 상호작용 모드일 때 플레이어 연결선
        if (isInInteractionMode && playerTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
            
            // 🔧 거리 범위 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minLookAtDistance);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}