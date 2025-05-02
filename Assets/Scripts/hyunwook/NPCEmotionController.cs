using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;  // VisualEffect 사용을 위한 네임스페이스 추가

namespace ZeeeingGaze
{
    [Serializable]
    public class EmotionTransition
    {
        public EmotionState fromEmotion;
        public EmotionState toEmotion;
        public float requiredIntensityToTransition = 0.8f; // 전환에 필요한 감정 강도
        public float minDurationInState = 2.0f; // 이 감정 상태에 머물러야 하는 최소 시간
    }

    [Serializable]
    public class EmotionSequence
    {
        public string sequenceName;
        public List<EmotionTransition> transitions = new List<EmotionTransition>();
        public bool loopSequence = false; // 시퀀스를 순환할지 여부
    }

    // 감정 상태에 따른 시퀀스 매핑 추가
    [Serializable]
    public class EmotionStateToSequence
    {
        public EmotionState fromState;
        public EmotionState toState;
        public string sequenceName;
    }

    // NPCEmotionController 클래스 확장
    public class NPCEmotionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EyeInteractable targetEyeInteractable; // 외부 EyeInteractable 참조

        [Header("Emotion-Gaze Mappings")]
        [SerializeField] private List<EmotionGazeMapping> emotionMappings = new List<EmotionGazeMapping>();
        
        [Header("Current State")]
        [SerializeField] private EmotionState currentEmotion = EmotionState.Neutral;
        [SerializeField] private float currentEmotionIntensity = 0f;
        [SerializeField] private bool isEmotionTriggered = false;
        
        [Header("Gaze Detection")]
        [SerializeField] private Transform eyeTrackingTarget;
        [SerializeField] private float gazeDetectionRadius = 0.1f;
        [SerializeField] private LayerMask playerGazeLayer;
        [SerializeField] private float gazeResetTime = 3.0f;  // 시선이 떨어진 후 감정 상태 리셋까지 시간
        
        [Header("Emotion Sequences")]
        [SerializeField] private List<EmotionSequence> emotionSequences = new List<EmotionSequence>();
        [SerializeField] private string activeSequenceName; // 현재 활성화된 시퀀스
        
        [Header("Auto Sequence Settings")]
        [SerializeField] private List<EmotionStateToSequence> autoSequenceMappings = new List<EmotionStateToSequence>();
        [SerializeField] private float autoSequenceThreshold = 0.8f;
        [SerializeField] private bool enableAutoSequence = true;
        
        [Header("Debugging")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logGazeEvents = false; // 로그 기본값을 false로 변경
        
        // 컴포넌트 참조
        private Animator animator;
        private AudioSource audioSource;
        private MeshRenderer meshRenderer;
        private EyeInteractable eyeInteractable;
        private Material originalMaterial;
        
        // 내부 상태 변수
        private bool isPlayerGazing = false;
        private float gazeDuration = 0f;
        private float gazeResetTimer = 0f;
        private Dictionary<EmotionState, EmotionGazeMapping> mappingDict = new Dictionary<EmotionState, EmotionGazeMapping>();
        
        // 시퀀스 관련 변수
        private EmotionSequence activeSequence;
        private int currentTransitionIndex = 0;
        private float timeInCurrentState = 0f;
        private bool isProcessingSequence = false;
        
        // 이벤트
        public event Action<EmotionEventData> EmotionTriggered;
        public event Action<EmotionEventData> EmotionChanged;
        public event Action<bool> GazeStatusChanged;
        
        private void Awake()
        {
            InitializeComponents();
            InitializeMappingDictionary();
        }
        
        private void InitializeComponents()
        {
            // 컴포넌트 참조 초기화
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            // 매핑이 없는 경우 기본 매핑 추가
            if (emotionMappings.Count == 0)
            {
                AddDefaultMappings();
            }
            
            // 오디오 소스가 없는 경우 추가
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // 3D 사운드
            }
            
            // 원본 재질 저장
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalMaterial = new Material(meshRenderer.material);
            }
        }
        
        private void InitializeMappingDictionary()
        {
            mappingDict.Clear();
            foreach (var mapping in emotionMappings)
            {
                if (mapping != null)
                {
                    mappingDict[mapping.emotionState] = mapping;
                }
            }
        }
        
        private void AddDefaultMappings()
        {
            emotionMappings.Add(EmotionGazeMapping.CreateHappyMapping());
            emotionMappings.Add(EmotionGazeMapping.CreateAngryMapping());
            emotionMappings.Add(EmotionGazeMapping.CreateSadMapping());
            emotionMappings.Add(EmotionGazeMapping.CreateEmbarrassedMapping());
            
            InitializeMappingDictionary();
        }
        
        private void OnEnable()
        {
            // 활성화될 때 EyeInteractable이 설정되어 있으면 구독 시작
            if (eyeInteractable != null)
            {
                // 추가적인 초기화 로직이 필요할 경우
            }
        }
        
        private void OnDisable()
        {
            // 비활성화될 때 필요한 정리 작업
            StopAllCoroutines();
        }
        
        private void Start()
        {
            SetupEyeInteractable();
            SetupEyeTrackingTarget();
            SetInitialEmotionState();
        }
        
        private void SetupEyeInteractable()
        {
            // 외부 EyeInteractable이 설정되어 있으면 사용
            if (targetEyeInteractable != null)
            {
                eyeInteractable = targetEyeInteractable;
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 외부 EyeInteractable 사용: {eyeInteractable.gameObject.name}");
            }
            // 아니면 자기 자신의 컴포넌트 확인
            else
            {
                eyeInteractable = GetComponent<EyeInteractable>();
                
                // 오브젝트에 EyeInteractable이 없는 경우 추가
                if (eyeInteractable == null)
                {
                    eyeInteractable = gameObject.AddComponent<EyeInteractable>();
                    // EyeInteractable 설정
                    Rigidbody rb = eyeInteractable.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true; // 물리 영향 안받도록 설정
                        rb.useGravity = false;
                    }
                    
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 자체 EyeInteractable 생성");
                }
            }
        }
        
        private void SetupEyeTrackingTarget()
        {
            // 시선 추적 타겟 설정
            if (eyeTrackingTarget == null)
            {
                // 외부 EyeInteractable이 있으면 그것의 Transform 사용
                if (targetEyeInteractable != null)
                {
                    eyeTrackingTarget = targetEyeInteractable.transform;
                }
                // 아니면 자기 자신의 Transform 사용
                else
                {
                    eyeTrackingTarget = transform;
                }
            }
        }
        
        private void SetInitialEmotionState()
        {
            // 기본 감정 상태 설정
            ChangeEmotionState(currentEmotion);
        }
        
        private void Update()
        {
            // 플레이어의 시선 감지
            DetectPlayerGaze();
            
            // 현재 감정 상태에 따른 시선 반응 처리
            ProcessEmotionResponse();
            
            // 시각적 피드백 적용
            ApplyVisualFeedback();
            
            // 감정 상태 리셋 타이머 처리
            HandleEmotionResetTimer();
            
            // 감정 시퀀스 처리
            if (isProcessingSequence)
            {
                ProcessEmotionSequence();
            }
        }
        
        // 플레이어의 시선 감지
        private void DetectPlayerGaze()
        {
            bool wasGazing = isPlayerGazing;
            
            // EyeInteractable을 통해 시선 감지
            isPlayerGazing = eyeInteractable != null && eyeInteractable.IsHovered;
            
            // 시선 상태 변경 이벤트 발생
            if (wasGazing != isPlayerGazing)
            {
                OnGazeStatusChanged(isPlayerGazing);
                
                if (isPlayerGazing)
                {
                    gazeResetTimer = 0f;
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 시선 감지 시작");
                }
                else
                {
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 시선 감지 종료 (유지 시간: {gazeDuration:F2}초)");
                    gazeDuration = 0f;
                }
            }
            
            if (isPlayerGazing)
            {
                gazeDuration += Time.deltaTime;
            }
        }
        
        // 현재 감정 상태에 따른 반응 처리
        private void ProcessEmotionResponse()
        {
            try
            {
                // 현재 감정에 맞는 매핑 찾기
                if (!mappingDict.TryGetValue(currentEmotion, out EmotionGazeMapping mapping))
                {
                    // 매핑이 없으면 기본값 생성 시도
                    if (currentEmotion == EmotionState.Happy)
                    {
                        mapping = EmotionGazeMapping.CreateHappyMapping();
                    }
                    else if (currentEmotion == EmotionState.Angry)
                    {
                        mapping = EmotionGazeMapping.CreateAngryMapping();
                    }
                    else if (currentEmotion == EmotionState.Sad)
                    {
                        mapping = EmotionGazeMapping.CreateSadMapping();
                    }
                    else if (currentEmotion == EmotionState.Embarrassed)
                    {
                        mapping = EmotionGazeMapping.CreateEmbarrassedMapping();
                    }
                    else
                    {
                        // 기본 매핑 없음 - 중립 감정 매핑 생성
                        mapping = new EmotionGazeMapping
                        {
                            emotionState = currentEmotion,
                            gazeSensitivity = 1.0f,
                            emotionBuildupRate = 0.1f,
                            emotionDecayRate = 0.05f,
                            gazeColor = Color.white
                        };
                    }
                    
                    // 새 매핑 추가
                    emotionMappings.Add(mapping);
                    mappingDict[currentEmotion] = mapping;
                    
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 없는 감정 상태({currentEmotion})에 대한 매핑 자동 생성");
                }
                
                if (isPlayerGazing)
                {
                    // 현재 감정 상태에 맞는 속도로 감정 강도 증가
                    float buildupMultiplier = mapping.gazeSensitivity * GetEmotionBuildupMultiplier();
                    currentEmotionIntensity += mapping.emotionBuildupRate * Time.deltaTime * buildupMultiplier;
                    
                    // 임계값 초과 시 반응 트리거
                    if (!isEmotionTriggered && 
                        currentEmotionIntensity >= mapping.emotionResponseThreshold && 
                        gazeDuration >= mapping.gazeStabilityDuration)
                    {
                        TriggerEmotionResponse(mapping);
                        isEmotionTriggered = true;
                        
                        // 자동 시퀀스 처리 추가
                        if (enableAutoSequence && currentEmotionIntensity >= autoSequenceThreshold && !isProcessingSequence)
                        {
                            TryStartAutoSequence();
                        }
                    }
                }
                else
                {
                    // 플레이어가 쳐다보지 않을 때 감정 강도 감소
                    currentEmotionIntensity -= mapping.emotionDecayRate * Time.deltaTime;
                    
                    if (isEmotionTriggered && currentEmotionIntensity < mapping.emotionResponseThreshold * 0.5f)
                    {
                        isEmotionTriggered = false;
                    }
                }
                
                // 감정 강도 범위 제한
                currentEmotionIntensity = Mathf.Clamp01(currentEmotionIntensity);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] 감정 반응 처리 중 오류 발생: {e.Message}");
            }
        }
        
        // 감정 증가 배율 계산 (추가 로직 가능)
        private float GetEmotionBuildupMultiplier()
        {
            // 기본 배율
            float multiplier = 1.0f;
            
            // 글로벌 감정 강도 배율이 설정되어 있는지 확인
            if (EmotionGazeManager.Instance != null)
            {
                multiplier *= EmotionGazeManager.Instance.GetGlobalEmotionIntensityMultiplier();
            }
            
            return multiplier;
        }
        
        // 자동 시퀀스 시작 시도 메소드 추가
        private void TryStartAutoSequence()
        {
            // 현재 감정 상태에 적합한 시퀀스 찾기
            EmotionStateToSequence mapping = autoSequenceMappings.Find(m => m.fromState == currentEmotion);
            
            if (mapping != null && !string.IsNullOrEmpty(mapping.sequenceName))
            {
                // 시퀀스 시작
                InitializeEmotionSequence(mapping.sequenceName);
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 자동 감정 시퀀스 시작: {mapping.sequenceName} ({mapping.fromState} -> {mapping.toState})");
            }
        }
        
        // 감정 반응 트리거
        private void TriggerEmotionResponse(EmotionGazeMapping mapping)
        {
            try
            {
                // 애니메이션 재생
                if (animator != null && mapping.emotionAnimation != null)
                {
                    animator.Play(mapping.emotionAnimation.name);
                }
                
                // 사운드 효과 재생
                if (audioSource != null && mapping.emotionSoundEffect != null)
                {
                    audioSource.PlayOneShot(mapping.emotionSoundEffect, mapping.soundEffectVolume);
                }
                
                // 이벤트 발생
                EmotionEventData eventData = new EmotionEventData(currentEmotion, currentEmotionIntensity, gameObject);
                OnEmotionTriggered(eventData);
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 반응 트리거: {currentEmotion}, 강도: {currentEmotionIntensity:F2}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] 감정 반응 트리거 중 오류 발생: {e.Message}");
            }
        }
        
        // 시각적 피드백 적용
        private void ApplyVisualFeedback()
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                try
                {
                    if (mappingDict.TryGetValue(currentEmotion, out EmotionGazeMapping mapping))
                    {
                        // 감정 강도에 따른 색상 보간
                        Color targetColor = Color.Lerp(Color.white, mapping.gazeColor, currentEmotionIntensity);
                        
                        // 셰이더가 지원하는 프로퍼티에만 접근
                        if (meshRenderer.material.HasProperty("_Color"))
                        {
                            // 색상을 서서히 변경 (갑작스러운 변화 방지)
                            Color currentColor = meshRenderer.material.color;
                            meshRenderer.material.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * 3f);
                        }
                        else if (meshRenderer.material.HasProperty("_BaseColor")) // URP/HDRP에서는 _BaseColor 사용
                        {
                            Color currentColor = meshRenderer.material.GetColor("_BaseColor");
                            meshRenderer.material.SetColor("_BaseColor", Color.Lerp(currentColor, targetColor, Time.deltaTime * 3f));
                        }
                        else
                        {
                            // 기타 가능한 색상 프로퍼티 시도
                            TrySetMaterialColor(meshRenderer.material, targetColor, Time.deltaTime * 3f);
                        }
                        
                        // 발광 색상도 설정 (있을 경우)
                        SetEmissionIfSupported(meshRenderer.material, targetColor, currentEmotionIntensity);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[{gameObject.name}] 시각적 피드백 적용 중 오류 발생: {e.Message}");
                }
            }
        }

        // 다양한 색상 프로퍼티에 대한 설정 시도
        private void TrySetMaterialColor(Material material, Color targetColor, float lerpFactor)
        {
            // 일반적으로 사용되는 색상 프로퍼티 이름들
            string[] possibleColorProperties = new string[] 
            {
                "_Color", "_BaseColor", "_MainColor", "_AlbedoColor", "_TintColor", "_Tint"
            };
            
            bool propertyFound = false;
            
            foreach (string propertyName in possibleColorProperties)
            {
                if (material.HasProperty(propertyName))
                {
                    Color currentColor = material.GetColor(propertyName);
                    material.SetColor(propertyName, Color.Lerp(currentColor, targetColor, lerpFactor));
                    propertyFound = true;
                    break; // 첫 번째 찾은 프로퍼티에 적용하고 종료
                }
            }
            
            if (!propertyFound && logGazeEvents)
            {
                Debug.LogWarning($"[{gameObject.name}] 지원되는 색상 프로퍼티를 찾을 수 없습니다: {material.shader.name}");
            }
        }

        // 발광 속성 지원 여부에 따라 설정
        private void SetEmissionIfSupported(Material material, Color baseColor, float intensity)
        {
            // 발광 프로퍼티 이름들
            string[] possibleEmissionProperties = new string[] 
            {
                "_EmissionColor", "_EmissiveColor", "_Emission", "_GlowColor"
            };
            
            // 발광 활성화 관련 프로퍼티 이름들
            string[] possibleEmissionEnableProperties = new string[] 
            {
                "_EmissionEnabled", "_UseEmission", "_Emission"
            };
            
            foreach (string propertyName in possibleEmissionProperties)
            {
                if (material.HasProperty(propertyName))
                {
                    // 발광 색상 설정 (강도에 따라 조절)
                    Color emissionColor = baseColor * intensity * 0.5f;
                    material.SetColor(propertyName, emissionColor);
                    
                    // 발광 활성화 시도
                    TryEnableEmission(material);
                    break;
                }
            }
        }

        // 발광 활성화 시도
        private void TryEnableEmission(Material material)
        {
            // Standard Shader에서 발광 활성화
            if (material.HasProperty("_EmissionEnabled"))
            {
                material.SetFloat("_EmissionEnabled", 1f);
            }
            
            // URP/HDRP에서 발광 활성화
            if (material.HasProperty("_UseEmission"))
            {
                material.SetFloat("_UseEmission", 1f);
            }
            
            // Unity의 글로벌 발광 활성화 (에디터에서만 작동)
            #if UNITY_EDITOR
            UnityEditor.MaterialEditor.FixupEmissiveFlag(material);
            #endif
            
            // 런타임에서 글로벌 발광 활성화
            if (!material.IsKeywordEnabled("_EMISSION"))
            {
                material.EnableKeyword("_EMISSION");
            }
        }
        
        // 감정 상태 리셋 타이머 처리
        private void HandleEmotionResetTimer()
        {
            if (!isPlayerGazing && !isEmotionTriggered && !isProcessingSequence)
            {
                gazeResetTimer += Time.deltaTime;
                
                if (gazeResetTimer >= gazeResetTime && currentEmotion != EmotionState.Neutral)
                {
                    ChangeEmotionState(EmotionState.Neutral);
                    gazeResetTimer = 0f;
                    
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 상태 리셋: Neutral");
                }
            }
        }
        
        // 외부에서 호출 가능한 감정 상태 변경 메소드
        public void ChangeEmotionState(EmotionState newEmotion)
        {
            try
            {
                // 이미 같은 감정 상태인 경우 무시
                if (currentEmotion == newEmotion)
                {
                    return;
                }
                
                EmotionState previousEmotion = currentEmotion;
                currentEmotion = newEmotion;
                
                // 시퀀스 처리 중이 아니라면 감정 강도 리셋
                if (!isProcessingSequence)
                {
                    currentEmotionIntensity = 0f;
                }
                
                isEmotionTriggered = false;
                
                // 감정 상태가 변경되면 현재 상태 지속 시간 업데이트 (시퀀스용)
                if (isProcessingSequence)
                {
                    timeInCurrentState = 0f;
                }
                
                // 애니메이션 설정 시도
                if (animator != null)
                {
                    // 애니메이션 파라미터가 있는지 확인 후 설정
                    try
                    {
                        animator.SetInteger("EmotionState", (int)currentEmotion);
                    }
                    catch (System.Exception)
                    {
                        // 애니메이션 파라미터가 없을 경우 무시
                    }
                }
                
                // 이벤트 발생
                EmotionEventData eventData = new EmotionEventData(currentEmotion, currentEmotionIntensity, gameObject);
                OnEmotionChanged(eventData);
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 상태 변경: {previousEmotion} -> {currentEmotion}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] 감정 상태 변경 중 오류 발생: {e.Message}");
            }
        }
        
        // 시퀀스 초기화 메서드
        public void InitializeEmotionSequence(string sequenceName)
        {
            try
            {
                // 이미 같은 시퀀스가 실행 중인지 확인
                if (isProcessingSequence && activeSequenceName == sequenceName)
                {
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 이미 해당 감정 시퀀스가 실행 중: {sequenceName}");
                    return;
                }
                
                // 실행 중인 시퀀스가 있다면 종료
                if (isProcessingSequence)
                {
                    StopEmotionSequence();
                }
                
                // 지정된 이름의 시퀀스 찾기
                activeSequence = emotionSequences.Find(seq => seq.sequenceName == sequenceName);
                
                if (activeSequence != null && activeSequence.transitions.Count > 0)
                {
                    activeSequenceName = sequenceName;
                    currentTransitionIndex = 0;
                    timeInCurrentState = 0f;
                    isProcessingSequence = true;
                    
                    // 시퀀스의 첫 번째 감정 상태로 설정
                    ChangeEmotionState(activeSequence.transitions[0].fromEmotion);
                    
                    if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 시퀀스 시작: {sequenceName}");
                }
                else
                {
                    isProcessingSequence = false;
                    activeSequence = null;
                    
                    if (logGazeEvents) 
                    {
                        if (emotionSequences.Count == 0)
                        {
                            Debug.LogWarning($"[{gameObject.name}] 감정 시퀀스 목록이 비어 있습니다.");
                        }
                        else
                        {
                            Debug.LogWarning($"[{gameObject.name}] 감정 시퀀스를 찾을 수 없거나 전환 항목이 없음: {sequenceName}");
                            Debug.Log($"사용 가능한 시퀀스: {string.Join(", ", emotionSequences.ConvertAll(seq => seq.sequenceName))}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] 감정 시퀀스 초기화 중 오류 발생: {e.Message}");
                isProcessingSequence = false;
                activeSequence = null;
            }
        }
        
        // 감정 시퀀스 처리
        private void ProcessEmotionSequence()
        {
            if (!isProcessingSequence || activeSequence == null) return;
            
            timeInCurrentState += Time.deltaTime;
            
            // 인덱스 유효성 검사 추가
            if (currentTransitionIndex < 0 || currentTransitionIndex >= activeSequence.transitions.Count)
            {
                Debug.LogWarning($"[{gameObject.name}] 시퀀스 처리 중 잘못된 전환 인덱스: {currentTransitionIndex}, 시퀀스: {activeSequenceName}");
                StopEmotionSequence();
                return;
            }
            
            EmotionTransition currentTransition = activeSequence.transitions[currentTransitionIndex];
            
            // 디버그 로그는 logGazeEvents가 활성화된 경우에만 표시
            if (logGazeEvents)
            {
                Debug.Log($"[{gameObject.name}] 시퀀스 처리 중: currentEmotion={currentEmotion}, targetEmotion={currentTransition.fromEmotion}, intensity={currentEmotionIntensity:F2}, requiredIntensity={currentTransition.requiredIntensityToTransition:F2}, timeInState={timeInCurrentState:F2}, minDuration={currentTransition.minDurationInState:F2}");
            }
            
            // 현재 감정이 전환 조건의 출발 감정과 같고, 조건을 만족하는지 확인
            if (currentEmotion == currentTransition.fromEmotion && 
                currentEmotionIntensity >= currentTransition.requiredIntensityToTransition &&
                timeInCurrentState >= currentTransition.minDurationInState)
            {
                if (logGazeEvents)
                {
                    Debug.Log($"[{gameObject.name}] 감정 전환 조건 충족: {currentEmotion} -> {currentTransition.toEmotion}");
                }
                
                // 다음 감정으로 전환
                ChangeEmotionState(currentTransition.toEmotion);
                timeInCurrentState = 0f;
                
                // 다음 전환 인덱스로 이동
                currentTransitionIndex++;
                
                // 시퀀스의 끝에 도달했는지 확인
                if (currentTransitionIndex >= activeSequence.transitions.Count)
                {
                    if (activeSequence.loopSequence)
                    {
                        currentTransitionIndex = 0; // 처음으로 돌아가기
                        if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 시퀀스 반복: {activeSequenceName}");
                    }
                    else
                    {
                        isProcessingSequence = false; // 시퀀스 종료
                        if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 시퀀스 완료: {activeSequenceName}");
                    }
                }
            }
        }
        
        // 시퀀스 강제 종료
        public void StopEmotionSequence()
        {
            if (isProcessingSequence)
            {
                isProcessingSequence = false;
                activeSequence = null;
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 시퀀스 중단: {activeSequenceName}");
                activeSequenceName = "";
            }
        }
        
        // 감정 트리거 이벤트 메소드
        protected virtual void OnEmotionTriggered(EmotionEventData eventData)
        {
            EmotionTriggered?.Invoke(eventData);
        }
        
        // 감정 변경 이벤트 메소드
        protected virtual void OnEmotionChanged(EmotionEventData eventData)
        {
            EmotionChanged?.Invoke(eventData);
        }
        
        // 시선 상태 변경 이벤트 메소드
        protected virtual void OnGazeStatusChanged(bool isGazing)
        {
            GazeStatusChanged?.Invoke(isGazing);
        }
        
        // 디버그용 시각화
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || eyeTrackingTarget == null)
            {
                return;
            }
            
            // 감정 상태 및 강도에 따른 색상 설정
            Color gizmoColor = Color.white;
            if (Application.isPlaying && mappingDict.TryGetValue(currentEmotion, out EmotionGazeMapping mapping))
            {
                gizmoColor = Color.Lerp(Color.white, mapping.gazeColor, currentEmotionIntensity);
            }
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(eyeTrackingTarget.position, gazeDetectionRadius);
            
            // 시선 방향 표시
            if (Application.isPlaying && isPlayerGazing)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(eyeTrackingTarget.position, eyeTrackingTarget.forward * 0.5f);
            }
        }
        
        // 리소스 정리
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            EmotionTriggered = null;
            EmotionChanged = null;
            GazeStatusChanged = null;
            
            // 시퀀스 처리 중지
            StopEmotionSequence();
            
            // 원본 재질 복원
            if (meshRenderer != null && originalMaterial != null)
            {
                meshRenderer.material = originalMaterial;
            }
            
            // 동적으로 생성한 리소스 정리
            if (originalMaterial != null)
            {
                Destroy(originalMaterial);
            }
        }
        
        // 외부에서 EyeInteractable 설정하는 메소드
        public void SetEyeInteractable(EyeInteractable interactable)
        {
            if (interactable != null)
            {
                targetEyeInteractable = interactable;
                eyeInteractable = interactable;
                
                // 시선 추적 타겟도 이에 맞게 업데이트
                eyeTrackingTarget = interactable.transform;
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] EyeInteractable 동적 설정: {interactable.gameObject.name}");
            }
        }
        
        // 현재 감정 상태 정보 가져오기
        public EmotionState GetCurrentEmotion()
        {
            return currentEmotion;
        }
        
        public float GetCurrentEmotionIntensity()
        {
            return currentEmotionIntensity;
        }
        
        public bool IsPlayerLookingAt()
        {
            return isPlayerGazing;
        }
        
        // 외부 참조용 EyeInteractable 반환
        public EyeInteractable GetEyeInteractable()
        {
            return eyeInteractable;
        }
        
        // 시퀀스 상태 확인 함수
        public bool IsSequenceActive()
        {
            return isProcessingSequence;
        }
        
        public string GetActiveSequenceName()
        {
            return activeSequenceName;
        }
        
        public int GetCurrentTransitionIndex()
        {
            return currentTransitionIndex;
        }
        
        // 감정 매핑 가져오기
        public EmotionGazeMapping GetEmotionMapping(EmotionState emotion)
        {
            if (mappingDict.TryGetValue(emotion, out EmotionGazeMapping mapping))
            {
                return mapping;
            }
            return null;
        }
        
        // 새로운 감정 매핑 추가 또는 기존 매핑 업데이트
        public void SetEmotionMapping(EmotionGazeMapping mapping)
        {
            if (mapping != null)
            {
                // 기존 매핑이 있는지 확인
                int existingIndex = emotionMappings.FindIndex(m => m.emotionState == mapping.emotionState);
                
                if (existingIndex >= 0)
                {
                    // 기존 매핑 업데이트
                    emotionMappings[existingIndex] = mapping;
                }
                else
                {
                    // 새 매핑 추가
                    emotionMappings.Add(mapping);
                }
                
                // 매핑 사전 업데이트
                mappingDict[mapping.emotionState] = mapping;
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 매핑 설정/업데이트됨: {mapping.emotionState}");
            }
        }
        
        // 감정 시퀀스 추가
        public void AddEmotionSequence(EmotionSequence sequence)
        {
            if (sequence != null && !string.IsNullOrEmpty(sequence.sequenceName))
            {
                // 기존 시퀀스가 있는지 확인
                int existingIndex = emotionSequences.FindIndex(s => s.sequenceName == sequence.sequenceName);
                
                if (existingIndex >= 0)
                {
                    // 기존 시퀀스 업데이트
                    emotionSequences[existingIndex] = sequence;
                }
                else
                {
                    // 새 시퀀스 추가
                    emotionSequences.Add(sequence);
                }
                
                if (logGazeEvents) Debug.Log($"[{gameObject.name}] 감정 시퀀스 추가/업데이트됨: {sequence.sequenceName}");
            }
        }
        
        // 감정 강도 직접 설정 (외부에서 호출 가능)
        public void SetEmotionIntensity(float intensity)
        {
            currentEmotionIntensity = Mathf.Clamp01(intensity);
        }
        
        // 로깅 활성화/비활성화 설정
        public void SetLogGazeEvents(bool enable)
        {
            logGazeEvents = enable;
        }
        
        // 디버그 시각화 활성화/비활성화 설정
        public void SetShowDebugGizmos(bool enable)
        {
            showDebugGizmos = enable;
        }
    }
}