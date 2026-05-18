using BinaryTestApp.Interface;
using BinaryTestApp.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryTestApp.Service
{
    /// <summary>
    /// 이력 저장/조회/파싱 단독 책임 서비스.
    ///
    /// 신규 구조의 핵심:
    ///   - GetEntries 는 바이너리를 읽지 않고 파일명 메타데이터만 반환한다.
    ///   - LoadMessage(fileKey) 는 지정된 1개 파일만 읽는다.
    ///   - EnqueueSave 성공 시 Saved 이벤트를 발행한다.
    ///   - ViewModel/Recorder 는 폴더·파일명 규칙을 모른다 (Store 내부에 캡슐화).
    /// </summary>
    public sealed class MsgHistoryStore
    {
        private static readonly MsgHistoryStore _instance = new MsgHistoryStore();
        public static MsgHistoryStore Instance => _instance;

        private readonly FilePathService _filePathService;
        private readonly object _saveLock = new object();

        private MsgHistoryStore()
        {
            _filePathService = FilePathService.Instance;
            _filePathService.InitializeHistoryDirectory();
        }

        /// <summary>
        /// 저장 성공 후 발행.
        /// </summary>
        public event EventHandler<MessageMetaEntry> Saved;

        /// <summary>
        /// 메시지를 바이너리로 직렬화하여 저장한다.
        /// 성공 시 Saved 이벤트를 메타데이터와 함께 발행.
        /// </summary>
        public void EnqueueSave<T>(T model, string messageTypeFolder, Func<T, UInt32> timestampExtractor)
            where T : struct, IMarshalSerialziable
        {
            if (timestampExtractor == null) { throw new ArgumentNullException(nameof(timestampExtractor)); }

            try
            {
                var timestamp = timestampExtractor(model);
                var filePath = _filePathService.GetFullFilePath(messageTypeFolder, timestamp);

                lock (_saveLock)
                {
                    File.WriteAllBytes(filePath, model.Serialize());
                }

                var fileKey = Path.GetFileName(filePath);
                Saved?.Invoke(this, new MessageMetaEntry(fileKey, timestamp, messageTypeFolder));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MsgHistoryStore] Save failed: {ex.Message}");
                // 저장 실패 시 Saved 이벤트는 발행하지 않는다.
            }
        }

        /// <summary>
        /// 폴더에 저장된 모든 파일의 메타데이터를 반환한다.
        /// 바이너리 본문은 읽지 않는다(파일명에서 수신 시각만 추출).
        /// </summary>
        public IReadOnlyList<MessageMetaEntry> GetEntries(string messageTypeFolder)
        {
            var result = new List<MessageMetaEntry>();
            var directory = _filePathService.GetMessageTypeDirectory(messageTypeFolder);
            if (!Directory.Exists(directory)) { return result; }

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, _filePathService.GetFileSearchPattern(messageTypeFolder));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MsgHistoryStore] GetEntries failed: {ex.Message}");
                return result;
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!_filePathService.TryExtractTimestampFromFileName(fileName, out var dt))
                {
                    continue;
                }

                var unix = (UInt32)new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Local)).ToUnixTimeSeconds();
                result.Add(new MessageMetaEntry(fileName, unix, messageTypeFolder));
            }

            return result.OrderBy(e => e.ReceiveTime).ToList();
        }

        /// <summary>
        /// 지정된 fileKey에 해당하는 단일 메시지만 디스크에서 읽어 반환한다.
        /// </summary>
        public bool TryLoadMessage<T>(string fileKey, string messageTypeFolder, out T model)
            where T : struct, IMarshalSerialziable
        {
            model = default(T);
            if (string.IsNullOrWhiteSpace(fileKey)) { return false; }

            // path traversal/rooted path 차단.
            if (Path.IsPathRooted(fileKey) ||
                fileKey.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 ||
                fileKey.Contains(".."))
            {
                return false;
            }

            var directory = _filePathService.GetMessageTypeDirectory(messageTypeFolder);
            var fullPath = Path.Combine(directory, fileKey);
            if (!File.Exists(fullPath)) { return false; }

            try
            {
                var bytes = File.ReadAllBytes(fullPath);
                model = default(T);
                model.Deserialize(bytes);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MsgHistoryStore] LoadMessage failed: {ex.Message}");
                return false;
            }
        }
    }
}
