using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeeeingGaze;

public class NPCInteractionManager : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private float minInteractionTime = 2.0f; // 감소 (3.0 -> 2.0)
    [SerializeField] private float interactionDistance = 4.0f; // 감소 (5.0 -> 4.0, 더 가까이)
    
    [Header("Distance-based UI Settings")]
    [SerializeField] private float uiShowDistance = 6.0f; // UI 표시 거리 (가까울 때도 표시)
    [SerializeField] private float uiHideDistance = 0.3f; // UI 숨김 거리 (너무 가까우면 숨김)
    [SerializeField] private bool showUIAtAllDistances = true; // 모든 거리에서 UI 표시
    
    [Header("Player References")]
    [SerializeField] private PlayerEmotionController playerEmotionController;
    
    [Header("Elite NPC Settings")]
    [SerializeField] private List<NPCController> eliteNPCs;
    
    [Header("Fast Tempo Settings")]
<<<<<<< Updated upstream
    [SerializeField] private float fastEmotionBuildupMultiplier = 2.0f; // 감정 쌓이는 속도 2배
    [SerializeField] private float regularNPCSuccessTime = 3.0f; // 감소 (기존 6.0 -> 3.0)
    [SerializeField] private float matchingEmotionBonus = 1.5f; // 감정 일치시 보너스
    
=======
    [SerializeField] private float fastEmotionBuildupMultiplier = 2.0f;
    [SerializeField] private float regularNPCSuccessTime = 3.0f;
    [SerializeField] private float matchingEmotionBonus = 1.5f;
    [Header("Interaction Delay Settings")]
    [SerializeField] private float interactionHoldTime = 1.0f; // 상호작용 유지 시간
    private float interactionLostTime = 0f; // 감지 상실 시점 기록

    [Header("Fever Mode Settings")]
    [SerializeField] private float feverModeSuccessTimeMultiplier = 0.5f; // 피버 모드일 때 성공 시간 배율
    private MiniGameManager miniGameManagerRef; // MiniGameManager 참조

>>>>>>> Stashed changes
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 현재 상호작용 중인 NPC
    private NPCController currentInteractingNPC;
    private float currentInteractionTime = 0f;
    private bool isInteracting = false;
    
    // 플레이어 참조
    private Transform playerTransform;
<<<<<<< Updated upstream
    
    private void Start()
    {
        playerTransform = Camera.main.transform;
        
        // 플레이어 감정 컨트롤러가 없으면 찾기
        if (playerEmotionController == null)
=======
    private Camera playerCamera;

    private void Start()
    {
        playerTransform = Camera.main.transform;
        playerCamera = Camera.main;

        // ⭐ 추가: MiniGameManager 참조 가져오기
        if (miniGameManagerRef == null)
        {
            miniGameManagerRef = FindAnyObjectByType<MiniGameManager>();
            if (miniGameManagerRef == null)
            {
                Debug.LogWarning("MiniGameManager를 찾을 수 없습니다. 피버 모드 기능이 제한될 수 있습니다.");
            }
        }

        // EyeTrackingRay 자동 찾기 및 이벤트 연결
        if (eyeTrackingRay == null)
        {
            eyeTrackingRay = FindAnyObjectByType<EyeTrackingRay>();
            if (eyeTrackingRay == null)
            {
                Debug.LogError("EyeTrackingRay를 찾을 수 없습니다! 카메라에 EyeTrackingRay 컴포넌트를 추가하세요.");
            }
        }

        if (enableDebugLogs)
>>>>>>> Stashed changes
        {
            Debug.Log("NPCInteractionManager 초기화 완료 (EyeTracking 전용 모드)");
        }
<<<<<<< Updated upstream
        
        Debug.Log("NPCInteractionManager 초기화 완료 (빠른 템포 모드)");
=======
>>>>>>> Stashed changes
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
            
            // 시선 방향 체크 (플레이어가 NPC를 바라보고 있는지)
            Vector3 directionToNPC = (npc.transform.position - playerTransform.position).normalized;
            float dotProduct = Vector3.Dot(playerTransform.forward, directionToNPC);
            
            if (enableDebugLogs)
            {
                Debug.Log($"NPC: {npc.GetName()}, XZ거리: {distanceXZ:F2}m, 도트곱: {dotProduct:F2}");
            }
            
            // 거리가 가깝고, 플레이어가 NPC를 바라보고 있으면 (더 관대한 각도)
            // 0.2은 약 78도 이내 (기존 0.3에서 더 완화)
            if (distanceXZ < minDistance && dotProduct > 0.2f)
            {
                nearest = npc;
                minDistance = distanceXZ;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"상호작용 가능한 NPC 발견: {npc.GetName()}, 거리: {distanceXZ:F2}m");
                }
            }
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
        
        Debug.Log($"NPC {npc.GetName()}와(과) 상호작용 시작 (빠른 템포 모드)");
    }
<<<<<<< Updated upstream
    
=======

>>>>>>> Stashed changes
    // 현재 상호작용 지속
    // ⭐ 수정된 ContinueInteraction 메서드 (기존 코드에 피버 모드 처리 추가)
    private void ContinueInteraction()
    {
        if (!isInteracting || currentInteractingNPC == null) return;
<<<<<<< Updated upstream
        
        // 감정 매칭 체크 - 새로 추가된 부분만
        bool emotionMatched = CheckEmotionMatch(currentInteractingNPC);
        
        // 감정이 매칭되지 않으면 꼬시기 진행 안됨
=======

        // 🔥 EyeTracking으로 여전히 감지되는지 확인
        bool stillDetected = IsNPCStillDetected(currentInteractingNPC);

        if (!stillDetected)
        {
            // ⭐ interactionHoldTime 딜레이 추가: 감지 상실 시점 기록
            if (interactionLostTime == 0f)
            {
                interactionLostTime = Time.time;
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ NPC 감지 상실 - {interactionHoldTime}초 대기 시작: {currentInteractingNPC.GetName()}");
                }
                return; // 바로 종료하지 않고 대기
            }

            // interactionHoldTime이 지났는지 확인
            if (Time.time - interactionLostTime >= interactionHoldTime)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ NPC 감지 상실 {interactionHoldTime}초 경과로 상호작용 종료: {currentInteractingNPC.GetName()}");
                }
                EndInteraction();
                return;
            }
            else
            {
                // 아직 설정 시간이 안 지났으므로 대기 계속
                if (enableDebugLogs)
                {
                    float remainingTime = interactionHoldTime - (Time.time - interactionLostTime);
                    Debug.Log($"👁️ NPC 감지 상실 대기 중: {currentInteractingNPC.GetName()} (남은시간: {remainingTime:F1}s)");
                }

                // ⭐ 중요: 대기 중에도 AutonomousDriver는 유지하고 꼬시기만 중단
                // 감정 매칭이나 꼬시기 진행은 하지 않지만 바라보기는 유지
                return; // 감정 매칭이나 시간 진행 없이 그냥 리턴
            }
        }
        else
        {
            // 다시 감지되면 대기 시간 리셋
            if (interactionLostTime > 0f)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ NPC 재감지 - 대기 상태 해제: {currentInteractingNPC.GetName()}");
                }
                interactionLostTime = 0f;
            }
        }

        // 여기서부터는 기존 코드와 동일 (감정 매칭, 꼬시기 진행 등)
        bool emotionMatched = CheckEmotionMatch(currentInteractingNPC);

>>>>>>> Stashed changes
        if (!emotionMatched)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"감정 불일치로 꼬시기 진행 안됨: {currentInteractingNPC.GetName()}");
            }
            return; // 시간 진행 안함
        }
<<<<<<< Updated upstream
        
        // 기존 코드 그대로 유지 - 감정이 매칭될 때만 실행
        currentInteractionTime += Time.deltaTime * fastEmotionBuildupMultiplier;
        
=======

        float previousTime = currentInteractionTime;
        currentInteractionTime += Time.deltaTime * fastEmotionBuildupMultiplier;

        if (enableDebugLogs)
        {
            // ⭐ 수정: 피버 모드 상태에 따른 성공 시간 표시
            float currentSuccessTime = GetCurrentRegularNPCSuccessTime();
            Debug.Log($"💖 꼬시기 진행 중: {currentInteractingNPC.GetName()} " +
                    $"({previousTime:F1}s → {currentInteractionTime:F1}s) " +
                    $"목표: {(IsEliteNPC(currentInteractingNPC) ? minInteractionTime : currentSuccessTime):F1}s" +
                    $"{(miniGameManagerRef != null && miniGameManagerRef.IsFeverModeActive() ? " 🔥(피버모드)" : "")}");
        }

>>>>>>> Stashed changes
        EmotionState playerEmotion = EmotionState.Neutral;
        if (playerEmotionController != null)
        {
            playerEmotion = playerEmotionController.GetCurrentEmotion();
        }

        NPCEmotionController emotionController = currentInteractingNPC.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            ReactToPlayerEmotion(emotionController, playerEmotion);
        }

        UpdateInteractionProgress();
<<<<<<< Updated upstream
        
        // 기존 Elite NPC 및 일반 NPC 로직 그대로 유지
=======

        // Elite NPC 체크
>>>>>>> Stashed changes
        if (currentInteractionTime >= minInteractionTime && IsEliteNPC(currentInteractingNPC))
        {
            if (ShouldTriggerMiniGame(currentInteractingNPC))
            {
                TriggerMiniGame(currentInteractingNPC);
            }
        }
<<<<<<< Updated upstream
        else if (!IsEliteNPC(currentInteractingNPC))
=======
        // ⭐ 수정: 일반 NPC 체크 - 피버 모드 상태에 따른 성공 시간 사용
        else if (currentInteractionTime >= GetCurrentRegularNPCSuccessTime() && !IsEliteNPC(currentInteractingNPC))
>>>>>>> Stashed changes
        {
            float requiredTime = regularNPCSuccessTime;
            
            if (emotionController != null && playerEmotion == emotionController.GetCurrentEmotion())
            {
                requiredTime *= (1.0f / matchingEmotionBonus);
            }
            
            if (currentInteractionTime >= requiredTime)
            {
                HandleRegularNPCSuccess();
            }
        }
    }
<<<<<<< Updated upstream
    
    private bool CheckEmotionMatch(NPCController npc)
=======

    // 🔥 NPC가 여전히 감지되는지 확인
    private bool IsNPCStillDetected(NPCController npc)
>>>>>>> Stashed changes
    {
        if (playerEmotionController == null) return false;
        
        NPCEmotionController npcEmotion = npc.GetComponent<NPCEmotionController>();
        if (npcEmotion == null) return false;

        EmotionState playerEmotion = playerEmotionController.GetCurrentEmotion();
        EmotionState npcEmotionState = npcEmotion.GetCurrentEmotion();
        
        bool isMatched = playerEmotion == npcEmotionState;
        
        if (enableDebugLogs && isMatched)
        {
            Debug.Log($"감정 매칭 성공: 플레이어({playerEmotion}) = NPC({npcEmotionState})");
        }
        
        return isMatched;
    }
    
    // 플레이어 감정에 따른 NPC 반응 처리 (강화된 반응)
    private void ReactToPlayerEmotion(NPCEmotionController npcEmotionController, EmotionState playerEmotion)
    {
        if (npcEmotionController == null) return;
        
        // NPC의 현재 감정 상태 및 강도 가져오기
        EmotionState npcEmotion = npcEmotionController.GetCurrentEmotion();
        float npcEmotionIntensity = npcEmotionController.GetCurrentEmotionIntensity();
        
        // 빠른 템포를 위한 감정 변화 배율
        float emotionChangeRate = fastEmotionBuildupMultiplier * Time.deltaTime;
        
        // 플레이어 감정에 따른 NPC 반응 로직 (강화됨)
        switch (playerEmotion)
        {
            case EmotionState.Happy:
                if (npcEmotion == EmotionState.Neutral || npcEmotion == EmotionState.Sad)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.04f * emotionChangeRate);
                    
                    if (npcEmotionIntensity > 0.6f && npcEmotion != EmotionState.Happy) // 임계값 낮춤
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Happy);
                    }
                }
                else if (npcEmotion == EmotionState.Angry)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.02f * emotionChangeRate);
                }
                break;
                
            case EmotionState.Sad:
                if (npcEmotion == EmotionState.Happy)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.02f * emotionChangeRate);
                    
                    if (npcEmotionIntensity < 0.3f) // 임계값 높임
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Neutral);
                    }
                }
                else if (npcEmotion == EmotionState.Neutral)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.03f * emotionChangeRate);
                    
                    if (npcEmotionIntensity > 0.5f) // 임계값 낮춤
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Sad);
                    }
                }
                break;
                
            case EmotionState.Angry:
                if (npcEmotion == EmotionState.Happy || npcEmotion == EmotionState.Neutral)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.03f * emotionChangeRate);
                    
                    if (npcEmotionIntensity < 0.4f && npcEmotion == EmotionState.Happy) // 임계값 높임
                    {
                        npcEmotionController.ChangeEmotionState(EmotionState.Neutral);
                    }
                }
                else if (npcEmotion == EmotionState.Angry)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.05f * emotionChangeRate);
                }
                break;
                
            case EmotionState.Neutral:
            default:
                // 중립 플레이어에 대한 NPC 반응 (더 빠른 수렴)
                if (npcEmotionIntensity > 0.5f)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity - 0.02f * emotionChangeRate);
                }
                else if (npcEmotionIntensity < 0.5f)
                {
                    npcEmotionController.SetEmotionIntensity(npcEmotionIntensity + 0.02f * emotionChangeRate);
                }
                break;
        }
    }
    
    // 상호작용 종료
    private void EndInteraction()
    {
        if (currentInteractingNPC != null)
        {
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
    
    // NPC가 이미 따라오고 있는지 확인
    public bool IsNPCFollowing(NPCController npc)
    {
        return ZeeeingGaze.FollowerManager.Instance != null && ZeeeingGaze.FollowerManager.Instance.IsNPCFollowing(npc);
    }
    
    // 미니게임을 트리거해야 하는지 확인 (더 낮은 임계값)
    private bool ShouldTriggerMiniGame(NPCController npc)
    {
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            // 감정 강도 임계값 낮춤 (0.7 -> 0.5)
            bool intensityCheck = emotionController.GetCurrentEmotionIntensity() >= 0.5f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"미니게임 트리거 조건 확인: NPC={npc.GetName()}, 감정강도={emotionController.GetCurrentEmotionIntensity():F2}, 임계값=0.5, 만족={intensityCheck}");
            }
            
            return intensityCheck;
        }
        
        return false;
    }
    
    // 미니게임 트리거
    private void TriggerMiniGame(NPCController npc)
    {
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
        
        // 난이도 결정 (더 쉽게 조정)
        int difficulty = Mathf.Max(0, DetermineDifficulty(npc) - 1); // 난이도 1단계 낮춤
        
        Debug.Log($"미니게임 시작: 타입={gameType}, 난이도={difficulty}, NPC={npc.GetName()} (빠른 템포 모드)");
        
        // 미니게임 시작
        bool started = miniGameManager.StartMiniGame(gameType, difficulty);
        
        if (started)
        {
            isInteracting = false;
            Debug.Log("미니게임이 성공적으로 시작됨");
            
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
        if (npc.UseRandomMiniGame())
        {
            // 2개 게임 중 랜덤 선택
            int randomType = UnityEngine.Random.Range(0, 2);
            MiniGameManager.MiniGameType randomGameType = (MiniGameManager.MiniGameType)randomType;
            Debug.Log($"랜덤 미니게임 선택: {randomGameType}");
            return randomGameType;
        }
        
        MiniGameManager.MiniGameType gameType = npc.GetPreferredMiniGameType();
        Debug.Log($"NPC 선호 미니게임 선택: {gameType}");
        return gameType;
    }

    // NPC 타입에 따른 난이도 결정
    private int DetermineDifficulty(NPCController npc)
    {
        return npc.GetMiniGameDifficulty();
    }
    
    // 일반 NPC 꼬시기 성공 처리 (수정된 버전)
    private void HandleRegularNPCSuccess()
    {
        if (currentInteractingNPC == null) return;
        
        Debug.Log($"일반 NPC {currentInteractingNPC.GetName()} 꼬시기 성공! (감정 매칭으로 성공)");
        
        // 기존 코드 그대로 유지
        NPCEmotionController emotionController = currentInteractingNPC.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            emotionController.ChangeEmotionState(EmotionState.Happy);
        }
        
        // Critical Hit VFX 표시 - 새로 추가된 부분만
        ShowSeductionSuccessVFX(currentInteractingNPC);
        
        // 기존 코드 그대로 유지
        currentInteractingNPC.SetSeducedByRegularInteraction();
        currentInteractingNPC.SetGhostMode(true);
        
        EndInteraction();
    }

    private void ShowSeductionSuccessVFX(NPCController npc)
    {
        if (EmotionGazeManager.Instance != null && EmotionGazeManager.Instance.HasDefaultVFXAsset())
        {
            // Critical Hit VFX 직접 생성
            var criticalHitVFXPrefab = EmotionGazeManager.Instance.GetType()
                .GetField("criticalHitVFXPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(EmotionGazeManager.Instance) as GameObject;
            
            if (criticalHitVFXPrefab != null)
            {
                Vector3 vfxPosition = npc.transform.position + Vector3.up * 1.5f;
                GameObject criticalVFX = Instantiate(criticalHitVFXPrefab, vfxPosition, Quaternion.identity);
                Destroy(criticalVFX, 2f);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"꼬시기 성공 VFX 표시: {npc.GetName()}");
                }
            }
        }
    }
<<<<<<< Updated upstream
    
=======

    // ⭐ 피버 모드 상태에 따른 성공 시간 반환 메서드 (새로 추가)
    private float GetCurrentRegularNPCSuccessTime()
    {
        // 피버 모드 상태 확인
        if (miniGameManagerRef != null && miniGameManagerRef.IsFeverModeActive())
        {
            float feverModeTime = regularNPCSuccessTime * feverModeSuccessTimeMultiplier;
            if (enableDebugLogs)
            {
                Debug.Log($"🔥 피버 모드 활성화 - NPC 성공 시간: {regularNPCSuccessTime}s → {feverModeTime}s");
            }
            return feverModeTime;
        }

        return regularNPCSuccessTime;
    }

    // NPC 하이라이트 처리
    private void HighlightNPC(NPCController npc, bool highlight)
    {
        // 하이라이트 로직 구현
    }
    
    // 플레이어 감정에 따른 NPC 반응
    private void ReactToPlayerEmotion(NPCEmotionController npcEmotion, EmotionState playerEmotion)
    {
        // 감정 반응 로직 구현
    }

    // 상호작용 진행도 업데이트
    // ⭐ 수정된 UpdateInteractionProgress 메서드 (피버 모드 반영)
    private void UpdateInteractionProgress()
    {
        float progress = IsEliteNPC(currentInteractingNPC) ?
            currentInteractionTime / minInteractionTime :
            currentInteractionTime / GetCurrentRegularNPCSuccessTime(); // ⭐ 수정: 피버 모드 상태 반영

        if (enableDebugLogs)
        {
            string feverStatus = (miniGameManagerRef != null && miniGameManagerRef.IsFeverModeActive()) ? " 🔥" : "";
            Debug.Log($"상호작용 진행도: {progress:P1}{feverStatus}");
        }
    }

>>>>>>> Stashed changes
    // 현재 상호작용 중인 NPC 반환
    public NPCController GetCurrentInteractingNPC()
    {
        return currentInteractingNPC;
    }
    
    // 디버그 로그 활성화/비활성화 설정
    public void SetDebugLogging(bool enable)
    {
        enableDebugLogs = enable;
        Debug.Log($"NPCInteractionManager 디버그 로그 {(enable ? "활성화" : "비활성화")} (빠른 템포 모드)");
    }
    
    // UI 거리 설정 메서드들
    public void SetUIDistanceSettings(float showDistance, float hideDistance, bool showAtAllDistances)
    {
        uiShowDistance = Mathf.Max(0.1f, showDistance);
        uiHideDistance = Mathf.Max(0.1f, hideDistance);
        showUIAtAllDistances = showAtAllDistances;
        
        Debug.Log($"UI 거리 설정 업데이트 - 표시: {uiShowDistance}m, 숨김: {uiHideDistance}m, 항상 표시: {showUIAtAllDistances}");
    }
    
    public float GetUIShowDistance() => uiShowDistance;
    public float GetUIHideDistance() => uiHideDistance;
    public bool GetShowUIAtAllDistances() => showUIAtAllDistances;
    
    // 상호작용 거리 설정
    public void SetInteractionDistance(float distance)
    {
        interactionDistance = Mathf.Max(0.5f, distance);
        Debug.Log($"상호작용 거리 설정: {interactionDistance}m");
    }
    
    public float GetInteractionDistance() => interactionDistance;
}