using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ZeeeingGaze
{
#if UNITY_EDITOR
    [CustomEditor(typeof(NPCEmotionController))]
    public class EmotionSequenceEditor : Editor
    {
        private SerializedProperty emotionSequences;
        private SerializedProperty activeSequenceName;
        private NPCEmotionController targetController;
        
        private void OnEnable()
        {
            emotionSequences = serializedObject.FindProperty("emotionSequences");
            activeSequenceName = serializedObject.FindProperty("activeSequenceName");
            targetController = (NPCEmotionController)target;
        }
        
        public override void OnInspectorGUI()
        {
            if (!target) return;
            // 기본 인스펙터 드로잉
            DrawDefaultInspector();
            
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("감정 시퀀스 제어", EditorStyles.boldLabel);
            
            // 플레이 모드에서만 시퀀스 제어 가능
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // 현재 활성화된 시퀀스 정보 표시
                bool isSequenceActive = targetController.IsSequenceActive();
                string sequenceName = targetController.GetActiveSequenceName();
                int currentIndex = targetController.GetCurrentTransitionIndex();
                
                if (isSequenceActive)
                {
                    EditorGUILayout.LabelField($"활성 시퀀스: {sequenceName} (전환 인덱스: {currentIndex})", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("시퀀스 중지"))
                    {
                        targetController.StopEmotionSequence();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("현재 실행 중인 시퀀스 없음", EditorStyles.boldLabel);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("시퀀스 시작:", EditorStyles.boldLabel);
                
                // 시퀀스 목록 생성
                SerializedProperty sequences = emotionSequences;
                for (int i = 0; i < sequences.arraySize; i++)
                {
                    SerializedProperty sequence = sequences.GetArrayElementAtIndex(i);
                    string seqName = sequence.FindPropertyRelative("sequenceName").stringValue;
                    
                    if (!string.IsNullOrEmpty(seqName))
                    {
                        if (GUILayout.Button($"시작: {seqName}"))
                        {
                            targetController.InitializeEmotionSequence(seqName);
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("시퀀스 제어는 플레이 모드에서만 사용할 수 있습니다.", MessageType.Info);
            }
            
            // 시퀀스 편집 도움말
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("시퀀스 설정 도움말", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Emotion Sequences 리스트에 항목 추가\n" +
                "2. 시퀀스 이름 부여 (예: 'AngryToHappy')\n" +
                "3. 트랜지션 리스트에 항목 추가\n" +
                "4. 각 트랜지션에 fromEmotion, toEmotion 설정\n" +
                "5. requiredIntensityToTransition: 다음 감정으로 넘어가기 위한 감정 강도 (0-1)\n" +
                "6. minDurationInState: 다음 감정으로 넘어가기 전 최소 유지 시간 (초)\n" +
                "7. loopSequence를 체크하면 시퀀스가 반복됨\n\n" +
                "Angry → Sad → Happy 시퀀스 예시:\n" +
                "Transition 0: From=Angry, To=Sad\n" +
                "Transition 1: From=Sad, To=Happy",
                MessageType.Info
            );
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}