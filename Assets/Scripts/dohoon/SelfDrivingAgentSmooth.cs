using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SmoothWanderAgent : MonoBehaviour
{
    [Header("Wander 설정")]
    public float minWanderRadius    = 5f;    // 최소 wander 반경
    public float maxWanderRadius    = 30f;   // 최대 wander 반경
    public float wanderInterval     = 4f;    // 새 목적지 뽑는 주기(초)
    public float minMoveDistance    = 1.5f;  // 너무 가까운 목표 무시 기준
    public float goalOffsetRange    = 2f;    // 목적지 오프셋 범위

    [Header("NavMeshAgent 설정")]
    public float speed              = 3f;    // 이동 속도
    public float acceleration       = 100f;  // 최대 가속도 (즉시 목표 속도 도달)
    public float angularSpeed       = 360f;  // 회전 속도 제한 (°/초)
    public float stoppingDistance   = 0.5f;  // 도착 판단 거리
    public ObstacleAvoidanceType avoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

    private NavMeshAgent agent;
    private float wanderTimer;
    private Vector3 lastDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath            = true;
        agent.updateRotation        = false;    // 직접 회전 제어
        agent.speed                 = speed;
        agent.acceleration          = acceleration;
        agent.angularSpeed          = angularSpeed;
        agent.stoppingDistance      = stoppingDistance;
        agent.obstacleAvoidanceType = avoidanceType;
        agent.radius                = 0.3f;

        wanderTimer = wanderInterval; // 즉시 첫 목적지 뽑기
    }

    void Update()
    {
        // 1) Wander 타이머 갱신
        wanderTimer += Time.deltaTime;

        // 2) 주기 도래 또는 도착 시 새 목적지 설정
        if (wanderTimer >= wanderInterval ||
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            Vector3 next = GetRandomNavPoint(transform.position);
            if (Vector3.Distance(transform.position, next) > minMoveDistance)
            {
                next += Random.insideUnitSphere * goalOffsetRange;
                next.y = transform.position.y;
                agent.SetDestination(next);
                lastDestination = next;
                wanderTimer = 0f;
            }
        }

        // 3) 매 프레임 부드러운 회전 제어
        Vector3 vel = agent.velocity;
        if (vel.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(vel.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                angularSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// √분포로 가까운~먼 거리 랜덤 선택 후 NavMesh 상 유효 지점 반환
    /// </summary>
    Vector3 GetRandomNavPoint(Vector3 origin)
    {
        for (int i = 0; i < 20; i++)
        {
            float t    = Mathf.Pow(Random.value, 0.5f);
            float dist = Mathf.Lerp(minWanderRadius, maxWanderRadius, t);

            Vector3 dir  = Random.insideUnitSphere * dist;
            dir.y        = 0;
            Vector3 cand = origin + dir;

            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, dist, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }
}
