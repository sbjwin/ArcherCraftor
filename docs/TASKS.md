# ArcherCraftor - 작업 현황 및 로드맵 (TASKS.md)

> ArcherCraftor 프로젝트의 하네스 구축 현황, 구현 백로그 및 품질 검증 상태입니다.

---

## 🚦 하네스 및 기반 인프라 (Harness Infrastructure)

- [x] **에이전트 거버넌스 하네스 (Agent Governance)**
  - [x] `AGENTS.md` 루트 지침서 작성 (아키텍처 경계, 금지 패턴, 가드레일)
  - [x] `.agents/rules/coding-standards.md` C# 코딩 규칙 정의
  - [x] `.agents/rules/combat-mechanics.md` 전투 수식 및 패링 명세 정의
  - [x] `.agents/rules/crafting-inventory.md` 현장 즉석 조합 명세 정의
- [x] **어셈블리 정의 및 구조화 (Assembly Definitions)**
  - [x] `Assets/Scripts/ArcherCraftor.Runtime.asmdef` 생성
  - [x] `Assets/Scripts/Editor/ArcherCraftor.Editor.asmdef` 생성
- [x] **유니티 테스트 하네스 (Unity Test Harness)**
  - [x] `Packages/manifest.json` 내 `com.unity.test-framework` 등록
  - [x] `Assets/Tests/Harness/TestHarnessHelper.cs` (테스트 픽스처 & 클린업)
  - [x] `Assets/Tests/EditMode/ArcherCraftor.Tests.EditMode.asmdef` 생성
  - [x] `Assets/Tests/EditMode/HealthTests.cs` (체력/대미지/힐/사망 단위 테스트)
  - [x] `Assets/Tests/EditMode/AimChargeMathTests.cs` (조준각/사거리/스펙 공식 테스트)
  - [x] `Assets/Tests/EditMode/ArrowParryMathTests.cs` (탄막 상쇄 거리 판정 테스트)
  - [x] `Assets/Tests/EditMode/FieldCraftingMathTests.cs` (현장 즉석 제작/인벤토리 테스트)
  - [x] `Assets/Tests/PlayMode/ArcherCraftor.Tests.PlayMode.asmdef` 생성
  - [x] `Assets/Tests/PlayMode/ArrowFlightPlayModeTests.cs` (투사체 이동 통합 테스트)
- [x] **자동화 검증 스크립트 (Automation Scripts)**
  - [x] `scripts/harness/check-meta.ps1` (.meta 파일 무결성 및 자동 복구 도구, `ExternalAssets` 선택 검사 지원)
  - [x] `scripts/harness/verify.ps1` (프로젝트 전역 5단계 하네스 게이트 검증기: `.meta`, `.asmdef`, C# 가드레일, 테스트 환경, 대용량/원본 에셋 차단)
  - [x] `scripts/harness/run-tests.ps1` (배치모드 UTF 테스트 실행기)
  - [x] `scripts/setup-external-assets.ps1` (`Assets/ExternalAssets` 고정 GUID 초기화 및 구글 드라이브 `.zip` 추출/`.meta` 포함 내보내기 도구)
- [x] **문서화 하네스 (Documentation Harness)**
  - [x] `docs/ARCHITECTURE.md` (아키텍처 계층, 이벤트 시퀀스, 다이어그램)
  - [x] `docs/TASKS.md` (태스크 보드 및 검증 상태)

---

## 🏹 향후 게임플레이 구현 백로그 (Next Up)

- [ ] **1. 현장 즉석 제작(Field Crafting) 런타임 구현**
  - [ ] `FieldCraftingManager.cs` 및 `ArrowRecipeSO.cs` (ScriptableObject 기반 레시피 데이터)
  - [ ] 월드 자원 드롭 및 자석 루팅 컴포넌트 (`PickupResource.cs`)
  - [ ] 마우스 휠 스크롤 탄종 변경 및 듀얼 발사 (`PlayerBow.cs`에 우클릭 조합 연동)
- [ ] **2. 5가지 특수 화살 기믹 런타임 구현**
  - [ ] 충격 화살(Impact): 넉백 및 벽 충돌 기절
  - [ ] 화염 화살(Blast): 착탄 폭발 및 장판 지속 대미지
  - [ ] 서리 화살(Frost): 이동 감속 및 광역 빙결
  - [ ] 번개 화살(Spark): 탄막 간 연쇄 방전 소거
- [ ] **3. UI / UX 고도화**
  - [ ] 조준선 옆 현재 선택된 특수 화살 아이콘 및 잔여 탄수 인디케이터
  - [ ] 현장 즉석 제작 즉발 피드백 (화살 조합 완료 사운드 및 플로팅 텍스트)
- [ ] **4. 챕터 1 프로토타입 맵 & 보스전 연출**
  - [ ] 1지역 '푸른 시작의 숲' 테마 샌드박스 확장
  - [ ] 중간 보스 (오염된 선임 근위병) 속사 대결 패턴
  - [ ] 1지역 수호신 (폭주한 숲의 수호사슴) 정화 연출

