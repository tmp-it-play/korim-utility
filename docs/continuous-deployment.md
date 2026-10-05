# 창작마당 자동 배포

`.github/workflows/workshop.yml`은 **GitHub 정식 Release 발행 → 검사 → ZIP 생성 → 기존 Steam 창작마당 항목 업데이트**를 실행합니다. 초안·사전 릴리스와 일반 push/PR에서는 업로드하지 않습니다. Actions 화면에서 수동 실행할 수도 있습니다.

최초 항목 생성과 Steam 인증이 끝나면 기존 항목을 자동 업데이트합니다. 제목, 소개문, 미리보기, 모드 파일, 변경 기록을 갱신하며 기존 공개 범위는 유지합니다.

## 한 번 설정할 항목

1. 원격 저장소는 [tmp-it-play/korim-utility](https://github.com/tmp-it-play/korim-utility)입니다. CD 워크플로는 `main`에 반영되어 있습니다. 릴리스 태그에도 이 워크플로가 포함되어야 합니다.
2. 최초 게임 내 게시는 완료했습니다. 기존 [KoRim Utility 게시물](https://steamcommunity.com/sharedfiles/filedetails/?id=3813930171)의 ID `3813930171`을 사용합니다. CD는 ID가 없거나 `0`이면 중단하므로 새 항목을 반복 생성하지 않습니다.
3. GitHub 저장소 **Settings → Environments → `steam-workshop`** 환경과 게시 ID 변수는 등록했습니다. 필수 검토자 없이 자동 실행되도록 설정되어 있습니다.
4. 해당 환경에 아래 Steam Secrets 두 개를 등록하면 업로드 인증 준비가 끝납니다.

| 종류 | 이름 | 값 |
| --- | --- | --- |
| Variable | `STEAM_WORKSHOP_ID` | `3813930171` (등록 완료) |
| Secret | `STEAM_USERNAME` | 해당 게시물을 소유한 Steam 계정의 로그인 이름 |
| Secret | `STEAM_CONFIG_VDF` | 아래 절차로 인증한 SteamCMD `config/config.vdf`의 전체 내용 |

환경의 배포 브랜치/태그 정책은 실제 릴리스 태그를 허용하도록 설정합니다. 필수 검토자를 설정하면 업로드 전에 수동 승인 단계가 생깁니다. 자동 배포가 목적이라면 필수 검토자 설정은 선택 사항입니다.

## Steam Guard 세션 준비

Valve의 [SteamCMD 설치 안내](https://developer.valvesoftware.com/wiki/SteamCMD)를 따라 별도 로컬 폴더에 SteamCMD를 설치합니다. 가능하면 CI와 같은 Linux 환경에서 인증합니다.

```sh
./steamcmd.sh +login YOUR_STEAM_LOGIN +quit
```

이 명령은 로컬에서 직접 실행하여 비밀번호와 Steam Guard 인증을 마칩니다. 같은 명령을 다시 실행해 비밀번호·인증 코드 없이 로그인되는지 확인합니다. 그 SteamCMD 설치의 `config/config.vdf` 내용을 GitHub Secret에 직접 넣습니다. 인증 파일을 저장소·채팅·Actions 아티팩트에 올리지 않습니다. Steam 데스크톱 클라이언트의 파일이 아니라 **로그인에 성공한 SteamCMD의 파일**을 사용합니다.

Windows에서는 저장소의 `.local/steamcmd` 폴더에 준비한 Valve SteamCMD를 사용할 수 있습니다. PowerShell에서 `YOUR_STEAM_LOGIN`을 게시물을 소유한 Steam 로그인 이름으로 바꿔 실행합니다. 비밀번호는 명령 인수에 넣지 않고 SteamCMD가 요청할 때 입력합니다.

```powershell
Set-Location D:\korim-utility\.local\steamcmd
.\steamcmd.exe +login YOUR_STEAM_LOGIN +quit
```

로그인 후 같은 명령을 한 번 더 실행해 자동 로그인을 확인합니다. 인증 파일은 `D:\korim-utility\.local\steamcmd\config\config.vdf`입니다. CI에서 재인증을 요구하면 이 파일을 갱신해야 하므로 첫 실제 업로드가 성공하기 전에는 인증 검증 완료로 보지 않습니다.

CD는 이 세션을 일회용 Ubuntu 러너에 복원하며 비밀번호나 2FA 시드를 사용하지 않습니다. 세션 만료나 Steam Guard 재인증 요청이 발생하면 실패로 종료합니다. 로컬 SteamCMD에서 다시 인증하고 Secret을 갱신합니다. 무인 인증이 계정 환경에 따라 거부될 수 있으므로 첫 실제 실행으로 확인해야 합니다.

## 실행

- **시험 실행:** Actions → Deploy Steam Workshop → Run workflow → `dry_run` 체크. 게시 ID는 필요하지만 Steam Secrets나 로그인은 사용하지 않습니다. 검사 후 ZIP을 Actions 아티팩트로 저장하고 업로드를 생략합니다.
- **수동 업로드:** 같은 화면에서 `dry_run` 체크 해제.
- **자동 업로드:** GitHub에서 정식 Release 발행. 릴리스 태그의 커밋을 checkout하여 검사·업로드합니다. 동일 창작마당 항목의 배포가 겹치지 않도록 직렬 처리합니다.

업로드 원본은 `dist/KoRimUtility.zip`을 푼 `dist/workshop/KoRimUtility`뿐입니다. 저장소, 테스트, 작업 원문은 업로드하지 않습니다. `Artwork/Preview.svg`는 매번 PNG로 렌더링하므로 SVG 변경도 배포에 반영됩니다. C# UI는 현재 비활성 틀이며 CD에서 DLL을 빌드하지 않습니다.

SteamCMD가 성공 메시지와 올바른 게시 ID를 반환해야 성공으로 처리합니다. 반환 코드만으로 성공 처리하지 않습니다. 인증정보 보호를 위해 원시 Steam 로그를 출력하거나 아티팩트로 보관하지 않습니다. 출력 형식이 바뀌어 성공 확인이 실패했다면 창작마당 상태부터 확인하고 재실행 여부를 결정합니다.

2026-10-05 게임 내 최초 업로드와 구독 다운로드 파일 대조를 완료했습니다. GitHub `steam-workshop` 환경과 게시 ID 등록 후 [CD 시험 실행](https://github.com/tmp-it-play/korim-utility/actions/runs/37291305685)에서 검사·테스트·미리보기 생성·ZIP 생성·업로드 준비·아티팩트 저장이 모두 통과했습니다. 시험 실행은 Steam 업로드를 생략합니다. Steam Secrets 등록과 CI에서의 실제 SteamCMD 업로드 검증은 아직 남아 있습니다. Secrets 등록 후 수동 업로드를 한 번 확인하면 이후 정식 Release 발행마다 자동 배포됩니다.

기술 근거: [Valve 창작마당 SteamCMD 문서](https://partner.steamgames.com/doc/features/workshop/implementation#SteamCmd), [SteamCMD 인증 세션을 이용하는 배포 액션의 설정 설명](https://github.com/m00nl1ght-dev/steam-workshop-deploy#configuration). 이 프로젝트는 해당 외부 액션 대신 자체 Python 스크립트와 Valve SteamCMD를 사용합니다.
