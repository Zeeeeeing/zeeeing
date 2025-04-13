using UnityEngine;
using System.Collections;

[System.Serializable]
public class MovementSegment
{
    [Tooltip("회전 각도 (°). +면 오른쪽, –면 왼쪽, 0이면 직진")]
    public float turnAngle = 0f;
    [Tooltip("회전 후 직진할 시간 (초)")]
    public float moveDuration = 1f;
}

public class CharacterGenericLoop : MonoBehaviour
{
    [Header("속도 설정")]
    public float moveSpeed = 5f;           // 이동 속도 (units/sec)
    public float rotationSpeed = 180f;     // 회전 속도 (degrees/sec)

    [Header("세그먼트 설정")]
    public MovementSegment[] segments;     // Inspector 에서 구간별로 각도·시간 설정

    [Header("복귀 설정")]
    [Tooltip("시작 위치·회전으로 돌아갈 최소 시간(초). 너무 짧으면 튕김이 발생할 수 있음")]
    public float minReturnDuration = 0.5f;

    // 초기 위치·회전 저장
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        StartCoroutine(LoopMovement());
    }

    IEnumerator LoopMovement()
    {
        while (true)
        {
            // 1) 정의된 세그먼트들 순서대로 실행
            foreach (var seg in segments)
            {
                yield return RotateBySmooth(seg.turnAngle);
                yield return MoveForward(seg.moveDuration);
            }

            // 2) 시작 위치·회전으로 완전 부드럽게 복귀
            yield return ReturnToStartSmooth();

            // 3) 잠시 대기 후 다음 사이클
            yield return null;
        }
    }

    IEnumerator MoveForward(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator RotateBySmooth(float angle)
    {
        Quaternion from = transform.rotation;
        Quaternion to   = from * Quaternion.Euler(0f, angle, 0f);
        float elapsed   = 0f;
        float duration  = Mathf.Abs(angle) / rotationSpeed;

        // 너무 짧으면 즉시 회전
        if (duration < 0.01f)
        {
            transform.rotation = to;
            yield break;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = to;
    }

    IEnumerator ReturnToStartSmooth()
    {
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;

        float dist = Vector3.Distance(fromPos, startPosition);
        float angle = Quaternion.Angle(fromRot, startRotation);

        float moveDur = moveSpeed > 0f     ? dist  / moveSpeed    : 0f;
        float rotDur  = rotationSpeed > 0f ? angle / rotationSpeed : 0f;
        // 실제 복귀 시간은 계산된 값과 최소값 중 큰 쪽
        float duration = Mathf.Max(moveDur, rotDur, minReturnDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 부드러운 Ease-In-Out
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(fromPos, startPosition, smoothT);
            transform.rotation = Quaternion.Slerp(fromRot, startRotation, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 오차 보정
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}
