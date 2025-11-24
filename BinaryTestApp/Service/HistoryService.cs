using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using BinaryTestApp.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace BinaryTestApp.Service
{
    /// <summary>
    /// 이력 관리 서비스
    /// - 메시지 수신 이벤트를 구독하여 바이너리 파일로 저장
    /// - SW 구동 시 모든 바이너리 파일을 읽어와 List로 관리
    /// - DisplayViewModel 요청 시 데이터 전달
    /// </summary>
    public class HistoryService
    {
        private static readonly HistoryService _instance = new HistoryService();
        public static HistoryService Instance => _instance;

        private readonly FilePathService _filePathService;
        
        // 메시지 타입별 List 관리 (object로 저장하여 제네릭 타입 지원)
        private readonly Dictionary<string, object> _messageCollections 
            = new Dictionary<string, object>();

        private HistoryService()
        {
            _filePathService = FilePathService.Instance;
            _filePathService.InitializeHistoryDirectory();
            
            // ReceiveViewModel 이벤트 구독
            ReceiveViewModel.Instance.MsgModelReceived += OnMessageReceived;
            
            // SW 구동 시 모든 이력 데이터 로드
            InitializeAllHistory();
        }

        /// <summary>
        /// 메시지 수신 이벤트 핸들러
        /// 바이너리 파일로 저장하고 List에 추가
        /// </summary>
        private void OnMessageReceived(object sender, MsgModel model)
        {
            try
            {
                // 바이너리 파일 저장
                SaveMessage(model, m => m.Header.ReceiveTime, MessageTypeConstants.MsgModel);
                
                // List에 추가
                var collection = GetOrCreateCollection<MsgModel>(MessageTypeConstants.MsgModel);
                collection.Add(model);
            }
            catch (ArgumentNullException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Null argument while processing received message: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid argument while processing received message: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found while processing received message: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"File not found while processing received message: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path too long while processing received message: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unauthorized access while processing received message: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security error while processing received message: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Operation not supported while processing received message: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while processing received message: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory while processing received message: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지를 바이너리 파일로 저장
        /// </summary>
        private void SaveMessage<T>(T model, Func<T, UInt32> timestampExtractor, string messageTypeFolder) where T : struct, IMarshalSerialziable
        {
            try
            {
                var timestamp = timestampExtractor(model);
                var filePath = _filePathService.GetFullFilePath(messageTypeFolder, timestamp);

                // 바이너리 데이터 저장
                var binaryData = model.Serialize();
                File.WriteAllBytes(filePath, binaryData);
            }
            catch (ArgumentNullException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Null argument while saving message: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid argument while saving message: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found while saving message: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"File not found while saving message: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path too long while saving message: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unauthorized access while saving message: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security error while saving message: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Operation not supported while saving message: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while saving message: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory while saving message: {ex.Message}");
            }
        }

        /// <summary>
        /// SW 구동 시 모든 이력 데이터 로드
        /// </summary>
        private void InitializeAllHistory()
        {
            try
            {
                // MsgModel 로드
                var msgModels = LoadAllMessages<MsgModel>(
                    m => m.Header.ReceiveTime,
                    MessageTypeConstants.MsgModel);
                
                var msgCollection = GetOrCreateCollection<MsgModel>(MessageTypeConstants.MsgModel);
                foreach (var model in msgModels)
                {
                    msgCollection.Add(model);
                }

                // 다른 메시지 타입도 여기에 추가
                // 예:
                // var otherModels = LoadAllMessages<OtherModel>(...);
                // var otherCollection = GetOrCreateCollection<OtherModel>(MessageTypeConstants.OtherModel);
                // foreach (var model in otherModels) { otherCollection.Add(model); }
            }
            catch (ArgumentNullException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Null argument while initializing history: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid argument while initializing history: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found while initializing history: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"File not found while initializing history: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path too long while initializing history: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unauthorized access while initializing history: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security error while initializing history: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid operation while initializing history: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Operation not supported while initializing history: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while initializing history: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory while initializing history: {ex.Message}");
            }
        }

        /// <summary>
        /// 메시지 타입별 List 가져오기 또는 생성
        /// </summary>
        private List<T> GetOrCreateCollection<T>(string messageTypeFolder) where T : struct, IMarshalSerialziable
        {
            if (!_messageCollections.ContainsKey(messageTypeFolder))
            {
                _messageCollections[messageTypeFolder] = new List<T>();
            }
            return (List<T>)_messageCollections[messageTypeFolder];
        }

        /// <summary>
        /// DisplayViewModel에서 요청 시 데이터 전달
        /// </summary>
        /// <typeparam name="T">IMarshalSerialziable을 구현한 구조체 타입</typeparam>
        /// <param name="messageTypeFolder">메시지 타입 폴더명 (MessageTypeConstants 사용)</param>
        /// <returns>List (없으면 빈 리스트 반환)</returns>
        public List<T> GetHistoryMessages<T>(string messageTypeFolder) where T : struct, IMarshalSerialziable
        {
            return GetOrCreateCollection<T>(messageTypeFolder);
        }

        /// <summary>
        /// 모든 저장된 메시지 로드 (파일에서)
        /// </summary>
        private List<T> LoadAllMessages<T>(
            Func<T, UInt32> timestampExtractor,
            string messageTypeFolder) where T : struct, IMarshalSerialziable
        {
            var messages = new List<T>();
            var messageTypeDirectory = _filePathService.GetMessageTypeDirectory(messageTypeFolder);

            if (!Directory.Exists(messageTypeDirectory))
            {
                return messages;
            }

            try
            {
                var searchPattern = _filePathService.GetFileSearchPattern(messageTypeFolder);
                var files = Directory.GetFiles(messageTypeDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(BuildFileSortKey)
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        var binaryData = File.ReadAllBytes(file);
                        var model = default(T);
                        model.Deserialize(binaryData);
                        messages.Add(model);
                    }
                    catch (ArgumentNullException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Null argument while loading message from {file}: {ex.Message}");
                    }
                    catch (ArgumentException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid argument while loading message from {file}: {ex.Message}");
                    }
                    catch (FileNotFoundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"File not found while loading message from {file}: {ex.Message}");
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Directory not found while loading message from {file}: {ex.Message}");
                    }
                    catch (PathTooLongException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Path too long while loading message from {file}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Unauthorized access while loading message from {file}: {ex.Message}");
                    }
                    catch (SecurityException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Security error while loading message from {file}: {ex.Message}");
                    }
                    catch (NotSupportedException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Operation not supported while loading message from {file}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"IO error while loading message from {file}: {ex.Message}");
                    }
                    catch (OutOfMemoryException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Out of memory while loading message from {file}: {ex.Message}");
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Null argument while loading all messages: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid argument while loading all messages: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found while loading all messages: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"File not found while loading all messages: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path too long while loading all messages: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unauthorized access while loading all messages: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security error while loading all messages: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid operation while loading all messages: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Operation not supported while loading all messages: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while loading all messages: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory while loading all messages: {ex.Message}");
            }

            return messages.OrderBy(timestampExtractor).ToList();
        }

        private long BuildFileSortKey(string filePath)
        {
            if (_filePathService.TryExtractTimestampFromFileName(Path.GetFileName(filePath), out var timestamp))
            {
                return new DateTimeOffset(timestamp).ToUnixTimeSeconds();
            }

            try
            {
                return new DateTimeOffset(File.GetCreationTimeUtc(filePath)).ToUnixTimeSeconds();
            }
            catch (IOException)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            catch (UnauthorizedAccessException)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// 저장된 날짜 목록 조회
        /// </summary>
        public List<DateTime> GetAvailableDates(string messageTypeFolder)
        {
            var dates = new HashSet<DateTime>();
            var messageTypeDirectory = _filePathService.GetMessageTypeDirectory(messageTypeFolder);

            if (!Directory.Exists(messageTypeDirectory))
            {
                return new List<DateTime>();
            }

            try
            {
                var searchPattern = _filePathService.GetFileSearchPattern(messageTypeFolder);
                var files = Directory.GetFiles(messageTypeDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    DateTime targetDate;
                    if (_filePathService.TryExtractTimestampFromFileName(Path.GetFileName(file), out var timestamp))
                    {
                        targetDate = timestamp.Date;
                    }
                    else
                    {
                        targetDate = File.GetCreationTime(file).Date;
                    }

                    dates.Add(targetDate);
                }
            }
            catch (ArgumentNullException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Null argument while getting available dates: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid argument while getting available dates: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Directory not found while getting available dates: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"File not found while getting available dates: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Path too long while getting available dates: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unauthorized access while getting available dates: {ex.Message}");
            }
            catch (SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Security error while getting available dates: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid operation while getting available dates: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Operation not supported while getting available dates: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while getting available dates: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory while getting available dates: {ex.Message}");
            }

            return dates
                .OrderByDescending(d => d)
                .ToList();
        }
    }
}
