# RJW 의료 아이템 아이콘

RJW 6.2.1의 신체 부품 44개와 재료 2개에 기본 게임용 및 Bionic icons용 그림을 연결합니다. 각 스타일은 19종의 부위·등급 조합입니다. 기본 부품 그림에 게임의 색상을 곱하는 방식으로 PNG 18개가 38개 표시 조합을 만듭니다.

## 자동 선택

| 활성 모드 | 로드하는 아이콘 |
| --- | --- |
| RJW 없음 | 교체 패치·텍스처 모두 로드하지 않음 |
| RJW, Bionic icons 없음 | `Integrations/RimJobWorld/Vanilla` |
| RJW와 Bionic icons 함께 사용 | `Integrations/RimJobWorld/BionicIcons` |

`LoadFolders.xml`의 `IfModActive`, `IfModNotActive`, `IfModActiveAll`을 사용합니다. 설치된 RimWorld 1.6의 `Verse.ModLoadFolders`와 `Verse.LoadFolder`에서 실제 지원을 확인했습니다. 화면에 표시되는 모드 이름 대신 `rim.job.world`, `automatic.bionicicons` 패키지 ID를 판별합니다. Bionic icons는 선택 사항이며 필수 의존성이 아닙니다.

## 그림과 색상

- 기본 게임용: 얇은 비스듬한 의료 상자. 무채색 부품 PNG 5개, 색상이 있는 수지·슬라임 용기 2개입니다. `drawSize`는 순정 의료 부품과 같은 `0.80`입니다.
- Bionic icons용: 정면의 정사각형 케이스, 짙은 테두리와 하단 띠. 무채색 부품 PNG 5개, 초월공학 전용 PNG 4개, 수지·슬라임 케이스 2개입니다. 해당 모드와 같은 `drawSize` `1.0`을 사용합니다.
- 일반 `(190,190,190)`, 의수·의족급 `(154,124,104)`, 생체공학 `(189,169,118)`, 초월공학 `(155,165,148)` 색상은 순정 Def에서 확인한 값입니다.
- Bionic icons용 초월공학 그림은 회녹색 몸체와 밝은 녹색 띠를 이미지에 포함합니다. 흰색 `(255,255,255)`으로 표시하여 중복 착색을 피합니다.
- 성별 기호, 두 개의 곡선, 조리개 모양, 평평한 흉부 기호로 부위를 구분합니다. 수지는 호박색 덩어리, 슬라임은 연녹색 젤로 구분합니다.

모든 배포 그림은 내장 `image_gen`으로 개별 생성한 128 × 128 RGBA PNG입니다. 입력 참고 이미지의 역할과 최종 프롬프트, 원본·결과 SHA-256은 [generation-prompts.json](../Artwork/RimJobWorld/generation-prompts.json)에 있습니다. 생성 이후에는 크기 축소 및 투명 여백 정리만 수행했습니다. 등급별 착색은 게임이 수행하며, 참고용 원본 게임/모드 이미지와 생성 고해상도 원본은 배포 ZIP에 포함하지 않습니다.

## 연결 범위

[icon-manifest.json](../Artwork/RimJobWorld/icon-manifest.json)에 아이템별 스타일·경로·색상·크기를 기록합니다. [icon-audit.json](../Artwork/RimJobWorld/icon-audit.json)은 생성 전 원본 조사 기록입니다.

XML 패치는 구체적인 ThingDef의 `graphicData`만 교체합니다. 패치가 XML 상속보다 먼저 실행되므로 `Inherit="False"`를 지정한 완전한 그래픽을 추가합니다. Def 이름·설명·수치·재료·레시피·신체 렌더링은 바꾸지 않습니다. 조사한 아이템에는 별도 `uiIconPath`가 없으며 기본 게임은 `graphicData`의 이미지와 색상을 목록 아이콘에도 사용합니다.

새 경로는 `KoRimUtility/RJW/{Vanilla|BionicIcons}/...`입니다. 설치된 Bionic icons의 원본 경로 매핑과 일치하지 않으므로 그 모드의 시작 시 자동 교체가 이미 생성한 그림을 다시 덮어쓰지 않습니다. Kilo의 RJW 텍스처 모드는 조사한 버전에서 이 부품 경로나 Def를 교체하지 않습니다. 이후 다른 모드가 같은 ThingDef의 그래픽을 다시 수정하면 그 모드와의 별도 확인이 필요합니다.

## 갱신과 검증

`python tools/rjw_icons.py`는 조사 기록에서 두 XML 패치와 연결 목록을 재생성합니다. `python tools/prepare_rjw_icons.py --jobs <생성 결과 JSON>`은 내장 도구의 출력 PNG를 원래 알파 채널을 유지하여 축소하고 프롬프트 기록과 로컬 비교표를 만듭니다. 후자에만 Pillow가 필요하며 사용자 게임에는 Python이 필요하지 않습니다.

자동 검증은 32가지 모드 활성 조합, 두 스타일 각각 46개 아이템의 중복·누락, PNG 경로·규격·투명도, 순정 등급 색상과 재료 분리, 배포 포함 여부를 확인합니다. 로컬 비교표는 `work/rjw-icon-audit/generated.html`이며 XML 색상 곱셈을 시각화한 것으로 실제 게임 캡처가 아닙니다. 실제 게임에서의 렌더링 확인은 별도입니다.
