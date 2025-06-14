using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ZeeeingGaze;

public class NPCInteractionManager : MonoBehaviour
{
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private float minInteractionTime = 2.0f;
    [SerializeField] private float interactionDistance = 6.0f; // 🔧 UI 표시 거리와 동일하게 설정
    
    [Header("EyeTracking Detection Settings")]
    [SerializeField] private float checkInterval = 0.15f; // 체크 주기
    [SerializeField] private bool pauseCacheDuringInteraction = true; // 상호작용 중 캐시 갱신 일시정지
    
    [Header("Distance-based UI Settings")]
    [SerializeField] private float uiShowDistance = 6.0f;
    [SerializeField] private float uiHideDistance = 0.3f;
    [SerializeField] private bool showUIAtAllDistances = false; // 🔧 false로 변경하여 거리 기반 제어 활성화
    
    [Header("Eye Tracking References")]
    [SerializeField] private EyeTrackingRay eyeTrackingRay;
    
    [Header("Player References")]
    [SerializeField] private PlayerEmotionController playerEmotionController;
    
    [Header("Elite NPC Settings")]
    [SerializeField] private List<NPCController> eliteNPCs;
    
    [Header("Fast Tempo Settings")]
    [SerializeField] private float fastEmotionBuildupMultiplier = 2.0f;
    [SerializeField] private float regularNPCSuccessTime = 3.0f;
    [SerializeField] private float matchingEmotionBonus = 1.5f;
    [Header("Interaction Delay Settings")]
    [SerializeField] private float interactionHoldTime = 1.0f; // 상호작용 유지 시간
    private float interactionLostTime = 0f; // 감지 상실 시점 기록
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showViewAngleGizmos = true;
    
    // 현재 상호작용 중인 NPC
    private NPCController currentInteractingNPC;
    private float currentInteractionTime = 0f;
    private bool isInteracting = false;
    
    // 최적화를 위한 변수들
    private float lastCheckTime = 0f;
    private float lastInteractionEndTime = 0f;
    
    // 시야각 내 NPC 캐시 (제거됨 - EyeTracking 전용)
    private NPCController lastDetectedNPC; // 마지막으로 감지된 NPC 기억
    
    // 플레이어 참조
    private Transform playerTransform;
    private Camera playerCamera;
    
    private void Start()
    {
        playerTransform = Camera.main.transform;
        playerCamera = Camera.main;
        
        // EyeTrackingRay 자동 찾기 및 이벤트 연결
        if (eyeTrackingRay == null)
        {
            eyeTrackingRay = FindAnyObjectByType<EyeTrackingRay>();
            if (eyeTrackingRay == null)
            {
                Debug.LogError("EyeTrackingRay를 찾을 수 없습니다! 카메라에 EyeTrackingRay 컴포넌트를 추가하세요.");
            }
            else
            {
                Debug.Log($"EyeTrackingRay 자동 연결: {eyeTrackingRay.gameObject.name}");
                // 이벤트 기반 처리를 위한 연결 (필요시 EyeTrackingRay에 이벤트 추가)
            }
        }
        
        // 플레이어 감정 컨트롤러가 없으면 찾기
        if (playerEmotionController == null)
        {
            playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
            if (playerEmotionController == null)
            {
                Debug.LogWarning("PlayerEmotionController를 찾을 수 없습니다. 감정 기반 상호작용이 작동하지 않을 수 있습니다.");
            }
        }
        
        Debug.Log("NPCInteractionManager 초기화 완료 (최적화된 시야각 기반 버전)");
    }
    
    private void Update()
    {  
        // 🔥 최적화된 업데이트: 낮은 빈도로 체크
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            
            // 쿨다운 체크
            if (Time.time - lastInteractionEndTime < 1.0f)
            {
                return; // 쿨다운 중
            }
            
            HandleOptimizedNPCDetection();
        }
        
        // 기존 상호작용 지속 처리
        if (isInteracting)
        {
            ContinueInteraction();
        }
    }
    
    // 🔥 EyeTracking 전용 NPC 감지 로직
    private void HandleOptimizedNPCDetection()
    {
        // EyeTrackingRay에서 현재 감지된 인터랙터블 확인
        NPCController eyeTrackedNPC = GetNPCFromEyeTracking();
        
        if (eyeTrackedNPC != null)
        {
            // EyeTracking으로 감지된 NPC가 있으면 바로 처리
            lastDetectedNPC = eyeTrackedNPC;
            
            if (enableDebugLogs)
            {
                Debug.Log($"👁️ [EyeTracking 감지] {eyeTrackedNPC.GetName()}");
            }
            
            // 거리 체크
            float distance = Vector3.Distance(playerTransform.position, eyeTrackedNPC.transform.position);
            if (distance <= interactionDistance && !IsNPCFollowing(eyeTrackedNPC))
            {
                HandleNPCInteraction(eyeTrackedNPC);
                return;
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ [EyeTracking 감지] {eyeTrackedNPC.GetName()} - 거리 초과 ({distance:F1}m > {interactionDistance}m) 또는 이미 따라옴");
                }
            }
        }
        
        // 🔧 상호작용 중이고 마지막 감지 NPC가 있으면 EyeTracking 재확인
        if (isInteracting && lastDetectedNPC != null && lastDetectedNPC == currentInteractingNPC)
        {
            // EyeTracking으로 여전히 감지되는지 확인
            NPCController stillEyeTracked = GetNPCFromEyeTracking();
            if (stillEyeTracked == lastDetectedNPC)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ [상호작용 지속] {lastDetectedNPC.GetName()} - EyeTracking 지속 감지");
                }
                HandleNPCInteraction(lastDetectedNPC);
                return;
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👁️ [상호작용 중단] {lastDetectedNPC.GetName()} - EyeTracking 상실");
                }
            }
        }
        
        // EyeTracking으로 감지되지 않으면 상호작용 종료
        if (enableDebugLogs)
        {
            Debug.Log($"👁️ [EyeTracking 전용] 감지된 NPC 없음 - 상호작용 없음");
        }
        
        HandleNPCInteraction(null);
    }
    
    // 🔥 EyeTrackingRay에서 현재 감지된 NPC 가져오기
    private NPCController GetNPCFromEyeTracking()
    {
        if (eyeTrackingRay == null) return null;
        
        EyeInteractable currentActive = eyeTrackingRay.GetCurrentActiveInteractable();
        if (currentActive == null) return null;
        
        // NPCEmotionController에서 NPCController 찾기
        NPCEmotionController emotionController = currentActive.GetComponent<NPCEmotionController>();
        if (emotionController == null)
        {
            emotionController = currentActive.GetComponentInParent<NPCEmotionController>();
        }
        
        if (emotionController != null)
        {
            NPCController npcController = emotionController.GetComponent<NPCController>();
            if (npcController == null)
            {
                npcController = emotionController.GetComponentInParent<NPCController>();
            }
            
            if (enableDebugLogs && npcController != null)
            {
                Debug.Log($"✅ [EyeTracking NPC] {npcController.GetName()}");
            }
            
            return npcController;
        }
        
        return null;
    }
    
    // NPC 상호작용 처리 로직
    private void HandleNPCInteraction(NPCController detectedNPC)
    {
        if (detectedNPC != null)
        {
            // 새로운 NPC를 보기 시작한 경우
            if (currentInteractingNPC != detectedNPC)
            {
                StartNewInteraction(detectedNPC);
            }
            // 기존 NPC와 계속 상호작용 중인 경우는 ContinueInteraction에서 처리
        }
        else if (isInteracting)
        {
            // ⭐ 즉시 종료하지 말고, ContinueInteraction에서 딜레이 처리하도록 변경
            // 감지된 NPC가 없어도 바로 종료하지 않음 - interactionHoldTime 딜레이 적용
            if (enableDebugLogs)
            {
                Debug.Log($"👁️ NPC 감지 안됨 - ContinueInteraction에서 딜레이 처리됨: {currentInteractingNPC?.GetName()}");
            }
            // EndInteraction(); // ⚠️ 이 줄 제거! ContinueInteraction()에서 딜레이 처리
        }
    }
    
    // 🆕 EyeInteractable 강제 해제 메서드 (기존 유지)
    public void ForceResetEyeInteractable(NPCController npc)
    {
        if (npc == null) 
        {
            Debug.LogWarning("ForceResetEyeInteractable: NPC가 null입니다.");
            return;
        }
        
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController == null) 
        {
            Debug.LogWarning($"ForceResetEyeInteractable: {npc.GetName()}에 NPCEmotionController가 없습니다.");
            return;
        }
        
        EyeInteractable eyeInteractable = emotionController.GetEyeInteractable();
        if (eyeInteractable == null) 
        {
            Debug.LogWarning($"ForceResetEyeInteractable: {npc.GetName()}에 EyeInteractable이 없습니다.");
            return;
        }
        
        // EyeInteractable 강제 비활성화
        eyeInteractable.IsHovered = false;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔧 [{npc.GetName()}] EyeInteractable.IsHovered = false 설정");
        }
        
        // EyeTrackingRay에서도 강제 해제
        if (eyeTrackingRay != null)
        {
            try
            {
                // eyeTrackingRay의 eyeInteractables에서 제거
                var eyeInteractablesField = eyeTrackingRay.GetType().GetField("eyeInteractables", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (eyeInteractablesField != null)
                {
                    var eyeInteractablesList = eyeInteractablesField.GetValue(eyeTrackingRay) as System.Collections.Generic.HashSet<EyeInteractable>;
                    if (eyeInteractablesList != null && eyeInteractablesList.Contains(eyeInteractable))
                    {
                        eyeInteractablesList.Remove(eyeInteractable);
                        if (enableDebugLogs)
                        {
                            Debug.Log($"🔧 [{npc.GetName()}] eyeInteractables 리스트에서 제거");
                        }
                    }
                }
                
                // currentActiveInteractable도 null로 설정
                var currentActiveField = eyeTrackingRay.GetType().GetField("currentActiveInteractable", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentActiveField != null)
                {
                    var currentActive = currentActiveField.GetValue(eyeTrackingRay) as EyeInteractable;
                    if (currentActive == eyeInteractable)
                    {
                        currentActiveField.SetValue(eyeTrackingRay, null);
                        if (enableDebugLogs)
                        {
                            Debug.Log($"🔧 [{npc.GetName()}] currentActiveInteractable을 null로 설정");
                        }
                    }
                }
                
                Debug.Log($"🎯 [{npc.GetName()}] EyeInteractable 강제 해제 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ForceResetEyeInteractable 리플렉션 오류: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("EyeTrackingRay가 null입니다. 완전한 해제가 불가능합니다.");
        }
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
        interactionLostTime = 0f; // ⭐ 대기 시간 리셋
        
        // NPC 움직임 정지 및 플레이어 바라보기
        StopNPCMovementAndLookAtPlayer(npc);
        
        // NPC 하이라이트 표시 등의 처리
        HighlightNPC(npc, true);
        
        Debug.Log($"🎯 NPC {npc.GetName()}와(과) 상호작용 시작 (최적화된 감지)");
    }
            
    // 현재 상호작용 지속
    private void ContinueInteraction()
    {
        if (!isInteracting || currentInteractingNPC == null) return;
        
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
        
        if (!emotionMatched)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"😔 감정 불일치로 꼬시기 진행 안됨: {currentInteractingNPC.GetName()}");
            }
            return;
        }
        
        float previousTime = currentInteractionTime;
        currentInteractionTime += Time.deltaTime * fastEmotionBuildupMultiplier;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💖 꼬시기 진행 중: {currentInteractingNPC.GetName()} " +
                    $"({previousTime:F1}s → {currentInteractionTime:F1}s) " +
                    $"목표: {(IsEliteNPC(currentInteractingNPC) ? minInteractionTime : regularNPCSuccessTime):F1}s");
        }
        
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
        
        // Elite NPC 체크
        if (currentInteractionTime >= minInteractionTime && IsEliteNPC(currentInteractingNPC))
        {
            if (ShouldTriggerMiniGame(currentInteractingNPC))
            {
                TriggerMiniGame(currentInteractingNPC);
            }
        }
        // 일반 NPC 체크
        else if (currentInteractionTime >= regularNPCSuccessTime && !IsEliteNPC(currentInteractingNPC))
        {
            CompleteRegularNPCSeduction();
        }
    }
    
    // 🔥 NPC가 여전히 감지되는지 확인
    private bool IsNPCStillDetected(NPCController npc)
    {
        // 1순위: EyeTracking으로 감지되는지 확인
        NPCController eyeTrackedNPC = GetNPCFromEyeTracking();
        if (eyeTrackedNPC == npc)
        {
            return true;
        }
        return false;
    }
    
    // 상호작용 종료
    private void EndInteraction()
    {
        if (currentInteractingNPC != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🛑 상호작용 종료 시작: {currentInteractingNPC.GetName()}");
            }
            
            // EyeInteractable 강제 해제 (가장 먼저!)
            ForceResetEyeInteractable(currentInteractingNPC);
            
            // NPC 상태 초기화
            HighlightNPC(currentInteractingNPC, false);
            
            // NPC 움직임 재개
            ResumeNPCMovementAndBehavior(currentInteractingNPC);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ 상호작용 종료 완료: {currentInteractingNPC.GetName()}");
            }
            
            currentInteractingNPC = null;
        }
        
        // 상태 리셋
        currentInteractionTime = 0f;
        isInteracting = false;
        lastInteractionEndTime = Time.time;
        interactionLostTime = 0f; // ⭐ 대기 시간 리셋
        
        if (enableDebugLogs)
        {
            Debug.Log("🔄 상호작용 상태 완전 리셋");
        }
    }
    
    // NPC 움직임 및 행동 복원
    private void ResumeNPCMovementAndBehavior(NPCController npc)
    {
        if (npc == null) 
        {
            Debug.LogWarning("ResumeNPCMovementAndBehavior: NPC가 null입니다.");
            return;
        }
        
        // 꼬셔진 NPC만 제외하고, 상호작용 중단된 NPC는 무조건 움직임 복원
        if (npc.IsSeduced() && npc.IsGhost()) 
        {
            if (enableDebugLogs)
            {
                Debug.Log($"⏩ [{npc.GetName()}] 이미 꼬셔진 NPC는 움직임 복원하지 않음");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔄 [{npc.GetName()}] 움직임 및 행동 복원 시작");
        }
        
        // 1단계: AutonomousDriver 상호작용 모드 해제
        AutonomousDriver driver = npc.GetComponent<AutonomousDriver>();
        if (driver != null)
        {
            driver.SetInteractionMode(false, null);
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [{npc.GetName()}] AutonomousDriver 상호작용 모드 해제 완료");
            }
        }
        
        // 2단계: NavMeshAgent 재개
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.updateRotation = true; // 🔧 자동 회전 다시 활성화
            agent.isStopped = false;
            if (driver != null)
            {
                agent.speed = driver.GetCurrentMoveSpeed();
            }
            agent.velocity = Vector3.zero;
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [{npc.GetName()}] NavMeshAgent 재개 (속도: {agent.speed}, updateRotation: {agent.updateRotation})");
            }
        }
        
        // 3단계: 애니메이터 속도를 1로 복원
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [{npc.GetName()}] 애니메이션 재개");
            }
        }
        
        Debug.Log($"🎯 [{npc.GetName()}] 모든 움직임 복원 완료!");
    }

    // NPC 움직임 정지 및 플레이어 바라보기
    private void StopNPCMovementAndLookAtPlayer(NPCController npc)
    {
        if (npc == null) return;
        
        if (enableDebugLogs)
        {
            float distanceToPlayer = Vector3.Distance(npc.transform.position, playerTransform.position);
            Debug.Log($"🛑 [{npc.GetName()}] 상호작용 시작 - 거리: {distanceToPlayer:F2}m");
        }
        
        // 1단계: NavMeshAgent 완전 정지 (가장 먼저!)
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updateRotation = false; // 🔧 자동 회전 비활성화 (중요!)
            
            if (enableDebugLogs)
            {
                Debug.Log($"🛑 [{npc.GetName()}] NavMeshAgent 정지 및 회전 제어 해제");
            }
        }
        
        // 2단계: 애니메이터 정지
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 0f;
            // 🔧 루트 모션 확인
            if (animator.applyRootMotion)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ [{npc.GetName()}] 애니메이터 루트 모션이 활성화됨 - 회전 간섭 가능성");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"🛑 [{npc.GetName()}] 애니메이션 일시정지");
            }
        }
        
        // 3단계: AutonomousDriver 상호작용 모드 활성화 (마지막에!)
        AutonomousDriver driver = npc.GetComponent<AutonomousDriver>();
        if (driver != null)
        {
            // 🔧 상세한 디버깅 정보
            if (enableDebugLogs)
            {
                Debug.Log($"🔧 [{npc.GetName()}] AutonomousDriver 상호작용 모드 설정 중...");
                Debug.Log($"   - 플레이어 위치: {playerTransform.position}");
                Debug.Log($"   - NPC 현재 회전: {npc.transform.rotation.eulerAngles}");
                
                // AutonomousDriver 내부 상태 확인 (리플렉션 사용)
                try
                {
                    var isInteractingField = driver.GetType().GetField("isInteracting", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (isInteractingField != null)
                    {
                        bool driverInteracting = (bool)isInteractingField.GetValue(driver);
                        Debug.Log($"   - AutonomousDriver.isInteracting: {driverInteracting}");
                    }
                    
                    var lookAtTargetField = driver.GetType().GetField("lookAtTarget", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (lookAtTargetField != null)
                    {
                        Transform lookAtTarget = lookAtTargetField.GetValue(driver) as Transform;
                        Debug.Log($"   - AutonomousDriver.lookAtTarget: {(lookAtTarget?.name ?? "null")}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"AutonomousDriver 내부 상태 확인 실패: {ex.Message}");
                }
            }
            
            driver.SetInteractionMode(true, playerTransform);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [{npc.GetName()}] AutonomousDriver 상호작용 모드 활성화 완료");
                
                // 설정 후 상태 재확인
                StartCoroutine(CheckLookAtStatusAfterDelay(npc, 0.1f));
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ [{npc.GetName()}] AutonomousDriver 컴포넌트를 찾을 수 없음");
        }
    }
    
    // 🔧 LookAt 상태 지연 확인 (디버깅용)
    private System.Collections.IEnumerator CheckLookAtStatusAfterDelay(NPCController npc, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (npc != null && enableDebugLogs)
        {
            Vector3 npcForward = npc.transform.forward;
            Vector3 directionToPlayer = (playerTransform.position - npc.transform.position).normalized;
            float dotProduct = Vector3.Dot(npcForward, directionToPlayer);
            float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            Debug.Log($"🔍 [{npc.GetName()}] {delay}초 후 바라보기 상태:");
            Debug.Log($"   - NPC Forward: {npcForward}");
            Debug.Log($"   - To Player: {directionToPlayer}");
            Debug.Log($"   - 각도 차이: {angle:F1}°");
            Debug.Log($"   - 바라보고 있음: {angle < 30f}");
            
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Debug.Log($"   - Agent.isStopped: {agent.isStopped}");
                Debug.Log($"   - Agent.updateRotation: {agent.updateRotation}");
                Debug.Log($"   - Agent.velocity: {agent.velocity}");
            }
        }
    }

    // 감정 매칭 체크
    private bool CheckEmotionMatch(NPCController npc)
    {
        if (playerEmotionController == null) return true;
        
        EmotionState playerEmotion = playerEmotionController.GetCurrentEmotion();
        NPCEmotionController npcEmotion = npc.GetComponent<NPCEmotionController>();
        
        if (npcEmotion == null) return true;
        
        EmotionState npcEmotionState = npcEmotion.GetCurrentEmotion();
        bool isMatched = playerEmotion == npcEmotionState;
        
        if (enableDebugLogs)
        {
            Debug.Log($"감정 매칭 체크 - 플레이어: {playerEmotion}, NPC: {npcEmotionState}, 일치: {isMatched}");
        }
        
        return isMatched;
    }
    
    // NPC가 따라오고 있는지 확인
    private bool IsNPCFollowing(NPCController npc)
    {
        if (ZeeeingGaze.FollowerManager.Instance != null)
        {
            return ZeeeingGaze.FollowerManager.Instance.IsNPCFollowing(npc);
        }
        return npc.IsSeduced() || npc.IsGhost();
    }
    
    // Elite NPC인지 확인
    private bool IsEliteNPC(NPCController npc)
    {
        return eliteNPCs.Contains(npc);
    }
    
    // 미니게임 트리거 조건 확인
    private bool ShouldTriggerMiniGame(NPCController npc)
    {
        return miniGameManager != null && !npc.IsSeduced();
    }

    // 미니게임 완료 처리
    private void OnMiniGameCompleted(bool success, int score)
    {
        Debug.Log($"🎮 미니게임 완료: 성공={success}, 점수={score}");
        
        if (miniGameManager != null)
        {
            miniGameManager.OnGameCompleted -= OnMiniGameCompleted;
        }
        
        if (success)
        {
            Debug.Log("✅ 미니게임 성공 - NPC 꼬시기 처리가 자동으로 진행됩니다.");
        }
        else
        {
            Debug.Log("❌ 미니게임 실패 - NPC 움직임 복원 후 상호작용 종료");
            if (currentInteractingNPC != null)
            {
                ResumeNPCMovementAndBehavior(currentInteractingNPC);
            }
        }
        
        EndInteraction();
    }

    // 미니게임 시작
    private void TriggerMiniGame(NPCController npc)
    {
        Debug.Log($"🎮 Elite NPC {npc.GetName()}에 대한 미니게임 시작!");
        
        if (miniGameManager != null)
        {
            MiniGameManager.MiniGameType gameType = DetermineGameType(npc);
            int difficulty = DetermineDifficulty(npc);
            
            bool started = miniGameManager.StartMiniGame(gameType, difficulty, npc);
            
            if (started)
            {
                Debug.Log("✅ 미니게임 시작 성공");
                miniGameManager.OnGameCompleted -= OnMiniGameCompleted;
                miniGameManager.OnGameCompleted += OnMiniGameCompleted;
            }
            else
            {
                Debug.LogWarning("❌ 미니게임 시작 실패. NPC 움직임을 재개합니다.");
                ResumeNPCMovementAndBehavior(npc);
                EndInteraction();
            }
        }
    }

    // NPC 타입에 따른 미니게임 타입 결정
    private MiniGameManager.MiniGameType DetermineGameType(NPCController npc)
    {
        if (npc.UseRandomMiniGame())
        {
            int randomType = UnityEngine.Random.Range(0, 2);
            return (MiniGameManager.MiniGameType)randomType;
        }
        return npc.GetPreferredMiniGameType();
    }

    // NPC 타입에 따른 난이도 결정
    private int DetermineDifficulty(NPCController npc)
    {
        return npc.GetMiniGameDifficulty();
    }
    
    // 일반 NPC 꼬시기 완료
    private void CompleteRegularNPCSeduction()
    {
        Debug.Log($"일반 NPC {currentInteractingNPC.GetName()} 꼬시기 성공! (감정 매칭으로 성공)");
        
        NPCEmotionController emotionController = currentInteractingNPC.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            emotionController.ChangeEmotionState(EmotionState.Happy);
        }
        
        ShowSeductionSuccessVFX(currentInteractingNPC);
        
        currentInteractingNPC.SetSeducedByRegularInteraction();
        currentInteractingNPC.SetGhostMode(true);
        
        EndInteraction();
    }
    
    // 꼬시기 성공 VFX 표시
    private void ShowSeductionSuccessVFX(NPCController npc)
    {
        if (EmotionGazeManager.Instance != null && EmotionGazeManager.Instance.HasDefaultVFXAsset())
        {
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
    private void UpdateInteractionProgress()
    {
        float progress = IsEliteNPC(currentInteractingNPC) ? 
            currentInteractionTime / minInteractionTime : 
            currentInteractionTime / regularNPCSuccessTime;
        
        if (enableDebugLogs)
        {
            Debug.Log($"상호작용 진행도: {progress:P1}");
        }
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
        Debug.Log($"NPCInteractionManager 디버그 로그 {(enable ? "활성화" : "비활성화")} (최적화된 시야각 기반 버전)");
    }
    
    // 상태 확인 메서드들
    public bool IsCurrentlyInteracting() => isInteracting;
    public float GetCurrentInteractionTime() => currentInteractionTime;
    public bool IsInCooldown() => Time.time - lastInteractionEndTime < 1.0f;
    
    // 디버그용 현재 상태 확인 메서드
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== NPCInteractionManager 현재 상태 (EyeTracking 전용) ===");
        Debug.Log($"isInteracting: {isInteracting}");
        Debug.Log($"currentInteractingNPC: {(currentInteractingNPC?.GetName() ?? "null")}");
        Debug.Log($"lastDetectedNPC: {(lastDetectedNPC?.GetName() ?? "null")}");
        Debug.Log($"currentInteractionTime: {currentInteractionTime:F2}s");
        Debug.Log($"검색 모드: EyeTracking 전용 (DotProduct 백업 없음)");
        
        if (eyeTrackingRay != null)
        {
            EyeInteractable currentActive = eyeTrackingRay.GetCurrentActiveInteractable();
            Debug.Log($"EyeTrackingRay CurrentActive: {(currentActive?.gameObject.name ?? "null")}");
            
            NPCController eyeTrackedNPC = GetNPCFromEyeTracking();
            Debug.Log($"EyeTracking으로 감지된 NPC: {(eyeTrackedNPC?.GetName() ?? "null")}");
        }
        else
        {
            Debug.LogError("EyeTrackingRay가 null입니다!");
        }
    }
    
    // EyeTracking 전용 시각화를 위한 Gizmos
    private void OnDrawGizmos()
    {
        if (!showViewAngleGizmos || playerCamera == null) return;
        
        Vector3 playerPos = playerTransform.position;
        Vector3 forward = playerCamera.transform.forward;
        
        // EyeTracking 방향 표시 (간단한 레이)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(playerPos, forward * interactionDistance);
        
        // 상호작용 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPos, interactionDistance);
        
        // 현재 상호작용 중인 NPC 강조
        if (Application.isPlaying && currentInteractingNPC != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentInteractingNPC.transform.position, 1f);
            Gizmos.DrawLine(playerPos, currentInteractingNPC.transform.position);
        }
        
        // EyeTracking으로 감지된 NPC 표시
        if (Application.isPlaying && lastDetectedNPC != null && lastDetectedNPC != currentInteractingNPC)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastDetectedNPC.transform.position, 0.5f);
        }
    }
}