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
        [Header("Emotion Settings")]
        [SerializeField] private EmotionState currentEmotion = EmotionState.Neutral;
        [SerializeField] private float emotionChangeDelay = 0.2f; // 감정 변경 간 지연 시간
        
        [Header("UI References")]
        [SerializeField] private Image emotionIndicatorImage;
        [SerializeField] private TMPro.TextMeshProUGUI emotionText;
        
        [Header("Input Settings")]
        [SerializeField] private OVRInput.Button happyButton = OVRInput.Button.PrimaryIndexTrigger;
        [SerializeField] private OVRInput.Controller happyController = OVRInput.Controller.RTouch;
        
        [SerializeField] private OVRInput.Button sadButton = OVRInput.Button.PrimaryIndexTrigger;
        [SerializeField] private OVRInput.Controller sadController = OVRInput.Controller.LTouch;
        
        [SerializeField] private OVRInput.Button angryButton = OVRInput.Button.PrimaryHandTrigger;
        [SerializeField] private OVRInput.Controller angryController = OVRInput.Controller.RTouch;
        
        [SerializeField] private OVRInput.Button neutralButton = OVRInput.Button.PrimaryHandTrigger;
        [SerializeField] private OVRInput.Controller neutralController = OVRInput.Controller.LTouch;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // 감정 변경 이벤트
        public event System.Action<EmotionState> OnEmotionChanged;
        
        private float lastEmotionChangeTime;
        
        private void Start()
        {
            // 초기 감정 상태 설정
            SetEmotion(currentEmotion);
            lastEmotionChangeTime = Time.time;
        }
        
        private void Update()
        {
            // 감정 변경 지연 시간 확인
            if (Time.time - lastEmotionChangeTime < emotionChangeDelay)
                return;
            
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
            
            // B 버튼으로 중립 감정 (기본값) 활성화
            else if (OVRInput.GetDown(neutralButton, neutralController))
            {
                SetEmotion(EmotionState.Neutral);
            }
            
            // 디버그 모드일 때 키보드 입력으로도 테스트 가능하게 설정
            if (showDebugInfo)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetEmotion(EmotionState.Happy);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SetEmotion(EmotionState.Sad);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SetEmotion(EmotionState.Angry);
                else if (Input.GetKeyDown(KeyCode.Alpha0)) SetEmotion(EmotionState.Neutral);
            }
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
            
            // UI 업데이트
            UpdateEmotionUI();
            
            // 디버그 로그
            if (showDebugInfo)
            {
                Debug.Log($"감정 변경: {previousEmotion} -> {currentEmotion}");
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
            
            if (emotionText != null)
            {
                // 감정 이름 표시
                emotionText.text = currentEmotion.GetEmotionName();
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
    }
}