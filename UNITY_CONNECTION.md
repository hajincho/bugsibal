# Unity와 PC의 Codex 연결

Unity가 실행 중인 PC에서 진행합니다. 이 설정은 현재 웹의 클라우드 대화를 PC에 연결하지 않습니다.

1. `codex/start-stage1-auto-entry` 브랜치의 최신 코드를 받고 Unity에서 프로젝트를 엽니다.
2. 컴파일이 끝나면 Unity 메뉴 **Tools → Codex → Connect Unity MCP**를 선택합니다. 로컬 서버가 준비되고 Unity Console에 `Unity MCP bridge verified`가 표시될 때까지 기다립니다.
3. 같은 PC의 터미널에서 다음 명령을 실행합니다.

   ```sh
   codex mcp add unityMCP --url http://127.0.0.1:8080/mcp
   ```

4. PC의 Codex를 재시작하고 이 프로젝트 폴더를 엽니다. Unity는 켜 둡니다. 새 대화에서 Unity 연결 상태 확인을 요청합니다.

서버 시작 시 uv/uvx를 찾지 못하면 Windows 터미널에서 `winget install --id astral-sh.uv -e`로 설치한 뒤 Unity를 다시 열고 연결 메뉴를 선택합니다. 첫 실행은 Python과 서버 패키지 다운로드 때문에 시간이 걸릴 수 있습니다.

에디터 연결 상태와 Codex 등록은 별개입니다. `Unity MCP bridge verified`는 Unity와 로컬 서버의 연결을 뜻하며, Codex가 Unity 도구를 표시해야 전체 연결을 확인할 수 있습니다. 로그는 `Library/CodexConnectionSetup.log`에 저장됩니다.
