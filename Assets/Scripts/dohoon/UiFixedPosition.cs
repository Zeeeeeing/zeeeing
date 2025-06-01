using UnityEngine;
using UnityEngine.UI;

public class UIDepthOverride : MonoBehaviour
{
    void Start()
    {
        // 모든 UI 이미지 컴포넌트 찾기
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            // 머티리얼 복제하여 수정
            Material mat = new Material(img.material);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            img.material = mat;
        }
        
        // 모든 UI 텍스트 컴포넌트 찾기
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text txt in texts)
        {
            // 머티리얼 복제하여 수정
            Material mat = new Material(txt.material);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            txt.material = mat;
        }
        
        // TMP 텍스트도 처리 (TMP를 사용하는 경우)
        TMPro.TextMeshProUGUI[] tmpTexts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI txt in tmpTexts)
        {
            // 머티리얼 복제하여 수정
            Material mat = new Material(txt.material);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            txt.material = mat;
        }
    }
}