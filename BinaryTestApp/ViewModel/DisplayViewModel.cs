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
    /// MsgModel 전시 ViewModel
    /// 단일 인스턴스로 관리되며, HistoryService에서 데이터를 가져와 필터링 및 바인딩 모델 매핑을 수행합니다.
    /// </summary>
    public class MsgDisplayViewModel : INotifyPropertyChanged
    {
        private readonly HistoryService _historyService = HistoryService.Instance;
        private MsgBindingModel _currentBindingModel;
        private ObservableCollection<MsgBindingModel> _filteredMessages;
        
        // 필터링 상태
        private DateTime _selectedStartDate;
        private DateTime _selectedEndDate;
        private DateTime _selectedStartTime;
        private DateTime _selectedEndTime;

        /// <summary>
        /// 생성자
        /// HistoryService에서 데이터를 가져와 ObservableCollection으로 변환하고 필터링합니다.
        /// </summary>
        public MsgDisplayViewModel()
        {
            // HistoryService에서 데이터 가져오기
            var allMessages = _historyService.GetHistoryMessages<MsgModel>(MessageTypeConstants.MsgModel);
            
            // 초기 필터링 상태 설정
            _selectedStartDate = DateTime.Today;
            _selectedEndDate = DateTime.Today;
            _selectedStartTime = DateTime.Today;
            _selectedEndTime = DateTime.Today.AddDays(1).AddSeconds(-1);
            
            // 필터링 및 바인딩 모델 매핑
            ApplyFilter(allMessages);

            // 실시간 메시지 수신 처리
            ReceiveViewModel.Instance.MsgModelReceived += (s, e) =>
            {
                CurrentBindingModel = new MsgBindingModel(e);
                UpdateFilteredCollectionOnNewMessage(e);
            };
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
            var allMessages = _historyService.GetHistoryMessages<MsgModel>(MessageTypeConstants.MsgModel);
            ApplyFilter(allMessages);
        }

        /// <summary>
        /// 실시간 메시지 수신 시 필터링된 컬렉션 업데이트
        /// </summary>
        private void UpdateFilteredCollectionOnNewMessage(MsgModel newMessage)
        {
            if (IsMessageInFilterRange(newMessage))
            {
                var bindingModel = new MsgBindingModel(newMessage);
                _filteredMessages.Add(bindingModel);
            }
        }

        /// <summary>
        /// 필터 적용
        /// </summary>
        private void ApplyFilter(List<MsgModel> allMessages)
        {
            var startDateTime = SelectedStartDate.Date.Add(SelectedStartTime.TimeOfDay);
            var endDateTime = SelectedEndDate.Date.Add(SelectedEndTime.TimeOfDay);
            var startUnixTime = ((DateTimeOffset)startDateTime).ToUnixTimeSeconds();
            var endUnixTime = ((DateTimeOffset)endDateTime).ToUnixTimeSeconds();

            var filtered = allMessages
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
