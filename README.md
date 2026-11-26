# ArcherCraftor (아처크래프터) - 기초 프로토타입 뼈대

> 《이스케이프 프롬 덕코프(Escape from Duckov)》 스타일의 **3D 로우폴리 탑다운 쿼터뷰** + **궁술 탄막 상쇄(카운터샷) 액션** 프로토타입 프로젝트입니다.

---

## 🎮 핵심 조작법 (Controls)

| 키 입력 | 동작 | 설명 |
| :--- | :--- | :--- |
| **W, A, S, D** | 이동 | 캐릭터 이동 (탑다운 쿼터뷰 평면 기준) |
| **마우스 이동** | 조준 & 회전 | 마우스 커서 방향으로 궁수 캐릭터 360도 실시간 회전 |
| **마우스 휠 (Scroll)** | **특수 화살 선택** | 휠을 굴려 특수 화살 탄종(충격, 화염, 서리, 번개 등) 즉시 순환 선택 |
| **좌클릭 (탭/홀드)** | **기본 화살 발사 (속사/차징)** | [무한 탄약] 빠른 탄막 요격/상쇄(탭) 및 관통 파워샷(홀드) |
| **우클릭 (탭/홀드)** | **특수 화살 발사 (속사/차징)** | [제작 소모] 선택된 특수 화살 즉발 발사(탭) 및 광역/특수 기믹 강화(홀드) |
| **Space Bar** | 대시 (회피) | 이동 방향으로 순간 대시 (무적 회피) |
| **R 키** | 씬 재시작 | 테스트 샌드박스 즉시 리셋 |

---

## 🚀 빠른 시작 가이드 (Unity Editor)

1. **Unity Hub**를 실행합니다.
2. **[열기(Open)]** 버튼을 누르고 **클론한 프로젝트 폴더**를 선택합니다.
   - 권장 에디터 버전: **Unity 6 (6000.3.22f1)** 또는 2022.3 LTS 이상
3. 프로젝트가 로드된 후 상단 메뉴바를 확인합니다:
   - **`[ArcherCraftor] -> [Setup Prototype Sandbox Scene]`** 클릭!
4. 클릭 한 번으로 바닥, 장애물 기둥, 궁수 플레이어, 적 골렘, 탄막/화살 프리팹, 쿼터뷰 카메라, FOV 시야각 메시, 조준선 UI가 자동으로 완벽하게 구성되어 **`Assets/Scenes/PrototypeSandbox.unity`**로 저장됩니다.
5. 에디터 상단의 **[ ▶ Play ]** 버튼을 누르면 즉시 플레이 가능합니다!

---

## 📦 대용량 외부 에셋 공유 가이드 (Google Drive / ExternalAssets)

GitHub Git LFS 무료 한도(월 10 GiB 스토리지 / 10 GiB 다운로드 대역폭)를 절약하기 위해 **경량 리소스만 Git LFS**로 관리하고, **무거운 에셋은 `.gitignore`로 제외된 `Assets/ExternalAssets/` 폴더**에서 구글 드라이브 공유 아카이브(`.zip`)로 동기화합니다.

| 구분 | 배치 위치 | 대상 예시 |
| :--- | :--- | :--- |
| **Git / Git LFS 관리** | 일반 `Assets/` 하위 | 5 MB 이하 실사용 스프라이트(`.png`), 압축 사운드(`.ogg`/`.mp3`), 폰트, 씬/프리팹 |
| **구글 드라이브 공유** | `Assets/ExternalAssets/` | 5 MB 초과 파일, 에셋 스토어 팩, 원본 작업 파일(`.psd`, `.blend`, `.mp4`), 압축 파일(`.zip`, `.unitypackage`) |

```powershell
# 1) 최초 클론 시 1회 실행: 고정 GUID로 로컬 Assets/ExternalAssets 폴더 초기화
powershell -ExecutionPolicy Bypass -File scripts/setup-external-assets.ps1

# 2) 구글 드라이브에서 받은 zip 적용 (-CleanExtract로 이전 버전 잔존 파일 정리 후 해제)
powershell -ExecutionPolicy Bypass -File scripts/setup-external-assets.ps1 -ArchivePath "다운로드경로\ExternalAssets.zip" -CleanExtract

# 3) 구글 드라이브 업로드용 zip 생성 (반드시 Unity 에디터에서 .meta 생성 후 실행)
powershell -ExecutionPolicy Bypass -File scripts/setup-external-assets.ps1 -ExportArchivePath "공유경로\ExternalAssets.zip"
```

> **중요**: 공유 `.zip`에는 반드시 **Unity가 생성한 `.meta` 파일**이 함께 포함되어야 두 개발자 PC에서 씬/프리팹의 에셋 참조(GUID)가 깨지지 않습니다. `-ExportArchivePath` 옵션을 사용하면 `.meta` 누락 여부를 점검한 뒤 함께 패키징합니다.

---

## 📂 프로젝트 아키텍처 구조

```
Assets/
├── Scripts/
│   ├── Combat/
│   │   ├── ArrowProjectile.cs     # 관통 및 자석 히트박스(OverlapSphere) 기반 탄막 상쇄
│   │   ├── EnemyBullet.cs         # 적 탄막 투사체 및 패링(OnParried) 연동
│   │   ├── Health.cs / IDamageable.cs # 피격/체력 및 무적/사망 처리
│   │   └── ImpactEffect.cs        # 충돌 스파크/이펙트 자동 파괴
│   ├── Player/
│   │   ├── PlayerController.cs    # 쿼터뷰 이동, 마우스 회전, 대시
│   │   └── PlayerBow.cs           # 퀵샷 vs 차징 파워샷 발사 및 화살 변경 시스템
│   ├── Enemy/
│   │   ├── EnemyAI.cs             # 카이팅 거리 유지 및 탄막 공격 인공지능
│   │   └── EnemyBulletEmitter.cs  # 단발 조준, 부채꼴 확산, 360도 링 탄막 생성기
│   ├── Vision/
│   │   └── FieldOfView.cs         # 덕코프식 장애물 차단 절차적 원뿔 시야각 메시
│   ├── Core/
│   │   ├── TopDownCameraFollow.cs # 60도 쿼터뷰 + 마우스 조준 방향 Aim Peek 시야 확장
│   │   └── GameManager.cs         # 상쇄 횟수 통계 및 게임 루프 관리
│   ├── UI/
│   │   ├── AimReticleUI.cs        # 마우스 크로스헤어, 사거리 링, 차징 게이지 UI
│   │   └── InGameHUD.cs           # HP 슬라이더 및 플레이어 상태
│   └── Editor/
│       └── ArcherCraftorSceneBuilder.cs # 1클릭 자동 프로토타입 씬 빌더 도구
├── Prefabs/                       # 자동 생성된 화살, 탄환, 스파크 프리팹
├── Materials/                     # URP 셰이더 기반 컬러 머티리얼
└── Scenes/                        # 샌드박스 씬
```

---

## 📚 기획 및 설정 문서 (Docs)
- [에이전트 개발 하네스 규약 (AGENTS.md)](AGENTS.md) : 프로젝트 규칙, 어셈블리 경계, 대용량 에셋(`ExternalAssets`) 분리 원칙
- [시스템 아키텍처 문서 (ARCHITECTURE.md)](docs/ARCHITECTURE.md) : 어셈블리 계층, 이벤트 시퀀스, 테스트 전략
- [태스크 로드맵 및 백로그 (TASKS.md)](docs/TASKS.md) : 하네스 구축 현황 및 구현 백로그
- [게임 기획 및 컨셉 문서 (GameConcept.md)](docs/GameConcept.md) : 조작 체계, 탄막 상쇄, 현장 즉석 제작 시스템
- [스토리 및 세계관 문서 (Story.md)](docs/Story.md) : 세계관 배경, 주인공 설정, 챕터별 서사 및 연출 방식

