using System;
using System.IO;

namespace BinaryTestApp.Service
{
    /// <summary>
    /// 파일 경로 관리 서비스
    /// 이력 파일의 저장 경로를 관리합니다.
    /// </summary>
    public class FilePathService
    {
        private static readonly FilePathService _instance = new FilePathService();
        public static FilePathService Instance => _instance;

        private readonly string _baseHistoryDirectory;
        private string _customHistoryDirectory;

        private FilePathService()
        {
            // 기본 경로: 실행 파일과 같은 디렉토리의 History 폴더
            _baseHistoryDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "History");
            _customHistoryDirectory = null;
        }

        /// <summary>
        /// 기본 이력 디렉토리 경로
        /// </summary>
        public string BaseHistoryDirectory => _baseHistoryDirectory;

        /// <summary>
        /// 현재 사용 중인 이력 디렉토리 경로
        /// </summary>
        public string HistoryDirectory => _customHistoryDirectory ?? _baseHistoryDirectory;

        /// <summary>
        /// 이력 디렉토리 경로 설정
        /// </summary>
        /// <param name="directoryPath">설정할 디렉토리 경로 (null이면 기본 경로로 복원)</param>
        public void SetHistoryDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                _customHistoryDirectory = null;
            }
            else
            {
                _customHistoryDirectory = directoryPath;
            }

            // 디렉토리가 없으면 생성
            EnsureDirectoryExists(HistoryDirectory);
        }

        /// <summary>
        /// 메시지 타입별 폴더 경로 생성
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명 (MessageTypeConstants 사용)</param>
        /// <returns>메시지 타입별 폴더 경로 (예: History/MsgModel/)</returns>
        public string GetMessageTypeDirectory(string messageTypeFolder)
        {
            var messageTypeDirectory = Path.Combine(HistoryDirectory, messageTypeFolder);
            EnsureDirectoryExists(messageTypeDirectory);
            return messageTypeDirectory;
        }

        /// <summary>
        /// 메시지 타입별 날짜 폴더 경로 생성
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명</param>
        /// <param name="date">날짜</param>
        /// <returns>메시지 타입별 날짜 폴더 경로 (예: History/MsgModel/2024-01-15/)</returns>
        public string GetMessageTypeDateDirectory(string messageTypeFolder, DateTime date)
        {
            var dateFolder = date.ToString("yyyy-MM-dd");
            var messageTypeDirectory = GetMessageTypeDirectory(messageTypeFolder);
            var dateDirectory = Path.Combine(messageTypeDirectory, dateFolder);
            EnsureDirectoryExists(dateDirectory);
            return dateDirectory;
        }

        /// <summary>
        /// Unix 타임스탬프로부터 메시지 타입별 날짜 폴더 경로 생성
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명</param>
        /// <param name="unixTimestamp">Unix 타임스탬프 (초 단위)</param>
        /// <returns>메시지 타입별 날짜 폴더 경로</returns>
        public string GetMessageTypeDateDirectoryFromUnixTime(string messageTypeFolder, UInt32 unixTimestamp)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToLocalTime().Date;
            return GetMessageTypeDateDirectory(messageTypeFolder, date);
        }

        /// <summary>
        /// 날짜별 폴더 경로 생성 (하위 호환성 유지, 사용 권장하지 않음)
        /// </summary>
        /// <param name="date">날짜</param>
        /// <returns>날짜별 폴더 경로 (예: History/2024-01-15/)</returns>
        [System.Obsolete("Use GetMessageTypeDateDirectory instead")]
        public string GetDateDirectory(DateTime date)
        {
            var dateFolder = date.ToString("yyyy-MM-dd");
            var dateDirectory = Path.Combine(HistoryDirectory, dateFolder);
            EnsureDirectoryExists(dateDirectory);
            return dateDirectory;
        }

        /// <summary>
        /// Unix 타임스탬프로부터 날짜별 폴더 경로 생성 (하위 호환성 유지, 사용 권장하지 않음)
        /// </summary>
        /// <param name="unixTimestamp">Unix 타임스탬프 (초 단위)</param>
        /// <returns>날짜별 폴더 경로</returns>
        [System.Obsolete("Use GetMessageTypeDateDirectoryFromUnixTime instead")]
        public string GetDateDirectoryFromUnixTime(UInt32 unixTimestamp)
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToLocalTime().Date;
            return GetDateDirectory(date);
        }

        /// <summary>
        /// 파일명 생성
        /// </summary>
        /// <param name="unixTimestamp">Unix 타임스탬프</param>
        /// <param name="messageTypeName">메시지 타입 이름 (선택사항)</param>
        /// <returns>파일명 (예: 1705276800_abc123.bin)</returns>
        public string GenerateFileName(UInt32 unixTimestamp, string messageTypeName = null)
        {
            var fileName = $"{unixTimestamp}_{Guid.NewGuid():N}";
            if (!string.IsNullOrWhiteSpace(messageTypeName))
            {
                fileName = $"{messageTypeName}_{fileName}";
            }
            return $"{fileName}.bin";
        }

        /// <summary>
        /// 전체 파일 경로 생성 (메시지 타입별 폴더 사용)
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명 (MessageTypeConstants 사용)</param>
        /// <param name="unixTimestamp">Unix 타임스탬프</param>
        /// <returns>전체 파일 경로 (예: History/MsgModel/2024-01-15/1705276800_abc123.bin)</returns>
        public string GetFullFilePath(string messageTypeFolder, UInt32 unixTimestamp)
        {
            var dateDirectory = GetMessageTypeDateDirectoryFromUnixTime(messageTypeFolder, unixTimestamp);
            var fileName = GenerateFileName(unixTimestamp);
            return Path.Combine(dateDirectory, fileName);
        }

        /// <summary>
        /// 전체 파일 경로 생성 (하위 호환성 유지, 사용 권장하지 않음)
        /// </summary>
        /// <param name="unixTimestamp">Unix 타임스탬프</param>
        /// <param name="messageTypeName">메시지 타입 이름 (선택사항)</param>
        /// <returns>전체 파일 경로</returns>
        [System.Obsolete("Use GetFullFilePath(string messageTypeFolder, UInt32 unixTimestamp) instead")]
        public string GetFullFilePath(UInt32 unixTimestamp, string messageTypeName = null)
        {
            var dateDirectory = GetDateDirectoryFromUnixTime(unixTimestamp);
            var fileName = GenerateFileName(unixTimestamp, messageTypeName);
            return Path.Combine(dateDirectory, fileName);
        }

        /// <summary>
        /// 디렉토리가 존재하는지 확인하고 없으면 생성
        /// </summary>
        /// <param name="directoryPath">디렉토리 경로</param>
        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 이력 디렉토리 초기화 (디렉토리 생성)
        /// </summary>
        public void InitializeHistoryDirectory()
        {
            EnsureDirectoryExists(HistoryDirectory);
        }
    }
}

