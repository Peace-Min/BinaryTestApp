---
description: ".NET Framework 4.7.2 환경. 대량의 로그 파일(IBIT) 처리를 위한 고성능 파일 서비스(File Service) 구현 가이드. Zero-Allocation 파싱 및 메모리 최적화 포함."
---

# High-Performance Log File Service

이 스킬은 UI나 바인딩 로직을 제외하고, **순수하게 로컬 파일시스템에서 대량의 로그 파일을 스캔, 파싱, 정리, 로드**하는 서비스 클래스를 구현할 때 활성화됩니다.

## 0. 환경 및 필수 전제 (Prerequisites)
- **Target Framework:** .NET Framework 4.7.2
- **Required NuGet:** `System.Memory` (필수)
- **File Naming:** `[Status]_[UnixTimestamp].bin` (예: `G_1709876543.bin`)

## 1. 데이터 모델 (Data Model)
가비지 컬렉션(GC) 부하를 없애기 위해 **Class 사용을 금지**하고 **Struct**를 사용합니다.

```csharp
public enum LogStatus : byte 
{ 
    Success = (byte)'G', 
    Error = (byte)'R', 
    Performance = (byte)'L' 
}

// 힙 할당 방지를 위한 Readonly Struct
public readonly struct HistoryMetadata
{
    public string FullPath { get; }
    public DateTime Timestamp { get; }
    public LogStatus Status { get; }

    public HistoryMetadata(string path, DateTime time, LogStatus status)
    {
        FullPath = path; Timestamp = time; Status = status;
    }
}


2. 파일 서비스 구현 (Service Logic)
A. 핵심 스캔 로직 (Scan & Parse)
Directory.GetFiles 대신 EnumerateFiles를 사용하며, 파일명 파싱 시 Span<T>을 사용하여 string 객체 생성을 원천 차단합니다.
public class LogFileService
{
    // UnixTime 파싱 최적화 (Span 지원용)
    private static long ParseUnixTime(ReadOnlySpan<char> span)
    {
        long result = 0;
        foreach (char c in span)
        {
            if (c < '0' || c > '9') continue; // 숫자 외 건너뜀
            result = result * 10 + (c - '0');
        }
        return result;
    }

    public IEnumerable<HistoryMetadata> ScanFiles(string directoryPath, int retentionDays = 0)
    {
        // 기준 시간 (보관 기간 설정 시 사용)
        DateTime retentionLimit = retentionDays > 0 ? DateTime.Now.AddDays(-retentionDays) : DateTime.MinValue;

        foreach (var path in Directory.EnumerateFiles(directoryPath, "*.bin"))
        {
            // 1. 파일명 Span으로 획득 (Zero Allocation)
            ReadOnlySpan<char> fileName = Path.GetFileNameWithoutExtension(path).AsSpan();
            if (fileName.Length < 3) continue;

            // 2. 파싱 (Status & UnixTime)
            LogStatus status = (LogStatus)fileName[0];
            
            // '_' 이후부터 숫자 파싱
            ReadOnlySpan<char> timePart = fileName.Slice(2); 
            long unixSeconds = ParseUnixTime(timePart);
            
            DateTime timestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;

            // 3. (옵션) 보관 기간 지난 파일 자동 삭제
            if (retentionDays > 0 && timestamp < retentionLimit)
            {
                // 삭제는 IO 작업이므로 별도 처리하거나 여기서 즉시 삭제
                try { File.Delete(path); } catch { }
                continue; // 리스트에 포함하지 않음
            }

            yield return new HistoryMetadata(path, timestamp, status);
        }
    }
}

B. 바이너리 읽기 (Async Read)
파일 전체를 읽을 때는 UI 스레드 차단을 막기 위해 비동기 I/O를 사용합니다.

public async Task<byte[]> LoadFileContentAsync(string fullPath)
{
    // .NET 4.7.2 호환 비동기 읽기
    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
    {
        byte[] buffer = new byte[fs.Length];
        await fs.ReadAsync(buffer, 0, buffer.Length);
        return buffer;
    }
}

3. 성능 검증 기준 (Checklist)
코드가 생성되면 다음을 확인하라:

할당 확인: ScanFiles 루프 안에서 new string이나 Split이 사용되지 않았는가?

타입 확인: HistoryMetadata가 struct인가?

IO 방식: EnumerateFiles를 사용했는가?