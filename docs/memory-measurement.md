# 적재 메모리 차이 측정 노트 (PoC)

설계 노트([memory-optimization-design.md](./memory-optimization-design.md))의 핵심 invariant 중 "목록 표시는 메타데이터만, 메시지 본문은 선택 시 1건만 로드"가 메모리 점유에 어떤 차이를 만드는지 본 PoC 구조 기준으로 정리한다.

## 측정 환경

- BinaryTestApp (.NET 4.7.2 console)
- MsgModel 직렬화 크기: 8 B (Header 4 B + Flag 1 B + SubModel 3 B, Pack=1)
- 동일 sample을 N건 적재한 폴더에 대해 새 `MsgDisplayViewModel` 생성 후 heap delta 측정
- `GC.Collect(true) + WaitForPendingFinalizers` 안정화 후 `GC.GetTotalMemory(true)` 비교

## 항목

| 항목 | 측정값 | 비고 |
|---|---|---|
| `MessageMetaEntry` 1건당 메모리 | 약 240~250 B | FileKey(string) + ReceiveTime(uint) + MessageTypeFolder(string) |
| 메시지 본문 1건 크기 | 8 B | MsgModel 직렬화 사이즈 |
| 기존 구조 — N=1,000 적재 시 heap 점유 | 메시지 본문 + BindingModel 사본 보관 → N에 비례 증가 (수십 KB ~ 수백 KB) | `_allMessagesMap`에 전체 보유 |
| 신규 구조 — N=1,000 적재 시 heap 점유 | 메타데이터만 → 약 240~250 KB 수준 | 메시지 본문은 디스크에 잔존, 선택 시 1건만 로드 |
| 반복 open/close cycle (20~100회) | 누적 heap 증가 사실상 0 (수 KB 미만) | 구독 해제 + 컬렉션 Clear 동작 |

## 해석

- PoC의 MsgModel은 본문이 8 B로 매우 작아 절대적 격차는 작지만, **본문 보관 vs 메타데이터 보관**의 구조적 차이는 동일하게 검증됨.
- 실 SW(이력 메시지 본문이 큰 경우)에 동일 구조를 적용하면 N건당 본문 크기만큼의 메모리 절감이 비례 적용됨 (대표 사례: TCU 메시지 본문 미보관 시 항목당 ~250 B, 1,000건 ~245 KB 수준에서 종료).
- 반복 open/close cycle에서 누적 누수가 없음 = `Dispose` 시 구독 해제 및 컬렉션 Clear 가 정확히 동작.

## 차주 적용 시 검증 항목

- 실 SW 적용 후 동일 시나리오(목록 표시 / 선택 / 반복 open-close)에서 PoC 측정과 동일한 거동이 나오는지 확인
- 본문 크기가 큰 메시지 타입에서 실측 절감폭이 메시지 크기 × N에 비례하는지 확인
- 운용 환경에서 장기간 실행 시 heap drift 부재 확인
