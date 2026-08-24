using System.Text;

namespace CommonHelp;

/// <summary>
/// UUID 批量生成结果：每行一个的 UUID 文本。
/// </summary>
public sealed record UuidGenerateResult(string Output);

/// <summary>
/// 随机与唯一标识工具：从序列随机抽取元素、批量生成 UUID/GUID。
/// </summary>
public class RandomHelp
{
    /// <summary>
    /// 从可遍历序列中随机抽取 count 个元素（返回新列表，不修改原序列）。
    /// </summary>
    public static List<T> GetRandomList<T>(IEnumerable<T> list, int count)
    {
        List<T> result = new List<T>(list); // 拷贝一份防止修改原列表
        int n = result.Count;
        count = Math.Clamp(count, 0, n);

        for (int i = 0; i < count; i++)
        {
            int r = i + Random.Shared.Next(n - i);
            (result[i], result[r]) = (result[r], result[i]);
        }

        result.RemoveRange(count, n - count);
        return result;
    }

    /// <summary>
    /// 批量生成 UUID/GUID 字符串：可选连字符（N 格式）与大小写，每行一个。
    /// </summary>
    public static Result<UuidGenerateResult> GenerateUuids(int count, bool includeHyphens, bool upperCase)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var uuid = Guid.NewGuid();
            var uuidString = includeHyphens ? uuid.ToString() : uuid.ToString("N");
            if (upperCase)
                uuidString = uuidString.ToUpperInvariant();

            sb.AppendLine(uuidString);
        }

        return Result<UuidGenerateResult>.Ok(new UuidGenerateResult(sb.ToString()));
    }
}
