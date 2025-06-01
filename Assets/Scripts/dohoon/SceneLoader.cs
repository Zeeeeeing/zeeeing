using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSchoolMonster() =>
        SceneManager.LoadScene("schoolmonster 1");   // 씬 이름 정확히!
}
