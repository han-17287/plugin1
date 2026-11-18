# v0.11.1 — Unity 6000.6 API 수정

사용자 Console에서 확인된 두 호출을 수정했다.

- AncientWorldBatch.cs의 GetInstanceID() 호출 제거. 텍스처 자체를 키로 사용하고 묶음 키는 관리 객체 참조/해시로 구분한다. EntityId의 버전별 숫자 형식에도 의존하지 않는다.
- AncientGame.cs의 시작 검색을 FindAnyObjectByType<AncientGame>()로 변경했다. 순서가 필요 없는 중복 부팅 방지 검색이므로 ID 정렬이 필요하지 않다.
- 대화/기록 창을 Esc로 닫는 입력이 같은 프레임에 정비 메뉴까지 여는 경우를 입력 차단 기간으로 방지했다.
- 정적 검사에 활성 프로젝트의 GetInstanceID()/FindFirstObjectByType 호출 금지 항목을 추가했다.

기존 v0.11.0을 새 폴더의 v0.11.1 전체 프로젝트로 교체하거나, GitHub 최신 AncientGame.cs / AncientWorldBatch.cs를 같은 프로젝트에 반영할 수 있다. 저장 키와 게임 진행 구조는 바꾸지 않는다.

구버전 Unity 참조 DLL 보조 검사는 6000.6의 API 폐기를 잡지 못했다. 실제 Unity 6000.6.7f1 Editor 실행은 여전히 이 환경에서 수행하지 못했으며, 해당 검사를 통과했다고 표시하지 않는다. 확인된 오류 호출은 소스에서 제거했다.

총 10시간 개발은 계속 진행 중이다. 이 파일은 완성본 발표가 아닌 실행 오류 수정이다.
