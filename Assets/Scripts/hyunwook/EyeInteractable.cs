using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace ZeeeingGaze
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EyeInteractable : MonoBehaviour
    {
        [Header("Interactable Settings")]
        [SerializeField] private Transform targetPoint; // 인스펙터에서 설정 가능한 타겟 포인트 (일반적으로 NPC의 눈 위치)
        [SerializeField] private float interactionRadius = 0.05f; // 상호작용 영역 반경
        
        [Header("Events")]
        [SerializeField] private UnityEvent<GameObject> OnObjectHover;
        [SerializeField] private UnityEvent<GameObject> OnObjectHoverExit;
        
        [Header("Visual Feedback")]
        [SerializeField] private Material OnHoverActiveMaterial;
        [SerializeField] private Material OnHoverInactiveMaterial;
        [SerializeField] private bool applyMaterialChange = true;
        [SerializeField] private bool useMaterialPropertyBlock = true; // 성능 최적화를 위한 옵션
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        
        private Renderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Material originalMaterial;
        private bool isInitialized = false;
        private bool wasHovered = false; // 이전 프레임의 Hover 상태
        
        public bool IsHovered { get; set; }
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (isInitialized) return;
            
            // 렌더러 컴포넌트 찾기
            FindRenderer();
            
            // Rigidbody 설정
            SetupRigidbody();
            
            // 콜라이더 설정
            SetupCollider();
            
            // PropertyBlock 초기화
            if (useMaterialPropertyBlock && meshRenderer != null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            
            // 원본 재질 저장
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalMaterial = new Material(meshRenderer.material);
            }
            
            isInitialized = true;
        }
        
        private void FindRenderer()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                // 만약 MeshRenderer가 없다면 SkinnedMeshRenderer를 찾아보기
                meshRenderer = GetComponent<SkinnedMeshRenderer>();
            }
            
            if (meshRenderer == null)
            {
                // 자식 오브젝트에서 렌더러 찾기
                meshRenderer = GetComponentInChildren<Renderer>();
            }
            
            if (meshRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No renderer found on this object or its children. Material changes will be disabled.");
                applyMaterialChange = false;
            }
        }
        
        private void SetupRigidbody()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 물리 영향 안받도록 설정
                rb.useGravity = false; // 중력 비활성화
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 연속 충돌 감지
            }
        }
        
        private void SetupCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true; // 트리거로 설정
            }
        }
        
        private void Start()
        {
            // Awake에서 초기화가 안 됐을 경우를 대비
            if (!isInitialized)
            {
                Initialize();
            }
            
            // 타겟 포인트가 설정되지 않은 경우 현재 오브젝트의 위치 사용
            if (targetPoint == null)
            {
                // 기본적으로 눈 위치를 찾기 위한 시도
                Transform eyeTransform = transform.Find("Eye") ?? transform.Find("Eyes") ?? 
                                         transform.Find("Head/Eye") ?? transform.Find("Head/Eyes");
                
                if (eyeTransform != null)
                {
                    targetPoint = eyeTransform;
                    Debug.Log($"[{gameObject.name}] 자동으로 눈 위치 타겟 포인트를 찾았습니다: {targetPoint.name}");
                }
                else
                {
                    // 못 찾으면 현재 객체의 위치에서 약간 위로 설정
                    GameObject target = new GameObject($"{gameObject.name}_EyeTarget");
                    target.transform.SetParent(transform);
                    target.transform.localPosition = new Vector3(0, 0.1f, 0);
                    targetPoint = target.transform;
                    Debug.Log($"[{gameObject.name}] 기본 타겟 포인트를 생성했습니다.");
                }
            }
        }
        
        private void Update()
        {
            // 호버 상태 변경 감지
            if (wasHovered != IsHovered)
            {
                if (IsHovered)
                {
                    OnHoverEnter();
                }
                else
                {
                    OnHoverExit();
                }
                
                wasHovered = IsHovered;
            }
            
            // 현재 상태에 따른 재질 업데이트
            UpdateMaterial();
        }
        
        private void OnHoverEnter()
        {
            // 호버 시작 이벤트 발생
            OnObjectHover?.Invoke(gameObject);
        }
        
        private void OnHoverExit()
        {
            // 호버 종료 이벤트 발생
            OnObjectHoverExit?.Invoke(gameObject);
        }
        
        private void UpdateMaterial()
        {
            // 재질 변경이 비활성화되었거나 렌더러가 없는 경우 무시
            if (!applyMaterialChange || meshRenderer == null) return;

            try
            {
                if (IsHovered)
                {
                    if (useMaterialPropertyBlock && propertyBlock != null)
                    {
                        // PropertyBlock 사용 (성능 최적화)
                        if (OnHoverActiveMaterial != null)
                        {
                            meshRenderer.GetPropertyBlock(propertyBlock);
                            
                            // 지원되는 프로퍼티에만 접근
                            ApplyColorToPropertyBlock(propertyBlock, OnHoverActiveMaterial);
                            
                            meshRenderer.SetPropertyBlock(propertyBlock);
                        }
                    }
                    else
                    {
                        // 직접 재질 변경
                        if (OnHoverActiveMaterial != null)
                        {
                            meshRenderer.material = OnHoverActiveMaterial;
                        }
                    }
                }
                else
                {
                    if (useMaterialPropertyBlock && propertyBlock != null)
                    {
                        // PropertyBlock 사용 (성능 최적화)
                        if (OnHoverInactiveMaterial != null)
                        {
                            meshRenderer.GetPropertyBlock(propertyBlock);
                            
                            // 지원되는 프로퍼티에만 접근
                            ApplyColorToPropertyBlock(propertyBlock, OnHoverInactiveMaterial);
                            
                            meshRenderer.SetPropertyBlock(propertyBlock);
                        }
                        else if (originalMaterial != null)
                        {
                            // 원래 색상으로 복원
                            meshRenderer.GetPropertyBlock(propertyBlock);
                            
                            // 지원되는 프로퍼티에만 접근
                            ApplyColorToPropertyBlock(propertyBlock, originalMaterial);
                            
                            meshRenderer.SetPropertyBlock(propertyBlock);
                        }
                    }
                    else
                    {
                        // 직접 재질 변경
                        if (OnHoverInactiveMaterial != null)
                        {
                            meshRenderer.material = OnHoverInactiveMaterial;
                        }
                        else if (originalMaterial != null)
                        {
                            meshRenderer.material = originalMaterial;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] 재질 업데이트 중 오류 발생: {e.Message}");
                // 오류 발생 시 재질 변경 비활성화
                applyMaterialChange = false;
            }
        }

        // 지원되는 색상 프로퍼티를 찾아 적용
        private void ApplyColorToPropertyBlock(MaterialPropertyBlock block, Material sourceMaterial)
        {
            // 렌더러의 현재 셰이더 확인
            Shader shader = meshRenderer.sharedMaterial.shader;
            
            // 일반적인 색상 프로퍼티 이름들
            string[] possibleColorProperties = new string[] 
            {
                "_Color", "_BaseColor", "_MainColor", "_AlbedoColor", "_TintColor", "_Tint"
            };
            
            // 일반적인 발광 프로퍼티 이름들
            string[] possibleEmissionProperties = new string[] 
            {
                "_EmissionColor", "_EmissiveColor", "_Emission", "_GlowColor"
            };

            // 색상 프로퍼티 적용
            bool colorPropertyApplied = false;
            foreach (string colorProperty in possibleColorProperties)
            {
                if (shader.FindPropertyIndex(colorProperty) != -1 && sourceMaterial.HasProperty(colorProperty))
                {
                    block.SetColor(colorProperty, sourceMaterial.GetColor(colorProperty));
                    colorPropertyApplied = true;
                    break;
                }
            }
            
            // 색상 프로퍼티를 찾지 못한 경우, 일반적인 색상으로 적용 시도
            if (!colorPropertyApplied && possibleColorProperties.Length > 0)
            {
                string firstColorProperty = possibleColorProperties[0];
                if (shader.FindPropertyIndex(firstColorProperty) != -1)
                {
                    // 소스 머티리얼에서 색상을 직접 가져올 수 없는 경우 기본값 사용
                    Color color = Color.white;
                    if (sourceMaterial.HasProperty("_Color"))
                    {
                        color = sourceMaterial.color;
                    }
                    
                    block.SetColor(firstColorProperty, color);
                }
            }
            
            // 발광 프로퍼티 적용
            foreach (string emissionProperty in possibleEmissionProperties)
            {
                if (shader.FindPropertyIndex(emissionProperty) != -1)
                {
                    // 소스 머티리얼에 해당 프로퍼티가 있으면 그 값을 사용
                    if (sourceMaterial.HasProperty(emissionProperty))
                    {
                        block.SetColor(emissionProperty, sourceMaterial.GetColor(emissionProperty));
                    }
                    // 없으면 기본 색상을 발광으로 사용
                    else if (colorPropertyApplied)
                    {
                        Color emissionColor = Color.black;
                        foreach (string colorProperty in possibleColorProperties)
                        {
                            if (sourceMaterial.HasProperty(colorProperty))
                            {
                                emissionColor = sourceMaterial.GetColor(colorProperty) * 0.5f;
                                break;
                            }
                        }
                        
                        block.SetColor(emissionProperty, emissionColor);
                    }
                    break;
                }
            }
        }
        
        // 타겟 포인트 위치 반환 메서드
        public Vector3 GetTargetPosition()
        {
            // 타겟 포인트가 설정되어 있으면 사용, 아니면 자신의 위치 반환
            return targetPoint != null ? targetPoint.position : transform.position;
        }
        
        // 시선 타겟 직접 설정 메서드
        public void SetTargetPoint(Transform target)
        {
            if (target != null)
            {
                targetPoint = target;
            }
        }
        
        // 호버 상태 강제 설정 (외부에서 호출 가능)
        public void SetHovered(bool hovered)
        {
            if (IsHovered != hovered)
            {
                IsHovered = hovered;
                // 상태 변경 이벤트는 Update에서 처리됨
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // 상호작용 영역 표시
            if (targetPoint != null)
            {
                Gizmos.color = IsHovered ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(targetPoint.position, interactionRadius);
            }
            else
            {
                Gizmos.color = IsHovered ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, interactionRadius);
            }
        }
        
        private void OnDestroy()
        {
            // 리소스 정리
            if (originalMaterial != null)
            {
                Destroy(originalMaterial);
            }
        }
    }
}