using UnityEngine;

public class Follower : MonoBehaviour
{
    [Header("▶ Test Follow (Inspector)")]
    [Tooltip("체크하면 따라다니기 시작, 해제하면 멈춤")]
    public bool testFollow = false;

    private bool isCharmed = false;
    private bool lastTestFollow = false;
    private FollowerManager manager;

    void Start()
    {
        // Obsolete 경고 없이 매니저 참조
        manager = Object.FindFirstObjectByType<FollowerManager>();
        lastTestFollow = testFollow;
        ApplyFollowState(testFollow);
    }

    void Update()
    {
        if (testFollow != lastTestFollow)
        {
            ApplyFollowState(testFollow);
            lastTestFollow = testFollow;
        }
    }

    private void ApplyFollowState(bool shouldFollow)
    {
        if (shouldFollow && !isCharmed)
            Charm();
        else if (!shouldFollow && isCharmed)
            Uncharm();
    }

    private void Charm()
    {
        isCharmed = true;
        manager?.RegisterFollower(transform);

        // Rigidbody 물리 끄기
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity   = false;
        }

        // Transform, Animator, OutfitSystem, Rigidbody, Follower 스크립트 자신만 남기고
        // **나머지 Behaviour & Collider만** 끄도록 변경
        DisableEverythingExceptCore();
    }

    private void Uncharm()
    {
        isCharmed = false;
        manager?.UnregisterFollower(transform);

        // Rigidbody 원복
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity   = true;
        }

        // (필요 시 다시 켜줄 로직을 여기에)
    }

    private void DisableEverythingExceptCore()
    {
        foreach (var comp in GetComponentsInChildren<Component>())
        {
            // 유지할 컴포넌트
            if (comp is Transform ||
                comp is Animator ||
                comp is Rigidbody ||
                comp is Follower ||
                comp.GetType().Name == "OutfitSystem")
            {
                continue;
            }

            // Behaviour (모든 MonoBehaviour, NavMeshAgent 등) 끄기
            if (comp is Behaviour b)
            {
                b.enabled = false;
                continue;
            }

            // Collider 끄기
            if (comp is Collider c)
            {
                c.enabled = false;
                continue;
            }

            // **더 이상 Renderer를 끄지 않습니다**
        }
    }
}
