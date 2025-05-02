using UnityEngine;
using UnityEngine.VFX;  // VisualEffect 사용을 위한 네임스페이스 추가

namespace ZeeeingGaze
{
    [System.Serializable]
    public class EmotionGazeMapping
    {
        [Header("Emotion Settings")]
        public EmotionState emotionState;
        public float gazeStabilityDuration = 1.0f;      // 시선을 얼마나 유지해야 하는지
        public float gazeSensitivity = 1.0f;            // 감정 상태에 따른 시선 민감도
        public float emotionResponseThreshold = 0.5f;   // 감정 반응이 트리거되는 임계값
        public float emotionBuildupRate = 0.1f;         // 감정이 쌓이는 속도
        public float emotionDecayRate = 0.05f;          // 감정이 감소하는 속도
        
        [Header("Visual Feedback")]
        public Color gazeColor = Color.white;           // 감정 상태에 따른 색상
        public VisualEffectAsset gazeVFXAsset;          // VisualEffectAsset으로 수정
        public AnimationClip emotionAnimation;          // 감정 애니메이션
        
        [Header("Audio Feedback")]
        public AudioClip emotionSoundEffect;            // 감정 사운드 효과
        [Range(0f, 1f)]
        public float soundEffectVolume = 0.7f;          // 사운드 효과 볼륨
        
        [Header("Gaze Behavior")]
        public bool maintainGaze = true;                // 시선 유지 여부
        public bool avoidGaze = false;                  // 시선 회피 여부
        public float gazeDurationVariation = 0.2f;      // 자연스러운 시선 변화를 위한 무작위 변화량
        
        // 제안서에 명시된 감정별 행동 규칙
        [Header("Predefined Behaviors")]
        [Tooltip("호감/행복 감정일 때 눈맞춤 관련 설정")]
        public bool enableDirectEyeContact = true;      // 직접적인 눈맞춤
        
        [Tooltip("경계/분노 감정일 때 위아래 훑어보기 설정")]
        public bool enableLookUpAndDown = false;        // 위아래 훑어보기
        
        [Tooltip("슬픔/우울 감정일 때 고개 떨구기 설정")]
        public bool enableLookDown = false;             // 고개 떨구기
        
        // 기본 생성자
        public EmotionGazeMapping()
        {
            // 기본 설정 초기화
            emotionState = EmotionState.Neutral;
            gazeColor = Color.white;
        }
        
        // 감정별 기본 매핑 생성 메소드
        public static EmotionGazeMapping CreateHappyMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Happy,
                gazeStabilityDuration = 2.0f,
                gazeSensitivity = 1.2f,
                emotionBuildupRate = 0.15f,
                emotionDecayRate = 0.03f,
                gazeColor = new Color(1f, 0.8f, 0f),  // 밝은 노란색
                maintainGaze = true,
                avoidGaze = false,
                enableDirectEyeContact = true,
                soundEffectVolume = 0.8f
            };
        }
        
        public static EmotionGazeMapping CreateAngryMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Angry,
                gazeStabilityDuration = 0.8f,
                gazeSensitivity = 1.5f,
                emotionBuildupRate = 0.2f,
                emotionDecayRate = 0.05f,
                gazeColor = new Color(1f, 0.3f, 0.3f),  // 빨간색
                maintainGaze = true,
                avoidGaze = false,
                enableLookUpAndDown = true,
                soundEffectVolume = 0.9f
            };
        }
        
        public static EmotionGazeMapping CreateSadMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Sad,
                gazeStabilityDuration = 1.5f,
                gazeSensitivity = 0.7f,
                emotionBuildupRate = 0.05f,
                emotionDecayRate = 0.02f,
                gazeColor = new Color(0.3f, 0.5f, 0.8f),  // 파란색
                maintainGaze = false,
                avoidGaze = true,
                enableLookDown = true,
                soundEffectVolume = 0.6f
            };
        }
        
        public static EmotionGazeMapping CreateEmbarrassedMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Embarrassed,
                gazeStabilityDuration = 0.5f,
                gazeSensitivity = 1.3f,
                emotionBuildupRate = 0.18f,
                emotionDecayRate = 0.08f,
                gazeColor = new Color(1f, 0.6f, 0.6f),  // 분홍색
                maintainGaze = false,
                avoidGaze = true,
                soundEffectVolume = 0.75f
            };
        }
        
        public static EmotionGazeMapping CreateInterestedMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Interested,
                gazeStabilityDuration = 1.2f,
                gazeSensitivity = 1.4f,
                emotionBuildupRate = 0.17f,
                emotionDecayRate = 0.04f,
                gazeColor = new Color(0.5f, 0.8f, 0.2f),  // 연두색
                maintainGaze = true,
                avoidGaze = false,
                enableDirectEyeContact = true,
                soundEffectVolume = 0.8f
            };
        }
        
        public static EmotionGazeMapping CreateSurprisedMapping()
        {
            return new EmotionGazeMapping
            {
                emotionState = EmotionState.Surprised,
                gazeStabilityDuration = 0.3f,
                gazeSensitivity = 1.6f,
                emotionBuildupRate = 0.25f,
                emotionDecayRate = 0.12f,
                gazeColor = new Color(1f, 0.8f, 0.2f),  // 밝은 노란색
                maintainGaze = true,
                avoidGaze = false,
                soundEffectVolume = 0.9f
            };
        }
        
        // 두 감정 매핑 사이를 보간하는 메소드
        public static EmotionGazeMapping Lerp(EmotionGazeMapping a, EmotionGazeMapping b, float t)
        {
            t = Mathf.Clamp01(t);
            
            EmotionGazeMapping result = new EmotionGazeMapping();
            
            // emotionState는 보간하지 않고 t값에 따라 결정
            result.emotionState = t < 0.5f ? a.emotionState : b.emotionState;
            
            // 수치형 속성 보간
            result.gazeStabilityDuration = Mathf.Lerp(a.gazeStabilityDuration, b.gazeStabilityDuration, t);
            result.gazeSensitivity = Mathf.Lerp(a.gazeSensitivity, b.gazeSensitivity, t);
            result.emotionResponseThreshold = Mathf.Lerp(a.emotionResponseThreshold, b.emotionResponseThreshold, t);
            result.emotionBuildupRate = Mathf.Lerp(a.emotionBuildupRate, b.emotionBuildupRate, t);
            result.emotionDecayRate = Mathf.Lerp(a.emotionDecayRate, b.emotionDecayRate, t);
            result.gazeColor = Color.Lerp(a.gazeColor, b.gazeColor, t);
            result.gazeDurationVariation = Mathf.Lerp(a.gazeDurationVariation, b.gazeDurationVariation, t);
            result.soundEffectVolume = Mathf.Lerp(a.soundEffectVolume, b.soundEffectVolume, t);
            
            // 참조형 속성은 t값에 따라 결정
            result.gazeVFXAsset = t < 0.5f ? a.gazeVFXAsset : b.gazeVFXAsset;
            result.emotionAnimation = t < 0.5f ? a.emotionAnimation : b.emotionAnimation;
            result.emotionSoundEffect = t < 0.5f ? a.emotionSoundEffect : b.emotionSoundEffect;
            
            // 불리언 속성은 t값에 따라 결정
            result.maintainGaze = t < 0.5f ? a.maintainGaze : b.maintainGaze;
            result.avoidGaze = t < 0.5f ? a.avoidGaze : b.avoidGaze;
            result.enableDirectEyeContact = t < 0.5f ? a.enableDirectEyeContact : b.enableDirectEyeContact;
            result.enableLookUpAndDown = t < 0.5f ? a.enableLookUpAndDown : b.enableLookUpAndDown;
            result.enableLookDown = t < 0.5f ? a.enableLookDown : b.enableLookDown;
            
            return result;
        }
        
        // 매핑 복제 메소드
        public EmotionGazeMapping Clone()
        {
            EmotionGazeMapping clone = new EmotionGazeMapping();
            
            // 모든 속성 복사
            clone.emotionState = this.emotionState;
            clone.gazeStabilityDuration = this.gazeStabilityDuration;
            clone.gazeSensitivity = this.gazeSensitivity;
            clone.emotionResponseThreshold = this.emotionResponseThreshold;
            clone.emotionBuildupRate = this.emotionBuildupRate;
            clone.emotionDecayRate = this.emotionDecayRate;
            clone.gazeColor = this.gazeColor;
            clone.gazeVFXAsset = this.gazeVFXAsset;
            clone.emotionAnimation = this.emotionAnimation;
            clone.emotionSoundEffect = this.emotionSoundEffect;
            clone.soundEffectVolume = this.soundEffectVolume;
            clone.maintainGaze = this.maintainGaze;
            clone.avoidGaze = this.avoidGaze;
            clone.gazeDurationVariation = this.gazeDurationVariation;
            clone.enableDirectEyeContact = this.enableDirectEyeContact;
            clone.enableLookUpAndDown = this.enableLookUpAndDown;
            clone.enableLookDown = this.enableLookDown;
            
            return clone;
        }
    }
}