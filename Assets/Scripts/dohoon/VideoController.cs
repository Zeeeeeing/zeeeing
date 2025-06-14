using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoController : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private int loopCount = 0;
    private const int maxLoopCount = 1; // 비디오를 2번 반복합니다.

    void Awake()
    {
        // VideoPlayer 컴포넌트를 가져옵니다.
        videoPlayer = GetComponent<VideoPlayer>();
        // loopPointReached 이벤트에 EndReached 함수를 구독(연결)합니다.
        videoPlayer.loopPointReached += EndReached;
    }

    // 비디오 재생이 끝났을 때 호출되는 함수입니다.
    void EndReached(VideoPlayer vp)
    {
        loopCount++;

        if (loopCount < maxLoopCount)
        {
            // 아직 최대 반복 횟수에 도달하지 않았다면 비디오를 다시 재생ㄴ합니다.
            vp.Play();
        }
        else
        {
            // 2번 반복이 끝나면 "EyeTracking 1" 씬으로 이동합니다.
            SceneManager.LoadScene("EyeTracking 1");
        }
    }
}