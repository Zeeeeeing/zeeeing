using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VRSceneTransition : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float holdDuration = 1f;

    [Header("VR Camera")]
    [SerializeField] private Camera vrCamera;
<<<<<<< Updated upstream
    
=======

    [Header("Scene Names - 빌드 설정과 정확히 일치해야 함")]
    [SerializeField] private string sSceneName = "S score";
    [SerializeField] private string aSceneName = "A score";
    [SerializeField] private string bSceneName = "B score";
    [SerializeField] private string cSceneName = "C score";
    [SerializeField] private string dSceneName = "D score";

>>>>>>> Stashed changes
    // 카메라 직접 제어 방식
    private Camera fadeCamera;
    private static VRSceneTransition instance;

    public static VRSceneTransition Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCamera();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateFadeCamera()
    {
        // 페이드 전용 카메라 생성
        GameObject fadeCamObj = new GameObject("VR_Fade_Camera");
        fadeCamera = fadeCamObj.AddComponent<Camera>();

        // 카메라 설정
        fadeCamera.clearFlags = CameraClearFlags.SolidColor;
        fadeCamera.backgroundColor = Color.black;
        fadeCamera.cullingMask = 0; // 아무것도 렌더링하지 않음
        fadeCamera.depth = 1000; // 최상위 렌더링
        fadeCamera.enabled = false; // 처음에는 비활성화

        DontDestroyOnLoad(fadeCamObj);

        // Debug.Log("[VR Fade] 페이드 카메라 생성 완료");
    }

    // 페이드 인: 검은 화면 표시
    private IEnumerator DoFadeIn()
    {
        // Debug.Log("[VR Fade] 페이드 인 시작");

        if (fadeCamera != null)
        {
            fadeCamera.enabled = true;
            // Debug.Log("[VR Fade] 페이드 카메라 활성화 - 검은 화면 표시됨");
        }

        yield return new WaitForSeconds(fadeInDuration);
        // Debug.Log("[VR Fade] 페이드 인 완료");
    }

    // 페이드 아웃: 검은 화면 숨김
    private IEnumerator DoFadeOut()
    {
        // Debug.Log("[VR Fade] 페이드 아웃 시작");

        yield return new WaitForSeconds(fadeOutDuration);

        if (fadeCamera != null)
        {
            fadeCamera.enabled = false;
            // Debug.Log("[VR Fade] 페이드 카메라 비활성화 - 검은 화면 숨겨짐");
        }

        // Debug.Log("[VR Fade] 페이드 아웃 완료");
    }

    // 씬 전환 메인 함수
    public void TransitionToScoreScene(float completionRate)
    {
        string targetScene = GetSceneNameByScore(completionRate);
<<<<<<< Updated upstream
        Debug.Log($"[VR Fade] 씬 전환 시작: {completionRate}% → {targetScene}");
        
        if (!IsSceneInBuildSettings(targetScene))
        {
            Debug.LogError($"씬 '{targetScene}'이 Build Settings에 없습니다!");
=======
        string grade = GetGradeByScore(completionRate);

        // Debug.Log($"[VR Fade] 씬 전환 시작: {completionRate}% → {grade}등급 → {targetScene}");

        if (!IsSceneInBuildSettings(targetScene))
        {
            // Debug.LogError($"씬 '{targetScene}'이 Build Settings에 없습니다!");
            ListAvailableScenes(); // 사용 가능한 씬 목록 출력
>>>>>>> Stashed changes
            return;
        }

        StartCoroutine(DoTransition(targetScene));
    }

    private IEnumerator DoTransition(string sceneName)
    {
        // Debug.Log("[VR Fade] === 전환 시작 ===");

        // 1. 페이드 인 (검은 화면)
        yield return StartCoroutine(DoFadeIn());

        // 2. 잠시 대기
        yield return new WaitForSeconds(holdDuration);

        // 3. 씬 로드
<<<<<<< Updated upstream
        Debug.Log("[VR Fade] 씬 로딩 시작");
=======
        // Debug.Log($"[VR Fade] '{sceneName}' 씬 로딩 시작");
>>>>>>> Stashed changes
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        while (async.progress < 0.9f)
        {
            yield return null;
        }

        async.allowSceneActivation = true;
        yield return new WaitUntil(() => async.isDone);
<<<<<<< Updated upstream
        Debug.Log("[VR Fade] 씬 로딩 완료");
        
=======
        // Debug.Log($"[VR Fade] '{sceneName}' 씬 로딩 완료");

>>>>>>> Stashed changes
        // 4. 페이드 아웃 (화면 복원)
        yield return StartCoroutine(DoFadeOut());

        // Debug.Log("[VR Fade] === 전환 완료 ===");
    }

    private string GetSceneNameByScore(float rate)
    {
<<<<<<< Updated upstream
        if (rate >= 100f) return "S score";
        if (rate >= 80f) return "A score";
        if (rate >= 60f) return "B score";
        if (rate >= 40f) return "C score";
        return "D score";
=======
        if (rate >= 80f) return sSceneName;
        if (rate >= 60f) return aSceneName;
        if (rate >= 40f) return bSceneName;
        if (rate >= 20f) return cSceneName;
        return dSceneName;
    }

    private string GetGradeByScore(float rate)
    {
        if (rate >= 80f) return "S";
        if (rate >= 60f) return "A";
        if (rate >= 40f) return "B";
        if (rate >= 20f) return "C";
        return "D";
>>>>>>> Stashed changes
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
<<<<<<< Updated upstream
    
=======

    // 사용 가능한 씬 목록 출력 (디버깅용)
    private void ListAvailableScenes()
    {
        // Debug.Log("=== Build Settings에 등록된 씬 목록 ===");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            // Debug.Log($"[{i}] {name} (경로: {path})");
        }
    }

>>>>>>> Stashed changes
    // 즉시 테스트 함수들
    [ContextMenu("TEST: 즉시 검은화면")]
    public void TestShowBlack()
    {
        if (fadeCamera != null)
        {
            fadeCamera.enabled = true;
            // Debug.Log("[VR Fade] 즉시 검은 화면 ON");
        }
    }

    [ContextMenu("TEST: 즉시 화면복원")]
    public void TestHideBlack()
    {
        if (fadeCamera != null)
        {
            fadeCamera.enabled = false;
            // Debug.Log("[VR Fade] 즉시 검은 화면 OFF");
        }
    }

    [ContextMenu("TEST: 페이드 인")]
    public void TestFadeIn()
    {
        StartCoroutine(DoFadeIn());
    }

    [ContextMenu("TEST: 페이드 아웃")]
    public void TestFadeOut()
    {
        StartCoroutine(DoFadeOut());
    }
<<<<<<< Updated upstream
    
    [ContextMenu("TEST: D등급 전환")]
=======

    [ContextMenu("TEST: S등급 전환 (100%)")]
    public void TestSGrade()
    {
        TransitionToScoreScene(100f);
    }

    [ContextMenu("TEST: A등급 전환 (85%)")]
    public void TestAGrade()
    {
        TransitionToScoreScene(85f);
    }

    [ContextMenu("TEST: B등급 전환 (65%)")]
    public void TestBGrade()
    {
        TransitionToScoreScene(65f);
    }

    [ContextMenu("TEST: C등급 전환 (45%)")]
    public void TestCGrade()
    {
        TransitionToScoreScene(45f);
    }

    [ContextMenu("TEST: D등급 전환 (25%)")]
>>>>>>> Stashed changes
    public void TestDGrade()
    {
        TransitionToScoreScene(25f);
    }
<<<<<<< Updated upstream
    
    [ContextMenu("DEBUG: 상태 확인")]
    public void DebugStatus()
    {
        Debug.Log("=== VR Fade 상태 ===");
        Debug.Log($"Fade Camera: {(fadeCamera ? "존재" : "NULL")}");
        if (fadeCamera)
        {
            Debug.Log($"Fade Camera Enabled: {fadeCamera.enabled}");
            Debug.Log($"Fade Camera Depth: {fadeCamera.depth}");
        }
    }
=======
>>>>>>> Stashed changes
}