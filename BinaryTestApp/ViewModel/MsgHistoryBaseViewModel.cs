using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using BinaryTestApp.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace BinaryTestApp.ViewModel
{
    /// <summary>
    /// 메타데이터 기반 목록 전시 + 선택 시 단일 메시지 지연 로드를 수행하는 베이스 ViewModel.
    ///
    /// 핵심:
    ///   - InspectionHistoryItems: FileKey/ReceiveTime 만 보관(메시지 본문/바인딩 모델 미보관)
    ///   - SelectedInspectionHistoryItem 변경 시 Store.TryLoadMessage(fileKey) 로 단건 로드
    ///   - Store.Saved 구독 → 현재 필터 범위면 목록 Add
    ///   - Dispose 시 구독 해제 + 컬렉션 Clear
    /// </summary>
    public abstract class MsgHistoryBaseViewModel<TModel> : INotifyPropertyChanged, IDisposable
        where TModel : struct, IMarshalSerialziable
    {
        private readonly string _messageTypeFolder;
        private readonly HashSet<string> _visibleFileKeys = new HashSet<string>();
        private readonly object _gate = new object();

        private ObservableCollection<MessageMetaEntry> _inspectionHistoryItems;
        private MessageMetaEntry _selectedInspectionHistoryItem;
        private TModel _currentMessage;
        private bool _hasCurrentMessage;
        private bool _isDisposed;

        private DateTime _startDateTime;
        private DateTime _endDateTime;

        protected MsgHistoryBaseViewModel(string messageTypeFolder)
        {
            _messageTypeFolder = messageTypeFolder;
            _inspectionHistoryItems = new ObservableCollection<MessageMetaEntry>();
            _startDateTime = DateTime.Today;
            _endDateTime = DateTime.Today.AddDays(1).AddSeconds(-1);
        }

        public ObservableCollection<MessageMetaEntry> InspectionHistoryItems
        {
            get => _inspectionHistoryItems;
            private set
            {
                _inspectionHistoryItems = value;
                OnPropertyChanged(nameof(InspectionHistoryItems));
            }
        }

        public MessageMetaEntry SelectedInspectionHistoryItem
        {
            get => _selectedInspectionHistoryItem;
            set
            {
                _selectedInspectionHistoryItem = value;
                OnPropertyChanged(nameof(SelectedInspectionHistoryItem));

                if (value == null)
                {
                    _hasCurrentMessage = false;
                    _currentMessage = default(TModel);
                }
                else
                {
                    LoadSelectedDetail(value.FileKey);
                }
                OnPropertyChanged(nameof(CurrentMessage));
                OnPropertyChanged(nameof(HasCurrentMessage));
            }
        }

        public TModel CurrentMessage => _currentMessage;
        public bool HasCurrentMessage => _hasCurrentMessage;

        /// <summary>
        /// Store.GetEntries 로 목록을 재구성한다. Saved 구독은 자식 ctor 호출 시점에 초기화.
        /// </summary>
        protected void InitializeHistory()
        {
            MsgHistoryStore.Instance.Saved += OnStoreSaved;
            Reload();
        }

        public void SetFilterCondition(DateTime startDate, DateTime endDate)
        {
            _startDateTime = startDate.Date;
            _endDateTime = endDate.Date.AddDays(1).AddSeconds(-1);
            Reload();
        }

        private void Reload()
        {
            if (_isDisposed) { return; }

            var entries = MsgHistoryStore.Instance.GetEntries(_messageTypeFolder);
            var startUnix = (UInt32)new DateTimeOffset(DateTime.SpecifyKind(_startDateTime, DateTimeKind.Local)).ToUnixTimeSeconds();
            var endUnix = (UInt32)new DateTimeOffset(DateTime.SpecifyKind(_endDateTime, DateTimeKind.Local)).ToUnixTimeSeconds();

            var items = new ObservableCollection<MessageMetaEntry>();
            lock (_gate)
            {
                _visibleFileKeys.Clear();
                foreach (var e in entries)
                {
                    if (e.ReceiveTime < startUnix || e.ReceiveTime > endUnix) { continue; }
                    if (!_visibleFileKeys.Add(e.FileKey)) { continue; }
                    items.Add(e);
                }
            }
            InspectionHistoryItems = items;
            SelectedInspectionHistoryItem = items.FirstOrDefault();
        }

        private void OnStoreSaved(object sender, MessageMetaEntry entry)
        {
            if (_isDisposed) { return; }
            if (entry.MessageTypeFolder != _messageTypeFolder) { return; }

            var startUnix = (UInt32)new DateTimeOffset(DateTime.SpecifyKind(_startDateTime, DateTimeKind.Local)).ToUnixTimeSeconds();
            var endUnix = (UInt32)new DateTimeOffset(DateTime.SpecifyKind(_endDateTime, DateTimeKind.Local)).ToUnixTimeSeconds();
            if (entry.ReceiveTime < startUnix || entry.ReceiveTime > endUnix) { return; }

            lock (_gate)
            {
                if (!_visibleFileKeys.Add(entry.FileKey)) { return; }
            }

            // 기존 선택 상세는 저장 이벤트로 변경하지 않는다.
            // 단, 목록이 비어 있었다면 첫 항목 자동 선택.
            var wasEmpty = _inspectionHistoryItems.Count == 0;
            _inspectionHistoryItems.Add(entry);
            if (wasEmpty)
            {
                SelectedInspectionHistoryItem = entry;
            }
        }

        private void LoadSelectedDetail(string fileKey)
        {
            if (MsgHistoryStore.Instance.TryLoadMessage<TModel>(fileKey, _messageTypeFolder, out var model))
            {
                _currentMessage = model;
                _hasCurrentMessage = true;
            }
            else
            {
                _currentMessage = default(TModel);
                _hasCurrentMessage = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) { return; }
            _isDisposed = true;

            MsgHistoryStore.Instance.Saved -= OnStoreSaved;

            lock (_gate)
            {
                _visibleFileKeys.Clear();
            }
            _inspectionHistoryItems.Clear();
            _selectedInspectionHistoryItem = null;
            _currentMessage = default(TModel);
            _hasCurrentMessage = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
