using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using ZeeeingGaze;

namespace ZeeeingGaze
{
    public class EmotionGazeManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private float globalEmotionIntensityMultiplier = 1.0f;
        
        [Header("VFX Settings")]
        [SerializeField] private VisualEffectAsset defaultGazeVFXAsset;
        [SerializeField] private GameObject criticalHitVFXPrefab;
        [SerializeField] private float criticalHitThreshold = 0.8f;
        [SerializeField] private float laserDuration = 0.5f;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip successfulGazeSound;
        [SerializeField] private AudioClip criticalHitSound;
        [SerializeField] private float successSoundVolume = 0.5f;
        [SerializeField] private float criticalHitSoundVolume = 0.7f;
        
        [Header("Emotion Matching")]
        [SerializeField] private PlayerEmotionController playerEmotionController;
        [SerializeField] private bool requireEmotionMatch = true; // 감정 매칭 필수 여부
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true; // 디버그 활성화
        
        // 싱글톤 패턴
        private static EmotionGazeManager _instance;
        public static EmotionGazeManager Instance 
        { 
            get 
            { 
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<EmotionGazeManager>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject("EmotionGazeManager");
                        _instance = singletonObject.AddComponent<EmotionGazeManager>();
                    }
                }
                return _instance; 
            } 
        }
        
        // 멤버 변수들
        private List<NPCEmotionController> activeNPCs = new List<NPCEmotionController>();
        private AudioSource audioSource;
        private int successfulGazeCount = 0;
        private int criticalHitCount = 0;
        private Dictionary<GameObject, float> lastVFXCreationTime = new Dictionary<GameObject, float>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            SetupAudioSource();
            
            // PlayerEmotionController 자동 찾기
            if (playerEmotionController == null)
            {
                playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
                if (playerEmotionController != null && enableDebugLogs)
                {
                    Debug.Log("[EmotionGazeManager] PlayerEmotionController를 자동으로 찾았습니다.");
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[EmotionGazeManager] PlayerEmotionController를 찾을 수 없습니다!");
                }
            }
        }

        private void Start()
        {
            RegisterExistingNPCs();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 초기화 완료:" +
                         $"\n  - PlayerEmotionController: {(playerEmotionController != null ? "연결됨" : "없음")}" +
                         $"\n  - DefaultGazeVFXAsset: {(defaultGazeVFXAsset != null ? "설정됨" : "없음")}" +
                         $"\n  - RequireEmotionMatch: {requireEmotionMatch}");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 사운드
        }

        private void RegisterExistingNPCs()
        {
            NPCEmotionController[] allNPCs = FindObjectsByType<NPCEmotionController>(FindObjectsSortMode.None);
            
            foreach (NPCEmotionController npc in allNPCs)
            {
                RegisterNPC(npc);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 총 {allNPCs.Length}개의 NPC가 등록되었습니다.");
            }
        }

        public void RegisterNPC(NPCEmotionController npc)
        {
            if (npc != null && !activeNPCs.Contains(npc))
            {
                activeNPCs.Add(npc);
                npc.EmotionTriggered += OnNPCEmotionTriggered;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[EmotionGazeManager] {npc.gameObject.name} NPC가 등록되었습니다.");
                }
            }
        }

        public void UnregisterNPC(NPCEmotionController npc)
        {
            if (npc != null && activeNPCs.Contains(npc))
            {
                activeNPCs.Remove(npc);
                npc.EmotionTriggered -= OnNPCEmotionTriggered;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[EmotionGazeManager] {npc.gameObject.name} NPC의 등록이 해제되었습니다.");
                }
            }
        }

        // 이벤트 핸들러 - 감정 강도 임계값 도달 시 호출 (기존 시스템)
        private void OnNPCEmotionTriggered(EmotionEventData eventData)
        {
            // 이제 이 메서드는 감정 강도가 충분히 쌓였을 때만 호출됨
            // 특별한 이펙트나 사운드 등을 위해 사용
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 감정 강도 임계값 도달: {eventData.Source.name}, 강도: {eventData.Intensity:F2}");
            }
            
            // 추가적인 특별 이펙트나 보너스 처리를 여기서 할 수 있음
        }

        // 🔥 새로운 메서드: EyeTrackingRay에서 직접 호출하는 즉시 시선 이벤트 처리
        public void HandleEyeGazeEvent(EmotionEventData eventData)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [EmotionGazeManager] HandleEyeGazeEvent 호출됨: {eventData.Source?.name ?? "null"}");
            }
            
            if (eventData.Source == null) 
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[EmotionGazeManager] eventData.Source가 null입니다!");
                }
                return;
            }
            
            // 감정 매칭 체크 (다시 한번 확인)
            bool emotionMatched = CheckEmotionMatch(eventData);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 직접 시선 이벤트 처리: {eventData.Source.name}" +
                         $"\n  - 감정 매칭: {emotionMatched}" +
                         $"\n  - requireEmotionMatch: {requireEmotionMatch}" +
                         $"\n  - 이벤트 강도: {eventData.Intensity:F2}");
            }
            
            // 감정이 매칭되지 않으면 VFX 없는 기본 처리만
            if (requireEmotionMatch && !emotionMatched)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[EmotionGazeManager] 감정 불일치로 HandleNonMatchingEmotion 호출");
                }
                HandleNonMatchingEmotion(eventData);
                return;
            }
            
            // 감정이 매칭될 때만 VFX 처리
            successfulGazeCount++;
            
            bool isCriticalHit = eventData.Intensity >= criticalHitThreshold;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] VFX 처리 시작:" +
                         $"\n  - 크리티컬 히트: {isCriticalHit} (강도: {eventData.Intensity:F2} >= 임계값: {criticalHitThreshold})" +
                         $"\n  - defaultGazeVFXAsset: {(defaultGazeVFXAsset != null ? "설정됨" : "null")}");
            }
            
            if (isCriticalHit)
            {
                criticalHitCount++;
                if (enableDebugLogs)
                {
                    Debug.Log($"🔥 [EmotionGazeManager] 크리티컬 히트 처리 시작");
                }
                HandleCriticalHit(eventData);
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"⚡ [EmotionGazeManager] 일반 시선 처리 시작");
                }
                HandleSuccessfulGaze(eventData);
            }
            
            UpdateGameState();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] HandleEyeGazeEvent 완료");
            }
        }

        // 감정 매칭 체크
        private bool CheckEmotionMatch(EmotionEventData eventData)
        {
            if (playerEmotionController == null) 
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[EmotionGazeManager] PlayerEmotionController가 설정되지 않음. 감정 매칭 체크 불가");
                }
                return !requireEmotionMatch; // 컨트롤러가 없으면 설정에 따라 결정
            }

            // NPC의 감정 상태 가져오기
            NPCEmotionController npcController = eventData.Source.GetComponent<NPCEmotionController>();
            if (npcController == null) 
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[EmotionGazeManager] {eventData.Source.name}에 NPCEmotionController가 없음");
                }
                return false;
            }

            EmotionState playerEmotion = playerEmotionController.GetCurrentEmotion();
            EmotionState npcEmotion = npcController.GetCurrentEmotion();
            
            bool isMatched = playerEmotion == npcEmotion;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 감정 매칭 체크: 플레이어({playerEmotion}) vs NPC({npcEmotion}) = {isMatched}");
            }
            
            return isMatched;
        }

        // 감정 불일치 시 처리
        private void HandleNonMatchingEmotion(EmotionEventData eventData)
        {
            // VFX 없는 기본 피드백만 제공
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 감정 불일치로 인한 기본 시선 교류: {eventData.Source.name}");
            }
            
            // 기본 사운드 재생 (볼륨 낮춤)
            if (audioSource != null && successfulGazeSound != null)
            {
                audioSource.PlayOneShot(successfulGazeSound, successSoundVolume * 0.3f);
            }
        }

        // 크리티컬 히트 처리
        private void HandleCriticalHit(EmotionEventData eventData)
        {
            if (lastVFXCreationTime.TryGetValue(eventData.Source, out float lastTime))
            {
                if (Time.time - lastTime < 0.5f)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[EmotionGazeManager] 크리티컬 히트 쿨다운 중: {Time.time - lastTime:F2}초");
                    }
                    return;
                }
            }
            
            lastVFXCreationTime[eventData.Source] = Time.time;
            
            if (criticalHitVFXPrefab != null && eventData.Source != null)
            {
                GameObject critVFX = Instantiate(criticalHitVFXPrefab, eventData.Source.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                Destroy(critVFX, 2.0f);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[EmotionGazeManager] 크리티컬 히트 VFX 생성: {criticalHitVFXPrefab.name}");
                }
            }
            
            CreateLaserVFX(eventData.Source, true);
            
            if (audioSource != null && criticalHitSound != null)
            {
                audioSource.PlayOneShot(criticalHitSound, criticalHitSoundVolume);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=yellow>🔥 크리티컬 히트!</color> {eventData.Source.name}에게 시선 공격 성공! 감정: {eventData.Emotion}, 강도: {eventData.Intensity:F2}");
            }
        }

        // 성공적인 시선 처리
        private void HandleSuccessfulGaze(EmotionEventData eventData)
        {
            if (lastVFXCreationTime.TryGetValue(eventData.Source, out float lastTime))
            {
                if (Time.time - lastTime < 0.3f)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[EmotionGazeManager] 일반 시선 쿨다운 중: {Time.time - lastTime:F2}초");
                    }
                    return;
                }
            }
            
            lastVFXCreationTime[eventData.Source] = Time.time;
            
            CreateLaserVFX(eventData.Source, false);
            
            if (audioSource != null && successfulGazeSound != null)
            {
                audioSource.PlayOneShot(successfulGazeSound, successSoundVolume);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"⚡ 시선 공격 성공: {eventData.Source.name}, 감정: {eventData.Emotion}, 강도: {eventData.Intensity:F2}");
            }
        }

        // 레이저 VFX 생성
        private void CreateLaserVFX(GameObject target, bool isCritical)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🚀 [CreateLaserVFX] 시작 - 대상: {target?.name ?? "null"}, 크리티컬: {isCritical}");
            }
            
            if (defaultGazeVFXAsset == null)
            {
                Debug.LogError("[CreateLaserVFX] defaultGazeVFXAsset이 null입니다! Inspector에서 설정하세요.");
                return;
            }
            
            if (target == null)
            {
                Debug.LogError("[CreateLaserVFX] target이 null입니다!");
                return;
            }
                
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[CreateLaserVFX] Camera.main이 null입니다!");
                return;
            }
                
            Vector3 startPos = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
            
            NPCEmotionController npcController = target.GetComponent<NPCEmotionController>();
            EyeInteractable targetEye = npcController?.GetEyeInteractable();
            Vector3 targetPos = targetEye != null ? 
                targetEye.GetTargetPosition() : 
                target.transform.position + Vector3.up * 1.5f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CreateLaserVFX] 위치 계산:" +
                         $"\n  - 시작점: {startPos}" +
                         $"\n  - 대상점: {targetPos}" +
                         $"\n  - EyeInteractable: {(targetEye != null ? "있음" : "없음")}" +
                         $"\n  - NPC 컨트롤러: {(npcController != null ? "있음" : "없음")}");
            }
            
            GameObject vfxObject = new GameObject(isCritical ? "CriticalHit_Laser" : "GazeVFX_Laser");
            vfxObject.transform.position = startPos;
            
            Vector3 direction = targetPos - startPos;
            vfxObject.transform.rotation = Quaternion.LookRotation(direction);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CreateLaserVFX] VFX 오브젝트 생성:" +
                         $"\n  - 이름: {vfxObject.name}" +
                         $"\n  - 위치: {vfxObject.transform.position}" +
                         $"\n  - 방향: {direction}" +
                         $"\n  - 거리: {direction.magnitude:F2}");
            }
            
            VisualEffect vfxComponent = vfxObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = defaultGazeVFXAsset;
            
            if (vfxComponent.HasFloat("LaserLength"))
            {
                vfxComponent.SetFloat("LaserLength", direction.magnitude);
                if (enableDebugLogs)
                {
                    Debug.Log($"[CreateLaserVFX] LaserLength 설정: {direction.magnitude:F2}");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("[CreateLaserVFX] VFX에 LaserLength 프로퍼티가 없습니다!");
            }
            
            if (vfxComponent.HasFloat("LaserWidth"))
            {
                float width = isCritical ? 0.05f : 0.02f;
                vfxComponent.SetFloat("LaserWidth", width);
                if (enableDebugLogs)
                {
                    Debug.Log($"[CreateLaserVFX] LaserWidth 설정: {width}");
                }
            }
            
            // 감정별 색상 설정
            Color laserColor = GetEmotionBasedColor(isCritical);
            string colorProperty = vfxComponent.HasVector4("LaserColor") ? "LaserColor" : 
                                   vfxComponent.HasVector4("Color") ? "Color" : "";
            
            if (!string.IsNullOrEmpty(colorProperty))
            {
                vfxComponent.SetVector4(colorProperty, laserColor);
                if (enableDebugLogs)
                {
                    Debug.Log($"[CreateLaserVFX] 색상 설정: {colorProperty} = {laserColor}");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("[CreateLaserVFX] VFX에 색상 프로퍼티가 없습니다!");
            }
            
            vfxComponent.Play();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CreateLaserVFX] VFX 재생 시작");
            }
            
            float destroyTime = isCritical ? laserDuration * 1.5f : laserDuration;
            Destroy(vfxObject, destroyTime);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [CreateLaserVFX] 완료 - {destroyTime}초 후 삭제 예정");
            }
        }

        // 감정 기반 색상 반환
        private Color GetEmotionBasedColor(bool isCritical)
        {
            if (isCritical)
            {
                return Color.magenta; // 크리티컬은 항상 마젠타
            }
            
            if (playerEmotionController != null)
            {
                try
                {
                    return playerEmotionController.GetCurrentEmotionColor();
                }
                catch (System.Exception e)
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"[EmotionGazeManager] GetCurrentEmotionColor 호출 실패: {e.Message}");
                    }
                }
            }
            
            return Color.Lerp(Color.white, Color.yellow, 0.7f); // 기본 색상
        }

        // 공용 레이저 생성 메서드
        public GameObject CreateGazeLaser(Vector3 startPosition, Vector3 targetPosition, Color? color = null)
        {
            if (defaultGazeVFXAsset == null)
            {
                Debug.LogWarning("[EmotionGazeManager] defaultGazeVFXAsset이 설정되지 않았습니다.");
                return null;
            }
                
            try
            {
                GameObject vfxObject = new GameObject("GazeVFX_Laser");
                vfxObject.transform.position = startPosition;
                
                Vector3 direction = targetPosition - startPosition;
                if (direction.sqrMagnitude < 0.001f)
                {
                    Debug.LogWarning("[EmotionGazeManager] 시작점과 끝점이 너무 가까움");
                    Destroy(vfxObject);
                    return null;
                }
                
                vfxObject.transform.rotation = Quaternion.LookRotation(direction);
                
                VisualEffect vfxComponent = vfxObject.AddComponent<VisualEffect>();
                vfxComponent.visualEffectAsset = defaultGazeVFXAsset;
                
                if (vfxComponent.HasFloat("LaserLength"))
                {
                    vfxComponent.SetFloat("LaserLength", direction.magnitude);
                }
                
                Color defaultColor = color.HasValue ? color.Value : Color.white;
                string colorProperty = "";
                
                if (vfxComponent.HasVector4("LaserColor"))
                    colorProperty = "LaserColor";
                else if (vfxComponent.HasVector4("Color"))
                    colorProperty = "Color";
                
                if (!string.IsNullOrEmpty(colorProperty))
                {
                    vfxComponent.SetVector4(colorProperty, defaultColor);
                }
                
                vfxComponent.Play();
                
                if (enableDebugLogs)
                {
                    Debug.Log("[EmotionGazeManager] 공용 레이저 생성 완료");
                }
                
                return vfxObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EmotionGazeManager] 레이저 생성 중 오류 발생: {e.Message}");
                return null;
            }
        }

        private void UpdateGameState()
        {
            // 게임 상태 업데이트 로직 (필요에 따라 구현)
        }

        // Public 메서드들
        public void SetGlobalEmotionIntensityMultiplier(float multiplier)
        {
            globalEmotionIntensityMultiplier = Mathf.Max(0.1f, multiplier);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 글로벌 감정 강도 배율이 {globalEmotionIntensityMultiplier}로 설정되었습니다.");
            }
        }
        
        public int GetSuccessfulGazeCount()
        {
            return successfulGazeCount;
        }
        
        public int GetCriticalHitCount()
        {
            return criticalHitCount;
        }
        
        public float GetCriticalHitRate()
        {
            if (successfulGazeCount == 0) return 0f;
            return (float)criticalHitCount / successfulGazeCount;
        }
        
        public void ResetStats()
        {
            successfulGazeCount = 0;
            criticalHitCount = 0;
            lastVFXCreationTime.Clear();
        }
        
        public int GetActiveNPCCount()
        {
            activeNPCs.RemoveAll(npc => npc == null);
            return activeNPCs.Count;
        }
        
        public float GetGlobalEmotionIntensityMultiplier()
        {
            return globalEmotionIntensityMultiplier;
        }
        
        public void SetDefaultGazeVFXAsset(VisualEffectAsset vfxAsset)
        {
            if (vfxAsset != null)
            {
                defaultGazeVFXAsset = vfxAsset;
                if (enableDebugLogs)
                    Debug.Log("[EmotionGazeManager] 기본 VFX 에셋이 변경되었습니다.");
            }
        }

        public bool HasDefaultVFXAsset()
        {
            return defaultGazeVFXAsset != null;
        }

        public PlayerEmotionController GetPlayerEmotionController()
        {
            return playerEmotionController;
        }

        public void SetPlayerEmotionController(PlayerEmotionController controller)
        {
            playerEmotionController = controller;
            if (enableDebugLogs)
            {
                Debug.Log("[EmotionGazeManager] PlayerEmotionController가 설정되었습니다.");
            }
        }

        public void SetRequireEmotionMatch(bool require)
        {
            requireEmotionMatch = require;
            if (enableDebugLogs)
            {
                Debug.Log($"[EmotionGazeManager] 감정 매칭 필수 여부가 {require}로 설정되었습니다.");
            }
        }

        public bool GetRequireEmotionMatch()
        {
            return requireEmotionMatch;
        }
        
        // 🔥 디버그용 메서드들
        [ContextMenu("Debug: Test VFX Creation")]
        public void DebugTestVFXCreation()
        {
            Debug.Log("=== VFX 생성 테스트 ===");
            
            // 가장 가까운 NPC 찾기
            NPCController[] allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            if (allNPCs.Length > 0)
            {
                NPCController testNPC = allNPCs[0];
                EmotionEventData testData = new EmotionEventData(
                    EmotionState.Happy,
                    1.0f,
                    testNPC.gameObject
                );
                
                Debug.Log($"테스트 대상: {testNPC.GetName()}");
                HandleEyeGazeEvent(testData);
            }
            else
            {
                Debug.LogWarning("테스트할 NPC가 없습니다!");
            }
        }
        
        [ContextMenu("Debug: Check All Settings")]
        public void DebugCheckAllSettings()
        {
            Debug.Log("=== EmotionGazeManager 설정 확인 ===");
            Debug.Log($"PlayerEmotionController: {(playerEmotionController != null ? "연결됨" : "null")}");
            Debug.Log($"DefaultGazeVFXAsset: {(defaultGazeVFXAsset != null ? "설정됨" : "null")}");
            Debug.Log($"CriticalHitVFXPrefab: {(criticalHitVFXPrefab != null ? "설정됨" : "null")}");
            Debug.Log($"RequireEmotionMatch: {requireEmotionMatch}");
            Debug.Log($"EnableDebugLogs: {enableDebugLogs}");
            Debug.Log($"Camera.main: {(Camera.main != null ? "존재함" : "null")}");
            Debug.Log($"AudioSource: {(audioSource != null ? "설정됨" : "null")}");
            Debug.Log($"등록된 NPC 수: {activeNPCs.Count}");
        }
    }
}