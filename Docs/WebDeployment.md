# GitHub Pages 웹 배포

공개 주소: https://roykim2703-crypto.github.io/jungang/

2026-11-02 배포 커밋: `gh-pages` 브랜치의 `92707dc`. 공개 HTTPS 주소에서 별도의 브라우저 두 개로 방 생성, 초대코드 참가, 증강 선택, 흑·백 착수 및 시계 동기화를 확인했습니다.

카드 확대창 수정 배포: `c09b0db`. 그림 영역을 카드 틀의 비율에 맞춰 늘리고 원본 비율을 유지한 채 영역을 채우도록 변경했습니다. 그림 아래의 불투명 바탕과 그림 위의 프레임을 분리해 뒤쪽 카드가 틈으로 비치지 않도록 했습니다. 증강 그림 20개의 영역 채움과 설명 배치를 검사했습니다.

초대코드 복사 수정 배포: `d9dfa9f`. Web 빌드에서는 실제 브라우저 입력 이벤트에서 Clipboard API를 호출하고 완료 결과를 UI에 표시합니다. 지원하지 않거나 차단된 경우 기존 복사 방식과 수동 복사 안내를 사용합니다. 공개 페이지에서 방 생성 후 복사 버튼을 누르고 클립보드 내용이 실제 코드와 같은지 검사했으며, 입력란에 붙여넣기도 확인했습니다. 버튼 영역, 화면 크기 변경, 키보드, 취소 및 권한 거부 처리는 별도 JavaScript 검사로 확인했습니다. 해당 공개 페이지 검사 중 서비스 설정 파일 요청에서 일시적인 HTTP 503이 기록됐으나 방 생성 및 복사·붙여넣기는 완료됐습니다.

이 프로젝트는 GitHub Pages에서 Unity Web 빌드 파일을 제공하고 Unity Gaming Services Relay에서 2인 통신을 중계합니다. 웹 브라우저가 방장이 될 수 있으며 별도의 게임 서버 프로세스는 필요하지 않습니다. 브라우저 사이의 연결에는 WSS를 사용합니다.

## 빌드

1. Unity 6000.6.0f1의 WebGL Build Support 모듈을 설치합니다.
2. Play Mode를 종료하고 `Tools > Build > GitHub Pages Web Player`를 실행합니다.
3. `Builds/pages-build-report.json`의 `status`가 `completed`인지 확인합니다.
4. `Builds/WebGL` 폴더의 내용을 저장소의 `gh-pages` 브랜치 루트에 배포합니다. `.nojekyll` 파일도 포함해야 합니다.
5. GitHub 저장소의 Pages 게시 원본을 `gh-pages` 브랜치의 `/`로 설정합니다.

빌드 스크립트는 Home, Game, Result 씬을 포함하고 gzip 압축과 Decompression Fallback을 활성화합니다. 정적 호스팅에서 별도의 Content-Encoding 헤더 설정 없이 압축된 빌드를 읽기 위한 설정입니다. WebGL Threads Support는 꺼진 상태를 유지해야 합니다.

## 확인

- 페이지를 연 후 게임 시작 → 방 만들기에서 초대코드가 생성되어야 합니다.
- 다른 브라우저에서 같은 페이지를 열어 초대코드로 참가하고 대국을 시작합니다.
- 양쪽의 착수, 증강 선택, 남은 시간 및 결과를 확인합니다.
- 방장의 브라우저 탭을 닫으면 대국이 중단됩니다. 재접속과 방장 이전은 아직 지원하지 않습니다.

Unity Gaming Services 프로젝트의 Authentication 및 Multiplayer/Relay 서비스 설정과 사용 가능 할당량이 필요합니다. GitHub Pages는 게임의 정적 파일만 호스팅합니다.
