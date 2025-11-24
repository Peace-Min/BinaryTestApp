using System;
using System.Globalization;
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
        /// 파일명 생성
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명 (확장자 분기용)</param>
        /// <param name="unixTimestamp">Unix 타임스탬프</param>
        /// <param name="messageTypeName">메시지 타입 이름 (선택사항)</param>
        /// <returns>파일명 (예: 241124_153045.MsgModel)</returns>
        public string GenerateFileName(string messageTypeFolder, UInt32 unixTimestamp, string messageTypeName = null)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToLocalTime().DateTime;
            var fileName = dateTime.ToString("yyMMdd_HHmmss");
            var extension = ResolveExtension(messageTypeFolder);
            return $"{fileName}{extension}";
        }

        /// <summary>
        /// 파일명에서 타임스탬프 추출
        /// </summary>
        public bool TryExtractTimestampFromFileName(string fileName, out DateTime timestamp)
        {
            timestamp = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return false;
            }

            var segments = nameWithoutExtension.Split('_', (char)StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return false;
            }

            var compact = $"{segments[0]}{segments[1]}";
            if (DateTime.TryParseExact(
                compact,
                "yyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            {
                timestamp = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 전체 파일 경로 생성 (메시지 타입별 폴더 사용)
        /// </summary>
        /// <param name="messageTypeFolder">메시지 타입 폴더명 (MessageTypeConstants 사용)</param>
        /// <param name="unixTimestamp">Unix 타임스탬프</param>
        /// <returns>전체 파일 경로 (예: History/MsgModel/1705276800_abc123.MsgModel)</returns>
        public string GetFullFilePath(string messageTypeFolder, UInt32 unixTimestamp)
        {
            var messageTypeDirectory = GetMessageTypeDirectory(messageTypeFolder);
            var fileName = GenerateFileName(messageTypeFolder, unixTimestamp);
            return Path.Combine(messageTypeDirectory, fileName);
        }

        /// <summary>
        /// 메시지 타입별 파일 확장자 조회
        /// </summary>
        public string GetFileExtension(string messageTypeFolder)
        {
            return ResolveExtension(messageTypeFolder);
        }

        /// <summary>
        /// 메시지 타입별 파일 검색 패턴 (*.확장자)
        /// </summary>
        public string GetFileSearchPattern(string messageTypeFolder)
        {
            return $"*{ResolveExtension(messageTypeFolder)}";
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

        private string ResolveExtension(string messageTypeFolder)
        {
            if (string.IsNullOrWhiteSpace(messageTypeFolder))
            {
                return ".bin";
            }

            var candidate = messageTypeFolder.Trim();
            if (!candidate.StartsWith("."))
            {
                candidate = "." + candidate;
            }

            return candidate;
        }
    }
}

