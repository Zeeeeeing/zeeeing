using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeeeingGaze;

public class NPCInteractionManager : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private float minInteractionTime = 3.0f; // 미니게임 시작 전 최소 상호작용 시간
    [SerializeField] private float interactionDistance = 5.0f; // NPC와의 최대 상호작용 거리
    
    [Header("Player References")]
    [SerializeField] private PlayerEmotionController playerEmotionController; // 플레이어 감정 컨트롤러 참조 추가
    
    [Header("Elite NPC Settings")]
    [SerializeField] private List<NPCController> eliteNPCs; // 미니게임이 필요한 특별 NPC
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = false; // 디버그 로그 활성화 옵션
    
    // 현재 상호작용 중인 NPC
    private NPCController currentInteractingNPC;
    private float currentInteractionTime = 0f;
    private bool isInteracting = false;
    
    // 플레이어 참조
    private Transform playerTransform;
    
    private void Start()
    {
        playerTransform = Camera.main.transform;
        
        // 플레이어 감정 컨트롤러가 없으면 찾기
        if (playerEmotionController == null)
        {
            playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
            if (playerEmotionController == null)
            {
                Debug.LogWarning("PlayerEmotionController를 찾을 수 없습니다. 감정 기반 상호작용이 작동하지 않을 수 있습니다.");
            }
        }
        
        Debug.Log("NPCInteractionManager 초기화 완료");
    }
    
    private void Update()
    {
        // 현재 상호작용 중인 NPC가 있는지 확인
        NPCController nearestNPC = FindNearestInteractableNPC();
        
        if (nearestNPC != null)
        {
            // 새로운 NPC를 보기 시작한 경우
            if (currentInteractingNPC != nearestNPC)
            {
                StartNewInteraction(nearestNPC);
            }
            else
            {
                // 기존 NPC와 계속 상호작용 중
                ContinueInteraction();
            }
        }
        else if (isInteracting)
        {
            // 상호작용 종료
            EndInteraction();
        }
    }
    
    // 가장 가까운 상호작용 가능한 NPC 찾기
    private NPCController FindNearestInteractableNPC()
    {
        NPCController nearest = null;
        float minDistance = interactionDistance;
        
        // FindObjectsOfType 대신 FindObjectsByType 사용
        NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        
        if (enableDebugLogs)
        {
            Debug.Log($"검색된 NPC 수: {allNPCs.Length}");
        }
        
        foreach (NPCController npc in allNPCs)
        {
            // 이미 따라오고 있는 NPC는 제외
            if (IsNPCFollowing(npc))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"NPC {npc.GetName()} - 이미 따라오는 중 (제외)");
                }
                continue;
            }
            
            // 거리 계산 - XZ 평면에서만 계산하여 높이 차이 무시
            Vector3 playerPosXZ = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            Vector3 npcPosXZ = new Vector3(npc.transform.position.x, 0, npc.transform.position.z);
            float distanceXZ = Vector3.Distance(playerPosXZ, npcPosXZ);
            
            // 일반 거리 계산 (높이 포함)
            float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
            
            // 시선 방향 체크 (플레이어가 NPC를 바라보고 있는지)
            Vector3 directionToNPC = (npc.transform.position - playerTransform.position).normalized;
            float dotProduct = Vector3.Dot(playerTransform.forward, directionToNPC);
            float angleToDegrees = Mathf.Acos(Mathf.Clamp(dotProduct, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            
            if (enableDebugLogs)
            {
                Debug.Log($"NPC: {npc.GetName()}, 거리: {distance:F2}m, XZ거리: {distanceXZ:F2}m, 각도: {angleToDegrees:F1}°, 도트곱: {dotProduct:F2}");
            }
            
            // 거리가 가깝고, 플레이어가 NPC를 바라보고 있으면
            // 0.5는 약 60도 이내 (0.7은 약 45도 이내)
            if (distanceXZ < minDistance && dotProduct > 0.5f)
            {
                nearest = npc;
                minDistance = distanceXZ;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"상호작용 가능한 NPC 발견: {npc.GetName()}, 거리: {distanceXZ:F2}m");
                }
            }
        }
        
        if (enableDebugLogs && nearest == null)
        {
            Debug.Log("상호작용 가능한 NPC가 없습니다.");
        }
        
        return nearest;
    }
    
    // 새로운 NPC와 상호작용 시작
    private void StartNewInteraction(NPCController npc)
    {
        // 이전 상호작용 종료
        if (isInteracting)
        {
            EndInteraction();
        }
        
        currentInteractingNPC = npc;
        currentInteractionTime = 0f;
        isInteracting = true;
        
        // NPC 하이라이트 표시 등의 처리
        HighlightNPC(npc, true);
        
        Debug.Log($"NPC {npc.GetName()}와(과) 상호작용 시작");
    }
    
    // 현재 상호작용 지속
    private void ContinueInteraction()
    {
        if (!isInteracting || currentInteractingNPC == null) return;
        
        // 상호작용 시간 증가
        currentInteractionTime += Time.deltaTime;
        
        // 플레이어의 현재 감정 상태 가져오기 (PlayerEmotionController 활용)
        EmotionState playerEmotion = EmotionState.Neutral; // 기본값
        if (playerEmotionController != null)
        {
            playerEmotion = playerEmotionController.GetCurrentEmotion();
        }
        
        // NPC의 감정 컨트롤러 가져오기
        NPCEmotionController emotionController = currentInteractingNPC.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            // 플레이어 감정에 따라 NPC 반응 트리거
            ReactToPlayerEmotion(emotionController, playerEmotion);
        }
        
        // 상호작용 진행률 표시 업데이트 등
        UpdateInteractionProgress();
        
        // 일정 시간 이상 상호작용했고, NPC가 미니게임을 필요로 하는 경우 미니게임 시작
        if (currentInteractionTime >= minInteractionTime && IsEliteNPC(currentInteractingNPC))
        {
            // 특정 조건이 만족되면 미니게임 시작
            if (ShouldTriggerMiniGame(currentInteractingNPC))
            {
                TriggerMiniGame(currentInteractingNPC);
            }
        }
        // 일반 NPC의 경우 일정 시간 이상 상호작용하면 '꼬시기' 성공
        else if (currentInteractionTime >= minInteractionTime * 2 && !IsEliteNPC(currentInteractingNPC))
        {
            // 플레이어 감정이 NPC의 현재 감정과 일치하면 더 빠르게 꼬시기 성공
            if (emotionController != null && playerEmotion == emotionController.GetCurrentEmotion())
            {
                HandleRegularNPCSuccess();
            }
            // 그렇지 않으면 기본 로직대로 처리
            else if (currentInteractionTime >= minInteractionTime * 3) // 더 오래 걸림
            {
                HandleRegularNPCSuccess();
            }
        }
    }
    
    // 플레이어 감정에 따른 NPC 반응 처리
    private void ReactToPlayerEmotion(NPCEmotionController npcEmotionController, EmotionState playerEmotion)
    {
        if (npcEmotionController == null) return;
        
        // NPC의 현재 감정 상태 및 강도 가져오기
        EmotionState npcEmotion = npcEmotionController.GetCurrentEmotion();
        float npcEmotionIntensity = npcEmotionController.GetCurrentEmotionIntensity();
        
        // NPC 타입이나 성격에 따라 다른 반응 로직 적용 가능
        NPCController npcController = npcEmotionController.GetComponent<NPCController>();
        
        // 플레이어 감정에 따른 NPC 반응 로직
        switch (playerEmotion)
        {
            case EmotionState.Happy:
                // 행복한 플레이어에 대한 NPC 반응
                if (npcEmotion == EmotionState.Neutral || npcEmotion == EmotionState.Sad)
                {
                    // 중립이나 슬픈 NPC는 행복한 플레이어에 점차 영향받음
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.02f);
                    
                    // 감정 강도가 충분히 높아지면 감정 변경
                    if (npcEmotionIntensity > 0.8f && npcEmotion != EmotionState.Happy)
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Happy);
                    }
                }
                else if (npcEmotion == EmotionState.Angry)
                {
                    // 화난 NPC는 행복한 플레이어에 냉담하게 반응
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.01f);
                }
                break;
                
            case EmotionState.Sad:
                // 슬픈 플레이어에 대한 NPC 반응
                if (npcEmotion == EmotionState.Happy)
                {
                    // 행복한 NPC는 슬픈 플레이어에 의해 감정이 누그러짐
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.01f);
                    
                    if (npcEmotionIntensity < 0.2f)
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Neutral);
                    }
                }
                else if (npcEmotion == EmotionState.Neutral)
                {
                    // 중립 NPC는 슬픈 플레이어에 영향받아 점차 슬퍼짐
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.02f);
                    
                    if (npcEmotionIntensity > 0.7f)
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Sad);
                    }
                }
                break;
                
            case EmotionState.Angry:
                // 화난 플레이어에 대한 NPC 반응
                if (npcEmotion == EmotionState.Happy || npcEmotion == EmotionState.Neutral)
                {
                    // 행복하거나 중립 NPC는 화난 플레이어에 위축됨
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.02f);
                    
                    if (npcEmotionIntensity < 0.3f && npcEmotion == EmotionState.Happy)
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Neutral);
                    }
                }
                else if (npcEmotion == EmotionState.Angry)
                {
                    // 화난 NPC는 화난 플레이어에 더 화가 남
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.03f);
                }
                break;
                
            case EmotionState.Neutral:
            default:
                // 중립 플레이어에 대한 NPC 반응은 미미함
                // NPC는 자신의 감정 상태를 천천히 원래대로 되돌림
                if (npcEmotionIntensity > 0.5f)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.01f);
                }
                else if (npcEmotionIntensity < 0.5f)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.01f);
                }
                break;
        }
    }
    
    // 상호작용 종료
    private void EndInteraction()
    {
        if (currentInteractingNPC != null)
        {
            // NPC 하이라이트 제거
            HighlightNPC(currentInteractingNPC, false);
            Debug.Log($"NPC {currentInteractingNPC.GetName()}와(과)의 상호작용 종료");
        }
        
        currentInteractingNPC = null;
        currentInteractionTime = 0f;
        isInteracting = false;
    }
    
    // NPC 하이라이트 표시/제거
    private void HighlightNPC(NPCController npc, bool highlight)
    {
        // 인터랙션 중임을 표시하는 효과 (아웃라인, 이펙트 등)
        // TODO: 구현 필요 - 현재는 빈 메서드
    }
    
    // 상호작용 진행률 업데이트
    private void UpdateInteractionProgress()
    {
        // 상호작용 진행률 UI 업데이트 (진행 상황에 따라 UI 변경)
        // TODO: 구현 필요 - 현재는 빈 메서드
    }
    
    // 해당 NPC가 특별 미니게임이 필요한 NPC인지 확인
    private bool IsEliteNPC(NPCController npc)
    {
        return eliteNPCs.Contains(npc) || npc.IsEliteNPC();
    }
    
    // NPC가 이미 따라오고 있는지 확인 (FollowerManager 활용)
    public bool IsNPCFollowing(NPCController npc)
    {
        return ZeeeingGaze.FollowerManager.Instance != null && ZeeeingGaze.FollowerManager.Instance.IsNPCFollowing(npc);
    }
    
    // 미니게임을 트리거해야 하는지 확인
    private bool ShouldTriggerMiniGame(NPCController npc)
    {
        // NPC의 감정 상태 확인
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            // 감정 강도가 특정 임계값을 넘었는지 확인
            bool intensityCheck = emotionController.GetCurrentEmotionIntensity() >= 0.7f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"미니게임 트리거 조건 확인: NPC={npc.GetName()}, 감정강도={emotionController.GetCurrentEmotionIntensity():F2}, 임계값=0.7, 만족={intensityCheck}");
            }
            
            return intensityCheck;
        }
        
        return false;
    }
    
    // 미니게임 트리거
    private void TriggerMiniGame(NPCController npc)
    {
        // miniGameManager 참조 확인
        if (miniGameManager == null)
        {
            miniGameManager = FindAnyObjectByType<MiniGameManager>();
            if (miniGameManager == null)
            {
                Debug.LogError("MiniGameManager를 찾을 수 없습니다! 미니게임을 시작할 수 없습니다.");
                return;
            }
        }
        
        // NPC 타입에 따른 미니게임 선택
        MiniGameManager.MiniGameType gameType = DetermineGameType(npc);
        
        // 난이도 결정 (NPC 속성에 따라 다르게 설정 가능)
        int difficulty = DetermineDifficulty(npc);
        
        Debug.Log($"미니게임 시작: 타입={gameType}, 난이도={difficulty}, NPC={npc.GetName()}");
        
        // 미니게임 시작
        bool started = miniGameManager.StartMiniGame(gameType, difficulty);
        
        if (started)
        {
            // 상호작용 일시 중지 (미니게임 진행 중에는 다른 NPC와 상호작용 불가)
            isInteracting = false;
            Debug.Log("미니게임이 성공적으로 시작됨");
            
            // 추가: UI가 표시되지 않은 경우를 대비해 직접 호출
            MiniGameUI ui = FindAnyObjectByType<MiniGameUI>();
            if (ui != null)
            {
                ui.ShowMiniGameUI(gameType);
            }
        }
        else
        {
            Debug.LogWarning("미니게임 시작 실패");
        }
    }

    // NPC 타입에 따른 미니게임 타입 결정
    private MiniGameManager.MiniGameType DetermineGameType(NPCController npc)
    {
        // NPC가 랜덤 미니게임을 사용한다면 랜덤 선택
        if (npc.UseRandomMiniGame())
        {
            int randomType = UnityEngine.Random.Range(0, 3);
            MiniGameManager.MiniGameType randomGameType = (MiniGameManager.MiniGameType)randomType;
            Debug.Log($"랜덤 미니게임 선택: {randomGameType}");
            return randomGameType;
        }
        
        // NPC의 선호 미니게임 타입 반환
        MiniGameManager.MiniGameType gameType = npc.GetPreferredMiniGameType();
        Debug.Log($"NPC 선호 미니게임 선택: {gameType}");
        return gameType;
    }

    // NPC 타입에 따른 난이도 결정
    private int DetermineDifficulty(NPCController npc)
    {
        // NPC에 설정된 미니게임 난이도 반환
        return npc.GetMiniGameDifficulty();
    }
    
    // 일반 NPC 꼬시기 성공 처리
    private void HandleRegularNPCSuccess()
    {
        if (currentInteractingNPC == null) return;
        
        Debug.Log($"일반 NPC {currentInteractingNPC.GetName()} 꼬시기 성공!");
        
        // 감정 상태 행복으로 변경
        NPCEmotionController emotionController = currentInteractingNPC.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            emotionController.ChangeEmotionState(EmotionState.Happy);
        }
        
        // NPC를 유령 모드로 설정
        currentInteractingNPC.SetSeduced();
        currentInteractingNPC.SetGhostMode(true);
        
        // 상호작용 종료
        EndInteraction();
    }
    
    // 현재 상호작용 중인 NPC 반환
    public NPCController GetCurrentInteractingNPC()
    {
        return currentInteractingNPC;
    }
    
    // 디버그 로그 활성화/비활성화 설정
    public void SetDebugLogging(bool enable)
    {
        enableDebugLogs = enable;
        Debug.Log($"NPCInteractionManager 디버그 로그 {(enable ? "활성화" : "비활성화")}");
    }
}