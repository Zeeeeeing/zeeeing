# 눈빛 보내기 VR (Gaze VR)

**Eye Tracking VR Romance Simulation Game**

Meta Quest Pro의 Eye Tracking 기술을 활용한 VR 시뮬레이션 게임입니다.

---

## 프로젝트 소개

고전 플래시 게임 '눈빛 보내기'를 VR 환경으로 현대적 재해석한 프로젝트입니다. Meta Quest Pro의 Eye Tracking API를 핵심 메커니즘으로 활용하여, 사용자의 실제 시선을 통한 감정적 상호작용과 게임플레이를 구현했습니다.

### 주요 특징
- **Eye Tracking 기반 상호작용**: 실제 시선으로 NPC와 감정 교류
- **감정-시선 매핑 시스템**: 4가지 감정 상태와 시선 패턴 연동
- **미니게임**: Color Gaze, Heart Gaze 등 시선 기반 게임
- **피버 모드**: 특별한 시각 효과와 점수 부스트
- **몰입감 있는 학교 환경**: 교실, 복도, 도서관 등 다양한 공간

---

## 팀 구성 및 역할

| 이름 | 역할 | 담당 업무 |
|------|------|-----------|
| **정현욱** | Eye Tracking & Minigame| 프로젝트 관리 및 문서화<br>• 제안서 작성 및 발표<br>• 중간 보고서 작성<br>• 기술 문서 및 Readme 작성<br>• 판넬 제작 디자인<br>• 캐릭터 에셋 제작<br>• 엔딩씬 제작<br><br>게임 시스템 개발 및 구현<br>• Eye Tracking Interaction 구현<br>• Player Locomotion 구현<br>• 게임 시스템 구현<br>• 피버 모드 구현<br>• 미니게임 구현 (ColorGaze, HeartGaze)<br>• 게임-VFX 연동<br><br>사운드 및 햅틱효과<br>• Laser 제외 bgm3개, SoundEffect 2개 탐색 및 도입<br>• Haptic Effect 도입<br><br>UI/UX 및 최적화<br>• UI 변수 정보 제공<br>• 유저 피드백 및 게임 밸런싱 조절<br>• 프레임 최적화<br><br>버그 해결 및 기능 개선<br>• 게임 각종 버그 해결 (NPC 응시, 점수 중복, 리스폰 위치 등)<br>• 눈빛을 보낸 NPC가 플레이어를 바라보도록 하는 기능<br><br>팀 리더 역할(조장)<br>• 팀원 버그 해결 지원<br>• 방향성 설정<br>• VFX 자료 제공 |
| **곽도훈** | UI/UX & System Integration | 프로젝트 관리 및 문서화<br>• 눈빛보내기 주제 제안<br>• 제안서 작성<br>• 중간 보고서 작성<br>• 판넬 제작<br>• 기술문서 및 readme 작성<br><br>게임 UI/UX 제작<br>• 게임 UI/UX 제작 (점수판, 미니게임 화면, 엔딩 화면, LOVE 게이지, 남은 시간, 튜토리얼)<br>• START 버튼을 눌러 게임 시작 기능<br>• NPC를 유혹하여 올라가는 점수 비율에 맞게 LOVE 게이지가 상승하여 피버모드가 되도록 하는 기능<br>• 미니게임 및 게임 시스템 UI 통합<br>• 엔딩 전환 제작<br><br>게임 콘텐츠 및 시스템 개발<br>• 엔딩씬 제작<br>• NPC 배치 및 AI 기반 움직임 제작<br>• NPC가 유혹되면 플레이어를 따라오도록 만드는 기능<br>• NPC 캐릭터 제작 (11명)<br>• 난이도 조절<br><br>최적화 및 기타<br>• 프레임 최적화 진행<br>• 레이저 효과음 선정<br><br>버그 해결 및 기능 개선<br>• 게임 각종 버그 해결 (NPC의 뒤통수를 바라봐야 꼬셔지는 문제, 점수 중복 계산 문제, 플레이어 초기 리스폰 위치 오류 문제)<br>• 눈빛을 보낸 NPC가 플레이어를 바라보도록 하는 기능 제작 |
| **류시우** | VFX & Animation | 프로젝트 관리 및 문서화<br>• 중간 발표<br>• 보고서 작성<br>• 유저 피드백 및 게임 밸런싱 조절<br><br> LASER VFX 개발<br>• Visual Effect Graph 기반 레이저 빔 시스템 구현<br>• 레이저 빔과 스파크 파티클 시스템으로 구성된 복합 효과<br>• 충돌 감지 기반 동적 파티클 생성 및 위치 기반 방향 제어<br>• 깊이 버퍼 충돌과 스케일 조정을 통한 사실적인 레이저 표현<br><br>Heart Particle 개발<br>• Legacy와 일반 버전으로 구성된 하트 파티클 효과<br>• 캐릭터 NPC 개발<br><br>감정별 Animator Controller<br>• Happy/Sad/Angry/Neutral 전용 컨트롤러<br>• CharacterAura 연동으로 캐릭터 주변 비주얼 이펙트 구현(피버용)<br>• Transform 기반 위치 추적과 파티클 생성률 제어<br><br>통합 애니메이션 개발<br>• 통일된 매개변수 시스템<br>• 모든 컨트롤러에서 공통 Bool 매개변수 사용 (Happy/Sad/Angry/Neutral)<br>• VFX와 애니메이션의 실시간 동기화로 몰입감 있는 캐릭터 표현<br><br>NPC 시스템 통합<br>• 애니메이터 연결<br>• 감정 관련 시스템 및 미니게임 연결<br>• 캐릭터 별 애니메이션에 따른 콜라이더 수 |

---

## 기술 스택

### 개발 환경
- **Unity 6000.0.43f1** - VR 게임 엔진
- **Meta XR SDK** - Quest Pro 하드웨어 최적화
- **XR Interaction Toolkit** - VR 상호작용 프레임워크

### 최적화 기술
- **Occlusion Culling** - 성능 최적화
- **Tunneling Vignette** - VR 멀미 방지
---

## 게임 플레이

### 게임 흐름
1. **튜토리얼** (10초) - Eye Tracking 사용법 학습
2. **메인 게임플레이** (180초) - NPC 상호작용 및 미니게임
3. **성과 평가** (20초) - S/A/B/C/D 등급 시스템
4. **엔딩** - 멀티 엔딩 시스템

### 게임 시스템 구조

#### 일반 NPC 꼬시기
- **Eye Tracking 호감도 시스템**: 시선을 통한 감정 교류
- **성공 시 보상**: +100점 획득 (피버모드 시 점수 추가)
- **플레이 방법**: 감정-시선 매핑 테이블에 따라 적절한 시선 패턴으로 상호작용

#### 엘리트 NPC 미니게임
- **특별한 NPC**와의 상호작용은 미니게임을 통해 진행
- **2가지 미니게임**: Color Gaze, Heart Gaze
- **성공 시 높은 점수 획득** (피버모드 시 점수 추가)

---

## 설치 및 실행

### 시스템 요구사항
- **VR 헤드셋**: Meta Quest Pro (Eye Tracking 필수)
- **개발 환경**: Windows 10/11, Unity 6000.0.43f1 이상

### 설치 방법

1. **저장소 클론**
```bash
git clone https://github.com/zeeeing-team/gaze-vr.git
cd gaze-vr
```

2. **Unity에서 프로젝트 열기**
   - Unity Hub에서 "Add project from disk" 선택
   - 클론한 폴더 선택하여 프로젝트 열기

3. **Meta XR SDK 설정**
   - Window > Package Manager에서 Meta XR SDK 설치
   - XR Management에서 Oculus 활성화

4. **Eye Tracking 권한 설정**
   - Quest Pro에서 Eye Tracking 권한 허용
   - Meta Developer Hub에서 개발자 모드 활성화
---

## 프로젝트 구조



---

## 문서

- [**제안서**](docs/TECHNICAL_GUIDE.md) - ~~~~
- [**기술 문서**](docs/DEVELOPER_GUIDE.md) - ~~~~
- [**사용자 매뉴얼**](docs/USER_MANUAL.md) - 게임 플레이 가이드

---

## 연락처

**Team zeeeing**
- **정현욱**: hyunwook7120@hanyang.ac.kr
- **곽도훈**: kdh0327@hanyang.ac.kr  
- **류시우**: siwoor1235@gmail.com

**한양대학교 ERICA 인공지능학과 | 2025**

---
- 자체 개발 코드: MIT License

---

**⭐ 이 프로젝트가 도움이 되셨다면 Star를 눌러주세요!**
