using UnityEngine;

public class VRButtonInteraction : MonoBehaviour
{
    [SerializeField] private GameStartManager gameStartManager;
    
    // 이 스크립트는 버튼 오브젝트에 연결됩니다
    private void OnTriggerEnter(Collider other)
    {
        // VR 컨트롤러와 충돌했는지 확인
        if (other.CompareTag("VRController"))
        {
            Debug.Log("VR 컨트롤러가 버튼을 눌렀습니다!");
            
            // 게임 시작 메서드 호출
            if (gameStartManager != null)
            {
                gameStartManager.OnStartButtonClicked();
            }
        }
    }
}