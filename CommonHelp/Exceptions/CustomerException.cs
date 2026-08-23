namespace CommonHelp;

/// <summary>
/// 文件超出预期大小限制时抛出的业务异常。
/// </summary>
public class FileBigException : Exception
{
    /// <summary>以错误消息创建异常。</summary>
    public FileBigException(string exMsg) : base(exMsg)
    {
    }

    /// <summary>以错误消息与内部异常创建异常。</summary>
    public FileBigException(string exMsg, Exception ex) : base(exMsg, ex)
    {
    }
}
