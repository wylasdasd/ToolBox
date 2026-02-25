# ToolBox

<img width="1865" height="1288" alt="image" src="https://github.com/user-attachments/assets/16b27dc7-36a1-4a92-b0bd-2ef2b6f54331" />

基于 .NET MAUI Blazor 的本地开发工具箱，目标是把日常高频小工具集中到一个应用里，减少开发过程中的上下文切换。

## 主要功能

- C# Runner：运行 C# 代码片段并查看输出
- 网络工具
- 请求转 cURL
- Socket 测试
- 计算工具
- 位运算
- 常用转换（进制、格式、颜色等）
- 文本工具
- 文本整理
- 正则匹配
- Base64 编解码
- URL 编解码
- JWT 解析
- 时间戳转换
- JSON 格式化/压缩
- JSON 转 C# Model
- UUID/GUID 批量生成
- 文本差异对比
- Base64 图片预览
- 剪贴板固定序列输入

## 项目结构

- `ToolBox/`：MAUI Blazor 应用主体
- `ToolBox/Components/`：Razor 组件（页面、布局）
- `CommonHelp/`：共享工具类库
- `ToolBox.slnx`：解决方案入口

## 开发环境

- .NET SDK 10（与项目 `net10.0-*` 目标框架一致）
- Windows 本地开发建议先使用 Windows 目标框架进行构建验证

## 构建

Windows 快速构建：

```bash
dotnet build ToolBox/ToolBox.csproj -c Debug -f net10.0-windows10.0.19041.0
```

构建整个解决方案：

```bash
dotnet build ToolBox.slnx -c Debug
```

## 开发约定

- 命名空间与目录结构保持一致（根命名空间：`ToolBox`）
- Razor 组件命名空间与 `ToolBox/Components/**` 目录一致
- Blazor 组件与对应 ViewModel 必须同目录
- ViewModel 文件名必须以 `VM.cs` 结尾
- 优先复用 `CommonHelp`，避免在业务目录新增零散工具函数

## 技术栈

- .NET MAUI + Blazor Hybrid
- MudBlazor
- Blazing.Mvvm
- Roslyn Scripting（C# Runner）
- DiffPlex / BlazorTextDiff（文本对比）
