# Steam 창작마당 배포

배포 파일은 **`dist/KoRimUtility.zip` 하나**입니다. `packageId`는 `snowykte0426.korimutility`이며, 모드 목록에도 KoRim Utility 하나만 표시됩니다.

게시물: [KoRim Utility](https://steamcommunity.com/sharedfiles/filedetails/?id=3813930171). 이후 배포는 이 항목을 업데이트합니다.

GitHub Release에서 창작마당을 자동 업데이트하는 설정은 [자동 배포 안내](continuous-deployment.md)를 참조하세요.

## 선택적 번역

| 대상 | 적용 조건 | 번역 범위 |
| --- | --- | --- |
| The Cooler | `GodlyAnnihilator.TheLowCooler` 활성화 | 냉방기 이름·설명 2개 |
| RimJobWorld | `rim.job.world` 활성화 | 6.2.1의 UI·알림 10개, Resin 관련 이름·설명 16개 |
| Character Editor | `void.charactereditor` 활성화 | Zombrella·Zombgrella 이름·설명·진입 문구 6개 |
| Replace Stuff - Continued | `Memegoddess.ReplaceStuff` 활성화 | 와이드 매립형 환풍구 이름·설명 2개 |

네 대상 모두 필수가 아닙니다. `LoadFolders.xml`의 `IfModActive` 조건으로 각 대상의 `Translations` 하위 폴더를 로드합니다. 대상이 없거나 비활성화되어 있으면 해당 번역 폴더도 로드하지 않습니다. 본체의 향후 아이템·UI 틀은 그대로 유지됩니다.

`loadAfter`는 함께 사용하는 모드 사이의 순서만 지정하며 필수 의존성이 아닙니다. 창작마당의 필요한 항목에도 번역 대상 모드를 필수로 등록하지 않습니다.

Resin은 식물에서 얻는 물질이라는 원문에 맞춰 **수지**로 통일합니다. 아이템(`ResinGlob`), 바닥 잔여물(`ResinCum`), 체액(`Resin`)과 관련 신체 부위의 이름·설명을 포함합니다.

Character Editor의 **좀브렐라**, **좀브그렐라**는 실행 중 생성되는 건물이므로 일반 `ThingDef` 번역 주입 시점에는 존재하지 않습니다. 자체 호환 DLL이 `ThingTool.CreateBuilding`의 이름·설명 인수만 한국어로 바꿉니다. 도면·재설치 도면·건설 중 표시는 원본의 생성 과정을 따릅니다. `Label.UpdateLabels` 뒤에는 설명과 진입 메뉴 문구를 적용하고, `EnterZGrave.reportString`은 일반 XML로 번역합니다. Def 이름과 저장 식별자는 유지합니다. 원본의 Harmony 의존성을 사용하며 Character Editor가 꺼져 있으면 DLL도 로드하지 않습니다.

Replace Stuff - Continued의 `Vent_Over2W`는 기존 번역의 `매립형 환풍구`, `매립형 냉방기 (와이드)`와 맞춰 **매립형 환풍구 (와이드)**로 번역합니다. 설명은 `Vent_Over`를 거쳐 순정 `Vent`에서 상속되는 원문을 대조했습니다. 이전 Replace Stuff와 패키지 ID가 다르므로 Continued에만 적용합니다.

RJW 의료 부품 46개에는 기본 게임용 및 Bionic icons용 생성 이미지를 연결했습니다. Bionic icons 활성 여부에 따라 해당 그림을 자동 선택합니다. [아이콘 적용 안내](rjw-icons.md)와 [연결 목록](../Artwork/RimJobWorld/icon-manifest.json)에 색상·경로·생성 기록을 정리했습니다. 원본 조사 자료와 생성 이미지 비교표는 로컬 `work/rjw-icon-audit`에 보관하며 패키징에서 제외합니다.

## 빌드와 설치

```sh
make check test test-compatibility pack
make install MODS_DIR="/actual/path/to/Mods"
```

소스 빌드에는 Python 3.10+와 .NET SDK 9가 필요합니다. `make compatibility`는 고정된 NuGet 참조(`Krafs.Rimworld.Ref` 1.6.4523, `Lib.Harmony` 2.4.1)로 자체 호환 DLL을 생성합니다. `make pack`과 `make install`도 이 빌드를 실행합니다. ZIP 사용자에게는 .NET SDK나 별도 설치가 필요하지 않습니다. 선택적 UI DLL과 달리 Character Editor 호환 DLL은 배포에 포함됩니다.

ZIP 설치 시 압축을 풀어 `KoRimUtility` 폴더를 게임 `Mods`에 넣습니다. 한국어로 설정하고 사용하는 번역 대상 모드보다 아래에 배치합니다. 원본 모드의 선행 모드는 원본 안내를 따릅니다.

기존 `KoRim Utility - The Cooler Korean`과 `KoRim Utility - RJW Korean Supplement`는 비활성화하고 통합본만 사용합니다. 이전 별도 패키지는 `work/legacy-separate-packages`에 보관했으며 배포에서 제외됩니다.

## 창작마당 업로드

1. 게임에서 네 번역 대상과 Bionic icons의 활성화 여부에 따른 서른두 가지 구성을 확인합니다. 냉방기와 와이드 매립형 환풍구의 이름·설명·도면·건설 중 표시, RJW 툴팁·알림·수지 관련 표시, Character Editor 두 건물의 이름·설명·도면·진입 메뉴·작업 문구를 확인합니다. Character Editor의 건축 메뉴 표시 설정을 켜고 끄는 경우와 한국어 외 언어도 확인합니다. RJW 아이템은 Bionic icons를 켠 경우와 끈 경우 각각 지도 위 그림·목록 아이콘·등급 색상·수지와 슬라임 구분을 확인합니다.
2. `dist/KoRimUtility.zip`을 별도 게시 작업 폴더에 풉니다. 개발 저장소나 개발용 심볼릭 링크 전체를 업로드하지 않습니다.
3. `distribution/Workshop-Core.bbcode`의 통합 소개문과 `About/Preview.png`를 사용합니다.
4. Steam에서 실행한 게임의 모드 업로드 기능으로 게시합니다. 비공개 상태에서 구독 설치 결과를 확인한 후 공개합니다.
5. 최초 게시 후 생성되는 `About/PublishedFileId.txt`를 보관하고 같은 항목의 업데이트에 사용합니다. 원본이나 예전 별도 모드의 게시 ID를 복사하지 않습니다.

소개문은 `소개 → 원본 링크가 있는 모드 목록 → 사용 방법 → 원본 파일 미포함 안내` 형식을 사용합니다. Steam에서 직접 편집한 최종 BBCode는 `distribution/Workshop-Core.bbcode`에 반영하고, `README.md`와 `About/About.xml`도 같은 문구와 순서로 맞춥니다. README는 Markdown, 게임 내 설명은 일반 텍스트를 사용합니다. 자동 배포는 이 BBCode 파일로 설명을 갱신하므로 Steam에서만 수정하면 다음 배포 때 덮어써집니다. 게임 내 업로드로 설명이 바뀐 경우에도 이 파일을 기준으로 복원합니다.

2026-10-05 Steam에서 실행한 RimWorld 1.6으로 최초 업로드를 완료하고 공개로 설정했습니다. `Translation`, `1.6` 태그와 상세 소개문을 적용했으며, 구독으로 내려받은 파일 11개(게시 ID 포함)가 업로드 원본과 SHA-256 기준으로 모두 일치함을 확인했습니다. 게시 ID `3813930171`은 로컬 `About/PublishedFileId.txt`에도 보관합니다. Git에는 이 파일을 추적하지 않습니다.

게임의 모드 목록에서 이름·썸네일·설명 인식은 최초 게시 때 확인했습니다. 현재 서른두 가지 로드 구성의 실제 플레이와 새 번역·아이콘 화면 검증은 아직 하지 않았습니다. 자동 검증은 XML, 번역 키·치환 변수, 서른두 가지 로드 구성, 아이콘 매핑 및 패키지 구조를 확인합니다. 별도 C# 검증은 실제 Harmony를 사용해 원본과 같은 메서드 형식의 테스트 대역에 번역을 적용하고, 생성 인수·파생 이름·언어 범위를 확인합니다. Unity와 실제 게임 화면 검증을 대체하지 않습니다.

미리보기 원본은 사용자가 수정한 `Artwork/Preview.svg`입니다. `make preview`로 배포용 PNG를 갱신합니다. 이 명령은 librsvg의 `rsvg-convert`를 사용합니다.

## 원문과 고지

- The Cooler: 사용자가 제공한 ZIP 기준입니다. [창작마당 원본](https://steamcommunity.com/sharedfiles/filedetails/?id=3221592105)의 식별자와 Def를 대조했으며 원본 ZIP 및 Def 해시는 `targets.json`에 기록했습니다. 첨부 파일에 라이선스는 없으며 원본 자산은 포함하지 않습니다.
- RimJobWorld: [원본 저장소](https://gitgud.io/Ed86/rjw)의 리비전 `ce78ea5a7f43771b34b32b7b78afaa0d84b29f9e` 기준입니다. MIT 고지를 `ThirdPartyNotices/RimJobWorld-LICENSE.txt`로 포함합니다.
- Resin 추가분은 설치된 RJW 6.2.1의 원본 Def와 대조했습니다. Character Editor는 설치된 1.6용 DLL과 `Defs/CharEditor.xml`을 확인했습니다. 설명상 버전은 1.6.3이며 `About.xml`에는 1.6.1로 기록되어 있습니다. 각 파일의 SHA-256은 `targets.json`에 보관합니다.
- Replace Stuff - Continued는 설치된 1.6용 `OverWallCoolerVent.xml`과 순정 `Vent.description`을 대조했습니다. MIT 고지는 `ThirdPartyNotices/ReplaceStuff-LICENSE.txt`에 포함합니다.
- 번역별 원문 기록은 각 `Translations/*/TranslationReference`에 유지하며 배포에서는 제외합니다. 원본 업데이트 시 재대조합니다.
- 통합 ZIP에는 번역, 본체 리소스, 생성한 RJW 아이템 그림·XML 패치, 자체 Character Editor 호환 DLL, 로드 설정, `NOTICE.txt` 및 라이선스 고지만 포함합니다. 원본 Def·텍스처·DLL·빌드 참조·작업 파일은 포함하지 않습니다.

게시 전 최종 게시물에 적용되는 [Steam 사용자 생성 콘텐츠 규칙](https://help.steampowered.com/en/faqs/view/6862-8119-C23E-EA7B)을 확인합니다. 원본 라이선스와 Steam의 게시 허용 여부는 별개이며, 통합 패키지 역시 게시 허용을 보장하지 않습니다.
