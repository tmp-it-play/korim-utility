# Steam 창작마당 배포

배포 파일은 **`dist/KoRimUtility.zip` 하나**입니다. `packageId`는 `snowykte0426.korimutility`이며, 모드 목록에도 KoRim Utility 하나만 표시됩니다.

GitHub Release에서 창작마당을 자동 업데이트하는 설정은 [자동 배포 안내](continuous-deployment.md)를 참조하세요.

## 선택적 번역

| 대상 | 적용 조건 | 번역 범위 |
| --- | --- | --- |
| The Cooler | `GodlyAnnihilator.TheLowCooler` 활성화 | 냉방기 이름·설명 2개 |
| RimJobWorld | `rim.job.world` 활성화 | 6.2.1의 누락 UI·알림 10개 |

두 대상 모두 필수가 아닙니다. `LoadFolders.xml`의 `IfModActive` 조건으로 각각 `Translations/TheCooler`, `Translations/RimJobWorld`를 로드합니다. 대상이 없거나 비활성화되어 있으면 해당 번역 폴더도 로드하지 않습니다. 본체의 향후 아이템·UI 틀은 그대로 유지됩니다.

`loadAfter`는 함께 사용하는 모드 사이의 순서만 지정하며 필수 의존성이 아닙니다. 창작마당의 필요한 항목에도 두 모드를 필수로 등록하지 않습니다.

## 빌드와 설치

```sh
make check test pack
make install MODS_DIR="/actual/path/to/Mods"
```

ZIP 설치 시 압축을 풀어 `KoRimUtility` 폴더를 게임 `Mods`에 넣습니다. 한국어로 설정하고 사용하는 번역 대상 모드보다 아래에 배치합니다. 원본 모드의 선행 모드는 원본 안내를 따릅니다.

기존 `KoRim Utility - The Cooler Korean`과 `KoRim Utility - RJW Korean Supplement`는 비활성화하고 통합본만 사용합니다. 이전 별도 패키지는 `work/legacy-separate-packages`에 보관했으며 배포에서 제외됩니다.

## 창작마당 업로드

1. 게임에서 대상 없음 / The Cooler만 / RJW만 / 둘 다 활성화한 네 가지 구성을 확인합니다. 냉방기의 이름·설명·도면·건설 중 표시와 RJW 툴팁·알림을 확인합니다.
2. `dist/KoRimUtility.zip`을 별도 게시 작업 폴더에 풉니다. 개발 저장소나 개발용 심볼릭 링크 전체를 업로드하지 않습니다.
3. `distribution/Workshop-Core.bbcode`의 통합 소개문과 `About/Preview.png`를 사용합니다.
4. Steam에서 실행한 게임의 모드 업로드 기능으로 게시합니다. 비공개 상태에서 구독 설치 결과를 확인한 후 공개합니다.
5. 최초 게시 후 생성되는 `About/PublishedFileId.txt`를 보관하고 같은 항목의 업데이트에 사용합니다. 원본이나 예전 별도 모드의 게시 ID를 복사하지 않습니다.

현재 실제 게임 실행과 창작마당 업로드는 하지 않았습니다. 자동 검증은 XML, 번역 키·치환 변수, 네 가지 로드 구성 및 패키지 구조를 확인하며 게임 실행 검증을 대체하지 않습니다.

미리보기 원본은 사용자가 수정한 `Artwork/Preview.svg`입니다. `make preview`로 배포용 PNG를 갱신합니다. 이 명령은 librsvg의 `rsvg-convert`를 사용합니다.

## 원문과 고지

- The Cooler: 사용자가 제공한 ZIP 기준입니다. [창작마당 원본](https://steamcommunity.com/sharedfiles/filedetails/?id=3221592105)의 식별자와 Def를 대조했으며 원본 ZIP 및 Def 해시는 `targets.json`에 기록했습니다. 첨부 파일에 라이선스는 없으며 원본 자산은 포함하지 않습니다.
- RimJobWorld: [원본 저장소](https://gitgud.io/Ed86/rjw)의 리비전 `ce78ea5a7f43771b34b32b7b78afaa0d84b29f9e` 기준입니다. MIT 고지를 `ThirdPartyNotices/RimJobWorld-LICENSE.txt`로 포함합니다.
- 번역별 원문 기록은 각 `Translations/*/TranslationReference`에 유지하며 배포에서는 제외합니다. 원본 업데이트 시 재대조합니다.
- 통합 ZIP에는 번역, 본체 리소스, 로드 설정, `NOTICE.txt` 및 라이선스 고지만 포함합니다. 원본 Def·텍스처·DLL·작업 파일은 포함하지 않습니다.

게시 전 최종 게시물에 적용되는 [Steam 사용자 생성 콘텐츠 규칙](https://help.steampowered.com/en/faqs/view/6862-8119-C23E-EA7B)을 확인합니다. 원본 라이선스와 Steam의 게시 허용 여부는 별개이며, 통합 패키지 역시 게시 허용을 보장하지 않습니다.
