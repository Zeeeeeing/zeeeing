using UnityEngine;
using System.Collections;

[System.Serializable]
public class MovementSegment
{
    [Tooltip("회전 각도 (°). +면 오른쪽, –면 왼쪽, 0이면 직진")]
    public float turnAngle = 0f;

    [Tooltip("회전 후 대기할 시간 (초)")]
    public float waitDuration = 0f;   // ★ 추가

    [Tooltip("대기 후 직진할 시간 (초)")]
    public float moveDuration = 1f;
}

public class CharacterGenericLoop : MonoBehaviour
{
    [Header("속도 설정")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    [Header("세그먼트 설정")]
    public MovementSegment[] segments;

    [Header("복귀 설정")]
    [Tooltip("시작 위치·회전으로 돌아갈 최소 시간(초). 너무 짧으면 튕김이 발생할 수 있음")]
    public float minReturnDuration = 0.5f;

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
            foreach (var seg in segments)
            {
                yield return RotateBySmooth(seg.turnAngle);

                if (seg.waitDuration > 0f)
                    yield return new WaitForSeconds(seg.waitDuration);

                if (seg.moveDuration > 0f)
                    yield return MoveForward(seg.moveDuration);
            }

            yield return ReturnToStartSmooth();
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
        float duration  = Mathf.Abs(angle) / rotationSpeed;
        if (duration < 0.01f)
        {
            transform.rotation = to;
            yield break;
        }

        float elapsed = 0f;
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
        Vector3 fromPos   = transform.position;
        Quaternion fromRot = transform.rotation;
        float dist   = Vector3.Distance(fromPos, startPosition);
        float angle  = Quaternion.Angle(fromRot, startRotation);
        float moveDur = moveSpeed > 0f     ? dist  / moveSpeed    : 0f;
        float rotDur  = rotationSpeed > 0f ? angle / rotationSpeed : 0f;
        float duration = Mathf.Max(moveDur, rotDur, minReturnDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t        = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(fromPos, startPosition, smoothT);
            transform.rotation = Quaternion.Slerp(fromRot, startRotation, smoothT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}
