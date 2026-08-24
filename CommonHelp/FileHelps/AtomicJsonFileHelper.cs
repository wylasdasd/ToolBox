using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static CommonHelp.JsonExtensions;

namespace CommonHelp;
/// <summary>
/// 原子性 JSON 文件读写工具，提供跨进程与进程内互斥的安全访问方式。
/// </summary>
/// <remarks>
/// 使用方式：<see cref="ReadAtomic{T}(string)"/> 返回句柄后修改 <c>handle.Data</c> 再调用 <c>Commit()</c> 写回；
/// 或直接用 <see cref="WriteAtomic{T}(string, T)"/> 独占覆盖写入。
/// 跨进程以 <c>FileShare.None</c> 独占；进程内按路径的监视器锁排队（锁对象随访问过的路径累积，不回收）。
/// 适用于低并发、强一致性场景。
/// </remarks>
public class AtomicJsonFileHelper
{
    // 定义一个进程内的对象锁，用于防止同一个应用内的多个线程同时开始操作。
    // 注意：这个锁只对当前进程有效，不能阻止其他独立进程访问文件；条目按访问过的路径累积，不回收。
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> fileLocks
        = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();

    /// <summary>读取（反序列化）选项；缓存静态实例，避免每次调用重建序列化元数据。</summary>
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    /// <summary>写入（序列化）选项：属性名全小写、非 ASCII 不转义、缩进；缓存静态实例，避免每次调用重建序列化元数据。</summary>
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = new LowerCaseNamingPolicy(),
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    /// 表示对某个 JSON 文件的原子性读写句柄。
    /// 当创建后会保持对文件的独占访问（FileShare.None）以及进程内监视器锁，直到 Dispose/Commit 被调用。
    /// </summary>
    public sealed class AtomicFileHandle<T> : IDisposable where T : class
    {
        private readonly object _fileLock;
        private FileStream _stream;
        private bool _committed;
        private bool _disposed;

        public T? Data { get; }

        internal AtomicFileHandle(object fileLock, FileStream stream, T? data)
        {
            _fileLock = fileLock;
            _stream = stream;
            Data = data;
            _committed = false;
            _disposed = false;
        }

        /// <summary>
        /// 将当前内存数据序列化并写回文件，然后释放文件句柄和锁。
        /// </summary>
        public void Commit()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_committed) return;

            try
            {
                // 截断并写入
                _stream.SetLength(0);
                _stream.Seek(0, SeekOrigin.Begin);
                JsonSerializer.Serialize(_stream, Data, WriteOptions);
                _stream.Flush();
                _committed = true;
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (_stream != null)
                {
                    try { _stream.Dispose(); } catch { }
                    _stream = null!;
                }
            }
            finally
            {
                _disposed = true;
                // 释放进程内锁
                System.Threading.Monitor.Exit(_fileLock);
            }
        }
    }


    /// <summary>
    /// 原子性地打开并返回文件句柄与反序列化的数据。返回的 <see cref="AtomicFileHandle{T}"/>
    /// 持有对文件的独占访问（FileShare.None）以及进程内的监视器锁，直到调用 <see cref="AtomicFileHandle{T}.Commit"/>
    /// 或 <see cref="AtomicFileHandle{T}.Dispose"/> 为止。
    /// 使用场景：调用方先调用 ReadAtomic 获取数据，对数据进行修改，然后调用 handle.Commit() 将变更写回；在此期间其他线程/进程无法修改该文件。
    /// </summary>
    public static AtomicFileHandle<T> ReadAtomic<T>(string filePath) where T : class
    {
        FileHelp.EnsureDirectoryExists(filePath);
        var fileLock = fileLocks.GetOrAdd(filePath, _ => new object());
        // 获取进程内的监视器锁并保持，直到 handle 被释放
        System.Threading.Monitor.Enter(fileLock);
        FileStream? stream = null;
        try
        {
            stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            T? data;
            if (stream.Length == 0)
            {
                data = default;
            }
            else
            {
                stream.Seek(0, SeekOrigin.Begin);
                data = JsonSerializer.Deserialize<T>(stream, ReadOptions) ?? default;
            }

            // 构造并返回句柄（句柄负责最终的写回与释放锁）
            return new AtomicFileHandle<T>(fileLock, stream, data);
        }
        catch
        {
            // 打开失败，释放监视器锁并关闭流（若已打开）
            if (stream != null)
            {
                try { stream.Dispose(); } catch { }
            }
            System.Threading.Monitor.Exit(fileLock);
            throw;
        }
    }

    /// <summary>
    /// 仅写入数据（独占写入），不做读出。
    /// 当需要在外部先读取并修改后再写入时，建议使用 <see cref="AtomicFileHandle{T}"/> 的 Commit 方法以保持整个事务的隔离性。
    /// </summary>
    public static void WriteAtomic<T>(string filePath, T data) where T : class
    {
        FileHelp.EnsureDirectoryExists(filePath);
        // 与 ReadAtomic/Commit 一致：先取进程内锁，避免同进程内排队竞争时直接抛 IOException
        var fileLock = fileLocks.GetOrAdd(filePath, _ => new object());
        lock (fileLock)
        {
            // 使用独占打开，仅在写入期间阻止其他访问
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, data, WriteOptions);
            stream.Flush();
        }
    }


}

