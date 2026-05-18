# 이력관리 메모리 최적화 설계 노트

## 배경

`HistoryService` + `MsgDisplayViewModel` 의 기존 구조는 SW 구동 시점에 저장된 모든 바이너리 파일을 한 번에 읽어 메모리(`_allMessagesMap`)에 적재한다. 이력 항목 수가 늘어날수록 다음 문제가 발생한다.

- 팝업/뷰 오픈 시점에 전체 바이너리 파일을 `ReadAllBytes` + `Deserialize`
- 항목 수에 비례한 메모리 점유 (메시지 본문 + 바인딩 모델 보관 가능성)
- `ViewModel`이 저장·로드·캐시·필터링을 동시에 책임
- 장기간 운용 시 메시지/바인딩 모델 참조가 누적될 가능성

## 목표

- 이력 목록은 **파일명 메타데이터만** 으로 구성한다
- 목록 항목은 `Time`, `Status`, `FileKey` 만 보관 (메시지 본문 미보유)
- 사용자가 항목을 선택한 시점에 **`Store.LoadMessage(fileKey)`** 로 1건만 로드
- 수신/저장 책임은 `ViewModel` → 별도 서비스(`MsgHistoryRecorder`)로 이동
- 저장 폴더·파일명·파싱 책임은 `MsgHistoryStore` 단독

## 구조 (Before / After)

### Before

```
ReceiveViewModel.Received
  → HistoryService(싱글톤)이 구독 → SaveMessage(파일)

MsgDisplayViewModel(싱글톤) ctor
  → HistoryService.LoadHistoryMessages<T>()  ← 전체 파일 ReadAllBytes + Deserialize
  → _allMessagesMap[ts] = MsgModel             ← 전체 메시지 메모리 적재
  → ObservableCollection<MsgBindingModel>(필터링 결과)
```

핵심 문제: 메시지 본문 N건이 모두 메모리 상주.

### After

```
ReceiveViewModel.Received
  → MsgHistoryRecorder가 구독 → MsgHistoryStore.EnqueueSave(message)
                                       │
                                       ↓
                                 [Store 내부] 파일명/폴더 결정 + 파일 저장
                                       │
                                       ↓ Saved 이벤트
                                 MsgHistoryBaseViewModel(메타데이터 리스트)
                                       │
사용자가 항목 Select  ──────────────→  Store.LoadMessage<T>(fileKey)  ← 단건 로드
                                       │
                                       ↓
                                 MsgBindingModel(1건) 갱신
```

## 핵심 invariant

| ID | 항목 | 비고 |
|----|------|------|
| G1 | 목록 표시 = 메타데이터만 (`Time`, `Status`, `FileKey`) | 메시지 본문 미보관 |
| G2 | 선택 시 단일 `LoadMessage(fileKey)` 1건만 로드 | |
| G3 | 뷰 닫힘/해제 시 `Store.Saved` 구독 해제 | |
| G4 | `ViewModel`이 파일명 규칙을 모름 | `FileKey` 추상 사용 |
| G5 | `Store`가 폴더/파일명/파싱 단독 책임 | |
| G6 | `Recorder`가 수신·저장 책임 (ViewModel에서 분리) | |
| G7 | `Saved`는 저장 성공 시에만 발행 | |

## 측정 기준 (PoC)

- 목록 N건 표시 시 heap 증분 (메타데이터 항목당 < 1KB 목표)
- 항목당 메모리 점유가 메시지 본문 크기보다 작은지
- 반복 open/close 사이클에서 누적 증가가 없는지

## 적용 순서 (이번 작업)

1. 설계 노트 정리(본 문서)
2. `MsgHistoryStore` 신규 (저장/조회/파싱 단독)
3. `MsgHistoryRecorder` 신규 (수신/저장 책임 이동)
4. `MsgHistoryBaseViewModel` 신규 + `MsgDisplayViewModel` 전환
5. `HistoryService` 제거
6. `FilePathService` 내부 흡수 후 외부 제거
7. 메모리 측정 결과 정리
