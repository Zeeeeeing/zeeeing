using UnityEngine;
using UnityEngine.AI;

namespace ZeeeingGaze
{
    public class NPCController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NPCEmotionController emotionController;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private NavMeshObstacle navMeshObstacle;
        [SerializeField] private Animator animator;
        
        [Header("NPC Data")]
        [SerializeField] private string npcName;
        [SerializeField] private int npcLevel = 1; // 난이도/레벨
        [SerializeField] private int pointValue = 100; // 꼬셔졌을 때 획득 점수 (기본 꼬시기 점수)
        [SerializeField] private bool isEliteNPC = false; // 미니게임이 필요한 NPC인지

        [Header("MiniGame Settings")]
        [SerializeField] private MiniGameManager.MiniGameType preferredMiniGameType = MiniGameManager.MiniGameType.ColorGaze;
        [SerializeField] private int miniGameDifficulty = 1; // 0: 쉬움, 1: 보통, 2: 어려움
        [SerializeField] private int miniGameBonusScore = 200; // 미니게임 성공 시 추가 점수
        [SerializeField] private bool useRandomMiniGame = false; // 랜덤 미니게임 사용 여부
        
        [Header("State")]
        [SerializeField] private bool isSeduced = false; // 꼬셔진 상태인지
        [SerializeField] private bool isGhost = false; // 유령 모드인지
        [SerializeField] private bool miniGameCompleted = false; // 미니게임 완료 여부
        
        [Header("Fever Mode Integration")]
        [SerializeField] private int feverModePointMultiplier = 2; // 피버 모드 중 추가 점수 배율

        [Header("VFX Settings")]
        [SerializeField] private GameObject persistentVFXPrefab; // 지속적으로 표시할 VFX 프리팹
        private GameObject currentPersistentVFX; // 현재 활성화된 지속 VFX
        
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
        
        // ⭐ 수정: CalculateFinalScore 메서드 제거 또는 사용하지 않음
        // 이제 MiniGameManager에서만 피버모드 배율을 적용합니다.

        /// <summary>
        /// 디버그용 피버모드 점수 계산 (참고용으로만 사용)
        /// </summary>
        private int CalculateFinalScore(int baseScore)
        {
            // 이 메서드는 이제 디버그 목적으로만 사용됩니다.
            // 실제 점수 계산은 MiniGameManager에서 처리됩니다.
            
            if (MiniGameManager.Instance != null && MiniGameManager.Instance.IsFeverModeActive())
            {
                int multipliedScore = baseScore * feverModePointMultiplier;
                Debug.Log($"🔥 [{gameObject.name}] 디버그: 피버모드 점수 계산 {baseScore} × {feverModePointMultiplier} = {multipliedScore}");
                return multipliedScore;
            }
            
            return baseScore;
        }
        
        /// <summary>
        /// 일반 꼬시기로 NPC를 꼬셔진 상태로 만듦 (MiniGameManager에서 피버모드 배율 적용)
        /// </summary>
        public void SetSeducedByRegularInteraction()
        {
            if (isSeduced) return;
            
            isSeduced = true;
            miniGameCompleted = false;
            
            // 꼬시기 점수 추가
            if (MiniGameManager.Instance != null)
            {
                bool isFeverMode = MiniGameManager.Instance.IsFeverModeActive();
                int finalScore = pointValue * (isFeverMode ? feverModePointMultiplier : 1);
                MiniGameManager.Instance.AddScore(finalScore, $"NPC_Seduced_{gameObject.name}");
                
                Debug.Log($"[{gameObject.name}] 일반 꼬시기 점수 추가: {finalScore}" + 
                        (isFeverMode ? $" (피버 모드 {feverModePointMultiplier}배)" : ""));
            }
            
            // 애니메이션 및 상태 설정
            SetSeducedAnimation();
            
            // 지속적 VFX 시작 - 새로 추가
            StartPersistentVFX();
            
            // 이벤트 알림
            OnNPCSeduced?.Invoke(this);
            
            Debug.Log($"[{gameObject.name}] 일반 상호작용으로 꼬셔졌습니다! 점수: {pointValue}");
        }
        
        /// <summary>
        /// 미니게임 성공으로 NPC를 꼬셔진 상태로 만듦 (MiniGameManager에서 피버모드 배율 적용)
        /// </summary>
        public void SetSeducedByMiniGame()
        {
            if (isSeduced) return;
            
            isSeduced = true;
            miniGameCompleted = true;
            
            // 기본 꼬시기 점수 + 미니게임 보너스 점수
            if (MiniGameManager.Instance != null)
            {
                bool isFeverMode = MiniGameManager.Instance.IsFeverModeActive();
                int multiplier = isFeverMode ? feverModePointMultiplier : 1;
                
                int seductionScore = pointValue * multiplier;
                int bonusScore = miniGameBonusScore * multiplier;
                int totalScore = seductionScore + bonusScore;
                
                MiniGameManager.Instance.AddScore(totalScore, $"NPC_MiniGame_{gameObject.name}");
                
                Debug.Log($"[{gameObject.name}] 미니게임 꼬시기 점수 추가: {totalScore} " +
                        $"(꼬시기: {seductionScore}, 보너스: {bonusScore})" +
                        (isFeverMode ? $" (피버 모드 {feverModePointMultiplier}배)" : ""));
            }
            
            // 애니메이션 및 상태 설정
            SetSeducedAnimation();
            
            // 지속적 VFX 시작 - 새로 추가
            StartPersistentVFX();
            
            // 이벤트 알림
            OnNPCSeduced?.Invoke(this);
            
            Debug.Log($"[{gameObject.name}] 미니게임으로 꼬셔졌습니다! " +
                    $"꼬시기: {pointValue}점, 보너스: {miniGameBonusScore}점");
        }

        private void StartPersistentVFX()
        {
            // 기존 VFX가 있으면 제거
            if (currentPersistentVFX != null)
            {
                Destroy(currentPersistentVFX);
            }
            
            // EmotionGazeManager에서 CriticalHitVFXPrefab 가져오기
            if (EmotionGazeManager.Instance != null)
            {
                var criticalHitVFXPrefab = EmotionGazeManager.Instance.GetType()
                    .GetField("criticalHitVFXPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(EmotionGazeManager.Instance) as GameObject;
                
                if (criticalHitVFXPrefab != null)
                {
                    Vector3 vfxPosition = transform.position + Vector3.up * 1.5f;
                    currentPersistentVFX = Instantiate(criticalHitVFXPrefab, vfxPosition, Quaternion.identity);
                    
                    // VFX를 NPC에 부착하여 함께 이동하도록 설정
                    currentPersistentVFX.transform.SetParent(transform, true);
                    
                    // 파티클 시스템을 루핑으로 설정
                    ParticleSystem[] particles = currentPersistentVFX.GetComponentsInChildren<ParticleSystem>();
                    foreach (var particle in particles)
                    {
                        var main = particle.main;
                        main.loop = true;
                        main.startLifetime = 2.0f; // 개별 파티클 수명
                    }
                    
                    Debug.Log($"[{gameObject.name}] 지속적 VFX 시작됨");
                }
            }
            
            // 대체 VFX가 설정되어 있다면 사용
            if (currentPersistentVFX == null && persistentVFXPrefab != null)
            {
                Vector3 vfxPosition = transform.position + Vector3.up * 1.5f;
                currentPersistentVFX = Instantiate(persistentVFXPrefab, vfxPosition, Quaternion.identity);
                currentPersistentVFX.transform.SetParent(transform, true);
            }
        }

        private void StopPersistentVFX()
        {
            if (currentPersistentVFX != null)
            {
                Destroy(currentPersistentVFX);
                currentPersistentVFX = null;
                Debug.Log($"[{gameObject.name}] 지속적 VFX 중지됨");
            }
        }
        
        /// <summary>
        /// 레거시 호환성을 위한 메서드 (기존 코드에서 호출하는 경우)
        /// </summary>
        public void SetSeduced()
        {
            // Elite NPC인지 확인하여 적절한 메서드 호출
            if (isEliteNPC && !miniGameCompleted)
            {
                Debug.LogWarning($"[{gameObject.name}] Elite NPC는 미니게임 완료 후에만 꼬셔질 수 있습니다!");
                return;
            }
            
            SetSeducedByRegularInteraction();
        }
        
        /// <summary>
        /// 꼬셔진 상태의 애니메이션 설정 (리팩토링)
        /// </summary>
        private void SetSeducedAnimation()
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
                    TryPlayAnimationState("Seduced", "Happy", "Idle");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 애니메이션 설정 중 오류: {e.Message}");
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
                
                Debug.Log($"[{gameObject.name}] 유령 모드 설정: {enabled}");
                
                // NavMesh 및 물리 컴포넌트 제어
                if (navMeshObstacle != null) navMeshObstacle.enabled = !enabled;
                if (navMeshAgent != null) navMeshAgent.enabled = !enabled;
                if (animator!= null) animator.enabled = !enabled;

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
                    SetGhostModeAnimation(enabled);
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
        
        /// <summary>
        /// 유령 모드 애니메이션 설정 (리팩토링)
        /// </summary>
        private void SetGhostModeAnimation(bool enabled)
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
                        TryPlayAnimationState("Following", "Ghost", "Idle");
                    }
                    else
                    {
                        TryPlayAnimationState("Idle");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 애니메이션 설정 중 오류: {e.Message}");
            }
        }
        
        /// <summary>
        /// 애니메이션 상태 재생 시도 (헬퍼 메서드)
        /// </summary>
        private void TryPlayAnimationState(params string[] stateNames)
        {
            if (animator == null || animator.layerCount == 0) return;
            
            foreach (string stateName in stateNames)
            {
                try
                {
                    animator.Play(stateName, 0);
                    Debug.Log($"[{gameObject.name}] {stateName} 애니메이션 재생 성공");
                    return; // 성공하면 종료
                }
                catch (System.Exception)
                {
                    // 해당 상태가 없으면 다음 상태 시도
                    continue;
                }
            }
            
            Debug.LogWarning($"[{gameObject.name}] 재생할 수 있는 애니메이션 상태를 찾을 수 없습니다: {string.Join(", ", stateNames)}");
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
                            SetMaterialTransparent(material);
                        }
                        else
                        {
                            SetMaterialOpaque(material);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{gameObject.name}] 유령 시각 효과 업데이트 중 오류: {e.Message}");
            }
        }
        
        /// <summary>
        /// 머티리얼을 투명 모드로 설정
        /// </summary>
        private void SetMaterialTransparent(Material material)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        
        /// <summary>
        /// 머티리얼을 불투명 모드로 설정
        /// </summary>
        private void SetMaterialOpaque(Material material)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
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
        public int GetMiniGameBonusScore() => miniGameBonusScore;
        public bool IsEliteNPC() => isEliteNPC;
        public bool IsSeduced() => isSeduced;
        public bool IsGhost() => isGhost;
        public bool IsMiniGameCompleted() => miniGameCompleted;
        
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
        
        // 피버 모드 점수 배율 설정
        public void SetFeverModePointMultiplier(int multiplier)
        {
            feverModePointMultiplier = Mathf.Max(1, multiplier);
            Debug.Log($"[{gameObject.name}] 피버 모드 점수 배율 설정: {feverModePointMultiplier}");
        }
        
        // 현재 피버 모드 점수 배율 얻기
        public int GetFeverModePointMultiplier() => feverModePointMultiplier;
        
        // 디버그용 수동 꼬시기 메서드
        [ContextMenu("Debug: Seduce This NPC (Regular)")]
        public void DebugSeduceNPCRegular()
        {
            Debug.Log($"🎯 [{gameObject.name}] 디버그 일반 꼬시기 실행! 점수: {pointValue}");
            SetSeducedByRegularInteraction();
        }
        
        [ContextMenu("Debug: Seduce This NPC (MiniGame)")]
        public void DebugSeduceNPCMiniGame()
        {
            Debug.Log($"🎮 [{gameObject.name}] 디버그 미니게임 꼬시기 실행! 꼬시기: {pointValue}점, 보너스: {miniGameBonusScore}점");
            SetSeducedByMiniGame();
        }
        
        // 디버그용 점수 체크
        [ContextMenu("Debug: Check Score Status")]
        public void DebugCheckScoreStatus()
        {
            bool isFeverMode = MiniGameManager.Instance != null ? MiniGameManager.Instance.IsFeverModeActive() : false;
            int currentTotalScore = MiniGameManager.Instance != null ? MiniGameManager.Instance.GetCurrentScore() : 0;
            
            Debug.Log($"💰 [{gameObject.name}] 점수 상태 체크:\n" +
                     $"- 현재 총점: {currentTotalScore}\n" +
                     $"- 이 NPC 꼬시기 점수: {pointValue}\n" +
                     $"- 이 NPC 미니게임 보너스: {miniGameBonusScore}\n" +
                     $"- Elite NPC: {isEliteNPC}\n" +
                     $"- 미니게임 완료: {miniGameCompleted}\n" +
                     $"- 피버 모드 활성화: {isFeverMode}\n" +
                     $"- MiniGameManager 연결됨: {(MiniGameManager.Instance != null)}");
        }
        
        [ContextMenu("Debug: Test NPC Score with Fever")]
        public void DebugTestNPCScoreWithFever()
        {
            Debug.Log("=== NPC 점수 피버모드 테스트 ===");
            
            bool isFeverMode = MiniGameManager.Instance != null ? MiniGameManager.Instance.IsFeverModeActive() : false;
            
            int normalSeductionScore = pointValue;
            int feverSeductionScore = CalculateFinalScore(pointValue);
            
            int normalBonusScore = miniGameBonusScore;
            int feverBonusScore = CalculateFinalScore(miniGameBonusScore);
            
            Debug.Log($"💰 [{gameObject.name}] 점수 테스트:");
            Debug.Log($"피버모드 상태: {isFeverMode}");
            Debug.Log($"꼬시기 점수: {normalSeductionScore} → {feverSeductionScore}");
            Debug.Log($"미니게임 보너스: {normalBonusScore} → {feverBonusScore}");
            Debug.Log($"총 점수: {normalSeductionScore + normalBonusScore} → {feverSeductionScore + feverBonusScore}");
        }
        
        // 이벤트
        public event System.Action<NPCController> OnNPCSeduced;

        private void OnDestroy()
        {
            StopPersistentVFX();
        }
    }
}