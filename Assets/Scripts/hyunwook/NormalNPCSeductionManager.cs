using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZeeeingGaze;

public class NormalNPCSeductionManager : MonoBehaviour
{
    [Header("Seduction Settings")]
    [SerializeField] private float requiredGazeTime = 3.0f; // 응시해야 하는 시간
    [SerializeField] private float emotionMatchBonus = 1.5f; // 감정 일치시 보너스 (시간 단축)
    [SerializeField] private float detectionDistance = 5.0f; // 감지 거리
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showProgressIndicator = true;
    [SerializeField] private GameObject progressIndicatorPrefab; // 진행도 표시 UI
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 현재 처리 중인 NPC들
    private Dictionary<NPCController, SeductionProgress> activeSeductions = new Dictionary<NPCController, SeductionProgress>();
    
    // 참조
    private PlayerEmotionController playerEmotionController;
    private Transform playerTransform;
    private NPCInteractionManager npcInteractionManager;
    
    // 진행도 추적 클래스
    private class SeductionProgress
    {
        public float currentTime = 0f;
        public float requiredTime = 0f;
        public bool isInProgress = false;
        public GameObject progressUI = null;
    }
    
    private void Start()
    {
        // 필요한 참조 찾기
        playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
        playerTransform = Camera.main.transform;
        npcInteractionManager = FindAnyObjectByType<NPCInteractionManager>();
        
        if (playerEmotionController == null)
        {
            Debug.LogWarning("PlayerEmotionController를 찾을 수 없습니다.");
        }
        
        Debug.Log("SimpleNPCSeductionManager 초기화 완료");
    }
    
    private void Update()
    {
        ProcessActiveSeductions();
        CheckForNewNPCs();
        CleanupCompletedSeductions();
    }
    
    private void ProcessActiveSeductions()
    {
        var toRemove = new List<NPCController>();
        
        foreach (var kvp in activeSeductions)
        {
            NPCController npc = kvp.Key;
            SeductionProgress progress = kvp.Value;
            
            if (npc == null || npc.IsSeduced())
            {
                toRemove.Add(npc);
                continue;
            }
            
            // 플레이어가 NPC를 바라보고 있는지 확인
            if (IsPlayerLookingAtNPC(npc))
            {
                if (!progress.isInProgress)
                {
                    StartSeduction(npc, progress);
                }
                
                // 시간 증가
                float timeMultiplier = GetEmotionMatchMultiplier(npc);
                progress.currentTime += Time.deltaTime * timeMultiplier;
                
                // 진행도 UI 업데이트
                UpdateProgressUI(npc, progress);
                
                // 완료 체크
                if (progress.currentTime >= progress.requiredTime)
                {
                    CompleteSeduction(npc);
                    toRemove.Add(npc);
                }
            }
            else
            {
                // 시선이 벗어나면 진행도 감소
                if (progress.isInProgress)
                {
                    progress.currentTime -= Time.deltaTime * 2f; // 빠르게 감소
                    progress.currentTime = Mathf.Max(0f, progress.currentTime);
                    
                    if (progress.currentTime <= 0f)
                    {
                        StopSeduction(npc, progress);
                    }
                    
                    UpdateProgressUI(npc, progress);
                }
            }
        }
        
        // 완료되거나 무효한 NPC 제거
        foreach (var npc in toRemove)
        {
            RemoveSeduction(npc);
        }
    }
    
    private void CheckForNewNPCs()
    {
        // GameFlowManager에서 씬 NPC들 가져오기
        GameFlowManager gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        if (gameFlowManager == null) return;
        
        // 리플렉션으로 sceneNPCs 접근 (또는 public 메서드 추가)
        var sceneNPCsField = gameFlowManager.GetType().GetField("sceneNPCs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (sceneNPCsField != null)
        {
            var sceneNPCs = sceneNPCsField.GetValue(gameFlowManager) as List<NPCController>;
            
            if (sceneNPCs != null)
            {
                foreach (NPCController npc in sceneNPCs)
                {
                    if (ShouldProcessNPC(npc))
                    {
                        if (!activeSeductions.ContainsKey(npc))
                        {
                            RegisterNPC(npc);
                        }
                    }
                }
            }
        }
    }
    
    private bool ShouldProcessNPC(NPCController npc)
    {
        if (npc == null || npc.IsSeduced()) return false;
        
        // Elite NPC는 제외 (미니게임으로 처리)
        if (npc.IsEliteNPC()) return false;
        
        // NPCInteractionManager의 Elite 리스트에 있는지 확인
        if (npcInteractionManager != null)
        {
            var eliteNPCsField = npcInteractionManager.GetType().GetField("eliteNPCs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (eliteNPCsField != null)
            {
                var eliteNPCs = eliteNPCsField.GetValue(npcInteractionManager) as List<NPCController>;
                if (eliteNPCs != null && eliteNPCs.Contains(npc))
                {
                    return false; // Elite NPC는 제외
                }
            }
        }
        
        // 거리 체크
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
            return distance <= detectionDistance;
        }
        
        return true;
    }
    
    private void RegisterNPC(NPCController npc)
    {
        var progress = new SeductionProgress
        {
            requiredTime = requiredGazeTime,
            currentTime = 0f,
            isInProgress = false
        };
        
        activeSeductions[npc] = progress;
        
        if (enableDebugLogs)
        {
            Debug.Log($"일반 NPC 등록: {npc.GetName()}");
        }
    }
    
    private bool IsPlayerLookingAtNPC(NPCController npc)
    {
        if (playerTransform == null || npc == null) return false;
        
        // 거리 체크
        float distance = Vector3.Distance(playerTransform.position, npc.transform.position);
        if (distance > detectionDistance) return false;
        
        // 시선 방향 체크
        Vector3 directionToNPC = (npc.transform.position - playerTransform.position).normalized;
        float dotProduct = Vector3.Dot(playerTransform.forward, directionToNPC);
        
        // EyeInteractable 체크 (더 정확한 시선 감지)
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            EyeInteractable eyeInteractable = emotionController.GetEyeInteractable();
            if (eyeInteractable != null)
            {
                return eyeInteractable.IsHovered;
            }
        }
        
        // EyeInteractable이 없으면 기본 시선 방향으로 판정 (더 관대하게)
        return dotProduct > 0.6f; // 약 53도 이내
    }
    
    private float GetEmotionMatchMultiplier(NPCController npc)
    {
        if (playerEmotionController == null) return 1f;
        
        // NPC의 현재 감정과 플레이어 감정 비교
        NPCEmotionController npcEmotion = npc.GetComponent<NPCEmotionController>();
        if (npcEmotion != null)
        {
            EmotionState playerEmotion = playerEmotionController.GetCurrentEmotion();
            EmotionState npcEmotionState = npcEmotion.GetCurrentEmotion();
            
            // 감정이 일치하면 보너스
            if (playerEmotion == npcEmotionState)
            {
                return emotionMatchBonus;
            }
            
            // 호환되는 감정 조합 (예: 플레이어 Happy + NPC Sad = 위로 효과)
            if ((playerEmotion == EmotionState.Happy && npcEmotionState == EmotionState.Sad) ||
                (playerEmotion == EmotionState.Sad && npcEmotionState == EmotionState.Happy))
            {
                return emotionMatchBonus * 0.8f;
            }
        }
        
        return 1f;
    }
    
    private void StartSeduction(NPCController npc, SeductionProgress progress)
    {
        progress.isInProgress = true;
        
        // 진행도 UI 생성
        if (showProgressIndicator && progressIndicatorPrefab != null)
        {
            Vector3 uiPosition = npc.transform.position + Vector3.up * 2f;
            progress.progressUI = Instantiate(progressIndicatorPrefab, uiPosition, Quaternion.identity);
            progress.progressUI.transform.SetParent(npc.transform);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"NPC {npc.GetName()} 꼬시기 시작");
        }
    }
    
    private void StopSeduction(NPCController npc, SeductionProgress progress)
    {
        progress.isInProgress = false;
        
        // 진행도 UI 제거
        if (progress.progressUI != null)
        {
            Destroy(progress.progressUI);
            progress.progressUI = null;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"NPC {npc.GetName()} 꼬시기 중단");
        }
    }
    
    private void CompleteSeduction(NPCController npc)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"NPC {npc.GetName()} 꼬시기 성공!");
        }
        
        // NPC를 꼬셔진 상태로 변경
        npc.SetSeduced();
        npc.SetGhostMode(true);
        
        // 감정 상태를 행복으로 변경
        NPCEmotionController emotionController = npc.GetComponent<NPCEmotionController>();
        if (emotionController != null)
        {
            emotionController.ChangeEmotionState(EmotionState.Happy);
        }
        
        // 성공 효과 (파티클, 사운드 등)
        PlaySuccessEffect(npc);
    }
    
    private void PlaySuccessEffect(NPCController npc)
    {
        // 여기에 성공 효과 추가 (파티클, 사운드 등)
        // 예: 하트 파티클 생성
        
        // 컨트롤러 진동
        try
        {
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, 0.7f, 0.2f);
            }
        }
        catch (System.Exception) { }
    }
    
    private void UpdateProgressUI(NPCController npc, SeductionProgress progress)
    {
        if (progress.progressUI == null) return;
        
        // 진행률 계산
        float progressRatio = progress.currentTime / progress.requiredTime;
        progressRatio = Mathf.Clamp01(progressRatio);
        
        // UI 업데이트 (Slider, Image 등)
        Slider progressSlider = progress.progressUI.GetComponent<Slider>();
        if (progressSlider != null)
        {
            progressSlider.value = progressRatio;
        }
        
        UnityEngine.UI.Image progressImage = progress.progressUI.GetComponent<UnityEngine.UI.Image>();
        if (progressImage != null)
        {
            progressImage.fillAmount = progressRatio;
        }
    }
    
    private void RemoveSeduction(NPCController npc)
    {
        if (activeSeductions.TryGetValue(npc, out SeductionProgress progress))
        {
            if (progress.progressUI != null)
            {
                Destroy(progress.progressUI);
            }
            
            activeSeductions.Remove(npc);
        }
    }
    
    private void CleanupCompletedSeductions()
    {
        var toRemove = new List<NPCController>();
        
        foreach (var kvp in activeSeductions)
        {
            if (kvp.Key == null || kvp.Key.IsSeduced())
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var npc in toRemove)
        {
            RemoveSeduction(npc);
        }
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SetRequiredGazeTime(float time)
    {
        requiredGazeTime = Mathf.Max(0.5f, time);
    }
    
    public void SetEmotionMatchBonus(float bonus)
    {
        emotionMatchBonus = Mathf.Max(1f, bonus);
    }
    
    public int GetActiveSeductionCount()
    {
        return activeSeductions.Count;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        
        // 감지 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, detectionDistance);
        
        // 활성 꼬시기 대상 표시
        Gizmos.color = Color.red;
        foreach (var kvp in activeSeductions)
        {
            if (kvp.Key != null && kvp.Value.isInProgress)
            {
                Gizmos.DrawLine(playerTransform.position, kvp.Key.transform.position);
            }
        }
    }
}