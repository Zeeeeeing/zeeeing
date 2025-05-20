using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;  // VisualEffect 사용을 위한 네임스페이스 추가

namespace ZeeeingGaze
{
    // 전체 감정-시선 시스템을 관리하는 매니저 클래스
    public class EmotionGazeManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private float globalEmotionIntensityMultiplier = 1.0f;
        
        [Header("VFX Settings")]
        [SerializeField] private VisualEffectAsset defaultGazeVFXAsset;    // VFX 에셋으로 변경
        [SerializeField] private GameObject criticalHitVFXPrefab;          // Prefab 유지
        [SerializeField] private float criticalHitThreshold = 0.8f;
        [SerializeField] private float laserDuration = 0.5f;              // 레이저 표시 시간
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip successfulGazeSound;
        [SerializeField] private AudioClip criticalHitSound;
        [SerializeField] private float successSoundVolume = 0.5f;
        [SerializeField] private float criticalHitSoundVolume = 0.7f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;  // 기본값을 false로 변경
        
        // 싱글톤 인스턴스
        private static EmotionGazeManager _instance;
        public static EmotionGazeManager Instance 
        { 
            get 
            { 
                // 인스턴스가 없으면 찾아보기
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<EmotionGazeManager>();
                    // 씬에 없으면 생성
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject("EmotionGazeManager");
                        _instance = singletonObject.AddComponent<EmotionGazeManager>();
                    }
                }
                
                return _instance;
            }
        }
        
        // 컴포넌트 캐싱
        private AudioSource audioSource;
        
        // NPC 트래킹
        private List<NPCEmotionController> activeNPCs = new List<NPCEmotionController>();
        
        // 게임 상태 및 통계
        private int successfulGazeCount = 0;
        private int criticalHitCount = 0;
        private Dictionary<GameObject, float> lastVFXCreationTime = new Dictionary<GameObject, float>();
        
        private void Awake()
        {
            // 싱글톤 설정
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            // DontDestroyOnLoad(gameObject);
            
            // 오디오 소스 컴포넌트 가져오기
            InitializeAudioSource();
            
            // 씬 로드 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void InitializeAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D 사운드
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 씬이 로드되면 NPC 찾기
            StartCoroutine(FindAllNPCsDelayed());
        }
        
        private IEnumerator FindAllNPCsDelayed()
        {
            // 씬 로드 후 약간의 지연 시간을 두고 NPC 찾기 (다른 객체들이 초기화될 시간 제공)
            yield return new WaitForSeconds(0.2f);
            FindAllNPCs();
        }
        
        private void Start()
        {
            // 씬에 있는 모든 NPC 찾기
            FindAllNPCs();
        }
        
        private void OnDestroy()
        {
            // 씬 로드 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 모든 NPC 등록 해제
            foreach (var npc in activeNPCs.ToArray())
            {
                if (npc != null)
                {
                    UnregisterNPC(npc);
                }
            }
            
            activeNPCs.Clear();
        }
        
        // 씬에 있는 모든 NPC를 찾아 등록
        public void FindAllNPCs()
        {
            // 기존 NPC 등록 해제
            foreach (var npc in activeNPCs.ToArray())
            {
                if (npc != null)
                {
                    UnregisterNPC(npc);
                }
            }
            
            activeNPCs.Clear();
            
            // 새 NPC 찾아 등록
            // Unity 버전에 따라 적절한 API 사용
            NPCEmotionController[] npcs;
            
            #if UNITY_2022_2_OR_NEWER
            npcs = FindObjectsByType<NPCEmotionController>(FindObjectsSortMode.None);
            #else
            npcs = FindObjectsOfType<NPCEmotionController>();
            #endif
            
            foreach (var npc in npcs)
            {
                RegisterNPC(npc);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"EmotionGazeManager: {activeNPCs.Count}개의 NPC를 찾아 등록했습니다.");
            }
        }
        
        // NPC 등록
        public void RegisterNPC(NPCEmotionController npc)
        {
            if (npc != null && !activeNPCs.Contains(npc))
            {
                activeNPCs.Add(npc);
                npc.EmotionTriggered += OnNPCEmotionTriggered;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"EmotionGazeManager: {npc.gameObject.name} NPC가 등록되었습니다.");
                }
            }
        }
        
        // NPC 등록 해제
        public void UnregisterNPC(NPCEmotionController npc)
        {
            if (npc != null && activeNPCs.Contains(npc))
            {
                activeNPCs.Remove(npc);
                npc.EmotionTriggered -= OnNPCEmotionTriggered;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"EmotionGazeManager: {npc.gameObject.name} NPC의 등록이 해제되었습니다.");
                }
            }
        }
        
        // NPC 감정 트리거 이벤트 핸들러
        private void OnNPCEmotionTriggered(EmotionEventData eventData)
        {
            if (eventData.Source == null) return;
            
            // 성공적인 시선 교류 카운트 증가
            successfulGazeCount++;
            
            // 크리티컬 히트 체크 (제안서에 명시된 "좋은 공격" 성공)
            bool isCriticalHit = eventData.Intensity >= criticalHitThreshold;
            
            if (isCriticalHit)
            {
                criticalHitCount++;
                HandleCriticalHit(eventData);
            }
            else
            {
                HandleSuccessfulGaze(eventData);
            }
            
            // UI 업데이트 등 필요한 처리
            UpdateGameState();
        }
        
        // 크리티컬 히트 처리
        private void HandleCriticalHit(EmotionEventData eventData)
        {
            // VFX 생성 간격 제한 (성능 최적화)
            if (lastVFXCreationTime.TryGetValue(eventData.Source, out float lastTime))
            {
                if (Time.time - lastTime < 0.5f)
                {
                    // 너무 빠른 재생성 방지
                    return;
                }
            }
            
            lastVFXCreationTime[eventData.Source] = Time.time;
            
            // Critical Hit VFX는 여전히 기존 Prefab 사용
            if (criticalHitVFXPrefab != null && eventData.Source != null)
            {
                GameObject critVFX = Instantiate(criticalHitVFXPrefab, eventData.Source.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                Destroy(critVFX, 2.0f); // 2초 후 자동 파괴
            }
            
            // 레이저 VFX 추가 (더 화려한 레이저 효과)
            CreateLaserVFX(eventData.Source, true);
            
            // 크리티컬 사운드 재생
            if (audioSource != null && criticalHitSound != null)
            {
                audioSource.PlayOneShot(criticalHitSound, criticalHitSoundVolume);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"<color=yellow>크리티컬 히트!</color> {eventData.Source.name}에게 시선 공격 성공! 강도: {eventData.Intensity:F2}");
            }
        }
        
        // 일반 성공 처리
        private void HandleSuccessfulGaze(EmotionEventData eventData)
        {
            // VFX 생성 간격 제한 (성능 최적화)
            if (lastVFXCreationTime.TryGetValue(eventData.Source, out float lastTime))
            {
                if (Time.time - lastTime < 0.3f)
                {
                    // 너무 빠른 재생성 방지
                    return;
                }
            }
            
            lastVFXCreationTime[eventData.Source] = Time.time;
            
            // 레이저 VFX 생성
            CreateLaserVFX(eventData.Source, false);
            
            // 성공 사운드 재생
            if (audioSource != null && successfulGazeSound != null)
            {
                audioSource.PlayOneShot(successfulGazeSound, successSoundVolume);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"시선 공격 성공: {eventData.Source.name}, 감정: {eventData.Emotion}, 강도: {eventData.Intensity:F2}");
            }
        }
        
        // 레이저 VFX 생성 메소드 (중복 코드 제거)
        private void CreateLaserVFX(GameObject target, bool isCritical)
        {
            if (defaultGazeVFXAsset == null || target == null)
                return;
                
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
                return;
                
            // 시선 시작 위치
            Vector3 startPos = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
            
            // 타겟 위치
            NPCEmotionController npcController = target.GetComponent<NPCEmotionController>();
            EyeInteractable targetEye = npcController?.GetEyeInteractable();
            Vector3 targetPos = targetEye != null ? 
                targetEye.GetTargetPosition() : 
                target.transform.position + Vector3.up * 1.5f;
            
            // VFX 오브젝트 생성
            GameObject vfxObject = new GameObject(isCritical ? "CriticalHit_Laser" : "GazeVFX_Laser");
            vfxObject.transform.position = startPos;
            
            // 레이저 방향 계산
            Vector3 direction = targetPos - startPos;
            vfxObject.transform.rotation = Quaternion.LookRotation(direction);
            
            // Visual Effect 컴포넌트 추가 및 에셋 설정
            VisualEffect vfxComponent = vfxObject.AddComponent<VisualEffect>();
            vfxComponent.visualEffectAsset = defaultGazeVFXAsset;
            
            // 레이저 길이 설정
            if (vfxComponent.HasFloat("LaserLength"))
            {
                vfxComponent.SetFloat("LaserLength", direction.magnitude);
            }
            
            // 레이저 두께 설정
            if (vfxComponent.HasFloat("LaserWidth"))
            {
                // Critical Hit는 더 굵은 레이저
                vfxComponent.SetFloat("LaserWidth", isCritical ? 0.05f : 0.02f);
            }
            
            // 레이저 색상 설정
            string colorProperty = vfxComponent.HasVector4("LaserColor") ? "LaserColor" : 
                                   vfxComponent.HasVector4("Color") ? "Color" : "";
            
            if (!string.IsNullOrEmpty(colorProperty))
            {
                // Critical Hit는 다른 색상
                Color laserColor = isCritical ? Color.magenta : Color.Lerp(Color.white, Color.yellow, 0.7f);
                vfxComponent.SetVector4(colorProperty, laserColor);
            }
            
            // VFX 재생
            vfxComponent.Play();
            
            // 일정 시간 후 자동 파괴
            float destroyTime = isCritical ? laserDuration * 1.5f : laserDuration;
            Destroy(vfxObject, destroyTime);
        }

        public GameObject CreateGazeLaser(Vector3 startPosition, Vector3 targetPosition, Color? color = null)
        {
            if (defaultGazeVFXAsset == null)
            {
                Debug.LogWarning("EmotionGazeManager: defaultGazeVFXAsset이 설정되지 않았습니다.");
                return null;
            }
                
            try
            {
                // VFX 오브젝트 생성
                GameObject vfxObject = new GameObject("GazeVFX_Laser");
                
                // 시선 시작 위치 설정
                vfxObject.transform.position = startPosition;
                
                // 방향 계산 및 회전 설정
                Vector3 direction = targetPosition - startPosition;
                if (direction.sqrMagnitude < 0.001f)
                {
                    Debug.LogWarning("EmotionGazeManager: 시작점과 끝점이 너무 가까움");
                    Destroy(vfxObject);
                    return null;
                }
                
                vfxObject.transform.rotation = Quaternion.LookRotation(direction);
                
                // Visual Effect 컴포넌트 추가 및 에셋 설정
                VisualEffect vfxComponent = vfxObject.AddComponent<VisualEffect>();
                vfxComponent.visualEffectAsset = defaultGazeVFXAsset;
                
                // 레이저 길이 설정 (VFX에 이 속성이 있다고 가정)
                if (vfxComponent.HasFloat("LaserLength"))
                {
                    vfxComponent.SetFloat("LaserLength", direction.magnitude);
                }
                
                // 기본 레이저 색상 설정
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
                
                // VFX 재생
                vfxComponent.Play();
                
                return vfxObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EmotionGazeManager: 레이저 생성 중 오류 발생: {e.Message}");
                return null;
            }
        }

        // 게임 상태 업데이트
        private void UpdateGameState()
        {
            // TODO: UI 업데이트, 게임 상태 변경 등
            // 이벤트를 발생시켜 UI 관리자에게 알릴 수 있음
        }
        
        // 글로벌 감정 강도 배율 설정
        public void SetGlobalEmotionIntensityMultiplier(float multiplier)
        {
            globalEmotionIntensityMultiplier = Mathf.Max(0.1f, multiplier);
            
            if (enableDebugLogs)
            {
                Debug.Log($"EmotionGazeManager: 글로벌 감정 강도 배율이 {globalEmotionIntensityMultiplier}로 설정되었습니다.");
            }
        }
        
        // 통계 관련 메소드
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
        
        // 통계 리셋
        public void ResetStats()
        {
            successfulGazeCount = 0;
            criticalHitCount = 0;
            lastVFXCreationTime.Clear();
        }
        
        // 현재 등록된 NPC 수 반환
        public int GetActiveNPCCount()
        {
            // 무효한 참조 제거
            activeNPCs.RemoveAll(npc => npc == null);
            return activeNPCs.Count;
        }
        
        // 글로벌 감정 강도 배율 반환
        public float GetGlobalEmotionIntensityMultiplier()
        {
            return globalEmotionIntensityMultiplier;
        }
        
        // defaultGazeVFXAsset 런타임 설정 메소드
        public void SetDefaultGazeVFXAsset(VisualEffectAsset vfxAsset)
        {
            if (vfxAsset != null)
            {
                defaultGazeVFXAsset = vfxAsset;
                if (enableDebugLogs)
                    Debug.Log("EmotionGazeManager: 기본 VFX 에셋이 변경되었습니다.");
            }
        }
        
        // VFX 에셋이 설정되어 있는지 확인
        public bool HasDefaultVFXAsset()
        {
            return defaultGazeVFXAsset != null;
        }
        
        // 특정 NPC 찾기
        public NPCEmotionController FindNPC(string npcName)
        {
            return activeNPCs.Find(npc => npc != null && npc.gameObject.name == npcName);
        }
    }
}