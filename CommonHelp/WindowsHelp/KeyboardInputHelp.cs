using System.Runtime.InteropServices;

namespace CommonHelp.WindowsHelp;

public static class KeyboardInputHelp
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const uint KeyeventfUnicode = 0x0004;
    private const ushort VkReturn = 0x0D;
    private const ushort VkTab = 0x09;

    public static async Task TypeTextAsync(string text, int startDelayMs = 3000, int charIntervalMs = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Keyboard simulation is only supported on Windows.");
        }

        if (startDelayMs > 0)
        {
            await Task.Delay(startDelayMs, cancellationToken);
        }

        var normalized = text.Replace("\r\n", "\n");
        foreach (var ch in normalized)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                SendVirtualKey(VkReturn);
            }
            else if (ch == '\t')
            {
                SendVirtualKey(VkTab);
            }
            else
            {
                SendUnicodeChar(ch);
            }

            if (charIntervalMs > 0)
            {
                await Task.Delay(charIntervalMs, cancellationToken);
            }
        }
    }

    public static async Task TypeBatchAsync(
        IReadOnlyList<string> items,
        int startDelayMs = 3000,
        int charIntervalMs = 30,
        int itemIntervalMs = 200,
        bool pressEnterAfterEach = true,
        CancellationToken cancellationToken = default)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Keyboard simulation is only supported on Windows.");
        }

        var delay = Math.Max(0, startDelayMs);
        var charDelay = Math.Max(0, charIntervalMs);
        var interval = Math.Max(0, itemIntervalMs);

        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i] ?? string.Empty;
            await TypeTextAsync(item, i == 0 ? delay : 0, charDelay, cancellationToken);

            if (pressEnterAfterEach)
            {
                SendVirtualKey(VkReturn);
            }

            if (interval > 0 && i < items.Count - 1)
            {
                await Task.Delay(interval, cancellationToken);
            }
        }
    }

    private static void SendUnicodeChar(char ch)
    {
        var inputs = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    KeyboardInput = new KeybdInput
                    {
                        WScan = ch,
                        DwFlags = KeyeventfUnicode
                    }
                }
            },
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    KeyboardInput = new KeybdInput
                    {
                        WScan = ch,
                        DwFlags = KeyeventfUnicode | KeyeventfKeyup
                    }
                }
            }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput failed for char '{ch}'. Win32Error={error}.");
        }
    }

    private static void SendVirtualKey(ushort key)
    {
        var inputs = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    KeyboardInput = new KeybdInput
                    {
                        WVk = key
                    }
                }
            },
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    KeyboardInput = new KeybdInput
                    {
                        WVk = key,
                        DwFlags = KeyeventfKeyup
                    }
                }
            }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput failed for virtual key '{key}'. Win32Error={error}.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint cInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput MouseInput;

        [FieldOffset(0)]
        public KeybdInput KeyboardInput;

        [FieldOffset(0)]
        public HardwareInput HardwareInput;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeybdInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint UMsg;
        public ushort WParamL;
        public ushort WParamH;
    }
}
