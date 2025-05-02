using UnityEngine;
using System;

namespace ZeeeingGaze
{
    // 감정 상태를 정의하는 열거형
    public enum EmotionState
    {
        Neutral = 0,
        Happy = 1,
        Interested = 2,
        Angry = 3,
        Sad = 4,
        Surprised = 5,
        Embarrassed = 6,
        Scared = 7
    }

    // 감정 반응 이벤트 데이터
    public struct EmotionEventData
    {
        public EmotionState Emotion { get; private set; }
        public float Intensity { get; private set; }
        public GameObject Source { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public EmotionEventData(EmotionState emotion, float intensity, GameObject source = null)
        {
            Emotion = emotion;
            Intensity = intensity;
            Source = source;
            TimeStamp = DateTime.Now;
        }
        
        // 감정 강도 구간에 따른 설명 반환
        public string GetIntensityDescription()
        {
            if (Intensity < 0.2f) return "미약한";
            if (Intensity < 0.4f) return "약한";
            if (Intensity < 0.6f) return "보통의";
            if (Intensity < 0.8f) return "강한";
            return "매우 강한";
        }
        
        // 감정 상태에 대한 설명 반환
        public string GetEmotionDescription()
        {
            switch (Emotion)
            {
                case EmotionState.Happy:
                    return "행복";
                case EmotionState.Interested:
                    return "관심";
                case EmotionState.Angry:
                    return "분노";
                case EmotionState.Sad:
                    return "슬픔";
                case EmotionState.Surprised:
                    return "놀람";
                case EmotionState.Embarrassed:
                    return "당황";
                case EmotionState.Scared:
                    return "두려움";
                case EmotionState.Neutral:
                default:
                    return "중립적인 감정";
            }
        }
        
        // 이벤트 설명 문자열 반환
        public override string ToString()
        {
            return $"{GetIntensityDescription()} {GetEmotionDescription()} (강도: {Intensity:F2})";
        }
    }
    
    // 감정 상태 확장 메소드
    public static class EmotionStateExtensions
    {
        // 감정 상태에 따른 색상 반환
        public static Color GetEmotionColor(this EmotionState state)
        {
            switch (state)
            {
                case EmotionState.Happy:
                    return new Color(1f, 0.8f, 0f); // 노란색
                case EmotionState.Interested:
                    return new Color(0.5f, 0.8f, 0.2f); // 연두색
                case EmotionState.Angry:
                    return new Color(1f, 0.3f, 0.3f); // 빨간색
                case EmotionState.Sad:
                    return new Color(0.3f, 0.5f, 0.8f); // 파란색
                case EmotionState.Surprised:
                    return new Color(1f, 0.8f, 0.2f); // 밝은 노란색
                case EmotionState.Embarrassed:
                    return new Color(1f, 0.6f, 0.6f); // 분홍색
                case EmotionState.Scared:
                    return new Color(0.6f, 0.4f, 0.8f); // 보라색
                case EmotionState.Neutral:
                default:
                    return Color.white;
            }
        }
        
        // 감정 상태 이름 반환
        public static string GetEmotionName(this EmotionState state)
        {
            switch (state)
            {
                case EmotionState.Happy:
                    return "행복";
                case EmotionState.Interested:
                    return "관심";
                case EmotionState.Angry:
                    return "분노";
                case EmotionState.Sad:
                    return "슬픔";
                case EmotionState.Surprised:
                    return "놀람";
                case EmotionState.Embarrassed:
                    return "당황";
                case EmotionState.Scared:
                    return "두려움";
                case EmotionState.Neutral:
                default:
                    return "중립";
            }
        }
    }
}