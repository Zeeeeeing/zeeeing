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
| **정현욱** | Eye Tracking & Minigame| • Eye Tracking API 구현<br>• 감정-시선 매핑 시스템<br>• 미니게임 구조 설계 |
| **곽도훈** | UI/UX & System Integration | • NPC AI 시스템<br>• UI/UX 디자인<br>• 게임 시스템 통합 |
| **류시우** | VFX & Animation | • VFX 제작 (Laser, Particle)<br>• 애니메이션 시스템<br>• 파티클 시스템<br>• 파티클 시스템 |

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
