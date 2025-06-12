using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZeeeingGaze
{
    [DefaultExecutionOrder(-100)]
    public class FollowerManager : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static FollowerManager _instance;
        
        // 싱글톤 접근자 프로퍼티
        public static FollowerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // FindObjectOfType 대신 FindFirstObjectByType 사용
                    _instance = Object.FindFirstObjectByType<FollowerManager>();
                    
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject("FollowerManager");
                        _instance = singletonObject.AddComponent<FollowerManager>();
                        // 중요: DontDestroyOnLoad를 호출하지 않음
                    }
                }
                return _instance;
            }
        }
        
        [Header("Target")]
        [Tooltip("플레이어 Transform")]
        public Transform player;
        
        [Header("Queue Settings")]
        [Tooltip("팔로워 간 줄 간격")]
        public float followDistance = 2f;
        [Tooltip("이동 속도")]
        public float moveSpeed = 4f;
        [Tooltip("회전 속도")]
        public float rotationSpeed = 3f;
        
        // 팔로워 정보 저장용 클래스
        private class FollowerData
        {
            public NPCController npcController;
            public Transform trans;
            public float initialY;
            public int pointValue;
        }
        
        private readonly List<FollowerData> followers = new List<FollowerData>();
        
        // 점수 관련 이벤트
        public event System.Action<int> OnPointsAdded;
        public event System.Action<int, int> OnScoreUpdated; // 총점, NPC 수
        
        // 디버깅용 플래그
        private bool isCleaningUp = false;
        
        private void Awake()
        {
            // 싱글톤 인스턴스 관리
            if (_instance != null && _instance != this)
            {
                Debug.Log($"[FollowerManager] 중복 인스턴스 파괴: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            // 중요: DontDestroyOnLoad 호출 제거
            
            Debug.Log($"[FollowerManager] 인스턴스 생성: {gameObject.name}");
            
            // 플레이어 참조가 없으면 메인 카메라 사용
            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }
        
        // SceneManager 이벤트 구독
        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[FollowerManager] 씬 이벤트 구독");
        }
        
        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            Debug.Log("[FollowerManager] 씬 이벤트 구독 해제");
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[FollowerManager] 씬 로드됨: {scene.name}, _instance: {_instance}, this: {this}");
            
            // 씬 로드 시 플레이어 참조 재설정 (필요한 경우)
            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform;
                Debug.Log($"[FollowerManager] 플레이어 참조 재설정: {player.name}");
            }
        }
        
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[FollowerManager] 씬 언로드됨: {scene.name}, _instance: {_instance}, this: {this}");
            
            // 씬 언로드 시 정리 작업 수행
            CleanupManager();
        }
        
        // 애플리케이션 일시정지 또는 포커스 상실 시
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[FollowerManager] 애플리케이션 일시정지");
            }
        }
        
        // 애플리케이션 종료 시
        private void OnApplicationQuit()
        {
            Debug.Log("[FollowerManager] 애플리케이션 종료");
            CleanupManager();
        }
        
        // 관리자 정리 메소드 (씬 전환, 애플리케이션 종료 등에서 호출)
        public void CleanupManager()
        {
            // 이미 정리 중이면 중복 실행 방지
            if (isCleaningUp)
            {
                Debug.Log("[FollowerManager] 이미 정리 중입니다.");
                return;
            }
            
            isCleaningUp = true;
            
            try
            {
                Debug.Log($"[FollowerManager] 정리 시작 - 팔로워 수: {(followers != null ? followers.Count : 0)}");
                
                // 모든 코루틴 중지
                StopAllCoroutines();
                
                // 이벤트 구독 해제
                OnPointsAdded = null;
                OnScoreUpdated = null;
                
                // 팔로워 목록 정리
                if (followers != null)
                {
                    // 팔로워 복사본 생성 (열거 중 수정 방지)
                    var followersCopy = new List<FollowerData>(followers);
                    
                    // 목록 비우기
                    followers.Clear();
                    
                    // 각 NPC의 유령 모드 해제 (선택적)
                    foreach (var data in followersCopy)
                    {
                        if (data != null && data.npcController != null)
                        {
                            try
                            {
                                // 유령 모드 해제 시도 (오류가 발생해도 계속 진행)
                                data.npcController.SetGhostMode(false);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[FollowerManager] NPC 유령 모드 해제 중 오류: {e.Message}");
                            }
                        }
                    }
                }
                
                Debug.Log("[FollowerManager] 정리 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FollowerManager] 정리 중 오류 발생: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isCleaningUp = false;
            }
        }
        
        public void RegisterFollower(NPCController npc)
        {
            if (npc == null) return;
            
            try
            {
                // 이미 등록된 NPC는 무시
                if (followers.Exists(f => f.npcController == npc)) 
                {
                    Debug.Log($"NPC {npc.GetName()}은(는) 이미 팔로워로 등록되어 있습니다.");
                    return;
                }
                
                Debug.Log($"NPC {npc.GetName()} 팔로워 등록");
                
                // NPC의 상태 확인
                if (!npc.IsSeduced() || !npc.IsGhost())
                {
                    Debug.LogWarning($"NPC {npc.GetName()}이(가) 올바른 상태가 아닙니다. 먼저 SetSeduced() 및 SetGhostMode(true) 호출 필요");
                }
                
                // 팔로워 데이터 생성 및 등록
                FollowerData data = new FollowerData
                {
                    npcController = npc,
                    trans = npc.transform,
                    initialY = npc.transform.position.y,
                    pointValue = npc.GetPointValue()
                };
                
                followers.Add(data);
                
                // 점수 이벤트 발생
                OnPointsAdded?.Invoke(data.pointValue);
                OnScoreUpdated?.Invoke(GetTotalPoints(), followers.Count);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FollowerManager] 팔로워 등록 중 오류: {e.Message}\n{e.StackTrace}");
            }
        }
        
        public void UnregisterFollower(NPCController npc)
        {
            if (npc == null) return;
            
            try
            {
                int index = followers.FindIndex(f => f.npcController == npc);
                if (index >= 0)
                {
                    // 순서 중요: 먼저 목록에서 제거하고 나중에 상태 변경
                    followers.RemoveAt(index);
                    Debug.Log($"NPC {npc.GetName()} removed from following container. Total: {followers.Count}");
                    
                    // 이제 상태 변경 (목록 외부이므로 수정 문제 없음)
                    // npc.SetGhostMode(false); // 주석 처리: 호출 측에서 직접 상태 변경하도록 함
                }
                
                OnScoreUpdated?.Invoke(GetTotalPoints(), followers.Count);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NPC {npc.GetName()} 제거 중 오류 발생: {e.Message}\n{e.StackTrace}");
            }
        }
        
        public bool IsNPCFollowing(NPCController npc)
        {
            return npc != null && followers.Exists(f => f.npcController == npc);
        }
        
        public int GetFollowingCount()
        {
            return followers != null ? followers.Count : 0;
        }
        
        private void Update()
        {
            if (player == null) return;
            
            // 무효한 팔로워 제거 (성능 최적화: 10프레임마다 실행)
            if (Time.frameCount % 10 == 0)
            {
                RemoveInvalidFollowers();
            }
            
            // 팔로워 위치 업데이트
            for (int i = 0; i < followers.Count; i++)
            {
                var data = followers[i];
                
                // null 체크 (안전)
                if (data == null || data.trans == null) continue;
                
                // ⭐ 추가 안전장치: Y축이 너무 낮아지면 초기 Y로 복구
                if (data.trans.position.y < data.initialY - 2f)
                {
                    // Debug.LogWarning($"[FollowerManager] {data.npcController.GetName()}이(가) 바닥 아래로 떨어짐! Y축 복구: {data.trans.position.y} → {data.initialY}");
                    Vector3 recoveryPos = data.trans.position;
                    recoveryPos.y = data.initialY;
                    data.trans.position = recoveryPos;
                }
                
                // 플레이어 뒤 (i+1)*followDistance 위치 계산 (XZ)
                Vector3 basePos = player.position - player.forward * (followDistance * (i + 1));
                Vector3 targetPos = new Vector3(
                    basePos.x,
                    data.initialY,  // 초기 Y 고정
                    basePos.z
                );
                
                try
                {
                    // ⭐ 수정: Y축은 항상 고정, XZ만 부드럽게 이동
                    Vector3 currentPos = data.trans.position;
                    Vector3 newPos = Vector3.MoveTowards(
                        new Vector3(currentPos.x, data.initialY, currentPos.z), // 현재 위치의 Y를 초기 Y로 강제 설정
                        targetPos,
                        moveSpeed * Time.deltaTime
                    );
                    
                    // Y축 재차 확인
                    newPos.y = data.initialY;
                    data.trans.position = newPos;
                    
                    // 플레이어 바라보며 부드럽게 회전
                    Vector3 lookDirection = player.position - data.trans.position;
                    lookDirection.y = 0; // Y축 회전만 처리
                    
                    if (lookDirection.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                        data.trans.rotation = Quaternion.Slerp(
                            data.trans.rotation,
                            targetRotation,
                            rotationSpeed * Time.deltaTime
                        );
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FollowerManager] 팔로워 위치 업데이트 중 오류: {e.Message}");
                }
            }
        }
        
        // 무효한 팔로워 제거 (null 참조 등)
        private void RemoveInvalidFollowers()
        {
            if (followers == null) return;
            
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                if (followers[i] == null || followers[i].npcController == null || followers[i].trans == null)
                {
                    // null 참조 발견, 제거
                    followers.RemoveAt(i);
                }
            }
        }
        
        // 기존 MiniGameManager에서 호출하던 AddScore 기능 제공
        public int GetTotalPoints()
        {
            if (followers == null) return 0;
            
            int total = 0;
            foreach (var data in followers)
            {
                if (data != null)
                {
                    total += data.pointValue;
                }
            }
            return total;
        }
        
        private void UpdateScore()
        {
            int totalPoints = GetTotalPoints();
            int npcCount = followers != null ? followers.Count : 0;
            
            OnScoreUpdated?.Invoke(totalPoints, npcCount);
        }
        
        // 앱 종료 시 인스턴스 정리
        private void OnDestroy()
        {
            Debug.Log($"[FollowerManager] OnDestroy 호출 - _instance: {_instance}, this: {this}, isCleaningUp: {isCleaningUp}");
            
            if (_instance == this && !isCleaningUp)
            {
                CleanupManager();
                _instance = null;
            }
        }
        
        // Unity 에디터에서 디버깅용 (프로덕션 코드에서는 제거)
        private void OnValidate()
        {
            // 인스펙터에서 값이 변경되었을 때 호출됨
            if (Application.isPlaying && _instance == this)
            {
                Debug.Log($"[FollowerManager] 값 변경됨 - 팔로워 수: {(followers != null ? followers.Count : 0)}");
            }
        }
    }
}