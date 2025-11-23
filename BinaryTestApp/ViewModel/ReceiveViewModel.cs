using BinaryTestApp.Model;
using System;

namespace BinaryTestApp.ViewModel
{
    /// <summary>
    /// 메시지 수신 ViewModel
    /// 메시지 수신만 담당하고, 수신 시 이벤트를 발생시킵니다.
    /// </summary>
    public class ReceiveViewModel
    {
        private readonly static ReceiveViewModel _instance = new ReceiveViewModel();
        public static ReceiveViewModel Instance => _instance;

        public ReceiveViewModel() { }

        /// <summary>
        /// MsgModel 수신 처리
        /// </summary>
        public void RecevMsgModel(MsgModel model)
        {
            // 이벤트만 발생 (이력 저장은 HistoryService가 처리)
            MsgModelReceived?.Invoke(this, model);
        }

        /// <summary>
        /// 메시지 수신 이벤트
        /// </summary>
        public event EventHandler<MsgModel> MsgModelReceived;
    }
}
