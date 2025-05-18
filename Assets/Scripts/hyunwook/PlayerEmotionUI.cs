using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeeeingGaze;

namespace ZeeeingGaze
{
    public class PlayerEmotionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEmotionController playerEmotionController;
        
        [Header("UI Elements")]
        [SerializeField] private Image currentEmotionIcon;
        [SerializeField] private TextMeshProUGUI currentEmotionText;
        [SerializeField] private CanvasGroup emotionPanelCanvasGroup;
        
        [Header("Icon Settings")]
        [SerializeField] private Sprite[] emotionIcons; // 각 감정별 아이콘 (EmotionState 순서대로)
        [SerializeField] private float fadeInSpeed = 3.0f;
        [SerializeField] private float fadeOutDelay = 3.0f; // 표시 후 사라지는 시간
        
        [Header("Control Hints")] 
        [SerializeField] private TextMeshProUGUI controlHintsText;
        [SerializeField] private float controlHintsShowTime = 5.0f; // 게임 시작 후 힌트 표시 시간
        
        private float lastEmotionChangeTime;
        private bool showingControlHints = true;
        private Coroutine fadeCoroutine;
        
        private void Start()
        {
            // 필요한 컴포넌트 찾기
            if (playerEmotionController == null)
            {
                playerEmotionController = FindAnyObjectByType<PlayerEmotionController>();
                if (playerEmotionController == null)
                {
                    Debug.LogWarning("PlayerEmotionController를 찾을 수 없습니다.");
                    enabled = false;
                    return;
                }
            }
            
            // 이벤트 구독
            playerEmotionController.OnEmotionChanged += OnEmotionChanged;
            
            // 초기 UI 상태 설정
            if (emotionPanelCanvasGroup != null)
            {
                emotionPanelCanvasGroup.alpha = 0f;
            }
            
            // 컨트롤 힌트 표시
            ShowControlHints();
            
            // 초기 감정 상태 UI 업데이트
            UpdateEmotionUI(playerEmotionController.GetCurrentEmotion());
            lastEmotionChangeTime = Time.time;
        }
        
        private void OnEmotionChanged(EmotionState newEmotion)
        {
            lastEmotionChangeTime = Time.time;
            UpdateEmotionUI(newEmotion);
            
            // UI 패널 표시
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeInAndOutPanel());
            
            // 컨트롤 힌트 숨기기
            if (showingControlHints)
            {
                HideControlHints();
                showingControlHints = false;
            }
        }
        
        private void UpdateEmotionUI(EmotionState emotion)
        {
            // 감정 이름 표시
            if (currentEmotionText != null)
            {
                currentEmotionText.text = emotion.GetEmotionName();
                currentEmotionText.color = emotion.GetEmotionColor();
            }
            
            // 감정 아이콘 표시
            if (currentEmotionIcon != null)
            {
                int emotionIndex = (int)emotion;
                if (emotionIcons != null && emotionIndex >= 0 && emotionIndex < emotionIcons.Length && emotionIcons[emotionIndex] != null)
                {
                    currentEmotionIcon.sprite = emotionIcons[emotionIndex];
                    currentEmotionIcon.color = emotion.GetEmotionColor();
                }
                else
                {
                    // 아이콘이 없으면 색상 원으로 표시
                    currentEmotionIcon.sprite = null;
                    currentEmotionIcon.color = emotion.GetEmotionColor();
                }
            }
        }
        
        private IEnumerator FadeInAndOutPanel()
        {
            if (emotionPanelCanvasGroup == null) yield break;
            
            // 페이드 인
            float targetAlpha = 1.0f;
            while (emotionPanelCanvasGroup.alpha < targetAlpha)
            {
                emotionPanelCanvasGroup.alpha += Time.deltaTime * fadeInSpeed;
                yield return null;
            }
            emotionPanelCanvasGroup.alpha = targetAlpha;
            
            // 지정된 시간 동안 대기
            yield return new WaitForSeconds(fadeOutDelay);
            
            // 페이드 아웃
            while (emotionPanelCanvasGroup.alpha > 0)
            {
                emotionPanelCanvasGroup.alpha -= Time.deltaTime * fadeInSpeed * 0.5f; // 페이드 아웃은 더 천천히
                yield return null;
            }
            emotionPanelCanvasGroup.alpha = 0;
        }
        
        private void ShowControlHints()
        {
            if (controlHintsText != null)
            {
                controlHintsText.gameObject.SetActive(true);
                controlHintsText.text = 
                    "감정 조작 방법:\n" +
                    "- 오른쪽 트리거: 행복 감정\n" +
                    "- 왼쪽 트리거: 슬픔 감정\n" +
                    "- 오른쪽 그립: 분노 감정\n" +
                    "- B 버튼: 중립 감정\n\n" +
                    "NPC와 시선을 맞추고 감정을 표현해 호감도를 쌓으세요!";
                
                // 일정 시간 후 힌트 자동 숨김
                StartCoroutine(AutoHideControlHints());
            }
        }

        private IEnumerator AutoHideControlHints()
        {
            yield return new WaitForSeconds(controlHintsShowTime);
            HideControlHints();
            showingControlHints = false;
        }
        
        private void HideControlHints()
        {
            if (controlHintsText != null)
            {
                StartCoroutine(FadeOutControlHints());
            }
        }
        
        private IEnumerator FadeOutControlHints()
        {
            if (controlHintsText == null) yield break;
            
            // 캔버스 그룹이 있다면 페이드 아웃, 없으면 그냥 비활성화
            CanvasGroup hintCanvasGroup = controlHintsText.GetComponent<CanvasGroup>();
            
            if (hintCanvasGroup != null)
            {
                while (hintCanvasGroup.alpha > 0)
                {
                    hintCanvasGroup.alpha -= Time.deltaTime;
                    yield return null;
                }
                controlHintsText.gameObject.SetActive(false);
            }
            else
            {
                controlHintsText.gameObject.SetActive(false);
            }
        }
        
        // 현재 감정에 해당하는 컨트롤러 버튼 힌트 표시
        public void ShowCurrentEmotionControlHint()
        {
            if (playerEmotionController == null) return;
            
            EmotionState currentEmotion = playerEmotionController.GetCurrentEmotion();
            string controlHint = "";
            
            switch (currentEmotion)
            {
                case EmotionState.Happy:
                    controlHint = "행복 감정: 오른쪽 트리거";
                    break;
                case EmotionState.Sad:
                    controlHint = "슬픔 감정: 왼쪽 트리거";
                    break;
                case EmotionState.Angry:
                    controlHint = "분노 감정: 오른쪽 그립";
                    break;
                case EmotionState.Neutral:
                    controlHint = "중립 감정: B 버튼";
                    break;
                default:
                    controlHint = "감정 선택: 컨트롤러 버튼 사용";
                    break;
            }
            
            if (controlHintsText != null)
            {
                controlHintsText.text = controlHint;
                controlHintsText.gameObject.SetActive(true);
                
                // 일정 시간 후 자동 숨김
                StartCoroutine(AutoHideCurrentHint());
            }
        }
        
        private IEnumerator AutoHideCurrentHint()
        {
            yield return new WaitForSeconds(2.0f);
            
            if (controlHintsText != null)
            {
                controlHintsText.gameObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (playerEmotionController != null)
            {
                playerEmotionController.OnEmotionChanged -= OnEmotionChanged;
            }
            
            // 코루틴 정리
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
        }
    }
}