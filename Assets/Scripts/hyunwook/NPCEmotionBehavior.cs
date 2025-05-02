using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZeeeingGaze
{
    // 감정 상태에 따른 NPC 행동을 정의하는 클래스
    public class NPCEmotionBehavior : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NPCEmotionController emotionController;
        [SerializeField] private Transform headTransform;
        
        [Header("Behavior Settings")]
        [SerializeField] private bool lookAtPlayerWhenHappy = true;
        [SerializeField] private bool avoidPlayerGazeWhenEmbarrassed = true;
        [SerializeField] private bool approachPlayerWhenInterested = true;
        [SerializeField] private bool lookDownWhenSad = true;
        [SerializeField] private bool lookUpAndDownWhenAngry = true;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private float rotateSpeed = 90.0f;
        [SerializeField] private float minApproachDistance = 1.5f;
        [SerializeField] private float behaviorDuration = 2.0f;
        [SerializeField] private bool returnToOriginalPosition = true; // 원래 위치로 돌아갈지 여부 설정 옵션 추가
        
        [Header("Head Movement")]
        [SerializeField] private Vector2 lookDownRange = new Vector2(30f, 45f);
        [SerializeField] private Vector2 lookUpRange = new Vector2(15f, 30f);
        [SerializeField] private float headMovementSpeed = 2.0f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        // 내부 상태
        private Transform playerTransform;
        private Coroutine currentBehaviorCoroutine;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Quaternion originalHeadRotation;
        private bool isPerformingBehavior = false;
        private bool isInitialized = false;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            
            // 초기 상태 저장
            SaveOriginalState();
            
            // 감정 이벤트 구독
            SubscribeToEmotionEvents();
        }
        
        private void Initialize()
        {
            // 감정 컨트롤러 찾기
            if (emotionController == null)
            {
                emotionController = GetComponent<NPCEmotionController>();
                if (emotionController == null)
                {
                    Debug.LogError($"[{gameObject.name}] NPCEmotionBehavior: EmotionController 컴포넌트를 찾을 수 없습니다.");
                }
            }
            
            // 플레이어 카메라 찾기
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] NPCEmotionBehavior: Main Camera를 찾을 수 없습니다. 플레이어 추적이 작동하지 않을 수 있습니다.");
            }
            
            // 헤드 트랜스폼이 없으면 현재 트랜스폼 사용
            if (headTransform == null)
            {
                headTransform = transform;
            }
            
            isInitialized = true;
        }
        
        private void SaveOriginalState()
        {
            // 현재 상태를 원본 상태로 저장
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalHeadRotation = headTransform.localRotation;
        }
        
        private void SubscribeToEmotionEvents()
        {
            if (emotionController != null)
            {
                // 기존 구독 해제 후 다시 구독
                emotionController.EmotionTriggered -= OnEmotionTriggered;
                emotionController.GazeStatusChanged -= OnGazeStatusChanged;
                
                emotionController.EmotionTriggered += OnEmotionTriggered;
                emotionController.GazeStatusChanged += OnGazeStatusChanged;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] NPCEmotionBehavior: EmotionController가 없습니다.");
            }
        }
        
        private void OnEnable()
        {
            // 활성화될 때 이벤트 재구독
            SubscribeToEmotionEvents();
        }
        
        private void OnDisable()
        {
            // 비활성화될 때 이벤트 구독 해제
            UnsubscribeFromEmotionEvents();
        }
        
        private void OnDestroy()
        {
            // 객체 파괴 시 이벤트 구독 해제
            UnsubscribeFromEmotionEvents();
        }
        
        private void UnsubscribeFromEmotionEvents()
        {
            // 이벤트 구독 해제
            if (emotionController != null)
            {
                emotionController.EmotionTriggered -= OnEmotionTriggered;
                emotionController.GazeStatusChanged -= OnGazeStatusChanged;
            }
        }
        
        // 여러 행동 간의 우선순위 처리를 위한 행동 유형 열거형 추가
        private enum BehaviorType
        {
            None,
            LookAt,
            Avoid,
            Approach,
            LookUpDown,
            LookDown
        }
        
        // 현재 실행 중인 행동 유형
        private BehaviorType currentBehaviorType = BehaviorType.None;
        
        // 시선 상태 변경 이벤트 핸들러
        private void OnGazeStatusChanged(bool isGazing)
        {
            if (isGazing)
            {
                // 시선이 감지됐을 때 즉각적인 반응
                ReactToGazeDetection();
            }
            else
            {
                // 시선이 끊겼을 때 반응
                ReactToGazeLost();
            }
        }
        
        // 시선 감지 시 즉각적인 반응
        private void ReactToGazeDetection()
        {
            // 애니메이션이나 짧은 반응 동작 실행 가능
            if (debugMode) Debug.Log($"[{gameObject.name}] 시선 감지에 반응");
        }
        
        // 시선 끊김 시 반응
        private void ReactToGazeLost()
        {
            // 애니메이션이나 짧은 반응 동작 실행 가능
            if (debugMode) Debug.Log($"[{gameObject.name}] 시선 끊김에 반응");
        }
        
        // 감정 트리거 이벤트 핸들러
        private void OnEmotionTriggered(EmotionEventData eventData)
        {
            // 이미 행동 중이면 중지
            StopCurrentBehavior();
            
            // 카메라 참조 확인
            if (playerTransform == null && Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
            
            if (playerTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 감정 행동 실행 실패: 플레이어 Transform을 찾을 수 없습니다.");
                return;
            }
            
            // 현재 감정 상태에 따른 행동 결정 및 우선순위 처리
            BehaviorType newBehaviorType = BehaviorType.None;
            
            switch (eventData.Emotion)
            {
                case EmotionState.Happy:
                    if (lookAtPlayerWhenHappy)
                    {
                        newBehaviorType = BehaviorType.LookAt;
                        currentBehaviorCoroutine = StartCoroutine(LookAtPlayer(behaviorDuration));
                    }
                    break;
                    
                case EmotionState.Embarrassed:
                    if (avoidPlayerGazeWhenEmbarrassed)
                    {
                        newBehaviorType = BehaviorType.Avoid;
                        currentBehaviorCoroutine = StartCoroutine(AvoidPlayerGaze(behaviorDuration));
                    }
                    break;
                    
                case EmotionState.Interested:
                    if (approachPlayerWhenInterested)
                    {
                        newBehaviorType = BehaviorType.Approach;
                        currentBehaviorCoroutine = StartCoroutine(ApproachPlayer(behaviorDuration, minApproachDistance));
                    }
                    break;
                    
                case EmotionState.Angry:
                    if (lookUpAndDownWhenAngry)
                    {
                        newBehaviorType = BehaviorType.LookUpDown;
                        currentBehaviorCoroutine = StartCoroutine(LookUpAndDown(behaviorDuration));
                    }
                    break;
                    
                case EmotionState.Sad:
                    if (lookDownWhenSad)
                    {
                        newBehaviorType = BehaviorType.LookDown;
                        currentBehaviorCoroutine = StartCoroutine(LookDown(behaviorDuration));
                    }
                    break;
            }
            
            currentBehaviorType = newBehaviorType;
            
            if (debugMode) Debug.Log($"[{gameObject.name}] 감정 행동 실행: {eventData.Emotion}, 행동 유형: {currentBehaviorType}");
        }
        
        // 현재 실행 중인 행동 중지
        private void StopCurrentBehavior()
        {
            if (currentBehaviorCoroutine != null)
            {
                StopCoroutine(currentBehaviorCoroutine);
                currentBehaviorCoroutine = null;
            }
            
            currentBehaviorType = BehaviorType.None;
            isPerformingBehavior = false;
        }
        
        // 플레이어를 바라보는 코루틴
        private IEnumerator LookAtPlayer(float duration)
        {
            if (playerTransform == null)
            {
                yield break;
            }
            
            isPerformingBehavior = true;
            float elapsed = 0f;
            
            while (elapsed < duration && playerTransform != null)
            {
                // 플레이어 방향으로 부드럽게 회전
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0; // Y축 회전만 처리
                
                if (direction.sqrMagnitude > 0.001f) // 0에 너무 가까울 경우 방지
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        rotateSpeed * Time.deltaTime
                    );
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원래 상태로 복귀 (설정 옵션에 따라)
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalState());
            }
            else
            {
                isPerformingBehavior = false;
            }
        }
        
        // 플레이어 시선을 피하는 코루틴
        private IEnumerator AvoidPlayerGaze(float duration)
        {
            if (playerTransform == null)
            {
                yield break;
            }
            
            isPerformingBehavior = true;
            float elapsed = 0f;
            
            while (elapsed < duration && playerTransform != null)
            {
                // 플레이어 반대 방향으로 부드럽게 회전
                Vector3 direction = transform.position - playerTransform.position;
                direction.y = 0; // Y축 회전만 처리
                
                if (direction.sqrMagnitude > 0.001f) // 0에 너무 가까울 경우 방지
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        rotateSpeed * Time.deltaTime
                    );
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원래 상태로 복귀 (설정 옵션에 따라)
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalState());
            }
            else
            {
                isPerformingBehavior = false;
            }
        }
        
        // 플레이어에게 접근하는 코루틴
        private IEnumerator ApproachPlayer(float duration, float minDistance)
        {
            if (playerTransform == null)
            {
                yield break;
            }
            
            isPerformingBehavior = true;
            float elapsed = 0f;
            Vector3 startPosition = transform.position; // 시작 위치 저장
            
            while (elapsed < duration && playerTransform != null)
            {
                // 플레이어와의 거리 확인
                Vector3 direction = playerTransform.position - transform.position;
                float distance = direction.magnitude;
                
                // 최소 거리 이상일 때만 이동
                if (distance > minDistance)
                {
                    direction.Normalize();
                    
                    // NavMeshAgent가 있을 경우 사용하는 것이 좋으나, 
                    // 현재 코드에서는 간단한 이동만 처리
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    
                    // 이동 방향으로 회전
                    direction.y = 0;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation, 
                            targetRotation, 
                            rotateSpeed * Time.deltaTime
                        );
                    }
                }
                else
                {
                    // 최소 거리에 도달하면 플레이어를 바라봄
                    Vector3 lookDirection = playerTransform.position - transform.position;
                    lookDirection.y = 0;
                    
                    if (lookDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation, 
                            targetRotation, 
                            rotateSpeed * Time.deltaTime
                        );
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원래 상태로 복귀 (설정 옵션에 따라)
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalState());
            }
            else
            {
                isPerformingBehavior = false;
            }
        }
        
        // 위아래로 훑어보는 코루틴 (경계/분노)
        private IEnumerator LookUpAndDown(float duration)
        {
            isPerformingBehavior = true;
            float elapsed = 0f;
            bool lookingUp = true;
            
            // 플레이어를 향해 먼저 회전 (플레이어 참조가 있을 경우)
            if (playerTransform != null)
            {
                Vector3 direction = playerTransform.position - transform.position;
                direction.y = 0;
                
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        rotateSpeed * 2f
                    );
                    
                    // 충분히 회전할 시간을 줌
                    yield return new WaitForSeconds(0.3f);
                }
            }
            
            // 머리 초기 각도 저장
            Quaternion initialHeadRotation = headTransform.localRotation;
            
            while (elapsed < duration)
            {
                // 위아래로 머리 움직임
                if (lookingUp)
                {
                    // 위로 올려보기
                    float upAngle = Random.Range(lookUpRange.x, lookUpRange.y);
                    Quaternion targetRotation = initialHeadRotation * Quaternion.Euler(upAngle, 0, 0);
                    
                    headTransform.localRotation = Quaternion.Slerp(
                        headTransform.localRotation,
                        targetRotation,
                        headMovementSpeed * Time.deltaTime
                    );
                    
                    if (Quaternion.Angle(headTransform.localRotation, targetRotation) < 5f)
                    {
                        lookingUp = false;
                        yield return new WaitForSeconds(0.2f); // 잠시 멈춤
                    }
                }
                else
                {
                    // 아래로 내려보기
                    float downAngle = Random.Range(lookDownRange.x, lookDownRange.y);
                    Quaternion targetRotation = initialHeadRotation * Quaternion.Euler(-downAngle, 0, 0);
                    
                    headTransform.localRotation = Quaternion.Slerp(
                        headTransform.localRotation,
                        targetRotation,
                        headMovementSpeed * Time.deltaTime
                    );
                    
                    if (Quaternion.Angle(headTransform.localRotation, targetRotation) < 5f)
                    {
                        lookingUp = true;
                        yield return new WaitForSeconds(0.2f); // 잠시 멈춤
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원래 상태로 복귀 (설정 옵션에 따라)
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalState());
            }
            else
            {
                // 머리 각도만 원래대로
                StartCoroutine(ReturnHeadToOriginalState());
            }
        }
        
        // 고개를 아래로 떨구는 코루틴 (슬픔/우울)
        private IEnumerator LookDown(float duration)
        {
            isPerformingBehavior = true;
            float elapsed = 0f;
            
            // 머리 초기 각도 저장
            Quaternion initialHeadRotation = headTransform.localRotation;
            
            // 아래를 바라보는 각도 설정
            float downAngle = Random.Range(lookDownRange.x, lookDownRange.y);
            Quaternion targetRotation = initialHeadRotation * Quaternion.Euler(-downAngle, 0, 0);
            
            // 천천히 고개 떨구기
            float lerpTime = 0f;
            while (lerpTime < 1f)
            {
                headTransform.localRotation = Quaternion.Slerp(
                    initialHeadRotation,
                    targetRotation,
                    lerpTime
                );
                
                lerpTime += Time.deltaTime * headMovementSpeed * 0.5f; // 천천히 움직임
                yield return null;
            }
            
            // 고개 떨군 상태 유지
            while (elapsed < duration)
            {
                // 약간의 미세한 움직임 추가 가능
                float microMovement = Mathf.Sin(Time.time * 2f) * 1.5f;
                headTransform.localRotation = targetRotation * Quaternion.Euler(microMovement, 0, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 원래 상태로 복귀 (설정 옵션에 따라)
            if (returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalState());
            }
            else
            {
                // 머리 각도만 원래대로
                StartCoroutine(ReturnHeadToOriginalState());
            }
        }
        
        // 머리 각도만 원래대로 돌리는 코루틴
        private IEnumerator ReturnHeadToOriginalState()
        {
            float returnDuration = 1.0f;
            float elapsed = 0f;
            
            Quaternion currentHeadRotation = headTransform.localRotation;
            
            while (elapsed < returnDuration)
            {
                float t = elapsed / returnDuration;
                headTransform.localRotation = Quaternion.Slerp(currentHeadRotation, originalHeadRotation, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            headTransform.localRotation = originalHeadRotation;
            isPerformingBehavior = false;
        }
        
        // 원래 상태로 복귀하는 코루틴
        private IEnumerator ReturnToOriginalState()
        {
            float returnDuration = 1.0f;
            float elapsed = 0f;
            
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;
            Quaternion currentHeadRotation = headTransform.localRotation;
            
            while (elapsed < returnDuration)
            {
                // 위치, 회전 보간
                float t = elapsed / returnDuration;
                transform.position = Vector3.Lerp(currentPosition, originalPosition, t);
                transform.rotation = Quaternion.Slerp(currentRotation, originalRotation, t);
                headTransform.localRotation = Quaternion.Slerp(currentHeadRotation, originalHeadRotation, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 정확한 원래 상태로 설정
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            headTransform.localRotation = originalHeadRotation;
            
            isPerformingBehavior = false;
        }
        
        // 원본 상태 저장 위치 업데이트 (예: NPC가 이동한 후 현재 위치를 새로운 원본으로 설정)
        public void UpdateOriginalState()
        {
            SaveOriginalState();
            if (debugMode) Debug.Log($"[{gameObject.name}] 원본 상태 업데이트됨");
        }
        
        // 외부에서 행동 중지 요청
        public void RequestStopBehavior()
        {
            StopCurrentBehavior();
            if (debugMode) Debug.Log($"[{gameObject.name}] 행동 중지 요청 처리됨");
        }
        
        // 현재 행동 상태 확인
        public bool IsPerformingBehavior()
        {
            return isPerformingBehavior;
        }
        
        // 현재 행동 유형 확인
        public string GetCurrentBehaviorType()
        {
            return currentBehaviorType.ToString();
        }
    }
}