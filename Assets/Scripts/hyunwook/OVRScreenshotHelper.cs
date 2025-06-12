using UnityEngine;
using System.Collections;

public class OVRScreenshotHelper : MonoBehaviour
{
    [Header("OVR Screenshot Settings")]
    [SerializeField] private KeyCode screenshotKey = KeyCode.F11;
    [SerializeField] private bool enableControllerShortcut = true;
    [SerializeField] private Camera centerEyeCamera; // CenterEye 카메라 참조
    [SerializeField] private Camera uiCamera; // UI 카메라 참조
    [SerializeField] private bool autoFindCameras = true;
    
    private void Start()
    {
        // 카메라들 자동 찾기
        if (autoFindCameras)
        {
            if (centerEyeCamera == null) FindCenterEyeCamera();
            if (uiCamera == null) FindUICamera();
        }
    }
    
    private void FindCenterEyeCamera()
    {
        // CenterEyeAnchor 찾기
        GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
        if (centerEyeAnchor != null)
        {
            centerEyeCamera = centerEyeAnchor.GetComponent<Camera>();
            Debug.Log($"[OVR] CenterEye 카메라 발견: {centerEyeCamera.name}");
            return;
        }
        
        // OVRCameraRig 하위에서 찾기
        GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
        if (ovrCameraRig != null)
        {
            Transform centerEye = ovrCameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            if (centerEye != null)
            {
                centerEyeCamera = centerEye.GetComponent<Camera>();
                Debug.Log($"[OVR] OVRCameraRig CenterEye 발견: {centerEyeCamera.name}");
                return;
            }
        }
        
        // 백업: Main Camera 사용
        if (centerEyeCamera == null)
        {
            centerEyeCamera = Camera.main;
            Debug.Log($"[OVR] 백업으로 Main Camera 사용: {centerEyeCamera?.name ?? "없음"}");
        }
    }
    
    private void FindUICamera()
    {
        // UI 카메라 찾기 (여러 방법 시도)
        Camera[] allCameras = FindObjectsOfType<Camera>();
        
        foreach (Camera cam in allCameras)
        {
            // 1순위: 이름에 "UI" 포함된 카메라
            if (cam.name.ToLower().Contains("ui"))
            {
                uiCamera = cam;
                Debug.Log($"[OVR] UI 카메라 발견 (이름): {uiCamera.name}");
                return;
            }
            
            // 2순위: UI 레이어만 렌더링하는 카메라
            if (cam.cullingMask == (1 << LayerMask.NameToLayer("UI")))
            {
                uiCamera = cam;
                Debug.Log($"[OVR] UI 카메라 발견 (레이어): {uiCamera.name}");
                return;
            }
            
            // 3순위: Depth가 높고 UI 레이어를 포함하는 카메라
            if (cam.depth > 0 && (cam.cullingMask & (1 << LayerMask.NameToLayer("UI"))) != 0)
            {
                uiCamera = cam;
                Debug.Log($"[OVR] UI 카메라 발견 (깊이): {uiCamera.name}");
                return;
            }
        }
        
        Debug.LogWarning("[OVR] UI 카메라를 찾지 못했습니다. UI가 스크린샷에 포함되지 않을 수 있습니다.");
    }
    
    private void Update()
    {
        // 키보드 단축키
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeOVRScreenshot();
        }
        
        // VR 컨트롤러 단축키 (Meta Quest)
        if (enableControllerShortcut)
        {
            // Meta 버튼 + 트리거 동시 누르기 (Quest 기본 스크린샷)
            if (OVRInput.Get(OVRInput.Button.Start) && OVRInput.Get(OVRInput.RawButton.RIndexTrigger))
            {
                Debug.Log("[OVR] 기본 스크린샷 단축키 감지됨");
            }
            
            // 사용자 정의: X + Y 버튼 동시 누르기
            if (OVRInput.GetDown(OVRInput.Button.Three) && OVRInput.Get(OVRInput.Button.Four))
            {
                TakeOVRScreenshot();
            }
        }
    }
    
    private void TakeOVRScreenshot()
    {
        // CenterEye 카메라 확인
        if (centerEyeCamera == null)
        {
            Debug.LogError("[OVR] CenterEye 카메라가 없습니다!");
            FindCenterEyeCamera();
            if (centerEyeCamera == null) return;
        }
        
        try
        {
            string cameraInfo = $"CenterEye: {centerEyeCamera.name}";
            if (uiCamera != null) cameraInfo += $", UI: {uiCamera.name}";
            
            Debug.Log($"[OVR] 통합 스크린샷 촬영 중... ({cameraInfo})");
            StartCoroutine(CaptureOVRFrame());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OVR] 스크린샷 실패: {e.Message}");
        }
    }
    
    private IEnumerator CaptureOVRFrame()
    {
        yield return new WaitForEndOfFrame();
        
        // Quest Pro 해상도로 캡처
        int width = 2880;
        int height = 1700;
        
        // ⭐ Overlay UI 때문에 ScreenCapture 방식 사용
        try
        {
            Debug.Log("[OVR] Overlay UI 포함 스크린샷 방식 사용...");
            
            // Unity의 ScreenCapture 사용 (Overlay UI 포함됨)
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            if (screenshot != null)
            {
                // Quest Pro 해상도로 리사이즈
                Texture2D resized = ResizeTexture(screenshot, width, height);
                
                // 파일 저장
                string fileName = $"VR_CenterEye_Overlay_Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                
                byte[] data = resized.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, data);
                
                Debug.Log($"[OVR] ✅ Overlay UI 포함 스크린샷 저장 완료: {filePath}");
                Debug.Log($"[OVR] 📷 CenterEye: {centerEyeCamera.transform.position}");
                if (uiCamera != null)
                {
                    Debug.Log($"[OVR] 🖼️ UI 카메라: {uiCamera.name} (Render Type: Overlay)");
                }
                
                // 메모리 정리
                DestroyImmediate(screenshot);
                DestroyImmediate(resized);
            }
            else
            {
                Debug.LogError("[OVR] ScreenCapture 실패, 백업 방식 시도...");
                CaptureWithoutOverlay(width, height);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OVR] Overlay 스크린샷 실패: {e.Message}");
            
            // ⭐ 백업: 기존 방식으로 시도
            CaptureWithoutOverlay(width, height);
        }
    }
    
    // ⭐ 백업용: Overlay 없는 캡처 방식 (완전한 void 메서드)
    private void CaptureWithoutOverlay(int width, int height)
    {
        Debug.Log("[OVR] 백업: Overlay 없는 캡처 시도...");
        
        RenderTexture finalRenderTexture = null;
        RenderTexture originalCenterEyeRT = null;
        Texture2D screenshot = null;
        
        try
        {
            // RenderTexture 생성
            finalRenderTexture = new RenderTexture(width, height, 24);
            originalCenterEyeRT = centerEyeCamera.targetTexture;
            
            // CenterEye 카메라로 메인 화면 렌더링
            centerEyeCamera.targetTexture = finalRenderTexture;
            centerEyeCamera.Render();
            
            // RenderTexture에서 Texture2D로 변환
            RenderTexture.active = finalRenderTexture;
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            
            if (screenshot != null)
            {
                // 파일 저장
                string fileName = $"VR_CenterEye_NoOverlay_Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
                
                byte[] data = screenshot.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, data);
                
                Debug.Log($"[OVR] ⚠️ UI 없는 스크린샷 저장: {filePath}");
            }
            else
            {
                Debug.LogError("[OVR] 백업 캡처도 실패했습니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OVR] 백업 캡처 실패: {e.Message}");
        }
        finally
        {
            // 메모리 정리
            if (screenshot != null)
            {
                DestroyImmediate(screenshot);
            }
            
            // 카메라 설정 복원
            if (centerEyeCamera != null && originalCenterEyeRT != null)
            {
                centerEyeCamera.targetTexture = originalCenterEyeRT;
            }
            
            // RenderTexture 정리
            RenderTexture.active = null;
            if (finalRenderTexture != null)
            {
                finalRenderTexture.Release();
                DestroyImmediate(finalRenderTexture);
            }
        }
        
        // ⭐ 명시적으로 메서드 끝에서 return (컴파일러 에러 방지)
        return;
    }
    
    // ⭐ 텍스처 리사이즈 함수 추가
    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;
        
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }
    
    // Inspector에서 즉시 실행 가능
    [ContextMenu("Take VR Screenshot")]
    public void TakeScreenshotNow()
    {
        TakeOVRScreenshot();
    }
    
    // 카메라 정보 확인
    [ContextMenu("Show Camera Info")]
    public void ShowCameraInfo()
    {
        Debug.Log("=== 카메라 정보 ===");
        
        if (centerEyeCamera != null)
        {
            Debug.Log($"CenterEye: {centerEyeCamera.name}");
            Debug.Log($"  위치: {centerEyeCamera.transform.position}");
            Debug.Log($"  깊이: {centerEyeCamera.depth}");
            Debug.Log($"  컬링마스크: {centerEyeCamera.cullingMask}");
        }
        else
        {
            Debug.LogWarning("CenterEye 카메라 없음!");
        }
        
        if (uiCamera != null)
        {
            Debug.Log($"UI 카메라: {uiCamera.name}");
            Debug.Log($"  위치: {uiCamera.transform.position}");
            Debug.Log($"  깊이: {uiCamera.depth}");
            Debug.Log($"  컬링마스크: {uiCamera.cullingMask}");
        }
        else
        {
            Debug.LogWarning("UI 카메라 없음!");
        }
    }
}