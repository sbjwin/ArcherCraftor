# ArcherCraftor (아처크래프터) - 에이전트 개발 하네스 (AGENTS.md)

이 문서는 ArcherCraftor 프로젝트에서 작업하는 모든 AI 코딩 에이전트(Antigravity, Cursor, Claude Code 등)와 개발자를 위한 **표준 하네스(Harness) 규약 및 행동 지침서**입니다. 코드를 작성하거나 수정하기 전에 반드시 본 지침을 준수해야 합니다.

---

## 1. 프로젝트 정체성 및 기술 스택

- **프로젝트명**: ArcherCraftor (아처크래프터)
- **Git 저장소**: `https://github.com/MammaNakta/ArcherCraftor.git`
- **엔진 및 버전**: Unity 6 (6000.3.22f1) / Universal Render Pipeline (URP)
- **타겟 플랫폼**: PC (Steam / Standalone Windows), 키보드 & 마우스 조작
- **장르**: 2D 탑다운 쿼터뷰 궁술 액션 슈터 (Tactical Archery & Bullet Intercept)
- **핵심 레퍼런스**: 《Escape from Duckov》 스타일의 시야각(FOV) + 탄막 상쇄(Counter-Shot) 궁술 액션 + 현장 즉석 제작(Field Crafting)
- **C# 버전**: C# 9.0 (.NET Standard 2.1)

---

## 2. 핵심 게임플레이 아키텍처 규칙

### 2.1. 듀얼 퀴버(Dual Quiver) & 조작 체계
1. **좌클릭 (Left Click) = 기본 화살 (무한 탄약)**
   - `탭 (Tap, 속사)`: 조준 각도 120°, 사거리 짧음. 날아오는 적 탄막을 즉시 요격/상쇄.
   - `홀드 (Hold, 차징)`: 조준 각도 0°(완전 직선), 사거리 최대, 물리 피해 및 관통력 극대화.
2. **우클릭 (Right Click) = 특수 화살 (소모 탄약, 현장 즉석 조합)**
   - 휠 스크롤로 탄종 변경 (`충격(넉백)` ↔ `화염(폭발)` ↔ `서리(빙결)` ↔ `번개(연쇄 전격)` ↔ `독성(지속 방깎)`).
   - `탭`: 근접 위기 탈출 즉발 발사.
   - `홀드`: 광역 기믹 및 환경 상호작용 극대화.
3. **근접 무기 배제 원칙**:
   - 플레이어는 칼이나 주먹을 쓰지 않습니다.
   - 근접해오는 적은 **충격 화살(우클릭)** 또는 **속사(좌클릭 탭) + 대시(Space)**를 통해 카이팅 및 제어합니다.

---

## 3. 어셈블리 및 코드 디렉토리 구조 (Assembly Boundaries)

모든 코드는 정의된 어셈블리 경계를 엄격히 준수해야 합니다.

```
Assets/
├── Scripts/
│   ├── ArcherCraftor.Runtime.asmdef      # 런타임 게임 시스템 루트 어셈블리
│   ├── Combat/                           # 투사체, 대미지, 탄막 상쇄, 체력
│   ├── Player/                           # 플레이어 컨트롤러, 활 발사/차징 로직
│   ├── Enemy/                            # 적 AI 상태머신, 탄막 에미터
│   ├── Crafting/                         # 재료 수집, 화살 즉석 조합, 퀴버 인벤토리
│   ├── Vision/                           # 절차적 시야각(FieldOfView) 원뿔 메시
│   ├── Core/                             # 카메라 팔로우, 게임 매니저, 이벤트 버스
│   ├── UI/                               # 조준선(AimReticle), HUD, 차징 게이지
│   └── Editor/
│       ├── ArcherCraftor.Editor.asmdef   # 에디터 전용 어셈블리 (Editor 플랫폼 한정)
│       └── ArcherCraftorSceneBuilder.cs  # 샌드박스 씬 원클릭 생성 도구
└── Tests/
    ├── EditMode/
    │   ├── ArcherCraftor.Tests.EditMode.asmdef # 에디트모드 단위 테스트 어셈블리
    │   ├── HealthTests.cs
    │   ├── AimChargeMathTests.cs
    │   ├── ArrowParryMathTests.cs
    │   └── FieldCraftingMathTests.cs
    ├── PlayMode/
    │   ├── ArcherCraftor.Tests.PlayMode.asmdef # 플레이모드 통합 테스트 어셈블리
    │   └── ArrowFlightPlayModeTests.cs
    └── Harness/
        └── TestHarnessHelper.cs          # 테스트 픽스처 및 목 객체 생성 헬퍼
```

---

## 4. 에이전트 행동 가드레일 (Golden Rules)

> [!IMPORTANT]
> 아래의 원칙을 위반하는 코드는 프로덕션에 병합될 수 없습니다.

1. **Unity `.meta` 파일 무결성 유지**:
   - C# 스크립트, 폴더, 머티리얼, 프리팹 생성/삭제 시 항상 일치하는 `.meta` 파일을 함께 생성하거나 삭제해야 합니다.
   - 고아 `.meta` 파일이나 메타 파일 누락은 Unity 컴파일 에러를 유발합니다.
2. **MonoBehaviour 순수성 및 도메인 로직 분리**:
   - 계산 수식(차징 각도, 사거리, 제작 비용, 스펙 계산)은 가급적 순수 C# 함수/클래스로 작성하여 EditMode 테스트에서 엔진 의존성 없이 즉시 검증 가능하게 만듭니다.
3. **가비지 컬렉션(GC) 알로케이션 최소화**:
   - `Update()` 또는 `FixedUpdate()` 루프 내에서 `new`, `LINQ`, `FindObjectsOfType`, 문자열 접합(`+`)을 사용하지 마십시오.
   - 물리 쿼리는 `Physics.OverlapSphereNonAlloc` 형태를 권장합니다.
4. **이벤트 기반 디커플링 (Event-Driven Decoupling)**:
   - UI와 Combat, Player 사이는 직접 참조 대신 `System.Action` 이벤트를 통해 결합도를 낮춥니다. (예: `OnHealthChanged`, `OnChargeUpdated`)
5. **텍스트(Git) vs 바이너리 에셋(구글 드라이브) 완전 분리 원칙**:
   - GitHub Git LFS 무료 한도(월 10 GiB, `upstream` 소유자 기준) 초과를 원천 차단하기 위해 **Git LFS를 사용하지 않고 바이너리 에셋과 짝꿍 `.meta`를 `.gitignore`로 전면 제외**합니다.
   - **Git 관리 대상**: 코드(`.cs`), 씬(`.unity`), 프리팹(`.prefab`), 머티리얼(`.mat`), `.asset`, `.anim`, `.controller` 등 텍스트 파일만 커밋합니다.
   - **구글 드라이브 동기화 대상 (`scripts/sync-assets.ps1`)**: `Assets/` 어느 경로에 만들든 이미지(`.png`, `.psd` 등), 3D 모델(`.fbx`, `.blend` 등), 사운드(`.wav`, `.ogg`, `.mp3` 등), 영상(`.mp4` 등), 폰트(`.ttf`, `.otf` 등), 아카이브 등 모든 바이너리 파일과 해당 `.meta` 파일은 구글 드라이브 공유 `.zip`으로 추출/복원합니다.
   - 바이너리 에셋을 추가/수정한 뒤에는 **반드시 Unity 에디터를 열어 `.meta`가 생성된 상태에서 `scripts/sync-assets.ps1 -Export`를 실행**해야 팀원 간 GUID 참조가 깨지지 않습니다.

---

## 5. 자동화 검증 하네스 (Verification Harness)

코드 변경 후에는 반드시 하네스 검증 스크립트를 실행하여 정합성을 확인합니다:

```powershell
# 1. 메타 파일, 어셈블리, 코드 규칙, Git 바이너리 차단 가드레일 통합 점검
powershell -ExecutionPolicy Bypass -File scripts/harness/verify.ps1

# 2. 메타 파일 정합성 전용 검사
powershell -ExecutionPolicy Bypass -File scripts/harness/check-meta.ps1

# 3. 유니티 테스트 실행 (에디터 연동 시)
powershell -ExecutionPolicy Bypass -File scripts/harness/run-tests.ps1

# 4. 바이너리 에셋 구글 드라이브 동기화 (경로 보존형 추출/복원)
powershell -ExecutionPolicy Bypass -File scripts/sync-assets.ps1 -Status
powershell -ExecutionPolicy Bypass -File scripts/sync-assets.ps1 -SetDefaultArchive "구글드라이브경로\ArcherCraftor_Assets.zip"
powershell -ExecutionPolicy Bypass -File scripts/sync-assets.ps1 -Export
powershell -ExecutionPolicy Bypass -File scripts/sync-assets.ps1 -Import -CleanBeforeImport
```

---

## 6. 기획 및 설정 참고 문서
- [게임 기획 및 컨셉 상세 (GameConcept.md)](docs/GameConcept.md)
- [스토리 및 세계관 설정 (Story.md)](docs/Story.md)
- [시스템 아키텍처 상세 (ARCHITECTURE.md)](docs/ARCHITECTURE.md)
- [태스크 로드맵 및 백로그 (TASKS.md)](docs/TASKS.md)

