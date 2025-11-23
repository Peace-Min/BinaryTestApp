using BinaryTestApp.Service;
using BinaryTestApp.ViewModel;
using System;

namespace BinaryTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // HistoryService 초기화 (싱글톤이므로 자동 초기화됨)
            // - SW 구동 시 모든 바이너리 파일 읽어와 ObservableCollection으로 관리
            // - ReceiveViewModel 이벤트 구독하여 실시간 메시지 저장 및 추가
            var historyService = HistoryService.Instance;

            Console.WriteLine("BinaryTestApp initialized.");
            Console.WriteLine($"History directory: {AppDomain.CurrentDomain.BaseDirectory}History");

            // DisplayViewModel 생성 (단일 인스턴스로 관리)
            // - HistoryService에서 데이터 요청
            // - ObservableCollection으로 받아서 필터링 및 바인딩 모델 매핑
            var msgDisplayViewModel = new MsgDisplayViewModel();
            Console.WriteLine($"MsgModel filtered messages: {msgDisplayViewModel.FilteredMessages.Count}");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
