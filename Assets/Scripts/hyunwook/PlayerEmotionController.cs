using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OVR; // Oculus VR 라이브러리
using ZeeeingGaze;

namespace ZeeeingGaze
{
    public class PlayerEmotionController : MonoBehaviour
    {
        [Header("Emotion Settings - Fast Flow")]
        [SerializeField] private EmotionState currentEmotion = EmotionState.Neutral;
        [SerializeField] private float emotionChangeDelay = 0.1f; // 감소 (0.2 -> 0.1, 더 빠른 반응)
        
        [Header("UI References")]
        [SerializeField] private Image emotionIndicatorImage;
        [SerializeField] private TMPro.TextMeshProUGUI emotionText;
        
        [Header("Input Settings - Optimized")]
        [SerializeField] private OVRInput.Button happyButton = OVRInput.Button.PrimaryIndexTrigger;
        [SerializeField] private OVRInput.Controller happyController = OVRInput.Controller.RTouch;
        
        [SerializeField] private OVRInput.Button sadButton = OVRInput.Button.PrimaryIndexTrigger;
        [SerializeField] private OVRInput.Controller sadController = OVRInput.Controller.LTouch;
        
        [SerializeField] private OVRInput.Button angryButton = OVRInput.Button.PrimaryHandTrigger;
        [SerializeField] private OVRInput.Controller angryController = OVRInput.Controller.RTouch;
        
        [SerializeField] private OVRInput.Button neutralButton = OVRInput.Button.PrimaryHandTrigger;
        [SerializeField] private OVRInput.Controller neutralController = OVRInput.Controller.LTouch;
        
        [Header("Auto-Suggestion System")]
        [SerializeField] private bool enableAutoSuggestion = true; // 자동 감정 제안 시스템
        [SerializeField] private float suggestionInterval = 8f; // 8초마다 제안
        [SerializeField] private float suggestionDuration = 3f; // 제안 표시 시간
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject emotionChangeEffect; // 감정 변경 시 이펙트
        [SerializeField] private AudioSource emotionAudioSource; // 감정 변경 사운드
        [SerializeField] private AudioClip[] emotionSounds; // 감정별 사운드
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // 감정 변경 이벤트
        public event System.Action<EmotionState> OnEmotionChanged;
        
        private float lastEmotionChangeTime;
        private float lastSuggestionTime;
        private EmotionState suggestedEmotion = EmotionState.Neutral;
        private bool isShowingSuggestion = false;
        
        // 감정 사용 통계 (데모용)
        private Dictionary<EmotionState, int> emotionUsageCount = new Dictionary<EmotionState, int>();
        
        private void Start()
        {
            // 초기 감정 상태 설정
            SetEmotion(currentEmotion);
            lastEmotionChangeTime = Time.time;
            lastSuggestionTime = Time.time;
            
            // 감정 사용 통계 초기화
            InitializeEmotionStats();
            
            // 오디오 소스 설정
            if (emotionAudioSource == null)
            {
                emotionAudioSource = gameObject.AddComponent<AudioSource>();
                emotionAudioSource.playOnAwake = false;
                emotionAudioSource.volume = 0.3f;
            }
        }
        
        private void InitializeEmotionStats()
        {
            emotionUsageCount[EmotionState.Happy] = 0;
            emotionUsageCount[EmotionState.Sad] = 0;
            emotionUsageCount[EmotionState.Angry] = 0;
            emotionUsageCount[EmotionState.Neutral] = 0;
        }
        
        private void Update()
        {
            // 감정 변경 지연 시간 확인
            if (Time.time - lastEmotionChangeTime < emotionChangeDelay)
                return;
            
            // VR 컨트롤러 입력 처리
            HandleVRInput();
            
            // 키보드 입력 처리 (디버그/테스트용)
            if (showDebugInfo)
            {
                HandleKeyboardInput();
            }
            
            // 자동 제안 시스템
            if (enableAutoSuggestion)
            {
                HandleAutoSuggestion();
            }
        }
        
        private void HandleVRInput()
        {
            // 오른쪽 트리거로 행복/호감 감정 활성화
            if (OVRInput.GetDown(happyButton, happyController))
            {
                SetEmotion(EmotionState.Happy);
            }
            
            // 왼쪽 트리거로 슬픔/우울 감정 활성화
            else if (OVRInput.GetDown(sadButton, sadController))
            {
                SetEmotion(EmotionState.Sad);
            }
            
            // 오른쪽 그립으로 분노/경계 감정 활성화
            else if (OVRInput.GetDown(angryButton, angryController))
            {
                SetEmotion(EmotionState.Angry);
            }
            
            // 왼쪽 그립으로 중립 감정 (기본값) 활성화
            else if (OVRInput.GetDown(neutralButton, neutralController))
            {
                SetEmotion(EmotionState.Neutral);
            }
        }
        
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetEmotion(EmotionState.Happy);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetEmotion(EmotionState.Sad);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetEmotion(EmotionState.Angry);
            else if (Input.GetKeyDown(KeyCode.Alpha0)) SetEmotion(EmotionState.Neutral);
        }
        
        private void HandleAutoSuggestion()
        {
            // 일정 시간마다 감정 제안
            if (Time.time - lastSuggestionTime >= suggestionInterval && !isShowingSuggestion)
            {
                SuggestOptimalEmotion();
                lastSuggestionTime = Time.time;
            }
        }
        
        private void SuggestOptimalEmotion()
        {
            // 현재 상호작용 중인 NPC 확인
            NPCInteractionManager interactionManager = FindAnyObjectByType<NPCInteractionManager>();
            if (interactionManager == null) return;
            
            NPCController currentNPC = interactionManager.GetCurrentInteractingNPC();
            if (currentNPC == null) return;
            
            // NPC 감정에 맞는 최적 감정 제안
            NPCEmotionController npcEmotion = currentNPC.GetComponent<NPCEmotionController>();
            if (npcEmotion != null)
            {
                EmotionState npcCurrentEmotion = npcEmotion.GetCurrentEmotion();
                suggestedEmotion = GetOptimalEmotionFor(npcCurrentEmotion);
                
                if (suggestedEmotion != currentEmotion)
                {
                    StartCoroutine(ShowEmotionSuggestion());
                }
            }
        }
        
        private EmotionState GetOptimalEmotionFor(EmotionState npcEmotion)
        {
            // NPC 감정에 따른 최적 플레이어 감정 반환
            switch (npcEmotion)
            {
                case EmotionState.Sad:
                    return EmotionState.Happy; // 슬픈 NPC에게는 행복한 감정
                case EmotionState.Angry:
                    return EmotionState.Neutral; // 화난 NPC에게는 중립 감정
                case EmotionState.Neutral:
                    return EmotionState.Happy; // 중립 NPC에게는 행복한 감정
                case EmotionState.Happy:
                    return EmotionState.Happy; // 행복한 NPC와는 같은 감정
                default:
                    return EmotionState.Happy;
            }
        }
        
        private IEnumerator ShowEmotionSuggestion()
        {
            isShowingSuggestion = true;
            
            // UI에 제안 표시 (간단한 텍스트 깜빡임)
            if (emotionText != null)
            {
                string originalText = emotionText.text;
                Color originalColor = emotionText.color;
                
                // 제안 표시
                for (int i = 0; i < 3; i++) // 3번 깜빡임
                {
                    emotionText.text = $"제안: {suggestedEmotion.GetEmotionName()}";
                    emotionText.color = suggestedEmotion.GetEmotionColor();
                    yield return new WaitForSeconds(0.5f);
                    
                    emotionText.text = originalText;
                    emotionText.color = originalColor;
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            yield return new WaitForSeconds(suggestionDuration);
            isShowingSuggestion = false;
        }
        
        private void SetEmotion(EmotionState emotion)
        {
            // 같은 감정이면 무시
            if (currentEmotion == emotion)
                return;
                
            EmotionState previousEmotion = currentEmotion;
            currentEmotion = emotion;
            
            // 감정 변경 시간 업데이트
            lastEmotionChangeTime = Time.time;
            
            // 감정 사용 통계 업데이트
            if (emotionUsageCount.ContainsKey(emotion))
            {
                emotionUsageCount[emotion]++;
            }
            
            // UI 업데이트
            UpdateEmotionUI();
            
            // 시각적 피드백
            PlayEmotionFeedback(emotion);
            
            // 디버그 로그
            if (showDebugInfo)
            {
                Debug.Log($"감정 변경: {previousEmotion} -> {currentEmotion} (빠른 플로우)");
            }
            
            // 이벤트 발생
            OnEmotionChanged?.Invoke(currentEmotion);
        }
        
        private void UpdateEmotionUI()
        {
            if (emotionIndicatorImage != null)
            {
                // 감정에 해당하는 색상 설정
                emotionIndicatorImage.color = currentEmotion.GetEmotionColor();
            }
            
            if (emotionText != null && !isShowingSuggestion)
            {
                // 감정 이름 표시
                emotionText.text = currentEmotion.GetEmotionName();
                emotionText.color = currentEmotion.GetEmotionColor();
            }
        }
        
        private void PlayEmotionFeedback(EmotionState emotion)
        {
            // 시각적 이펙트
            if (emotionChangeEffect != null)
            {
                GameObject effect = Instantiate(emotionChangeEffect, transform.position, Quaternion.identity);
                
                // 이펙트 색상을 감정에 맞게 조정
                ParticleSystem particles = effect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    var main = particles.main;
                    main.startColor = emotion.GetEmotionColor();
                }
                
                Destroy(effect, 2f);
            }
            
            // 오디오 피드백
            PlayEmotionSound(emotion);
            
            // 햅틱 피드백 (VR 컨트롤러 진동)
            PlayHapticFeedback(emotion);
        }
        
        private void PlayEmotionSound(EmotionState emotion)
        {
            if (emotionAudioSource != null && emotionSounds != null)
            {
                int emotionIndex = (int)emotion;
                if (emotionIndex >= 0 && emotionIndex < emotionSounds.Length && emotionSounds[emotionIndex] != null)
                {
                    emotionAudioSource.PlayOneShot(emotionSounds[emotionIndex]);
                }
            }
        }
        
        private void PlayHapticFeedback(EmotionState emotion)
        {
            try
            {
                // 감정에 따른 다른 진동 패턴
                float intensity = 0.3f;
                float duration = 0.1f;
                
                switch (emotion)
                {
                    case EmotionState.Happy:
                        intensity = 0.4f;
                        duration = 0.15f;
                        break;
                    case EmotionState.Angry:
                        intensity = 0.6f;
                        duration = 0.2f;
                        break;
                    case EmotionState.Sad:
                        intensity = 0.2f;
                        duration = 0.3f;
                        break;
                    case EmotionState.Neutral:
                        intensity = 0.1f;
                        duration = 0.05f;
                        break;
                }
                
                // 양쪽 컨트롤러에 진동
                var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                var leftDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                
                if (rightDevice.TryGetHapticCapabilities(out var rightCap) && rightCap.supportsImpulse)
                {
                    rightDevice.SendHapticImpulse(0, intensity, duration);
                }
                
                if (leftDevice.TryGetHapticCapabilities(out var leftCap) && leftCap.supportsImpulse)
                {
                    leftDevice.SendHapticImpulse(0, intensity * 0.7f, duration); // 왼쪽은 약간 약하게
                }
            }
            catch (System.Exception)
            {
                // VR 컨트롤러가 없을 경우 무시
            }
        }
        
        public EmotionState GetCurrentEmotion()
        {
            return currentEmotion;
        }
        
        // 외부에서 강제로 감정 설정
        public void ForceSetEmotion(EmotionState emotion)
        {
            SetEmotion(emotion);
        }
        
        // 현재 감정과 관련된 추가 정보 반환
        public Color GetCurrentEmotionColor()
        {
            return currentEmotion.GetEmotionColor();
        }
        
        public string GetCurrentEmotionName()
        {
            return currentEmotion.GetEmotionName();
        }
        
        // 감정 사용 통계 반환 (데모 완료 시 표시용)
        public Dictionary<EmotionState, int> GetEmotionUsageStats()
        {
            return new Dictionary<EmotionState, int>(emotionUsageCount);
        }
        
        // 가장 많이 사용한 감정 반환
        public EmotionState GetMostUsedEmotion()
        {
            EmotionState mostUsed = EmotionState.Neutral;
            int maxCount = 0;
            
            foreach (var kvp in emotionUsageCount)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostUsed = kvp.Key;
                }
            }
            
            return mostUsed;
        }
        
        // 자동 제안 시스템 활성화/비활성화
        public void SetAutoSuggestionEnabled(bool enabled)
        {
            enableAutoSuggestion = enabled;
            if (!enabled)
            {
                isShowingSuggestion = false;
                StopAllCoroutines();
            }
        }
        
        // 빠른 감정 전환을 위한 연속 입력 감지
        private float lastInputTime = 0f;
        private int rapidInputCount = 0;
        private const float rapidInputWindow = 1.0f; // 1초 내 연속 입력
        
        private bool IsRapidInput()
        {
            if (Time.time - lastInputTime <= rapidInputWindow)
            {
                rapidInputCount++;
                if (rapidInputCount >= 3) // 3번 연속 입력
                {
                    rapidInputCount = 0;
                    return true;
                }
            }
            else
            {
                rapidInputCount = 1;
            }
            
            lastInputTime = Time.time;
            return false;
        }
        
        // 감정 변경 지연 시간 동적 조정
        public void SetEmotionChangeDelay(float delay)
        {
            emotionChangeDelay = Mathf.Max(0.05f, delay); // 최소 0.05초
        }
        
        // 디버그 정보 표시
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"현재 감정: {currentEmotion.GetEmotionName()}");
            GUILayout.Label($"제안 시스템: {(enableAutoSuggestion ? "활성" : "비활성")}");
            GUILayout.Label($"제안 중: {isShowingSuggestion}");
            
            if (isShowingSuggestion)
            {
                GUILayout.Label($"제안 감정: {suggestedEmotion.GetEmotionName()}");
            }
            
            GUILayout.Label("감정 사용 통계:");
            foreach (var kvp in emotionUsageCount)
            {
                GUILayout.Label($"- {kvp.Key.GetEmotionName()}: {kvp.Value}회");
            }
            
            GUILayout.EndArea();
        }
    }
}