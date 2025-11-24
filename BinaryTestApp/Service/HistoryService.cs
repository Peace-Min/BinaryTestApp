using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using BinaryTestApp.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace BinaryTestApp.Service
{
    /// <summary>
    /// 이력 관리 서비스
    /// - 메시지 수신 이벤트를 구독하여 바이너리 파일로 저장
    /// - 데이터 로드 기능 제공 (상태 관리 안함)
    /// </summary>
    public class HistoryService
    {
        private static readonly HistoryService _instance = new HistoryService();
        public static HistoryService Instance => _instance;

        private readonly FilePathService _filePathService;
        
        private HistoryService()
        {
            _filePathService = FilePathService.Instance;
            _filePathService.InitializeHistoryDirectory();
            
            // 오래된 파일 정리 (30일 보관)
            _filePathService.DeleteOldFiles(MessageTypeConstants.ECS.MsgModel, 30);

            // ReceiveViewModel 이벤트 구독
            ReceiveViewModel.Instance.MsgModelReceived += OnMessageReceived;
        }

        /// <summary>
        /// 메시지 수신 이벤트 핸들러
        /// 바이너리 파일로 저장 (메모리에 보관하지 않음)
        /// </summary>
        private void OnMessageReceived(object sender, MsgModel model)
        {
            try
            {
                // 바이너리 파일 저장
                SaveMessage(model, m => m.Header.ReceiveTime, MessageTypeConstants.ECS.MsgModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while processing received message: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while saving message: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 모든 메시지 로드 (파일에서)
        /// Static Method: 인스턴스 없이 호출 가능
        /// </summary>
        public static List<T> LoadHistoryMessages<T>(string messageTypeFolder, Func<T, UInt32> timestampExtractor) where T : struct, IMarshalSerialziable
        {
            var messages = new List<T>();
            var filePathService = FilePathService.Instance;
            var messageTypeDirectory = filePathService.GetMessageTypeDirectory(messageTypeFolder);

            if (!Directory.Exists(messageTypeDirectory))
            {
                return messages;
            }

            try
            {
                var searchPattern = filePathService.GetFileSearchPattern(messageTypeFolder);
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error while loading message from {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while loading all messages: {ex.Message}");
            }

            return messages.OrderBy(timestampExtractor).ToList();
        }

        private static long BuildFileSortKey(string filePath)
        {
            if (FilePathService.Instance.TryExtractTimestampFromFileName(Path.GetFileName(filePath), out var timestamp))
            {
                return new DateTimeOffset(timestamp).ToUnixTimeSeconds();
            }

            try
            {
                return new DateTimeOffset(File.GetCreationTimeUtc(filePath)).ToUnixTimeSeconds();
            }
            catch
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// 저장된 날짜 목록 조회
        /// Static Method: 인스턴스 없이 호출 가능
        /// </summary>
        public static List<DateTime> GetAvailableDates(string messageTypeFolder)
        {
            var dates = new HashSet<DateTime>();
            var filePathService = FilePathService.Instance;
            var messageTypeDirectory = filePathService.GetMessageTypeDirectory(messageTypeFolder);

            if (!Directory.Exists(messageTypeDirectory))
            {
                return new List<DateTime>();
            }

            try
            {
                var searchPattern = filePathService.GetFileSearchPattern(messageTypeFolder);
                var files = Directory.GetFiles(messageTypeDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    DateTime targetDate;
                    if (filePathService.TryExtractTimestampFromFileName(Path.GetFileName(file), out var timestamp))
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while getting available dates: {ex.Message}");
            }

            return dates
                .OrderByDescending(d => d)
                .ToList();
        }
    }
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
