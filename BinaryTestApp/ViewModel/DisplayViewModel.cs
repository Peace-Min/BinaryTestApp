using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using BinaryTestApp.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace BinaryTestApp.ViewModel
{
    /// <summary>
    /// MsgModel 전시 ViewModel
    /// 단일 인스턴스로 관리되며, HistoryService에서 데이터를 가져와 필터링 및 바인딩 모델 매핑을 수행합니다.
    /// </summary>
    public class MsgDisplayViewModel : INotifyPropertyChanged
    {
        private static readonly MsgDisplayViewModel _instance = new MsgDisplayViewModel();
        public static MsgDisplayViewModel Instance => _instance;

        private readonly HistoryService _historyService = HistoryService.Instance;
        private MsgBindingModel _currentBindingModel;
        private ObservableCollection<MsgBindingModel> _filteredMessages;
        
        // 데이터 캐싱 및 동기화
        // Key: ReceiveTime (Unix Timestamp) - 중복 방지용
        // 주의: 동일 초에 수신된 메시지는 덮어씌워질 수 있음 (파일 시스템 제약과 동일)
        private readonly Dictionary<uint, MsgModel> _allMessagesMap = new Dictionary<uint, MsgModel>();
        private readonly object _dataLock = new object();

        // 필터링 상태
        private DateTime _selectedStartDate;
        private DateTime _selectedEndDate;
        private DateTime _selectedStartTime;
        private DateTime _selectedEndTime;

        /// <summary>
        /// 생성자 (Singleton)
        /// </summary>
        private MsgDisplayViewModel()
        {
            // 초기화
            _filteredMessages = new ObservableCollection<MsgBindingModel>();
            
            // 초기 필터링 상태 설정
            _selectedStartDate = DateTime.Today;
            _selectedEndDate = DateTime.Today;
            _selectedStartTime = DateTime.Today;
            _selectedEndTime = DateTime.Today.AddDays(1).AddSeconds(-1);

            InitializeData();
        }

        private void InitializeData()
        {
            // 1. 실시간 메시지 수신 구독 (가장 먼저 수행하여 유실 방지)
            ReceiveViewModel.Instance.MsgModelReceived += (s, e) =>
            {
                CurrentBindingModel = new MsgBindingModel(e);
                UpdateFilteredCollectionOnNewMessage(e);
            };

            // 2. 저장된 이력 로드 (Static Method 호출)
            // 구독 이후에 로드하므로, 로드 도중 수신된 메시지가 있을 수 있음 -> Dictionary로 중복 처리
            var historyData = HistoryService.LoadHistoryMessages<MsgModel>(
                MessageTypeConstants.ECS.MsgModel, 
                m => m.Header.ReceiveTime);

            lock (_dataLock)
            {
                foreach (var msg in historyData)
                {
                    // 키가 이미 존재하면(실시간 수신으로 먼저 들어옴), 덮어쓰거나 무시
                    // 여기서는 파일이 '확정된 과거'이므로 파일 내용을 신뢰하거나, 
                    // 메모리가 '최신'이므로 메모리를 유지할 수 있음.
                    // 동일 데이터라면 상관없음.
                    if (!_allMessagesMap.ContainsKey(msg.Header.ReceiveTime))
                    {
                        _allMessagesMap.Add(msg.Header.ReceiveTime, msg);
                    }
                }
            }

            // 3. 초기 필터링 적용
            RefreshFilter();
        }

        /// <summary>
        /// 현재 표시 중인 바인딩 모델
        /// </summary>
        public MsgBindingModel CurrentBindingModel
        {
            get => _currentBindingModel;
            set
            {
                _currentBindingModel = value;
                OnPropertyChanged(nameof(CurrentBindingModel));
            }
        }

        /// <summary>
        /// 필터링된 메시지 컬렉션
        /// </summary>
        public ObservableCollection<MsgBindingModel> FilteredMessages
        {
            get => _filteredMessages;
            set
            {
                _filteredMessages = value;
                OnPropertyChanged(nameof(FilteredMessages));
            }
        }

        /// <summary>
        /// 시작 날짜
        /// </summary>
        public DateTime SelectedStartDate
        {
            get => _selectedStartDate;
            set
            {
                _selectedStartDate = value;
                OnPropertyChanged(nameof(SelectedStartDate));
                RefreshFilter();
            }
        }

        /// <summary>
        /// 종료 날짜
        /// </summary>
        public DateTime SelectedEndDate
        {
            get => _selectedEndDate;
            set
            {
                _selectedEndDate = value;
                OnPropertyChanged(nameof(SelectedEndDate));
                RefreshFilter();
            }
        }

        /// <summary>
        /// 시작 시간
        /// </summary>
        public DateTime SelectedStartTime
        {
            get => _selectedStartTime;
            set
            {
                _selectedStartTime = value;
                OnPropertyChanged(nameof(SelectedStartTime));
                RefreshFilter();
            }
        }

        /// <summary>
        /// 종료 시간
        /// </summary>
        public DateTime SelectedEndTime
        {
            get => _selectedEndTime;
            set
            {
                _selectedEndTime = value;
                OnPropertyChanged(nameof(SelectedEndTime));
                RefreshFilter();
            }
        }

        /// <summary>
        /// 필터 새로고침
        /// </summary>
        public void RefreshFilter()
        {
            lock (_dataLock)
            {
                // Dictionary -> List 변환 후 필터링
                ApplyFilter(_allMessagesMap.Values.ToList());
            }
        }

        /// <summary>
        /// 실시간 메시지 수신 시 필터링된 컬렉션 업데이트
        /// </summary>
        private void UpdateFilteredCollectionOnNewMessage(MsgModel newMessage)
        {
            // 캐시 업데이트
            lock (_dataLock)
            {
                // 중복 체크 (혹시 로드와 동시에 들어왔을 경우)
                if (!_allMessagesMap.ContainsKey(newMessage.Header.ReceiveTime))
                {
                    _allMessagesMap.Add(newMessage.Header.ReceiveTime, newMessage);
                }
            }

            if (IsMessageInFilterRange(newMessage))
            {
                var bindingModel = new MsgBindingModel(newMessage);
                AddFilteredMessage(bindingModel);
            }
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        private void ApplyFilter(List<MsgModel> sourceMessages)
        {
            var startDateTime = SelectedStartDate.Date.Add(SelectedStartTime.TimeOfDay);
            var endDateTime = SelectedEndDate.Date.Add(SelectedEndTime.TimeOfDay);
            var startUnixTime = ((DateTimeOffset)startDateTime).ToUnixTimeSeconds();
            var endUnixTime = ((DateTimeOffset)endDateTime).ToUnixTimeSeconds();

            var filtered = sourceMessages
                .Where(m => 
                {
                    var timestamp = m.Header.ReceiveTime;
                    return timestamp >= startUnixTime && timestamp <= endUnixTime;
                })
                .OrderBy(m => m.Header.ReceiveTime)
                .Select(m => new MsgBindingModel(m))
                .ToList();

            FilteredMessages = new ObservableCollection<MsgBindingModel>(filtered);
        }

        /// <summary>
        /// UI 스레드에서만 필터링된 메시지 추가
        /// </summary>
        private void AddFilteredMessage(MsgBindingModel model)
        {
            if (model == null)
            {
                return;
            }

            void AddAction()
            {
                // 기존 컬렉션에 추가 (초기화하지 않음)
                if (_filteredMessages == null)
                {
                    _filteredMessages = new ObservableCollection<MsgBindingModel>();
                }
                _filteredMessages.Add(model);
            }

            var dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                AddAction();
            }
            else
            {
                dispatcher.Invoke(AddAction);
            }
        }

        /// <summary>
        /// 메시지가 현재 필터 범위에 있는지 확인
        /// </summary>
        private bool IsMessageInFilterRange(MsgModel message)
        {
            var messageTimestamp = message.Header.ReceiveTime;
            var messageDateTime = DateTimeOffset.FromUnixTimeSeconds(messageTimestamp).LocalDateTime;

            var startDateTime = SelectedStartDate.Date.Add(SelectedStartTime.TimeOfDay);
            var endDateTime = SelectedEndDate.Date.Add(SelectedEndTime.TimeOfDay);

            return messageDateTime >= startDateTime && messageDateTime <= endDateTime;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
