using BinaryTestApp.Service;
using BinaryTestApp.ViewModel;
using System;

namespace BinaryTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 수신·저장 책임은 별도 서비스(Recorder)가 담당.
            // - app 수명 동안 단일 인스턴스로 ReceiveViewModel을 구독
            // - 수신된 메시지는 MsgHistoryStore.EnqueueSave로 위임
            var recorder = MsgHistoryRecorder.Instance;

            Console.WriteLine("BinaryTestApp initialized.");
            Console.WriteLine($"History directory: {AppDomain.CurrentDomain.BaseDirectory}History");

            // DisplayViewModel 단일 인스턴스 사용 (Singleton)
            // - HistoryService에서 데이터 요청
            // - ObservableCollection으로 받아서 필터링 및 바인딩 모델 매핑
            var msgDisplayViewModel = MsgDisplayViewModel.Instance;
            Console.WriteLine($"MsgModel filtered messages: {msgDisplayViewModel.FilteredMessages.Count}");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
