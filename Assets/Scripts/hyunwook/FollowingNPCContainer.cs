using System.Collections.Generic;
using UnityEngine;
using ZeeeingGaze;

public class FollowingNPCContainer : MonoBehaviour
{
    [SerializeField] private List<NPCController> followingNPCs = new List<NPCController>();
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float followHeight = 0.5f;
    [SerializeField] private float orbitSpeed = 20f; // NPC가 플레이어 주위를 도는 속도
    
    private void Start()
    {
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }
    
    private void Update()
    {
        // 모든 따라오는 NPC 업데이트
        for (int i = 0; i < followingNPCs.Count; i++)
        {
            UpdateNPCPosition(followingNPCs[i], i);
        }
    }
    
    public void AddNPC(NPCController npc)
    {
        if (npc != null && !followingNPCs.Contains(npc))
        {
            followingNPCs.Add(npc);
            
            // NPC 유령 모드 설정
            npc.SetGhostMode(true);
            
            Debug.Log($"NPC {npc.GetName()} added to following container. Total: {followingNPCs.Count}");
        }
    }
    
    public void RemoveNPC(NPCController npc)
    {
        if (npc != null && followingNPCs.Contains(npc))
        {
            followingNPCs.Remove(npc);
            
            // NPC 유령 모드 해제 (선택적)
            npc.SetGhostMode(false);
            
            Debug.Log($"NPC {npc.GetName()} removed from following container. Total: {followingNPCs.Count}");
        }
    }
    
    public bool IsNPCFollowing(NPCController npc)
    {
        return followingNPCs.Contains(npc);
    }
    
    public int GetFollowingCount()
    {
        return followingNPCs.Count;
    }
    
    private void UpdateNPCPosition(NPCController npc, int index)
    {
        if (npc == null || playerTransform == null) return;
        
        // 각 NPC마다 다른 위치 계산 (원형 배치)
        float angleOffset = (360f / Mathf.Max(1, followingNPCs.Count)) * index;
        float currentAngle = angleOffset + Time.time * orbitSpeed;
        
        Vector3 offset = new Vector3(
            Mathf.Sin(Mathf.Deg2Rad * currentAngle) * followDistance,
            followHeight,
            Mathf.Cos(Mathf.Deg2Rad * currentAngle) * followDistance
        );
        
        Vector3 targetPosition = playerTransform.position + offset;
        
        // NPC 부드러운 이동
        npc.transform.position = Vector3.Lerp(
            npc.transform.position,
            targetPosition,
            Time.deltaTime * 3.0f
        );
        
        // NPC가 플레이어를 바라보도록 회전
        Vector3 lookDirection = playerTransform.position - npc.transform.position;
        lookDirection.y = 0; // Y축 회전만 처리
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            npc.transform.rotation = Quaternion.Slerp(
                npc.transform.rotation,
                targetRotation,
                Time.deltaTime * 5.0f
            );
        }
    }

    // 플레이어 Transform 설정 메서드
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
}