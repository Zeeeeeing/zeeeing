using UnityEngine;

public class HeadsetUIFollow : MonoBehaviour
{
    [SerializeField] private Transform headsetTransform; // CenterEyeAnchor 참조
    [SerializeField] private float followDistance = 1.0f; // 시야로부터의 거리
    [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, 0); // 추가 오프셋

    private void Awake()
    {
        // 헤드셋 참조가 없으면 찾기
        if (headsetTransform == null)
        {
            // Oculus 헤드셋의 경우
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
                headsetTransform = centerEye.transform;
            else
                headsetTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headsetTransform == null) return;

        // 1. 헤드셋 위치와 방향을 기반으로 UI 위치 결정
        transform.position = headsetTransform.position + 
                            (headsetTransform.forward * followDistance) + 
                            (headsetTransform.right * positionOffset.x) + 
                            (headsetTransform.up * positionOffset.y);

        // 2. UI가 항상 사용자를 향하도록 회전
        transform.rotation = Quaternion.LookRotation(
            transform.position - headsetTransform.position
        );
    }
}