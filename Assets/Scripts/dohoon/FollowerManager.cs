using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]  // 팔로워보다 먼저 Awake/Start
public class FollowerManager : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("플레이어 Transform")]
    public Transform player;

    [Header("Queue Settings")]
    [Tooltip("팔로워 간 줄 간격")]
    public float followDistance = 2f;
    [Tooltip("이동 속도")]
    public float moveSpeed = 4f;

    // 팔로워 정보 저장용 클래스
    private class FollowerData
    {
        public Transform trans;
        public float initialY;
    }

    private readonly List<FollowerData> followers = new List<FollowerData>();

    public void RegisterFollower(Transform follower)
    {
        if (followers.Exists(f => f.trans == follower)) return;
        followers.Add(new FollowerData {
            trans = follower,
            initialY = follower.position.y
        });
    }

    public void UnregisterFollower(Transform follower)
    {
        followers.RemoveAll(f => f.trans == follower);
    }

    void Update()
    {
        if (player == null) return;

        for (int i = 0; i < followers.Count; i++)
        {
            var data = followers[i];
            var f = data.trans;

            // 플레이어 뒤 (i+1)*followDistance 위치 계산 (XZ)
            Vector3 basePos = player.position - player.forward * (followDistance * (i + 1));
            Vector3 targetPos = new Vector3(
                basePos.x,
                data.initialY,  // 초기 Y 고정
                basePos.z
            );

            // 부드러운 이동
            f.position = Vector3.MoveTowards(
                f.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            // 플레이어 바라보며 부드럽게 회전
            f.rotation = Quaternion.Slerp(
                f.rotation,
                player.rotation,
                moveSpeed * Time.deltaTime
            );
        }
    }
}
