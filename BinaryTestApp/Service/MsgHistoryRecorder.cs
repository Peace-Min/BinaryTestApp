using BinaryTestApp.Model;
using BinaryTestApp.ViewModel;

namespace BinaryTestApp.Service
{
    /// <summary>
    /// 메시지 수신 → Store 저장 책임을 ViewModel에서 분리한 서비스.
    /// app 구동 시점에 단일 인스턴스로 생성되어 ReceiveViewModel을 구독한다.
    /// </summary>
    public sealed class MsgHistoryRecorder
    {
        private static readonly MsgHistoryRecorder _instance = new MsgHistoryRecorder();
        public static MsgHistoryRecorder Instance => _instance;

        private MsgHistoryRecorder()
        {
            ReceiveViewModel.Instance.MsgModelReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MsgModel model)
        {
            MsgHistoryStore.Instance.EnqueueSave(
                model,
                MessageTypeConstants.ECS.MsgModel,
                m => m.Header.ReceiveTime);
        }
    }
}
