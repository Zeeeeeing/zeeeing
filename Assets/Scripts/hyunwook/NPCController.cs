using UnityEngine;
using UnityEngine.AI;

namespace ZeeeingGaze
{
    public class NPCController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NPCEmotionController emotionController;
        [SerializeField] private NavMeshObstacle navMeshObstacle;
        [SerializeField] private Animator animator;
        
        [Header("NPC Data")]
        [SerializeField] private string npcName;
        [SerializeField] private int npcLevel = 1; // 난이도/레벨
        [SerializeField] private int pointValue = 100; // 꼬셔졌을 때 획득 점수
        [SerializeField] private bool isEliteNPC = false; // 미니게임이 필요한 NPC인지

        [Header("MiniGame Settings")]
        [SerializeField] private MiniGameManager.MiniGameType preferredMiniGameType = MiniGameManager.MiniGameType.ColorGaze;
        [SerializeField] private int miniGameDifficulty = 1; // 0: 쉬움, 1: 보통, 2: 어려움
        [SerializeField] private bool useRandomMiniGame = false; // 랜덤 미니게임 사용 여부
        
        [Header("State")]
        [SerializeField] private bool isSeduced = false; // 꼬셔진 상태인지
        [SerializeField] private bool isGhost = false; // 유령 모드인지
        
        // 현재 활성화된 행동
        private MonoBehaviour currentActiveBehavior;
        // 재귀 호출 방지용 플래그
        private bool isProcessingStateChange = false;
        
        private void Awake()
        {
            // 컴포넌트 자동 할당 (없을 경우)
            if (emotionController == null)
                emotionController = GetComponent<NPCEmotionController>();
                
            if (navMeshObstacle == null)
                navMeshObstacle = GetComponent<NavMeshObstacle>();
                
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        // 미니게임 관련 속성 접근자
        public MiniGameManager.MiniGameType GetPreferredMiniGameType() => preferredMiniGameType;
        public int GetMiniGameDifficulty() => miniGameDifficulty;
        public bool UseRandomMiniGame() => useRandomMiniGame;
        
        // NPC를 '꼬셔진' 상태로 만듦
        public void SetSeduced()
        {
            // 재귀 호출 방지
            if (isProcessingStateChange) return;
            isProcessingStateChange = true;
            
            try
            {
                isSeduced = true;
                
                // 감정 상태 행복으로 변경
                if (emotionController != null)
                {
                    emotionController.ChangeEmotionState(EmotionState.Happy);
                }
                
                // 애니메이션 설정 (IsSeduced 파라미터 사용)
                if (animator != null)
                {
                    try
                    {
                        bool hasSeducedParam = false;
                        
                        // IsSeduced 파라미터 확인 및 설정
                        foreach (AnimatorControllerParameter param in animator.parameters)
                        {
                            if (param.name == "IsSeduced" && param.type == AnimatorControllerParameterType.Bool)
                            {
                                animator.SetBool("IsSeduced", true);
                                hasSeducedParam = true;
                                Debug.Log($"[{gameObject.name}] IsSeduced 파라미터 설정됨");
                                break;
                            }
                        }
                        
                        // 파라미터가 없으면 상태명으로 직접 재생 시도
                        if (!hasSeducedParam && animator.layerCount > 0)
                        {
                            try
                            {
                                animator.Play("Seduced", 0);
                                Debug.Log($"[{gameObject.name}] Seduced 애니메이션 직접 재생");
                            }
                            catch (System.Exception)
                            {
                                // Seduced 없으면 Happy 시도
                                try
                                {
                                    animator.Play("Happy", 0);
                                    Debug.Log($"[{gameObject.name}] Happy 애니메이션 대체 재생");
                                }
                                catch (System.Exception)
                                {
                                    // Happy도 없으면 Idle 시도
                                    try
                                    {
                                        animator.Play("Idle", 0);
                                        Debug.Log($"[{gameObject.name}] Idle 애니메이션으로 폴백");
                                    }
                                    catch (System.Exception)
                                    {
                                        // 무시
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[{gameObject.name}] 애니메이션 설정 중 오류: {e.Message}");
                    }
                }
                
                // 이벤트 발생
                OnNPCSeduced?.Invoke(this);
            }
            finally
            {
                // 처리 완료
                isProcessingStateChange = false;
            }
        }
        
        // 유령 모드 설정 (꼬셔진 후 따라다니는 상태)
        public void SetGhostMode(bool enabled)
        {
            // 재귀 호출 방지 및 중복 호출 방지
            if (isProcessingStateChange || isGhost == enabled) return;
            isProcessingStateChange = true;
            
            try
            {
                isGhost = enabled;
                
                // NavMesh 및 물리 컴포넌트 제어
                if (navMeshObstacle != null) navMeshObstacle.enabled = !enabled;
                
                AutonomousDriver driver = GetComponent<AutonomousDriver>();
                if (driver != null) driver.enabled = !enabled;
                
                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = !enabled;
                }
                
                // 유령 시각 효과 적용
                UpdateGhostVisuals(enabled);
                
                // 애니메이션 설정
                if (animator != null)
                {
                    try
                    {
                        bool hasFollowingParam = false;
                        bool hasGhostParam = false;
                        
                        // 먼저 애니메이터 파라미터 확인
                        foreach (AnimatorControllerParameter param in animator.parameters)
                        {
                            if (param.name == "IsFollowing" && param.type == AnimatorControllerParameterType.Bool)
                            {
                                animator.SetBool("IsFollowing", enabled);
                                hasFollowingParam = true;
                                Debug.Log($"[{gameObject.name}] IsFollowing 파라미터 설정됨: {enabled}");
                            }
                            else if (param.name == "IsGhost" && param.type == AnimatorControllerParameterType.Bool)
                            {
                                animator.SetBool("IsGhost", enabled);
                                hasGhostParam = true;
                                Debug.Log($"[{gameObject.name}] IsGhost 파라미터 설정됨: {enabled}");
                            }
                        }
                        
                        // 적절한 파라미터가 없으면 직접 상태 재생
                        if (!hasFollowingParam && !hasGhostParam && animator.layerCount > 0)
                        {
                            if (enabled)
                            {
                                // 유령 상태 활성화 시
                                try
                                {
                                    animator.Play("Following", 0);
                                    Debug.Log($"[{gameObject.name}] Following 애니메이션 직접 재생");
                                }
                                catch (System.Exception)
                                {
                                    try
                                    {
                                        animator.Play("Ghost", 0);
                                        Debug.Log($"[{gameObject.name}] Ghost 애니메이션 직접 재생");
                                    }
                                    catch (System.Exception)
                                    {
                                        try
                                        {
                                            animator.Play("Idle", 0);
                                            Debug.Log($"[{gameObject.name}] Idle 애니메이션으로 폴백");
                                        }
                                        catch (System.Exception)
                                        {
                                            // 무시
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // 유령 상태 비활성화 시
                                try
                                {
                                    animator.Play("Idle", 0);
                                    Debug.Log($"[{gameObject.name}] Idle 애니메이션 직접 재생");
                                }
                                catch (System.Exception)
                                {
                                    // 무시
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[{gameObject.name}] 애니메이션 설정 중 오류: {e.Message}");
                    }
                }
                
                // 팔로워 관리자 등록/해제
                if (enabled)
                {
                    if (FollowerManager.Instance != null)
                        FollowerManager.Instance.RegisterFollower(this);
                }
                else
                {
                    if (FollowerManager.Instance != null)
                        FollowerManager.Instance.UnregisterFollower(this);
                }
            }
            finally
            {
                // 처리 완료
                isProcessingStateChange = false;
            }
        }
        
        // 애니메이션 트리거 시도 (헬퍼 메서드)
        private void TryPlayAnimationTrigger(string triggerName)
        {
            if (animator == null) return;
            
            try
            {
                // 해당 이름의 트리거가 있는지 확인
                bool hasTrigger = false;
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.SetTrigger(triggerName);
                        hasTrigger = true;
                        return;
                    }
                }
                
                // 없으면 기본 상태 활성화 시도
                if (!hasTrigger)
                {
                    bool hasStateParam = false;
                    foreach (AnimatorControllerParameter param in animator.parameters)
                    {
                        if (param.name == "State" && param.type == AnimatorControllerParameterType.Int)
                        {
                            // Ghost = 1, Happy = 2 등으로 매핑
                            int stateValue = 0;
                            switch (triggerName)
                            {
                                case "Happy": stateValue = 2; break;
                                case "Ghost": stateValue = 1; break;
                                case "Neutral": stateValue = 0; break;
                            }
                            
                            animator.SetInteger("State", stateValue);
                            hasStateParam = true;
                            return;
                        }
                    }
                    
                    // 아무 파라미터도 없으면 기본 애니메이션 재생
                    if (!hasStateParam && animator.layerCount > 0)
                    {
                        try
                        {
                            animator.Play("Idle", 0); // 기본 애니메이션
                        }
                        catch (System.Exception)
                        {
                            // 무시
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 애니메이션 트리거 시도 중 오류: {e.Message}");
            }
        }
        
        // 유령 시각 효과 업데이트
        private void UpdateGhostVisuals(bool enabled)
        {
            try
            {
                // 렌더러 찾기
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    
                    foreach (Material material in renderer.materials)
                    {
                        if (material == null) continue;
                        
                        // 투명도 설정
                        if (material.HasProperty("_Color"))
                        {
                            Color color = material.color;
                            color.a = enabled ? 0.7f : 1.0f;
                            material.color = color;
                        }
                        
                        // 렌더 모드 설정 (투명/불투명)
                        if (enabled)
                        {
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            material.SetInt("_ZWrite", 0);
                            material.DisableKeyword("_ALPHATEST_ON");
                            material.EnableKeyword("_ALPHABLEND_ON");
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            material.renderQueue = 3000;
                        }
                        else
                        {
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                            material.SetInt("_ZWrite", 1);
                            material.DisableKeyword("_ALPHATEST_ON");
                            material.DisableKeyword("_ALPHABLEND_ON");
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            material.renderQueue = -1;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 유령 시각 효과 업데이트 중 오류: {e.Message}");
            }
        }
        
        // 행동 설정 (기존 AutonomousDriver 등의 행동 스크립트 활성화/비활성화)
        public void SetActiveBehavior(MonoBehaviour behavior)
        {
            if (behavior == null) return;
            
            try
            {
                // 현재 활성화된 행동 비활성화
                if (currentActiveBehavior != null && currentActiveBehavior != behavior)
                {
                    currentActiveBehavior.enabled = false;
                }
                
                // 새 행동 활성화
                behavior.enabled = true;
                currentActiveBehavior = behavior;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 행동 설정 중 오류: {e.Message}");
            }
        }
        
        // NPC 관련 속성 접근자
        public string GetName() => npcName;
        public int GetLevel() => npcLevel;
        public int GetPointValue() => pointValue;
        public bool IsEliteNPC() => isEliteNPC;
        public bool IsSeduced() => isSeduced;
        public bool IsGhost() => isGhost;
        
        // UI 접근을 위한 public 메서드 (MiniGameManager에서 사용)
        public void HideGameUI()
        {
            // 구현 필요 없음 - 이 클래스는 게임 UI를 직접 다루지 않음
        }
        
        // gameUI 상태 확인용 메서드 (MiniGameManager에서 사용)
        public bool IsGameUIActive()
        {
            // 구현 필요 없음 - 이 클래스는 게임 UI를 직접 다루지 않음
            return false;
        }

        // 감정 컨트롤러 참조 얻기
        public NPCEmotionController GetEmotionController() => emotionController;
        
        // 이벤트
        public event System.Action<NPCController> OnNPCSeduced;
    }
}