using BinaryTestApp.Model;
using BinaryTestApp.Service;
using System.ComponentModel;

namespace BinaryTestApp.ViewModel
{
    /// <summary>
    /// MsgModel 전시 ViewModel.
    /// 메타데이터 기반 목록 + 선택 시 1건 지연 로드 (MsgHistoryBaseViewModel 상속).
    /// </summary>
    public class MsgDisplayViewModel : MsgHistoryBaseViewModel<MsgModel>, INotifyPropertyChanged
    {
        private static readonly MsgDisplayViewModel _instance = new MsgDisplayViewModel();
        public static MsgDisplayViewModel Instance => _instance;

        private MsgBindingModel _currentBindingModel;

        public MsgDisplayViewModel()
            : base(MessageTypeConstants.ECS.MsgModel)
        {
            // Base가 Store 구독 + Reload 수행.
            InitializeHistory();
            PropertyChanged += OnSelfPropertyChanged;
        }

        /// <summary>
        /// 현재 선택된 메시지의 바인딩 모델(지연 로드 결과로 갱신).
        /// </summary>
        public MsgBindingModel CurrentBindingModel
        {
            get => _currentBindingModel;
            private set
            {
                _currentBindingModel = value;
                OnPropertyChanged(nameof(CurrentBindingModel));
            }
        }

        /// <summary>
        /// 호환을 위한 별칭(기존 FilteredMessages 접근 코드와 동일 시그니처).
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<MessageMetaEntry> FilteredMessages
            => InspectionHistoryItems;

        private void OnSelfPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // base가 CurrentMessage/HasCurrentMessage를 갱신할 때 BindingModel 재구성.
            if (e.PropertyName == nameof(CurrentMessage) || e.PropertyName == nameof(HasCurrentMessage))
            {
                CurrentBindingModel = HasCurrentMessage ? new MsgBindingModel(CurrentMessage) : null;
            }
        }
    }
}
