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

    private NavMeshAgent agent;
    private NavMeshTriangulation navTri;
    private Vector3 destination;

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

        // NavMesh 정보 캐시
        navTri = NavMesh.CalculateTriangulation();

        // 첫 목적지 설정
        PickNewDestination();
    }

    void Update()
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

    // 디버그용 목적지 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(destination, 0.2f);
        Gizmos.DrawLine(transform.position, destination);
    }
}
