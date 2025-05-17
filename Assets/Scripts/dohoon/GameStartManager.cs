using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStartManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startMenuCanvas;
    [SerializeField] private GameObject gameCanvas;
    [SerializeField] private Button startButton;

    [Header("VR Interaction")]
    [SerializeField] private Transform playerStartPosition;
    [SerializeField] private Transform playerGamePosition;

    [Header("Game Controllers")]
    [SerializeField] private HUDController hudController;

    [Header("Player References")]
    [SerializeField] private Transform playerRig; // VR 리그 직접 참조 (중요!)

    [Header("Additional Settings")]
    [SerializeField] private float delayBeforeGameStart = 1.0f;

    private bool gameStarted = false;

    private void Awake()
    {
        // Awake에서 플레이어 위치 설정 - 씬 로드 직후 즉시 이동
        SetPlayerToStartPosition();
    }

    private void Start()
    {
        Debug.Log("GameStartManager: Start() 호출됨");

        // 시작 위치 설정 확인 (Awake에서 이미 설정했지만 다시 한 번 확인)
        SetPlayerToStartPosition();

        // 게임 캔버스는 비활성화, 시작 메뉴는 활성화
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
            Debug.Log("게임 캔버스 비활성화됨");
        }

        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(true);
            Debug.Log("시작 메뉴 캔버스 활성화됨");
        }

        // UI 버튼에 리스너 추가
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log("시작 버튼에 리스너 추가됨");
        }
    }

    // 플레이어를 시작 위치로 강제 이동시키는 함수
    private void SetPlayerToStartPosition()
    {
        // 직접 참조된 VR 리그 사용 (가장 확실한 방법)
        if (playerRig != null && playerStartPosition != null)
        {
            playerRig.position = playerStartPosition.position;
            playerRig.rotation = playerStartPosition.rotation;
            Debug.Log("플레이어 리그를 시작 위치로 이동 (직접 참조): " + playerStartPosition.position);
            return;
        }

        // 직접 참조가 없는 경우, VR 리그를 찾아보기
        if (playerStartPosition != null)
        {
            // 방법 1: "Player" 태그로 찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = playerStartPosition.position;
                player.transform.rotation = playerStartPosition.rotation;
                Debug.Log("플레이어를 시작 위치로 이동 (태그): " + playerStartPosition.position);
                return;
            }

            // 방법 2: XR Origin 또는 XR Rig 이름으로 찾기
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = playerStartPosition.position;
                xrOrigin.transform.rotation = playerStartPosition.rotation;
                Debug.Log("XR Origin을 시작 위치로 이동: " + playerStartPosition.position);
                return;
            }

            GameObject xrRig = GameObject.Find("XR Rig");
            if (xrRig != null)
            {
                xrRig.transform.position = playerStartPosition.position;
                xrRig.transform.rotation = playerStartPosition.rotation;
                Debug.Log("XR Rig를 시작 위치로 이동: " + playerStartPosition.position);
                return;
            }

            // 방법 3: 메인 카메라의 부모 찾기
            if (Camera.main != null && Camera.main.transform.parent != null)
            {
                // 메인 카메라의 부모로 올라가면서 최상위 VR 리그 찾기
                Transform topParent = Camera.main.transform.parent;
                while (topParent.parent != null)
                {
                    topParent = topParent.parent;
                }

                topParent.position = playerStartPosition.position;
                topParent.rotation = playerStartPosition.rotation;
                Debug.Log("카메라 최상위 부모를 시작 위치로 이동: " + playerStartPosition.position);
                return;
            }

            Debug.LogError("플레이어/VR 리그를 찾을 수 없습니다! Inspector에서 'playerRig'에 VR 리그를 직접 할당해주세요.");
        }
        else
        {
            Debug.LogError("playerStartPosition이 할당되지 않았습니다!");
        }
    }

    private void Update()
    {
        // VR 컨트롤러 버튼 입력 확인 (A 버튼 등)
        if (!gameStarted && Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A 키 입력 감지! 게임 시작됨");
            OnStartButtonClicked();
        }
    }

    // 시작 버튼 클릭 처리 메서드 (VRButtonInteractable 이벤트에서 호출됨)
    public void OnStartButtonClicked()
    {
        if (gameStarted) return;

        gameStarted = true;
        Debug.Log("시작 버튼이 클릭됨!");

        // 시작 메뉴 비활성화
        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(false);
        }

        // 게임 시작 로직
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("StartGame() 호출됨");

        // 플레이어 위치 변경 (즉시 이동)
        MovePlayerToGamePosition();

        // 딜레이 후 UI 업데이트
        Invoke("ShowGameUI", delayBeforeGameStart);
    }

    private void MovePlayerToGamePosition()
    {
        if (playerGamePosition == null)
        {
            Debug.LogError("playerGamePosition이 할당되지 않았습니다!");
            return;
        }

        // 직접 참조된 VR 리그 사용
        if (playerRig != null)
        {
            playerRig.position = playerGamePosition.position;
            playerRig.rotation = playerGamePosition.rotation;
            Debug.Log("플레이어 리그를 게임 위치로 이동 (직접 참조): " + playerGamePosition.position);
            return;
        }

        // 직접 참조가 없는 경우, 시작 위치 설정과 동일한 방법으로 찾기
        // 방법 1: "Player" 태그로 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerGamePosition.position;
            player.transform.rotation = playerGamePosition.rotation;
            Debug.Log("플레이어를 게임 위치로 이동 (태그): " + playerGamePosition.position);
            return;
        }

        // 방법 2: XR Origin 또는 XR Rig 이름으로 찾기
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = playerGamePosition.position;
            xrOrigin.transform.rotation = playerGamePosition.rotation;
            Debug.Log("XR Origin을 게임 위치로 이동: " + playerGamePosition.position);
            return;
        }

        GameObject xrRig = GameObject.Find("XR Rig");
        if (xrRig != null)
        {
            xrRig.transform.position = playerGamePosition.position;
            xrRig.transform.rotation = playerGamePosition.rotation;
            Debug.Log("XR Rig를 게임 위치로 이동: " + playerGamePosition.position);
            return;
        }

        // 방법 3: 메인 카메라의 부모 찾기
        if (Camera.main != null && Camera.main.transform.parent != null)
        {
            // 메인 카메라의 부모로 올라가면서 최상위 VR 리그 찾기
            Transform topParent = Camera.main.transform.parent;
            while (topParent.parent != null)
            {
                topParent = topParent.parent;
            }

            topParent.position = playerGamePosition.position;
            topParent.rotation = playerGamePosition.rotation;
            Debug.Log("카메라 최상위 부모를 게임 위치로 이동: " + playerGamePosition.position);
            return;
        }

        Debug.LogError("플레이어/VR 리그를 찾을 수 없습니다! Inspector에서 'playerRig'에 VR 리그를 직접 할당해주세요.");
    }

    private void ShowGameUI()
    {
        Debug.Log("[GSM] ShowGameUI() 호출됨");

        // 게임 캔버스 활성화
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(true);
            Debug.Log("[GSM] 게임 캔버스 활성화됨");
        }
        else
        {
            Debug.LogError("[GSM] gameCanvas가 null입니다!");
        }

        // HUD 컨트롤러의 타이머 시작
        if (hudController != null)
        {
            Debug.Log("[GSM] hudController 참조 확인됨, StartTimer() 호출 시도");
            hudController.StartTimer();

            // 타이머 시작 확인
            bool isRunning = hudController.IsTimerRunning();
            Debug.Log("[GSM] HUD 타이머 시작 시도 완료, 타이머 실행 상태: " + isRunning);

            // 타이머가 시작되지 않았다면 강제 시작 시도
            if (!isRunning)
            {
                Debug.LogWarning("[GSM] 타이머가 시작되지 않았습니다. 강제 시작 시도");
                hudController.ForceStartTimer();
            }
        }
        else
        {
            Debug.LogError("[GSM] hudController가 할당되지 않았습니다! Inspector에서 확인하세요.");
        }

        Debug.Log("[GSM] 게임이 시작되었습니다!");
    }
}