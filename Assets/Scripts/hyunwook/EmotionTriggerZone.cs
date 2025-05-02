using UnityEngine;

namespace ZeeeingGaze
{
    // 감정 변화 트리거를 처리하는 클래스
    [RequireComponent(typeof(Collider))]
    public class EmotionTriggerZone : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private EmotionState targetEmotion;
        [SerializeField] private string targetTag = "NPC";
        [SerializeField] private bool oneTimeOnly = false;
        [SerializeField] private float cooldownTime = 2.0f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;
        
        private bool isTriggered = false;
        private float cooldownTimer = 0f;
        
        private void Awake()
        {
            // 트리거 설정
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
        
        private void Update()
        {
            // 쿨다운 타이머 처리
            if (isTriggered && !oneTimeOnly)
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer >= cooldownTime)
                {
                    isTriggered = false;
                    cooldownTimer = 0f;
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered)
            {
                return;
            }
            
            if (other.CompareTag(targetTag))
            {
                NPCEmotionController emotionController = other.GetComponent<NPCEmotionController>();
                if (emotionController != null)
                {
                    emotionController.ChangeEmotionState(targetEmotion);
                    isTriggered = true;
                    
                    Debug.Log($"감정 트리거 존: {other.name}의 감정 상태를 {targetEmotion}으로 변경했습니다.");
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos)
            {
                return;
            }
            
            Gizmos.color = gizmoColor;
            
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                if (collider is BoxCollider)
                {
                    BoxCollider boxCollider = (BoxCollider)collider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (collider is SphereCollider)
                {
                    SphereCollider sphereCollider = (SphereCollider)collider;
                    Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
                }
                else if (collider is CapsuleCollider)
                {
                    CapsuleCollider capsuleCollider = (CapsuleCollider)collider;
                    // 캡슐 콜라이더는 그리기 복잡하여 간단한 와이어스피어로 표시
                    Gizmos.DrawWireSphere(transform.position + capsuleCollider.center, capsuleCollider.radius);
                }
            }
            
            // 트리거 영역 레이블 표시
            Gizmos.color = Color.white;
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"감정 트리거: {targetEmotion}");
            }
        }
        
        // 트리거 리셋 메소드 (외부에서 호출 가능)
        public void ResetTrigger()
        {
            isTriggered = false;
            cooldownTimer = 0f;
        }
    }
}