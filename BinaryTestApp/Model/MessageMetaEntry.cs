using System;

namespace BinaryTestApp.Model
{
    /// <summary>
    /// 이력 파일 메타데이터.
    /// 목록 전시에 필요한 최소 정보만 보관하고, 실제 메시지 본문은 FileKey로 지연 로드한다.
    /// </summary>
    public sealed class MessageMetaEntry
    {
        public MessageMetaEntry(string fileKey, UInt32 receiveTime, string messageTypeFolder)
        {
            FileKey = fileKey;
            ReceiveTime = receiveTime;
            MessageTypeFolder = messageTypeFolder;
        }

        /// <summary>
        /// Store가 메시지 본문을 다시 로드할 때 사용하는 식별자(파일명).
        /// </summary>
        public string FileKey { get; }

        /// <summary>
        /// 헤더에서 추출한 수신 시각(Unix sec).
        /// </summary>
        public UInt32 ReceiveTime { get; }

        /// <summary>
        /// 메시지 타입 폴더명. Store가 LoadMessage 시 경로 결정에 사용.
        /// </summary>
        public string MessageTypeFolder { get; }
    }
}
