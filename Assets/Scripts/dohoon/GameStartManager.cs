using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 코루틴 사용을 위해 필수
using Unity.XR.CoreUtils; // XROrigin을 직접 찾기 위해 추가
using ZeeeingGaze;

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
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("Player References")]
    [SerializeField] private Transform playerRig; // VR 리그 직접 참조 (중요!)

    [Header("Additional Settings")]
    [SerializeField] private float delayBeforeGameStart = 1.0f;

    private bool gameStarted = false;

    private void Awake()
    {
        // playerRig가 인스펙터에서 할당되지 않았다면 자동으로 찾아 할당
        if (playerRig == null)
        {
            // XR Origin을 사용하는 것이 가장 확실합니다.
            XROrigin xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                playerRig = xrOrigin.transform;
                // Debug.Log("XR Origin을 찾아 playerRig에 할당했습니다.");
            }
            else // 차선책으로 "Player" 태그를 찾습니다.
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerRig = playerObject.transform;
                    // Debug.Log("'Player' 태그를 가진 오브젝트를 찾아 playerRig에 할당했습니다.");
                }
                else
                {
                    // Debug.LogError("플레이어 리그(XR Origin 또는 'Player' 태그)를 찾을 수 없습니다! 인스펙터에서 'playerRig'를 직접 할당해주세요.");
                }
            }
        }

        // GameFlowManager 자동 찾기
        if (gameFlowManager == null)
            gameFlowManager = FindAnyObjectByType<GameFlowManager>();

        // GameFlowManager 초기 비활성화
        if (gameFlowManager != null)
        {
            gameFlowManager.gameObject.SetActive(false);
            // Debug.Log("GameFlowManager 초기 비활성화");
        }
    }

    private void Start()
    {
        // Debug.Log("GameStartManager: Start() 호출됨. 초기 위치 설정을 위해 코루틴을 시작합니다.");

        // 바로 함수를 호출하는 대신 코루틴을 시작합니다.
        StartCoroutine(InitialSetupRoutine());

        // 나머지 UI 설정은 그대로 둡니다.
        if (gameCanvas != null) gameCanvas.SetActive(false);
        if (startMenuCanvas != null) startMenuCanvas.SetActive(true);
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            // Debug.Log("시작 버튼에 리스너 추가됨");
        }
    }

    /// <summary>
    /// 한 프레임 대기 후 초기 위치 및 회전을 설정하는 코루틴
    /// </summary>
    private IEnumerator InitialSetupRoutine()
    {
        // 다음 프레임까지 대기합니다. 이 한 줄이 VR 트래킹이 초기화될 시간을 줍니다.
        yield return null;

        // Debug.Log("한 프레임 대기 완료. 플레이어 위치와 회전을 설정합니다.");
        SetPlayerToStartPosition();
    }

    private void Update()
    {
        // VR 컨트롤러 버튼 입력 확인 (A 버튼 등)
        if (!gameStarted && OVRInput.GetDown(OVRInput.Button.One))
        {
            // Debug.Log("VR 컨트롤러 A 버튼 입력 감지! 게임 시작");
            OnStartButtonClicked();
        }
    }

    // 시작 버튼 클릭 또는 VR 컨트롤러 입력 시 호출
    public void OnStartButtonClicked()
    {
        if (gameStarted) return;
        gameStarted = true;

        // Debug.Log("시작 버튼이 클릭됨!");

        if (startMenuCanvas != null)
        {
            startMenuCanvas.SetActive(false);
        }

        if (gameFlowManager != null)
        {
            gameFlowManager.gameObject.SetActive(true);
            // Debug.Log("GameFlowManager 활성화됨");
        }
        else
        {
            // Debug.LogError("GameFlowManager가 할당되지 않았습니다!");
        }

        if (AudioHapticManager.Instance != null)
        {
            AudioHapticManager.Instance.PlayStartButtonSFX();
        }

        StartGame();
    }

    private void StartGame()
    {
        // Debug.Log("StartGame() 호출됨");
        MovePlayerToGamePosition();
        Invoke("ShowGameUI", delayBeforeGameStart);
    }

    /// <summary>
    /// 플레이어를 시작 위치로 이동시키고 'Start' 버튼을 바라보게 합니다.
    /// </summary>
    private void SetPlayerToStartPosition()
    {
        if (playerStartPosition == null || playerRig == null)
        {
            // Debug.LogError("'playerStartPosition' 또는 'playerRig'가 할당되지 않았습니다!");
            return;
        }

        if (startButton == null)
        {
            // Debug.LogWarning("'startButton'이 할당되지 않아 위치만 이동합니다. 바라보기 기능은 작동하지 않습니다.");
            MovePlayerTo(playerStartPosition, "시작 위치");
            return;
        }

        // --- 위치 이동 로직 ---
        Transform cameraTransform = Camera.main.transform;
        Vector3 positionOffset = cameraTransform.position - playerRig.position;
        positionOffset.y = 0; // 높이 오프셋은 제거
        Vector3 targetRigPosition = playerStartPosition.position - positionOffset;
        playerRig.position = targetRigPosition;

        // --- 회전 로직 (Start 버튼 바라보기) ---
        Vector3 lookTargetPosition = startButton.transform.position;
        lookTargetPosition.y = playerRig.position.y;
        Vector3 directionToLook = (lookTargetPosition - playerRig.position).normalized;

        if (directionToLook == Vector3.zero) return; // 같은 위치에 있을 경우 계산 오류 방지

        Quaternion targetLookRotation = Quaternion.LookRotation(directionToLook);
        float cameraYawOffset = cameraTransform.eulerAngles.y - playerRig.eulerAngles.y;
        Quaternion targetRigRotation = targetLookRotation * Quaternion.Euler(0, -cameraYawOffset, 0);
        playerRig.rotation = targetRigRotation;

        // Debug.Log($"[SetPlayerToStartPosition] 카메라 Y축 오프셋: {cameraYawOffset} / 최종 리그 회전: {targetRigRotation.eulerAngles}");
        // Debug.Log("플레이어를 시작 위치로 이동시키고 'Start' 버튼을 바라보게 설정을 완료했습니다.");
    }

    private void MovePlayerToGamePosition()
    {
        MovePlayerTo(playerGamePosition, "게임 위치");
    }

    /// <summary>
    /// 플레이어 리그를 지정된 목표 Transform으로 이동시키는 공통 함수 (게임 위치 이동 시 사용)
    /// </summary>
    private void MovePlayerTo(Transform targetTransform, string locationName)
    {
        if (targetTransform == null || playerRig == null)
        {
            // Debug.LogError($"'{locationName}'로 이동할 수 없습니다. targetTransform 또는 playerRig가 할당되지 않았습니다!");
            return;
        }

        Transform cameraTransform = Camera.main.transform;
        Vector3 positionOffset = cameraTransform.position - playerRig.position;
        positionOffset.y = 0;
        Vector3 targetRigPosition = targetTransform.position - positionOffset;

        float angleOffset = cameraTransform.eulerAngles.y - playerRig.eulerAngles.y;
        Quaternion targetRigRotation = Quaternion.Euler(0, targetTransform.eulerAngles.y - angleOffset, 0);

        playerRig.position = targetRigPosition;
        playerRig.rotation = targetRigRotation;
        // Debug.Log($"플레이어 리그를 오프셋 보정하여 '{locationName}'(으)로 이동 완료. 최종 리그 위치: {targetRigPosition}");
    }

    private void ShowGameUI()
    {
        // Debug.Log("[GSM] ShowGameUI() 호출됨");
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(true);
            // Debug.Log("[GSM] 게임 캔버스 활성화됨");
        }
        else
        {
            // Debug.LogError("[GSM] gameCanvas가 null입니다!");
        }
        // Debug.Log("[GSM] 게임이 시작되었습니다!");
    }
}