using UnityEngine;
using UnityEngine.Events;

public class VRButtonInteractable : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private float highlightScale = 1.2f; // 강조 표시 크기 증가
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private bool useHighlight = true; // 하이라이트 효과 사용 여부
    
    [Header("Events")]
    public UnityEvent onButtonTriggered; // 버튼 클릭 이벤트
    
    private Renderer buttonRenderer; // 버튼의 렌더러
    private Vector3 originalScale; // 원래 크기
    private bool isHighlighted = false; // 현재 강조 상태
    
    private void Start()
    {
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();
            
        originalScale = transform.localScale;
        
        // 기본 색상 설정
        if (buttonRenderer != null && useHighlight)
            SetButtonColor(normalColor);
            
        // 콜라이더 확인
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogWarning("VRButtonInteractable에 콜라이더가 없습니다. 상호작용이 작동하지 않을 수 있습니다.");
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2, 2, 0.5f); // 더 큰 콜라이더
        }
    }
    
    // 레이가 버튼에 들어왔을 때
    public void OnRayEnter()
    {
        isHighlighted = true;
        
        if (!useHighlight) return;
        
        // 색상 변경
        if (buttonRenderer != null)
            SetButtonColor(highlightColor);
            
        // 크기 변경 (약간 확대)
        transform.localScale = originalScale * highlightScale;
        
        Debug.Log("레이가 버튼에 닿았습니다: " + gameObject.name);
        
        // 효과음 재생 (옵션)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.Play();
    }
    
    // 레이가 버튼에서 나갔을 때
    public void OnRayExit()
    {
        isHighlighted = false;
        
        if (!useHighlight) return;
        
        // 색상 원복
        if (buttonRenderer != null)
            SetButtonColor(normalColor);
            
        // 크기 원복
        transform.localScale = originalScale;
        
        Debug.Log("레이가 버튼에서 벗어났습니다: " + gameObject.name);
    }
    
    // 레이가 버튼에 있는 상태에서 트리거 버튼을 눌렀을 때
    public void OnRayTrigger()
    {
        Debug.Log("버튼이 클릭되었습니다: " + gameObject.name);
        
        // 효과음 재생 (옵션)
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
        
        // 등록된 이벤트 실행
        onButtonTriggered.Invoke();
        
        // 짧은 애니메이션 효과 (눌렸다가 돌아오는)
        StartCoroutine(ButtonPressAnimation());
    }
    
    // 버튼 눌림 애니메이션
    private System.Collections.IEnumerator ButtonPressAnimation()
    {
        // 버튼 눌림 효과 (살짝 더 축소)
        Vector3 pressedScale = originalScale * 0.8f;
        transform.localScale = pressedScale;
        yield return new WaitForSeconds(0.1f);
        
        // 원래 강조 크기로 복원
        if (isHighlighted && useHighlight)
            transform.localScale = originalScale * highlightScale;
        else
            transform.localScale = originalScale;
    }
    
    // 버튼 색상 설정 (머티리얼 속성 변경)
    private void SetButtonColor(Color color)
    {
        if (buttonRenderer == null || buttonRenderer.material == null) return;
        
        if (buttonRenderer.material.HasProperty("_Color"))
            buttonRenderer.material.color = color;
        else if (buttonRenderer.material.HasProperty("_BaseColor"))
            buttonRenderer.material.SetColor("_BaseColor", color);
    }
    
    // 직접 버튼 트리거 (외부에서 호출 가능)
    public void TriggerButton()
    {
        OnRayTrigger();
    }
}