using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class StartOnAnyButton : MonoBehaviour
{
    [SerializeField] private string nextScene = "schoolmonster 1";
    [SerializeField] private float pressThreshold = 0.05f;   // 아날로그 트리거 감지 임계값
    bool loaded;

    // 자주 쓰는 FeatureUsage 모음
    readonly InputFeatureUsage<bool>[] digitalButtons = {
        CommonUsages.primaryButton, CommonUsages.secondaryButton,
        CommonUsages.menuButton,    CommonUsages.gripButton,
        CommonUsages.triggerButton
    };
    readonly InputFeatureUsage<float>[] analogs = {
        CommonUsages.trigger, CommonUsages.grip
    };

    void Update()
    {
        if (loaded) return;

        // ─ 1) 키보드·마우스 테스트 입력 ─
        if (Input.anyKeyDown) { Load(); return; }

        // ─ 2) 모든 XR 디바이스 버튼 스캔 ─
        List<InputDevice> devices = new();
        InputDevices.GetDevices(devices);

        foreach (var dev in devices)
        {
            // (a) 디지털 버튼
            foreach (var usage in digitalButtons)
                if (dev.TryGetFeatureValue(usage, out bool pressed) && pressed)
                { Load(); return; }

            // (b) 아날로그 트리거/그립
            foreach (var usage in analogs)
                if (dev.TryGetFeatureValue(usage, out float val) && val > pressThreshold)
                { Load(); return; }
        }
    }

    void Load()
    {
        loaded = true;
        SceneManager.LoadScene(nextScene);
    }
}
