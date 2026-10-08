# 카드 획득 설정

Game 씬의 CardManager와 CardAcquisitionCanvas를 사용합니다.

- 카드 획득은 대국 시작 직후, 흑·백 합산 성공한 일반 착수 20수, 40수에 총 3번 표시합니다. 금수·중복 착수는 포함하지 않으며 세 번째 획득 뒤에는 더 이상 획득창을 열지 않습니다.
- EarlyPhaseLastStone = 20 / MiddlePhaseLastStone = 40: 20수는 초반, 40수는 중반, 60수부터 후반입니다.
- CardList가 카드 내용의 원본입니다. Name은 고유 정수 ID, DisplayName은 표시 이름, Value는 등급, ItemImage는 그림, explanation은 설명입니다.
- AvailableFrom / AvailableUntil은 획득 가능한 시작·종료 시기(모두 포함)입니다. Early~Early는 초반 전용, Early~Middle은 초반·중반 전용입니다.
- 각 시기에 서로 다른 Name을 가진 카드가 최소 6장 필요합니다. 부족하면 경고 후 대국을 계속합니다.
- 기존의 빈 리스트에 설정 예시용 샘플 18장을 등록했습니다. 실제 카드 내용으로 수정하세요. 카드 효과 실행은 별도로 구현해야 합니다.
- UseFixedSeed를 켜면 FixedSeed를 사용합니다. 끄면 게임 시작 시 시드를 생성하고 Current Seed에 기록합니다. 동일한 리스트·시드·추첨 순서는 같은 결과를 만듭니다.
- RandomInRange(startInclusive, endExclusive)는 시작 포함/끝 제외 난수 함수입니다.
- TryDrawOffers(phase, startInclusive, endExclusive, out black, out white)는 CardList의 인덱스 범위와 카드별 획득 시기를 모두 적용해 중복 없이 각각 3장을 뽑습니다.
- 중복 제한은 한 회차에 제시되는 6장 전체에 적용됩니다. 다음 획득 회차에는 같은 카드가 다시 등장할 수 있습니다.
- 먼저 흑 카드 3장만 표시하고, 흑이 1장을 고르면 같은 위치에 백 카드 3장 화면으로 전환합니다. 백까지 고르면 Canvas를 닫고 다음 프레임에 대국을 재개합니다. gibo의 Seed 버튼에는 CardManager가 이번 게임에 생성한 Current Seed를 표시하며, 누르면 시드를 클립보드에 복사합니다. 획득 카드는 사이드 탭의 위쪽 흑 카드 버튼 3개와 아래쪽 백 카드 버튼 3개에 표시하고 Black Inventory / White Inventory에 각각 보관합니다. 현재 차례인 플레이어의 카드 버튼만 활성화되며, 한 장을 사용하면 그 턴의 나머지 카드 버튼이 비활성화됩니다. 다음 플레이어의 턴이 시작되면 다시 한 장을 사용할 수 있습니다. CheckCard(name, black), UseCard(name, black)에서 true는 흑, false는 백이며 UseCard는 자기 턴·턴당 1장 제한을 함께 검사합니다. 기존 인자 1개 API는 기존 CardList 보유 상태용입니다.
- 사용한 카드는 사이드 탭에서 사라지지 않고 `(사용됨)` 상태로 남습니다. 카드 Button의 Transition을 사용하지 않아 활성·비활성, 마우스 오버, 클릭 시 색상 전환 모션이 없습니다.
- gibo 상단에는 작은 글씨로 Seed를 표시하고, 아래 ScrollRect에는 `순번. 흑/백 (x,y)` 형식으로 전체 착수 위치를 표시합니다. 새 착수 때 최신 수를 보여주며 마우스 휠이나 드래그로 위로 올려 이전 기보를 볼 수 있습니다. 상단 베젤의 기존 Scrollbar에는 다음 카드(증강) 획득까지 남은 일반 착수 수를 표시합니다.
- CardMatchBridge가 기존 대국 스크립트를 수정하지 않고 턴·착수·카드 획득을 연결합니다. 19×19 보드가 승자 없이 가득 차면 획득한 카드 전체(사용 카드 포함)의 Value 합계를 비교하며, 합계가 높은 플레이어가 패배합니다. 합계까지 같으면 최종 무승부입니다.
- 멀티플레이 확장을 위해 카드 API는 PlayerSide를 받고, 착수는 CardMoveRecord 이벤트로 내보냅니다. 네트워크 계층은 플레이어·카드 ID·착수 좌표를 권한 서버에서 검증한 뒤 같은 API에 전달할 수 있습니다.

외형은 Assets/Prefeb/카드_0.prefab의 연결된 인스턴스 6개를 사용합니다. 화면용 위치·크기는 인스턴스 오버라이드입니다. Game 씬에서 Canvas를 활성화하면 배치를 편집할 수 있고 게임 시작 시 자동으로 숨겨집니다.

Canvas를 다시 구성해야 할 때는 Game 씬을 연 상태에서 Tools > Cards > Setup Acquisition Canvas를 실행합니다. 기존 Canvas가 있으면 중복 생성하지 않습니다.
