using UnityEngine;
using System.Collections;

public class DoorRotation : MonoBehaviour
{
    [Header("타이밍 설정")]
    [Tooltip("첫 회전 전 대기 시간 (초)")]
    public float preRotateDelay = 10f;
    [Tooltip("회전 후 유지할 시간 (초)")]
    public float holdDuration    = 15f;

    [Header("회전 설정")]
    [Tooltip("회전할 각도 (°)")]
    public float rotationAngle   = 110f;
    [Tooltip("회전 속도 (°/초)")]
    public float rotationSpeed   = 90f;

    void Start()
    {
        StartCoroutine(RotationCycle());
    }

    IEnumerator RotationCycle()
    {
        while (true)
        {
            // 1) 첫 회전 전 대기
            yield return new WaitForSeconds(preRotateDelay);

            // 2) –rotationAngle 회전
            yield return SmoothRotate(-rotationAngle);

            // 3) 회전 후 유지
            yield return new WaitForSeconds(holdDuration);

            // 4) +rotationAngle 회전
            yield return SmoothRotate(rotationAngle);
        }
    }

    // 현재 회전에서 angle 만큼 부드럽게 회전
    IEnumerator SmoothRotate(float angle)
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot   = startRot * Quaternion.Euler(0f, angle, 0f);

        float duration = Mathf.Abs(angle) / rotationSpeed;
        float elapsed  = 0f;

        if (duration < 0.01f)
        {
            transform.rotation = endRot;
            yield break;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 정확히 마무리
        transform.rotation = endRot;
    }
}
