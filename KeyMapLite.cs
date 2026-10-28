// KeyMapLite.cs
// 单文件临时改键工具：打开即生效，关掉即还原。
// 编译：csc /target:winexe /platform:anycpu /optimize+ /codepage:65001 /win32icon:app.ico KeyMapLite.cs
// 设计约束：C# 5 语法（Framework 4.0 编译器），零第三方依赖，零配置文件编辑。

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KeyMapLite
{
    // =====================================================================
    // 入口
    // =====================================================================
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Length > 0 &&
                string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                return SelfTest.Run();
            }

            try { Win32.SetProcessDPIAware(); }
            catch { }

            bool createdNew = false;
            using (Mutex mutex = new Mutex(true, "Local\\KeyMapLite.SingleInstance.v1", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("KeyMap Lite 已经在运行了。\n请到任务栏找到它，或先关掉再开。",
                        "KeyMap Lite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 兜住 UI 线程里没预料到的异常，别让整个程序消失、把钩子留在系统里
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate { };

                Application.Run(new MainForm());
            }
            return 0;
        }
    }

    // =====================================================================
    // Win32
    // =====================================================================
    internal static class Win32
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;

        public const uint LLKHF_EXTENDED = 0x01;

        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;

        public const uint XBUTTON1 = 0x0001;
        public const uint XBUTTON2 = 0x0002;

        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;

        public const uint MAPVK_VK_TO_VSC = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
    }

    // =====================================================================
    // 键位编码
    //   id == 0                  : 屏蔽（无输出）
    //   0x0001..0x01FF           : 键盘 vk | (扩展键 ? 0x100 : 0)
    //   0x1001..0x1007           : 鼠标键 / 滚轮
    // 修饰键统一规范化为 0xA0..0xA5（左右可分），不带扩展位。
    // =====================================================================
    internal static class KeyCode
    {
        public const int BLOCK = 0;

        public const int MOUSE_L = 0x1001;
        public const int MOUSE_R = 0x1002;
        public const int MOUSE_M = 0x1003;
        public const int MOUSE_X1 = 0x1004;
        public const int MOUSE_X2 = 0x1005;
        public const int WHEEL_UP = 0x1006;
        public const int WHEEL_DOWN = 0x1007;

        public const int EXT_FLAG = 0x100;
        public const int MOUSE_BASE = 0x1000;

        public static bool IsMouse(int id)
        {
            return id >= MOUSE_BASE && id < MOUSE_BASE + 0x100;
        }

        public static bool IsWheel(int id)
        {
            return id == WHEEL_UP || id == WHEEL_DOWN;
        }

        /// <summary>把低级钩子给的原生键信息规范化成内部 id。</summary>
        public static int Normalize(uint vk, uint scan, uint flags)
        {
            bool ext = (flags & Win32.LLKHF_EXTENDED) != 0;
            switch (vk)
            {
                case 0x10: return scan == 0x36 ? 0xA1 : 0xA0;   // VK_SHIFT
                case 0x11: return ext ? 0xA3 : 0xA2;            // VK_CONTROL
                case 0x12: return ext ? 0xA5 : 0xA4;            // VK_MENU
                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                    return (int)vk;
            }
            return (int)vk | (ext ? EXT_FLAG : 0);
        }

        private static Dictionary<int, string> _names;

        public static string Name(int id)
        {
            if (id == BLOCK) return "屏蔽（无输出）";
            if (IsMouse(id))
            {
                switch (id)
                {
                    case MOUSE_L: return "鼠标左键";
                    case MOUSE_R: return "鼠标右键";
                    case MOUSE_M: return "鼠标中键";
                    case MOUSE_X1: return "鼠标侧键 1";
                    case MOUSE_X2: return "鼠标侧键 2";
                    case WHEEL_UP: return "滚轮向上";
                    case WHEEL_DOWN: return "滚轮向下";
                    default: return "鼠标 ?";
                }
            }

            string name;
            if (Names.TryGetValue(id, out name)) return name;

            // 兜底：问系统要本地化键名
            int vk = id & 0xFF;
            bool ext = (id & EXT_FLAG) != 0;
            uint scan = Win32.MapVirtualKey((uint)vk, Win32.MAPVK_VK_TO_VSC);
            if (scan != 0)
            {
                int lparam = (int)(scan << 16);
                if (ext) lparam |= 1 << 24;
                StringBuilder sb = new StringBuilder(64);
                if (Win32.GetKeyNameText(lparam, sb, sb.Capacity) > 0 && sb.Length > 0)
                    return sb.ToString();
            }
            if (Names.TryGetValue(vk, out name)) return name;
            return "VK_" + vk.ToString("X2");
        }

        /// <summary>短名，用于紧凑显示。</summary>
        public static Dictionary<int, string> Names
        {
            get
            {
                if (_names == null) _names = BuildNames();
                return _names;
            }
        }

        private static Dictionary<int, string> BuildNames()
        {
            Dictionary<int, string> d = new Dictionary<int, string>();

            d[0x08] = "Backspace";
            d[0x09] = "Tab";
            d[0x0D] = "Enter";
            d[0x10D] = "小键盘 Enter";
            d[0x13] = "Pause";
            d[0x14] = "CapsLock";
            d[0x1B] = "Esc";
            d[0x20] = "空格";
            d[0x21] = "PgUp";
            d[0x121] = "PgUp";
            d[0x22] = "PgDn";
            d[0x122] = "PgDn";
            d[0x23] = "End";
            d[0x123] = "End";
            d[0x24] = "Home";
            d[0x124] = "Home";
            d[0x25] = "←";
            d[0x125] = "←";
            d[0x26] = "↑";
            d[0x126] = "↑";
            d[0x27] = "→";
            d[0x127] = "→";
            d[0x28] = "↓";
            d[0x128] = "↓";
            d[0x2C] = "PrintScreen";
            d[0x12C] = "PrintScreen";
            d[0x2D] = "Insert";
            d[0x12D] = "Insert";
            d[0x2E] = "Delete";
            d[0x12E] = "Delete";
            d[0x5B] = "左 Win";
            d[0x5C] = "右 Win";
            d[0x5D] = "菜单键";
            d[0x90] = "NumLock";
            d[0x91] = "ScrollLock";

            d[0xA0] = "左 Shift";
            d[0xA1] = "右 Shift";
            d[0xA2] = "左 Ctrl";
            d[0xA3] = "右 Ctrl";
            d[0xA4] = "左 Alt";
            d[0xA5] = "右 Alt";

            d[0x60] = "小键盘 0";
            d[0x61] = "小键盘 1";
            d[0x62] = "小键盘 2";
            d[0x63] = "小键盘 3";
            d[0x64] = "小键盘 4";
            d[0x65] = "小键盘 5";
            d[0x66] = "小键盘 6";
            d[0x67] = "小键盘 7";
            d[0x68] = "小键盘 8";
            d[0x69] = "小键盘 9";
            d[0x6A] = "小键盘 *";
            d[0x6B] = "小键盘 +";
            d[0x6D] = "小键盘 -";
            d[0x6E] = "小键盘 .";
            d[0x6F] = "小键盘 /";

            d[0xBA] = ";";
            d[0xBB] = "=";
            d[0xBC] = ",";
            d[0xBD] = "-";
            d[0xBE] = ".";
            d[0xBF] = "/";
            d[0xC0] = "`";
            d[0xDB] = "[";
            d[0xDC] = "\\";
            d[0xDD] = "]";
            d[0xDE] = "'";
            d[0xE2] = "OEM102";

            d[0xA6] = "浏览器后退";
            d[0xA7] = "浏览器前进";
            d[0xA8] = "浏览器刷新";
            d[0xA9] = "浏览器停止";
            d[0xAA] = "浏览器搜索";
            d[0xAB] = "浏览器收藏";
            d[0xAC] = "浏览器主页";
            d[0xAD] = "静音";
            d[0xAE] = "音量 -";
            d[0xAF] = "音量 +";
            d[0xB0] = "下一曲";
            d[0xB1] = "上一曲";
            d[0xB2] = "停止播放";
            d[0xB3] = "播放/暂停";

            for (int i = 0; i < 26; i++)
                d[0x41 + i] = ((char)('A' + i)).ToString();

            for (int i = 0; i < 10; i++)
                d[0x30 + i] = ((char)('0' + i)).ToString();

            for (int i = 0; i < 24; i++)
                d[0x70 + i] = "F" + (i + 1).ToString();

            return d;
        }
    }

    // =====================================================================
    // 规则与持久化
    // =====================================================================
    internal sealed class Rule
    {
        public int Src;
        public int Dst;          // 0 = 屏蔽
        public bool Enabled;

        public Rule() { }

        public Rule(int src, int dst, bool enabled)
        {
            Src = src;
            Dst = dst;
            Enabled = enabled;
        }

        public Rule Clone()
        {
            return new Rule(Src, Dst, Enabled);
        }
    }

    internal sealed class UndoEntry
    {
        public int Src;
        public bool Existed;
        public int OldDst;
    }

    internal static class StateStore
    {
        private static string _resolved;

        /// <summary>
        /// 状态文件优先放在 exe 同目录（便携）。若该目录不可写
        /// （例如被放在 Program Files），自动回退到用户漫游目录。
        /// </summary>
        public static string DefaultPath
        {
            get
            {
                if (_resolved == null) _resolved = Resolve();
                return _resolved;
            }
        }

        private static string Resolve()
        {
            string dir = null;
            try
            {
                string exe = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exe)) dir = Path.GetDirectoryName(exe);
            }
            catch { }

            if (!string.IsNullOrEmpty(dir) && CanWrite(dir))
                return Path.Combine(dir, "state.txt");

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KeyMapLite", "state.txt");
        }

        private static bool CanWrite(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".kml_probe");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Load(string path, out bool paused, out bool passThrough, List<Rule> rules)
        {
            paused = false;
            passThrough = false;
            rules.Clear();
            if (!File.Exists(path)) return false;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#")) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();

                    if (key == "paused")
                    {
                        paused = val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (key == "mode")
                    {
                        passThrough = string.Equals(val, "both", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (key == "rule")
                    {
                        string[] parts = val.Split(',');
                        if (parts.Length < 3) continue;

                        int src, dst;
                        if (!int.TryParse(parts[0], out src)) continue;
                        if (!int.TryParse(parts[1], out dst)) continue;
                        if (src <= 0) continue;

                        bool enabled = parts[2].Trim() == "1";
                        rules.Add(new Rule(src, dst, enabled));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Save(string path, bool paused, bool passThrough, List<Rule> rules)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# KeyMapLite 状态文件，程序自动读写，无需手动编辑");
                sb.AppendLine("version=1");
                sb.AppendLine("paused=" + (paused ? "1" : "0"));
                sb.AppendLine("mode=" + (passThrough ? "both" : "replace"));
                foreach (Rule r in rules)
                {
                    sb.Append("rule=");
                    sb.Append(r.Src.ToString());
                    sb.Append(',');
                    sb.Append(r.Dst.ToString());
                    sb.Append(',');
                    sb.Append(r.Enabled ? "1" : "0");
                    sb.AppendLine();
                }

                // 单次直接写入：桌面常被 OneDrive/杀软盯着，少两次文件操作就少两次被拦的机会
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
                // 存不下来不影响本次使用
            }
        }
    }

    // =====================================================================
    // 映射引擎：低级钩子 + SendInput
    // =====================================================================
    internal sealed class Engine : IDisposable
    {
        // 自身合成事件的指纹，钩子里据此放行，避免自映射死循环
        private static readonly IntPtr MAGIC = new IntPtr(0x4B4D4C31);

        private readonly Dictionary<int, int> _map = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _activeSrc = new Dictionary<int, int>();

        private Win32.HookProc _kbProc;
        private Win32.HookProc _msProc;
        private IntPtr _kbHook = IntPtr.Zero;
        private IntPtr _msHook = IntPtr.Zero;
        private bool _disposed;

        // 诊断通道：仅当设置了环境变量 KEYMAPLITE_TRACE 时才写盘，正式使用零开销
        private static bool _traceInit;
        private static string _tracePath;

        private static bool TraceOn
        {
            get
            {
                if (!_traceInit)
                {
                    _traceInit = true;
                    try { _tracePath = Environment.GetEnvironmentVariable("KEYMAPLITE_TRACE"); }
                    catch { _tracePath = null; }
                }
                return !string.IsNullOrEmpty(_tracePath);
            }
        }

        private static void Trace(string msg)
        {
            if (!TraceOn) return;
            try { File.AppendAllText(_tracePath, msg + "\r\n"); }
            catch { }
        }

        public bool Paused { get; set; }

        /// <summary>true 表示原键不被吞掉，原键与映射键同时输出。</summary>
        public bool PassThrough { get; set; }

        public Engine()
        {
        }

        public bool Hooked
        {
            get { return _kbHook != IntPtr.Zero && _msHook != IntPtr.Zero; }
        }

        public void SetMap(Dictionary<int, int> map)
        {
            _map.Clear();
            if (map != null)
            {
                foreach (KeyValuePair<int, int> kv in map)
                    _map[kv.Key] = kv.Value;
            }
        }

        public void Start()
        {
            if (_disposed) return;
            if (_kbHook != IntPtr.Zero) return;

            _kbProc = KbHookProc;
            _msProc = MsHookProc;

            _kbHook = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _kbProc, IntPtr.Zero, 0);
            _msHook = Win32.SetWindowsHookEx(Win32.WH_MOUSE_LL, _msProc, IntPtr.Zero, 0);
        }

        /// <summary>
        /// 合成输出。kind: 0=按下 1=抬起 2=点一下（滚轮/瞬时）
        /// 返回是否真的注入成功——失败时必须放行原键，否则会变成"按下没反应"的死键。
        /// </summary>
        private bool Enqueue(int key, int kind)
        {
            try
            {
                if (kind == 0) return SendPress(key);
                if (kind == 1) return SendRelease(key);
                return SendTap(key);
            }
            catch { return false; }
        }

        public void Stop()
        {
            if (_kbHook != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(_kbHook);
                _kbHook = IntPtr.Zero;
            }
            if (_msHook != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(_msHook);
                _msHook = IntPtr.Zero;
            }
            _kbProc = null;
            _msProc = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }

        // ---------------- 键盘钩子 ----------------

        [HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCritical]
        private IntPtr KbHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || lParam == IntPtr.Zero)
                return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);

            // 暂停时立刻放行，不做任何解析
            if (Paused) return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);

            try
            {
                int msg = wParam.ToInt32();
                bool down = msg == Win32.WM_KEYDOWN || msg == Win32.WM_SYSKEYDOWN;
                bool up = msg == Win32.WM_KEYUP || msg == Win32.WM_SYSKEYUP;
                if (!down && !up)
                    return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);

                // 直接读内存偏移，避免 PtrToStructure 每帧反射解析类型
                if (Marshal.ReadIntPtr(lParam, 16) == MAGIC)
                    return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);

                int id = KeyCode.Normalize(
                    (uint)Marshal.ReadInt32(lParam, 0),    // vkCode
                    (uint)Marshal.ReadInt32(lParam, 4),    // scanCode
                    (uint)Marshal.ReadInt32(lParam, 8));   // flags
                if (TraceOn) Trace("kb in " + id.ToString("X3") + (down ? " down" : " up"));

                if (down)
                {
                    int dst;
                    if (!_map.TryGetValue(id, out dst))
                    {
                        if (TraceOn) Trace("kb " + id.ToString("X3") + " down: no map");
                        return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                    }

                    if (TraceOn) Trace("kb " + id.ToString("X3") + " down: -> " + dst.ToString("X3"));
                    _activeSrc[id] = dst;

                    // 屏蔽规则永远只吞不输出，不受输出模式影响
                    if (dst == KeyCode.BLOCK) return (IntPtr)1;

                    if (PassThrough)
                    {
                        // 先让原键照常输出，再投递映射键，顺序才符合直觉
                        IntPtr r = Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                        Enqueue(dst, 0);
                        return r;
                    }

                    // 注入失败（常见于目标程序权限更高，SendInput 被 UIPI 拦下）
                    // 就放行原键，否则这个键会变成"按下去毫无反应"的死键
                    if (!Enqueue(dst, 0))
                    {
                        _activeSrc.Remove(id);
                        return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                    }
                    return (IntPtr)1;
                }
                else
                {
                    int dst;
                    if (_activeSrc.TryGetValue(id, out dst))
                    {
                        if (TraceOn) Trace("kb " + id.ToString("X3") + " up: release " + dst.ToString("X3"));
                        _activeSrc.Remove(id);

                        if (dst == KeyCode.BLOCK) return (IntPtr)1;

                        if (PassThrough)
                        {
                            IntPtr r = Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                            Enqueue(dst, 1);
                            return r;
                        }

                        if (!Enqueue(dst, 1))
                            return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                        return (IntPtr)1;
                    }

                    // 兜底：没有按下记录说明这一轮没接管成功，
                    // 只尝试释放目标键，原键的抬起必须放行，否则原键会卡在按下状态
                    if (_map.TryGetValue(id, out dst))
                    {
                        if (TraceOn) Trace("kb " + id.ToString("X3") + " up: fallback release " + dst.ToString("X3"));
                        if (dst != KeyCode.BLOCK) Enqueue(dst, 1);
                    }

                    return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
                }
            }
            catch
            {
                return Win32.CallNextHookEx(_kbHook, nCode, wParam, lParam);
            }
        }

        // ---------------- 鼠标钩子 ----------------

        [HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCritical]
        private IntPtr MsHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || lParam == IntPtr.Zero)
                return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

            int msg = wParam.ToInt32();

            // 鼠标移动是频率最高的事件（每秒可达数百次），直接放行，不解析结构体
            if (msg == Win32.WM_MOUSEMOVE)
                return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

            // 暂停时立刻放行
            if (Paused) return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

            try
            {
                // MSLLHOOKSTRUCT 里 dwExtraInfo 在 x64 对齐到偏移 24，x86 是 20
                if (Marshal.ReadIntPtr(lParam, IntPtr.Size == 8 ? 24 : 20) == MAGIC)
                    return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

                uint mouseData = (uint)Marshal.ReadInt32(lParam, 8);

                int id;
                bool down = false;
                bool up = false;
                bool wheel = false;

                switch (msg)
                {
                    case Win32.WM_LBUTTONDOWN: id = KeyCode.MOUSE_L; down = true; break;
                    case Win32.WM_LBUTTONUP: id = KeyCode.MOUSE_L; up = true; break;
                    case Win32.WM_RBUTTONDOWN: id = KeyCode.MOUSE_R; down = true; break;
                    case Win32.WM_RBUTTONUP: id = KeyCode.MOUSE_R; up = true; break;
                    case Win32.WM_MBUTTONDOWN: id = KeyCode.MOUSE_M; down = true; break;
                    case Win32.WM_MBUTTONUP: id = KeyCode.MOUSE_M; up = true; break;
                    case Win32.WM_XBUTTONDOWN:
                        id = ((mouseData >> 16) & 0xFFFF) == Win32.XBUTTON2 ? KeyCode.MOUSE_X2 : KeyCode.MOUSE_X1;
                        down = true;
                        break;
                    case Win32.WM_XBUTTONUP:
                        id = ((mouseData >> 16) & 0xFFFF) == Win32.XBUTTON2 ? KeyCode.MOUSE_X2 : KeyCode.MOUSE_X1;
                        up = true;
                        break;
                    case Win32.WM_MOUSEWHEEL:
                        id = unchecked((int)mouseData) > 0 ? KeyCode.WHEEL_UP : KeyCode.WHEEL_DOWN;
                        wheel = true;
                        break;
                    default:
                        return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                }

                if (wheel)
                {
                    int dst;
                    if (!_map.TryGetValue(id, out dst))
                        return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

                    if (dst == KeyCode.BLOCK) return (IntPtr)1;

                    if (PassThrough)
                    {
                        IntPtr r = Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                        Enqueue(dst, 2);
                        return r;
                    }

                    if (!Enqueue(dst, 2))
                        return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                    return (IntPtr)1;
                }

                if (down)
                {
                    int dst;
                    if (!_map.TryGetValue(id, out dst))
                        return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

                    if (dst == KeyCode.BLOCK)
                    {
                        _activeSrc[id] = dst;
                        return (IntPtr)1;
                    }

                    // 注入失败就放行原键，不记录按下状态，避免抬起时被误吞
                    if (!Enqueue(dst, 0))
                        return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);

                    _activeSrc[id] = dst;
                    return (IntPtr)1;
                }
                else if (up)
                {
                    int dst;
                    if (_activeSrc.TryGetValue(id, out dst))
                    {
                        _activeSrc.Remove(id);

                        if (dst == KeyCode.BLOCK) return (IntPtr)1;

                        if (PassThrough)
                        {
                            IntPtr r = Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                            Enqueue(dst, 1);
                            return r;
                        }

                        if (!Enqueue(dst, 1))
                            return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                        return (IntPtr)1;
                    }

                    // 没有按下记录，说明这轮没接管成功：只尝试释放目标键，原键抬起必须放行
                    if (_map.TryGetValue(id, out dst))
                    {
                        if (dst != KeyCode.BLOCK) Enqueue(dst, 1);
                    }

                    return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
                }

                return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
            }
            catch
            {
                return Win32.CallNextHookEx(_msHook, nCode, wParam, lParam);
            }
        }

        // ---------------- 合成输出 ----------------

        private static bool SendPress(int dst)
        {
            if (KeyCode.IsMouse(dst)) return SendMouseButton(dst, false);
            return SendKey(dst, false);
        }

        private static bool SendRelease(int dst)
        {
            if (KeyCode.IsMouse(dst)) return SendMouseButton(dst, true);
            return SendKey(dst, true);
        }

        private static bool SendTap(int dst)
        {
            if (dst == KeyCode.WHEEL_UP) return SendWheel(120);
            if (dst == KeyCode.WHEEL_DOWN) return SendWheel(-120);
            if (!SendPress(dst)) return false;
            return SendRelease(dst);
        }

        private static bool SendKey(int id, bool up)
        {
            ushort vk;
            ushort scan;
            bool ext = (id & KeyCode.EXT_FLAG) != 0;
            int code = id & 0xFF;

            switch (code)
            {
                case 0xA0: vk = 0x10; scan = 0x2A; ext = false; break;
                case 0xA1: vk = 0x10; scan = 0x36; ext = false; break;
                case 0xA2: vk = 0x11; scan = 0x1D; ext = false; break;
                case 0xA3: vk = 0x11; scan = 0x1D; ext = true; break;
                case 0xA4: vk = 0x12; scan = 0x38; ext = false; break;
                case 0xA5: vk = 0x12; scan = 0x38; ext = true; break;
                default:
                    vk = (ushort)code;
                    scan = (ushort)Win32.MapVirtualKey((uint)code, Win32.MAPVK_VK_TO_VSC);
                    break;
            }

            Win32.INPUT[] inputs = new Win32.INPUT[1];
            inputs[0].type = Win32.INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = vk;
            inputs[0].U.ki.wScan = scan;
            inputs[0].U.ki.dwFlags = (ext ? Win32.KEYEVENTF_EXTENDEDKEY : 0u)
                                     | (up ? Win32.KEYEVENTF_KEYUP : 0u);
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = MAGIC;

            try { return Win32.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32.INPUT))) == 1; }
            catch { return false; }
        }

        private static bool SendMouseButton(int id, bool up)
        {
            uint flag;
            uint data = 0;

            switch (id)
            {
                case KeyCode.MOUSE_L: flag = up ? Win32.MOUSEEVENTF_LEFTUP : Win32.MOUSEEVENTF_LEFTDOWN; break;
                case KeyCode.MOUSE_R: flag = up ? Win32.MOUSEEVENTF_RIGHTUP : Win32.MOUSEEVENTF_RIGHTDOWN; break;
                case KeyCode.MOUSE_M: flag = up ? Win32.MOUSEEVENTF_MIDDLEUP : Win32.MOUSEEVENTF_MIDDLEDOWN; break;
                case KeyCode.MOUSE_X1: flag = up ? Win32.MOUSEEVENTF_XUP : Win32.MOUSEEVENTF_XDOWN; data = Win32.XBUTTON1; break;
                case KeyCode.MOUSE_X2: flag = up ? Win32.MOUSEEVENTF_XUP : Win32.MOUSEEVENTF_XDOWN; data = Win32.XBUTTON2; break;
                default: return false;
            }

            Win32.INPUT[] inputs = new Win32.INPUT[1];
            inputs[0].type = Win32.INPUT_MOUSE;
            inputs[0].U.mi.dx = 0;
            inputs[0].U.mi.dy = 0;
            inputs[0].U.mi.mouseData = data;
            inputs[0].U.mi.dwFlags = flag;
            inputs[0].U.mi.time = 0;
            inputs[0].U.mi.dwExtraInfo = MAGIC;

            try { return Win32.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32.INPUT))) == 1; }
            catch { return false; }
        }

        private static bool SendWheel(int delta)
        {
            Win32.INPUT[] inputs = new Win32.INPUT[1];
            inputs[0].type = Win32.INPUT_MOUSE;
            inputs[0].U.mi.dx = 0;
            inputs[0].U.mi.dy = 0;
            inputs[0].U.mi.mouseData = unchecked((uint)delta);
            inputs[0].U.mi.dwFlags = Win32.MOUSEEVENTF_WHEEL;
            inputs[0].U.mi.time = 0;
            inputs[0].U.mi.dwExtraInfo = MAGIC;

            try { return Win32.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32.INPUT))) == 1; }
            catch { return false; }
        }
    }

    // =====================================================================
    // 主窗口
    // =====================================================================
    internal sealed class MainForm : Form
    {
        private static readonly Color C_BG = Color.FromArgb(0xF3, 0xF4, 0xF7);
        private static readonly Color C_CARD = Color.White;
        private static readonly Color C_LINE = Color.FromArgb(0xE2, 0xE5, 0xEB);
        private static readonly Color C_BORDER = Color.FromArgb(0xD3, 0xD7, 0xE0);
        private static readonly Color C_TEXT = Color.FromArgb(0x11, 0x18, 0x27);
        private static readonly Color C_MUTED = Color.FromArgb(0x6B, 0x72, 0x80);
        private static readonly Color C_ACCENT = Color.FromArgb(0x4F, 0x46, 0xE5);
        private static readonly Color C_ACCENT_HOVER = Color.FromArgb(0x43, 0x3C, 0xC8);
        private static readonly Color C_DARK = Color.FromArgb(0x11, 0x18, 0x27);
        private static readonly Color C_OK = Color.FromArgb(0x05, 0x96, 0x69);
        private static readonly Color C_WARN = Color.FromArgb(0xD9, 0x77, 0x06);

        private readonly List<Rule> _rules = new List<Rule>();
        private readonly Stack<UndoEntry> _undo = new Stack<UndoEntry>();
        private readonly Engine _engine;

        private ListView _list;
        private Button _btnSrc;
        private Button _btnPause;
        private Button _btnDelete;
        private Button _btnClear;
        private Label _lblStatus;
        private Label _lblHint;
        private Label _lblEmpty;
        private ImageList _rowHeight;
        private RadioButton _rbReplace;
        private RadioButton _rbBoth;
        private bool _loadingMode;
        private NotifyIcon _tray;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _miPause;

        private bool _suppress;
        private bool _trayHintShown;
        private float _scale = 1f;
        private System.Windows.Forms.Timer _saveTimer;
        private bool _savePending;
        private string _lastTrayText;

        public MainForm()
        {
            _engine = new Engine();

            BuildUi();
            BuildTray();
            LoadState();
            RefreshList();
            ApplyRules();
            _engine.Start();
            UpdateButtonState();
            SyncPauseButton();
            UpdateStatus();

            if (!_engine.Hooked)
            {
                _lblHint.Text = "钩子安装失败：可能有安全软件拦截，映射无法生效。";
                _lblHint.ForeColor = C_WARN;
            }
        }

        // ---------------- 界面 ----------------

        private void BuildUi()
        {
            SuspendLayout();

            Text = "KeyMap Lite — 临时改键";
            ClientSize = new Size(760, 624);
            MinimumSize = new Size(680, 544);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_BG;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            KeyPreview = true;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.BackColor = C_BG;
            root.Padding = new Padding(0);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildBody(), 0, 1);

            Controls.Add(root);
            ResumeLayout(false);

            Resize += delegate { AdjustColumns(); };
        }

        // ---------------- 托盘 ----------------

        private void BuildTray()
        {
            Icon appIcon;
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { appIcon = null; }
            if (appIcon == null) appIcon = SystemIcons.Application;

            Font menuFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Font = menuFont;
            _trayMenu.ShowImageMargin = false;

            ToolStripMenuItem miShow = new ToolStripMenuItem("显示主界面");
            miShow.Font = new Font(menuFont, FontStyle.Bold);
            miShow.Click += delegate { RestoreFromTray(); };

            _miPause = new ToolStripMenuItem("暂停映射");
            _miPause.Click += delegate { TogglePause(); };

            ToolStripMenuItem miExit = new ToolStripMenuItem("退出（键盘立即恢复）");
            miExit.Click += delegate { ExitApp(); };

            _trayMenu.Items.Add(miShow);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_miPause);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(miExit);

            _tray = new NotifyIcon();
            _tray.Icon = appIcon;
            _tray.Text = "KeyMap Lite";
            _tray.ContextMenuStrip = _trayMenu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { RestoreFromTray(); };
            _tray.BalloonTipClicked += delegate { RestoreFromTray(); };
        }

        private void HideToTray()
        {
            Hide();
            if (_trayHintShown) return;
            _trayHintShown = true;
            try
            {
                _tray.ShowBalloonTip(2500, "KeyMap Lite 仍在后台运行",
                    "改键依然生效。双击托盘图标可以把窗口调回来。", ToolTipIcon.Info);
            }
            catch { }
        }

        private void RestoreFromTray()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Show();
            Activate();
            BringToFront();
        }

        private void ExitApp()
        {
            if (_tray != null) _tray.Visible = false;
            Close();
        }

        private void UpdateTrayText()
        {
            if (_tray == null) return;

            string s;
            if (_engine.Paused)
            {
                s = "KeyMap Lite · 已暂停";
            }
            else
            {
                int n = 0;
                foreach (Rule r in _rules) if (r.Enabled) n++;
                s = n == 0 ? "KeyMap Lite · 无规则" : "KeyMap Lite · 生效中 " + n.ToString() + " 条";
            }
            if (s.Length > 60) s = s.Substring(0, 60);

            // NotifyIcon.Text 每次赋值都要走一轮 Shell_NotifyIcon，文字没变就别碰
            if (s != _lastTrayText)
            {
                _lastTrayText = s;
                try { _tray.Text = s; }
                catch { }
            }

            if (_miPause != null)
            {
                if (_miPause.Checked != _engine.Paused) _miPause.Checked = _engine.Paused;
                string mt = _engine.Paused ? "恢复映射" : "暂停映射";
                if (_miPause.Text != mt) _miPause.Text = mt;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && Visible)
                HideToTray();
        }

        private Control BuildHeader()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = C_DARK;
            p.Margin = new Padding(0);

            Label title = new Label();
            title.Text = "KeyMap Lite";
            title.AutoSize = true;
            title.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.White;
            title.Location = new Point(20, 12);
            p.Controls.Add(title);

            Label sub = new Label();
            sub.Text = "临时改键 · 打开即生效 · 关掉即还原";
            sub.AutoSize = true;
            sub.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            sub.ForeColor = Color.FromArgb(0x9C, 0xA3, 0xAF);
            sub.Location = new Point(22, 42);
            p.Controls.Add(sub);

            Label badge = new Label();
            badge.Text = "单文件 · 免安装";
            badge.AutoSize = true;
            badge.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            badge.ForeColor = Color.FromArgb(0x6B, 0x72, 0x80);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            badge.Location = new Point(p.Width - 120, 24);
            p.Controls.Add(badge);
            p.Resize += delegate { badge.Location = new Point(p.Width - badge.Width - 22, 26); };

            return p;
        }

        private Control BuildBody()
        {
            TableLayoutPanel body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.ColumnCount = 1;
            body.RowCount = 4;
            body.BackColor = C_BG;
            body.Padding = new Padding(16, 14, 16, 6);
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            body.Controls.Add(BuildCaptureCard(), 0, 0);
            body.Controls.Add(BuildModeRow(), 0, 1);
            body.Controls.Add(BuildList(), 0, 2);
            body.Controls.Add(BuildActionRow(), 0, 3);

            return body;
        }

        private Control BuildCaptureCard()
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = C_CARD;
            card.Margin = new Padding(0, 0, 0, 12);
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = KeyboardBoard.RoundRect(
                    new RectangleF(0.5f, 0.5f, card.Width - 1.5f, card.Height - 1.5f), 10f))
                using (Pen pen = new Pen(C_LINE))
                    e.Graphics.DrawPath(pen, path);
            };

            _btnSrc = MakeMainButton();
            _btnSrc.Click += delegate { OpenKeyPicker(); };

            _lblHint = new Label();
            _lblHint.AutoSize = false;
            _lblHint.TextAlign = ContentAlignment.MiddleCenter;
            _lblHint.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _lblHint.ForeColor = Color.FromArgb(0x9C, 0xA3, 0xAF);
            _lblHint.Text = "点一个键，再点一个键，就改好一条 · 键盘 104 键 + 鼠标侧键 / 滚轮";

            card.Controls.Add(_btnSrc);
            card.Controls.Add(_lblHint);

            card.Resize += delegate
            {
                _btnSrc.SetBounds(18, 18, Math.Max(80, card.Width - 36), 60);
                _lblHint.SetBounds(18, 86, Math.Max(80, card.Width - 36), 20);
                RoundControl(_btnSrc, 10);
            };

            return card;
        }

        private Control BuildModeRow()
        {
            Panel row = new Panel();
            row.Dock = DockStyle.Fill;
            row.Margin = new Padding(0, 0, 0, 10);
            row.BackColor = C_BG;

            _rbReplace = new RadioButton();
            _rbReplace.Text = "替换原键";
            _rbReplace.AutoSize = true;
            _rbReplace.Checked = true;
            _rbReplace.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _rbReplace.ForeColor = C_TEXT;
            _rbReplace.Location = new Point(2, 11);
            _rbReplace.CheckedChanged += delegate { ApplyMode(); };

            _rbBoth = new RadioButton();
            _rbBoth.Text = "同时输出原键和映射键";
            _rbBoth.AutoSize = true;
            _rbBoth.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _rbBoth.ForeColor = C_TEXT;
            _rbBoth.Location = new Point(132, 11);
            _rbBoth.CheckedChanged += delegate { ApplyMode(); };

            Label tip = new Label();
            tip.Text = "屏蔽规则不受影响，始终只吞不输出";
            tip.AutoSize = true;
            tip.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            tip.ForeColor = Color.FromArgb(0x9C, 0xA3, 0xAF);
            tip.Location = new Point(340, 14);

            row.Controls.Add(_rbReplace);
            row.Controls.Add(_rbBoth);
            row.Controls.Add(tip);
            row.Resize += delegate
            {
                tip.Location = new Point(Math.Max(340, row.Width - tip.Width - 4), 14);
            };

            return row;
        }

        private Button MakeMainButton()
        {
            Button b = new Button();
            b.Text = "①  打开键盘图，点一下改一个键";
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = C_ACCENT;
            b.ForeColor = Color.White;
            b.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.MouseEnter += delegate { b.BackColor = C_ACCENT_HOVER; };
            b.MouseLeave += delegate { b.BackColor = C_ACCENT; };
            return b;
        }

        private static void RoundControl(Control c, int r)
        {
            if (c == null || c.Width <= 0 || c.Height <= 0) return;
            using (GraphicsPath path = KeyboardBoard.RoundRect(
                new RectangleF(0, 0, c.Width, c.Height), r))
            {
                Region old = c.Region;
                c.Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        private Control BuildList()
        {
            Panel wrap = new Panel();
            wrap.Dock = DockStyle.Fill;
            wrap.BackColor = C_CARD;
            wrap.Margin = new Padding(0, 0, 0, 10);
            wrap.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pen = new Pen(C_LINE))
                    e.Graphics.DrawRectangle(pen, 0, 0, wrap.Width - 1, wrap.Height - 1);
            };

            _rowHeight = new ImageList();
            _rowHeight.ImageSize = new Size(1, 30);
            _rowHeight.ColorDepth = ColorDepth.Depth32Bit;
            _rowHeight.Images.Add(new Bitmap(1, 30));

            _list = new ListView();
            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.CheckBoxes = true;
            _list.FullRowSelect = true;
            _list.HideSelection = false;
            _list.MultiSelect = false;
            _list.BorderStyle = BorderStyle.None;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.SmallImageList = _rowHeight;
            _list.BackColor = C_CARD;
            _list.ForeColor = C_TEXT;
            _list.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            _list.Columns.Add("启用", 60, HorizontalAlignment.Left);
            _list.Columns.Add("按下的键", 200, HorizontalAlignment.Left);
            _list.Columns.Add("变成", 200, HorizontalAlignment.Left);
            _list.Columns.Add("说明", 160, HorizontalAlignment.Left);

            _list.ItemChecked += List_ItemChecked;
            _list.MouseDoubleClick += List_MouseDoubleClick;
            _list.SelectedIndexChanged += delegate { UpdateButtonState(); };
            _list.Resize += delegate { AdjustColumns(); };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("重新选择键位", null, delegate { ReRecordSelected(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("设为屏蔽（不输出）", null, delegate { SetSelectedBlocked(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("删除这条", null, delegate { DeleteSelected(); });
            _list.ContextMenuStrip = menu;

            _lblEmpty = new Label();
            _lblEmpty.Text = "还没有规则\r\n点上面的「①」打开键盘图，点两下就设好了";
            _lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            _lblEmpty.Dock = DockStyle.Fill;
            _lblEmpty.BackColor = C_CARD;
            _lblEmpty.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            _lblEmpty.ForeColor = Color.FromArgb(0x9C, 0xA3, 0xAF);
            _lblEmpty.Margin = new Padding(0);
            _lblEmpty.Visible = false;

            wrap.Controls.Add(_list);
            wrap.Controls.Add(_lblEmpty);
            _lblEmpty.BringToFront();
            return wrap;
        }

        private Control BuildActionRow()
        {
            Panel row = new Panel();
            row.Dock = DockStyle.Fill;
            row.Margin = new Padding(0);
            row.BackColor = C_BG;

            _btnPause = MakeSmallButton("暂停映射");
            _btnPause.Location = new Point(0, 4);
            _btnPause.Click += delegate { TogglePause(); };

            _btnDelete = MakeSmallButton("删除选中");
            _btnDelete.Location = new Point(104, 4);
            _btnDelete.Click += delegate { DeleteSelected(); };

            _btnClear = MakeSmallButton("清空全部");
            _btnClear.Location = new Point(208, 4);
            _btnClear.Click += delegate { ClearAll(); };

            _lblStatus = new Label();
            _lblStatus.AutoSize = true;
            _lblStatus.TextAlign = ContentAlignment.MiddleRight;
            _lblStatus.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _lblStatus.ForeColor = C_MUTED;
            _lblStatus.BackColor = C_BG;

            row.Controls.Add(_btnPause);
            row.Controls.Add(_btnDelete);
            row.Controls.Add(_btnClear);
            row.Controls.Add(_lblStatus);

            row.Resize += delegate { PositionStatus(); };

            return row;
        }

        private void PositionStatus()
        {
            if (_lblStatus == null || _lblStatus.Parent == null) return;
            Control row = _lblStatus.Parent;
            int y = (row.Height - _lblStatus.Height) / 2;
            int x = row.Width - _lblStatus.Width;
            if (x < 0) x = 0;
            _lblStatus.Location = new Point(x, y < 0 ? 0 : y);
        }

        private Button MakeSmallButton(string text)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = false;
            b.Size = new Size(96, 32);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = C_BORDER;
            b.BackColor = C_CARD;
            b.ForeColor = C_TEXT;
            b.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.Margin = new Padding(0, 0, 8, 0);
            b.MouseEnter += delegate { b.BackColor = Color.FromArgb(0xF7, 0xF8, 0xFB); };
            b.MouseLeave += delegate { b.BackColor = C_CARD; };
            return b;
        }

        // ---------------- 布局细节 ----------------

        private void AdjustColumns()
        {
            if (_list == null || _list.Columns.Count < 4) return;
            int unit = (int)Math.Round(60 * _scale);
            int w0 = (int)Math.Round(58 * _scale);
            int w1 = (int)Math.Round(210 * _scale);
            int w2 = (int)Math.Round(210 * _scale);
            int rest = _list.ClientSize.Width - w0 - w1 - w2 - 4;
            if (rest < unit) rest = unit;

            _list.Columns[0].Width = w0;
            _list.Columns[1].Width = w1;
            _list.Columns[2].Width = w2;
            _list.Columns[3].Width = rest;

            if (_rowHeight != null)
                _rowHeight.ImageSize = new Size(1, (int)Math.Round(30 * _scale));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            using (Graphics g = CreateGraphics())
                _scale = g.DpiX / 96f;
            if (_scale <= 0f) _scale = 1f;

            AdjustColumns();
            UpdateButtonState();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_rules.Count == 0)
                SetStatus("还没有规则。点上面的紫色按钮开始。", C_MUTED);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_saveTimer != null)
            {
                _saveTimer.Stop();
                _saveTimer.Dispose();
                _saveTimer = null;
            }
            FlushSave();
            _engine.Dispose();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
            base.OnFormClosing(e);
        }

        // ---------------- 状态 ----------------

        private void LoadState()
        {
            bool paused;
            bool passThrough;
            StateStore.Load(StateStore.DefaultPath, out paused, out passThrough, _rules);
            _engine.Paused = paused;
            _engine.PassThrough = passThrough;

            _loadingMode = true;
            _rbBoth.Checked = passThrough;
            _rbReplace.Checked = !passThrough;
            _loadingMode = false;
        }

        private void SaveState()
        {
            _savePending = true;

            if (_saveTimer == null)
            {
                _saveTimer = new System.Windows.Forms.Timer();
                _saveTimer.Interval = 400;
                _saveTimer.Tick += delegate
                {
                    _saveTimer.Stop();
                    FlushSave();
                };
            }

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void FlushSave()
        {
            if (!_savePending) return;
            _savePending = false;
            StateStore.Save(StateStore.DefaultPath, _engine.Paused, _engine.PassThrough, _rules);
        }

        private void ApplyMode()
        {
            if (_loadingMode || _rbBoth == null) return;
            _engine.PassThrough = _rbBoth.Checked;
            SaveState();
            UpdateStatus();
        }

        private void ApplyRules()
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            foreach (Rule r in _rules)
            {
                if (!r.Enabled) continue;
                if (r.Src <= 0) continue;
                map[r.Src] = r.Dst;
            }
            _engine.SetMap(map);
        }

        private void UpdateStatus()
        {
            int enabled = 0;
            foreach (Rule r in _rules) if (r.Enabled) enabled++;

            UpdateTrayText();

            string head = _engine.Paused ? "已暂停 · 键盘恢复原样" : "生效中";
            Color c = _engine.Paused ? C_WARN : C_OK;

            if (_rules.Count == 0)
            {
                SetStatus("还没有规则。点上面的「①」打开键盘图开始。", C_MUTED);
                return;
            }

            SetStatus(string.Format("{0} · 共 {1} 条，启用 {2} 条", head, _rules.Count, enabled), c);
        }

        private void SetStatus(string text, Color color)
        {
            if (_lblStatus == null) return;
            if (_lblStatus.Text == text && _lblStatus.ForeColor == color) return;
            _lblStatus.Text = text;
            _lblStatus.ForeColor = color;
            PositionStatus();
        }

        // ---------------- 列表 ----------------

        private void RefreshList()
        {
            if (_list == null) return;
            _suppress = true;
            try
            {
                int keep = _list.SelectedIndices.Count > 0 ? _list.SelectedIndices[0] : -1;
                _list.BeginUpdate();
                _list.Items.Clear();
                foreach (Rule r in _rules)
                {
                    ListViewItem item = new ListViewItem(new string[]
                    {
                        "",
                        KeyCode.Name(r.Src),
                        r.Dst == KeyCode.BLOCK ? "屏蔽（无输出）" : KeyCode.Name(r.Dst),
                        r.Dst == KeyCode.BLOCK ? "吞掉按键" : "改键"
                    });
                    item.Checked = r.Enabled;
                    item.ImageIndex = 0;
                    item.Tag = r;
                    _list.Items.Add(item);
                }
                if (keep >= 0 && keep < _list.Items.Count)
                    _list.Items[keep].Selected = true;
                _list.EndUpdate();
            }
            finally
            {
                _suppress = false;
            }

            if (_lblEmpty != null)
                _lblEmpty.Visible = _rules.Count == 0;
        }

        private void List_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppress) return;
            Rule r = e.Item.Tag as Rule;
            if (r == null) return;
            r.Enabled = e.Item.Checked;
            ApplyRules();
            SaveState();
            UpdateStatus();
        }

        private void List_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = _list.HitTest(e.Location);
            if (hit.Item == null) return;
            hit.Item.Selected = true;
            ReRecordSelected();
        }

        private void DeleteSelected()
        {
            if (_list.SelectedIndices.Count == 0)
            {
                SetStatus("先在列表里选一条要删的规则。", C_MUTED);
                return;
            }
            int idx = _list.SelectedIndices[0];
            if (idx < 0 || idx >= _rules.Count) return;
            _rules.RemoveAt(idx);
            RefreshList();
            ApplyRules();
            SaveState();
            UpdateStatus();
        }

        private void ClearAll()
        {
            if (_rules.Count == 0) return;

            // 模态弹窗会阻塞 UI 线程，期间让映射直通，避免钩子超时把目标键卡住
            bool wasPaused = _engine.Paused;
            _engine.Paused = true;
            DialogResult r;
            try
            {
                r = MessageBox.Show(
                    "清空全部 " + _rules.Count.ToString() + " 条改键规则？\n清空后键盘立刻恢复原样。",
                    "KeyMap Lite", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            }
            finally
            {
                _engine.Paused = wasPaused;
            }

            if (r != DialogResult.OK) return;

            _rules.Clear();
            RefreshList();
            ApplyRules();
            SaveState();
            UpdateStatus();
        }

        private void SetSelectedBlocked()
        {
            if (_list.SelectedIndices.Count == 0) return;
            int idx = _list.SelectedIndices[0];
            if (idx < 0 || idx >= _rules.Count) return;
            _rules[idx].Dst = KeyCode.BLOCK;
            _rules[idx].Enabled = true;
            RefreshList();
            ApplyRules();
            SaveState();
            UpdateStatus();
        }

        private void SyncPauseButton()
        {
            if (_btnPause == null) return;
            _btnPause.Text = _engine.Paused ? "恢复映射" : "暂停映射";
            _btnPause.ForeColor = _engine.Paused ? C_ACCENT : C_TEXT;
            UpdateTrayText();
        }

        private void TogglePause()
        {
            _engine.Paused = !_engine.Paused;
            SyncPauseButton();
            SaveState();
            UpdateStatus();
            if (_engine.Paused)
                SetStatus("已暂停 · 键盘恢复原样（再点一次恢复）", C_WARN);
        }

        // ---------------- 选键 ----------------

        private void OpenKeyPicker()
        {
            using (KeyPickerForm f = new KeyPickerForm(0))
            {
                f.Commit = PickerCommit;
                f.Undo = PickerUndo;
                f.ShowDialog(this);
            }
            UpdateButtonState();
        }

        private void ReRecordSelected()
        {
            if (_list.SelectedIndices.Count == 0) return;
            int idx = _list.SelectedIndices[0];
            if (idx < 0 || idx >= _rules.Count) return;

            using (KeyPickerForm f = new KeyPickerForm(_rules[idx].Src))
            {
                f.Commit = PickerCommit;
                f.Undo = PickerUndo;
                f.ShowDialog(this);
            }
            UpdateButtonState();
        }

        private void PickerCommit(int src, int dst)
        {
            if (src == 0) return;
            if (dst != KeyCode.BLOCK && dst == src) return;

            Rule exist = null;
            foreach (Rule r in _rules) if (r.Src == src) { exist = r; break; }

            UndoEntry u = new UndoEntry();
            u.Src = src;

            if (exist != null)
            {
                u.Existed = true;
                u.OldDst = exist.Dst;
                exist.Dst = dst;
                exist.Enabled = true;
            }
            else
            {
                u.Existed = false;
                _rules.Add(new Rule(src, dst, true));
            }
            _undo.Push(u);

            RefreshList();
            ApplyRules();
            SaveState();
            UpdateStatus();
            UpdateButtonState();

            SetStatus(dst == KeyCode.BLOCK
                ? KeyCode.Name(src) + " 已屏蔽"
                : KeyCode.Name(src) + " → " + KeyCode.Name(dst), C_OK);
        }

        private bool PickerUndo()
        {
            if (_undo.Count == 0) return false;
            UndoEntry u = _undo.Pop();

            if (u.Existed)
            {
                foreach (Rule r in _rules) if (r.Src == u.Src) { r.Dst = u.OldDst; break; }
            }
            else
            {
                for (int i = _rules.Count - 1; i >= 0; i--)
                {
                    if (_rules[i].Src == u.Src) { _rules.RemoveAt(i); break; }
                }
            }

            RefreshList();
            ApplyRules();
            SaveState();
            UpdateStatus();
            UpdateButtonState();
            return true;
        }

        private void UpdateButtonState()
        {
            _btnDelete.Enabled = _rules.Count > 0;
            _btnClear.Enabled = _rules.Count > 0;
        }
    }

    // =====================================================================
    // 可视化键盘图
    // =====================================================================
    internal sealed class KeyDef
    {
        public int Id;
        public string Label;
        public float X;
        public float Y;
        public float W;
        public float H;
    }

    internal sealed class KeyboardBoard : Control
    {
        private const float UnitsWide = 23f;
        private const float UnitsHigh = 7.25f;

        private static readonly Color C_BG = Color.FromArgb(0xF7, 0xF8, 0xFB);
        private static readonly Color C_KEY = Color.White;
        private static readonly Color C_BORDER = Color.FromArgb(0xD0, 0xD5, 0xDF);
        private static readonly Color C_TEXT = Color.FromArgb(0x1F, 0x29, 0x37);
        private static readonly Color C_HOVER = Color.FromArgb(0xEE, 0xF2, 0xFF);
        private static readonly Color C_ACCENT = Color.FromArgb(0x4F, 0x46, 0xE5);
        private static readonly Color C_SRC = Color.FromArgb(0x4F, 0x46, 0xE5);
        private static readonly Color C_DST = Color.FromArgb(0x05, 0x96, 0x69);

        private readonly List<KeyDef> _keys;
        private int _hover;

        public int SourceId;
        public int TargetId;
        public Action<int> KeyChosen;

        public KeyboardBoard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw, true);
            _keys = BuildLayout();
            Cursor = Cursors.Hand;
            BackColor = C_BG;
        }

        public float Unit
        {
            get
            {
                float u = Width / UnitsWide;
                return u <= 0f ? 1f : u;
            }
        }

        public int BoardHeight
        {
            get { return (int)Math.Ceiling(Unit * UnitsHigh); }
        }

        private RectangleF RectOf(KeyDef k, float unit)
        {
            float gap = Math.Max(1.5f, unit * 0.055f);
            return new RectangleF(
                k.X * unit + gap * 0.5f,
                k.Y * unit + gap * 0.5f,
                k.W * unit - gap,
                k.H * unit - gap);
        }

        private KeyDef HitTest(Point p)
        {
            float unit = Unit;
            for (int i = 0; i < _keys.Count; i++)
            {
                if (RectOf(_keys[i], unit).Contains(p)) return _keys[i];
            }
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            KeyDef k = HitTest(e.Location);
            int id = k == null ? 0 : k.Id;
            if (id != _hover)
            {
                _hover = id;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != 0)
            {
                _hover = 0;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            KeyDef k = HitTest(e.Location);
            if (k == null) return;
            if (KeyChosen != null) KeyChosen(k.Id);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C_BG);

            float unit = Unit;
            float px = Math.Max(7f, unit * 0.31f);

            using (Font normal = new Font("Microsoft YaHei UI", px, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font small = new Font("Microsoft YaHei UI", px * 0.86f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    KeyDef k = _keys[i];
                    RectangleF r = RectOf(k, unit);

                    Color back = C_KEY;
                    Color border = C_BORDER;
                    Color fore = C_TEXT;

                    if (k.Id == SourceId) { back = C_SRC; border = C_SRC; fore = Color.White; }
                    else if (k.Id == TargetId) { back = C_DST; border = C_DST; fore = Color.White; }
                    else if (k.Id == _hover) { back = C_HOVER; border = C_ACCENT; fore = C_TEXT; }

                    using (GraphicsPath path = RoundRect(r, Math.Max(2f, unit * 0.1f)))
                    {
                        using (SolidBrush b = new SolidBrush(back)) g.FillPath(b, path);
                        using (Pen pen = new Pen(border, 1f)) g.DrawPath(pen, path);
                    }

                    Font f = k.Label.Length >= 4 ? small : normal;
                    TextRenderer.DrawText(g, k.Label, f, Rectangle.Round(r), fore,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                }
            }
        }

        internal static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0f)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void K(List<KeyDef> l, int id, string label, float x, float y, float w, float h)
        {
            KeyDef k = new KeyDef();
            k.Id = id;
            k.Label = label;
            k.X = x;
            k.Y = y;
            k.W = w;
            k.H = h;
            l.Add(k);
        }

        private static List<KeyDef> BuildLayout()
        {
            List<KeyDef> l = new List<KeyDef>();

            // 功能键行
            K(l, 0x1B, "Esc", 0f, 0f, 1f, 1f);
            K(l, 0x70, "F1", 2f, 0f, 1f, 1f);
            K(l, 0x71, "F2", 3f, 0f, 1f, 1f);
            K(l, 0x72, "F3", 4f, 0f, 1f, 1f);
            K(l, 0x73, "F4", 5f, 0f, 1f, 1f);
            K(l, 0x74, "F5", 6.5f, 0f, 1f, 1f);
            K(l, 0x75, "F6", 7.5f, 0f, 1f, 1f);
            K(l, 0x76, "F7", 8.5f, 0f, 1f, 1f);
            K(l, 0x77, "F8", 9.5f, 0f, 1f, 1f);
            K(l, 0x78, "F9", 11f, 0f, 1f, 1f);
            K(l, 0x79, "F10", 12f, 0f, 1f, 1f);
            K(l, 0x7A, "F11", 13f, 0f, 1f, 1f);
            K(l, 0x7B, "F12", 14f, 0f, 1f, 1f);
            K(l, 0x12C, "PrtSc", 15.5f, 0f, 1f, 1f);
            K(l, 0x91, "ScrLk", 16.5f, 0f, 1f, 1f);
            K(l, 0x13, "Pause", 17.5f, 0f, 1f, 1f);

            // 数字行
            K(l, 0xC0, "`", 0f, 1f, 1f, 1f);
            int[] dId = { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0xBD, 0xBB };
            string[] dLb = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
            for (int i = 0; i < dId.Length; i++) K(l, dId[i], dLb[i], 1f + i, 1f, 1f, 1f);
            K(l, 0x08, "Backspace", 13f, 1f, 2f, 1f);
            K(l, 0x12D, "Insert", 15.5f, 1f, 1f, 1f);
            K(l, 0x124, "Home", 16.5f, 1f, 1f, 1f);
            K(l, 0x121, "PgUp", 17.5f, 1f, 1f, 1f);
            K(l, 0x90, "NumLk", 19f, 1f, 1f, 1f);
            K(l, 0x6F, "/", 20f, 1f, 1f, 1f);
            K(l, 0x6A, "*", 21f, 1f, 1f, 1f);
            K(l, 0x6D, "-", 22f, 1f, 1f, 1f);

            // QWERTY 行
            K(l, 0x09, "Tab", 0f, 2f, 1.5f, 1f);
            int[] r2Id = { 0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4F, 0x50 };
            string[] r2Lb = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
            for (int i = 0; i < r2Id.Length; i++) K(l, r2Id[i], r2Lb[i], 1.5f + i, 2f, 1f, 1f);
            K(l, 0xDB, "[", 11.5f, 2f, 1f, 1f);
            K(l, 0xDD, "]", 12.5f, 2f, 1f, 1f);
            K(l, 0xDC, "\\", 13.5f, 2f, 1.5f, 1f);
            K(l, 0x12E, "Delete", 15.5f, 2f, 1f, 1f);
            K(l, 0x123, "End", 16.5f, 2f, 1f, 1f);
            K(l, 0x122, "PgDn", 17.5f, 2f, 1f, 1f);
            K(l, 0x67, "7", 19f, 2f, 1f, 1f);
            K(l, 0x68, "8", 20f, 2f, 1f, 1f);
            K(l, 0x69, "9", 21f, 2f, 1f, 1f);
            K(l, 0x6B, "+", 22f, 2f, 1f, 2f);

            // 主行
            K(l, 0x14, "Caps", 0f, 3f, 1.75f, 1f);
            int[] r3Id = { 0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4A, 0x4B, 0x4C };
            string[] r3Lb = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
            for (int i = 0; i < r3Id.Length; i++) K(l, r3Id[i], r3Lb[i], 1.75f + i, 3f, 1f, 1f);
            K(l, 0xBA, ";", 10.75f, 3f, 1f, 1f);
            K(l, 0xDE, "'", 11.75f, 3f, 1f, 1f);
            K(l, 0x0D, "Enter", 12.75f, 3f, 2.25f, 1f);
            K(l, 0x64, "4", 19f, 3f, 1f, 1f);
            K(l, 0x65, "5", 20f, 3f, 1f, 1f);
            K(l, 0x66, "6", 21f, 3f, 1f, 1f);

            // 底行上方
            K(l, 0xA0, "Shift", 0f, 4f, 2.25f, 1f);
            int[] r4Id = { 0x5A, 0x58, 0x43, 0x56, 0x42, 0x4E, 0x4D, 0xBC, 0xBE, 0xBF };
            string[] r4Lb = { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" };
            for (int i = 0; i < r4Id.Length; i++) K(l, r4Id[i], r4Lb[i], 2.25f + i, 4f, 1f, 1f);
            K(l, 0xA1, "Shift", 12.25f, 4f, 2.75f, 1f);
            K(l, 0x126, "↑", 16.5f, 4f, 1f, 1f);
            K(l, 0x61, "1", 19f, 4f, 1f, 1f);
            K(l, 0x62, "2", 20f, 4f, 1f, 1f);
            K(l, 0x63, "3", 21f, 4f, 1f, 1f);
            K(l, 0x10D, "Enter", 22f, 4f, 1f, 2f);

            // 最底行
            K(l, 0xA2, "Ctrl", 0f, 5f, 1.25f, 1f);
            K(l, 0x5B, "Win", 1.25f, 5f, 1.25f, 1f);
            K(l, 0xA4, "Alt", 2.5f, 5f, 1.25f, 1f);
            K(l, 0x20, "空格", 3.75f, 5f, 6.25f, 1f);
            K(l, 0xA5, "Alt", 10f, 5f, 1.25f, 1f);
            K(l, 0x5C, "Win", 11.25f, 5f, 1.25f, 1f);
            K(l, 0x5D, "Menu", 12.5f, 5f, 1.25f, 1f);
            K(l, 0xA3, "Ctrl", 13.75f, 5f, 1.25f, 1f);
            K(l, 0x125, "←", 15.5f, 5f, 1f, 1f);
            K(l, 0x128, "↓", 16.5f, 5f, 1f, 1f);
            K(l, 0x127, "→", 17.5f, 5f, 1f, 1f);
            K(l, 0x60, "0", 19f, 5f, 2f, 1f);
            K(l, 0x6E, ".", 21f, 5f, 1f, 1f);

            // 鼠标区
            K(l, KeyCode.MOUSE_L, "左键", 0f, 6.25f, 2f, 1f);
            K(l, KeyCode.MOUSE_R, "右键", 2.1f, 6.25f, 2f, 1f);
            K(l, KeyCode.MOUSE_M, "中键", 4.2f, 6.25f, 2f, 1f);
            K(l, KeyCode.MOUSE_X1, "侧键1", 6.3f, 6.25f, 2f, 1f);
            K(l, KeyCode.MOUSE_X2, "侧键2", 8.4f, 6.25f, 2f, 1f);
            K(l, KeyCode.WHEEL_UP, "滚轮↑", 10.5f, 6.25f, 2f, 1f);
            K(l, KeyCode.WHEEL_DOWN, "滚轮↓", 12.6f, 6.25f, 2f, 1f);

            return l;
        }

        /// <summary>给自检用：按图上标签反查绑定的键码。</summary>
        internal static int IdOfLabel(string label)
        {
            List<KeyDef> keys = BuildLayout();
            for (int i = 0; i < keys.Count; i++)
                if (keys[i].Label == label) return keys[i].Id;
            return -1;
        }
    }

    internal sealed class KeyPickerForm : Form
    {
        private static readonly Color C_BG = Color.FromArgb(0xF3, 0xF4, 0xF7);
        private static readonly Color C_TEXT = Color.FromArgb(0x11, 0x18, 0x27);
        private static readonly Color C_MUTED = Color.FromArgb(0x6B, 0x72, 0x80);
        private static readonly Color C_ACCENT = Color.FromArgb(0x4F, 0x46, 0xE5);
        private static readonly Color C_BORDER = Color.FromArgb(0xD3, 0xD7, 0xE0);
        private static readonly Color C_WARN = Color.FromArgb(0xD9, 0x77, 0x06);

        private KeyboardBoard _board;
        private Label _lblTitle;
        private Label _lblSub;
        private Label _lblState;
        private Button _btnBlock;
        private Button _btnUndo;
        private Button _btnDone;

        private int _pendingSrc;
        private int _added;
        private string _lastDesc;
        private string _note;

        public Action<int, int> Commit;
        public Func<bool> Undo;

        public KeyPickerForm(int initialSource)
        {
            _pendingSrc = initialSource;
            BuildUi();
            UpdateUi();
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "选择键位 · 连续录入";
            ClientSize = new Size(900, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = C_BG;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

            _lblTitle = new Label();
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            _lblTitle.ForeColor = C_TEXT;
            _lblTitle.Location = new Point(22, 16);
            _lblTitle.Text = "点一个键，再点一个键，就改好一条";

            _lblSub = new Label();
            _lblSub.AutoSize = true;
            _lblSub.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _lblSub.ForeColor = C_MUTED;
            _lblSub.Location = new Point(24, 46);
            _lblSub.Text = "可以一直点下去，每点好一条立刻生效。小键盘数字需 NumLock 开启才认得出来。Esc 关闭。";

            _board = new KeyboardBoard();
            _board.KeyChosen = OnKeyChosen;

            _lblState = new Label();
            _lblState.AutoSize = false;
            _lblState.AutoEllipsis = true;
            _lblState.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            _lblState.ForeColor = C_TEXT;

            _btnBlock = MakeButton("设为屏蔽", false);
            _btnBlock.Click += delegate { SetBlock(); };
            _btnUndo = MakeButton("撤销上一条", false);
            _btnUndo.Click += delegate { UndoLast(); };
            _btnDone = MakeButton("完成", true);
            _btnDone.Click += delegate { Close(); };

            Controls.Add(_lblTitle);
            Controls.Add(_lblSub);
            Controls.Add(_board);
            Controls.Add(_lblState);
            Controls.Add(_btnBlock);
            Controls.Add(_btnUndo);
            Controls.Add(_btnDone);

            CancelButton = _btnDone;

            ResumeLayout(false);
        }

        private Button MakeButton(string text, bool primary)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = primary ? C_ACCENT : C_BORDER;
            b.BackColor = primary ? C_ACCENT : Color.White;
            b.ForeColor = primary ? Color.White : C_TEXT;
            b.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.Size = new Size(96, 36);
            return b;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            const int pad = 22;
            int w = ClientSize.Width - pad * 2;
            if (w < 320) w = 320;

            _board.SetBounds(pad, 76, w, 0);
            int h = _board.BoardHeight;
            _board.SetBounds(pad, 76, w, h);

            int y = 76 + h + 14;

            _btnDone.SetBounds(ClientSize.Width - pad - 96, y, 96, 36);
            _btnUndo.SetBounds(ClientSize.Width - pad - 96 - 8 - 96, y, 96, 36);
            _btnBlock.SetBounds(ClientSize.Width - pad - 96 - 8 - 96 - 8 - 96, y, 96, 36);

            int stateRight = _btnBlock.Left - 16;
            _lblState.SetBounds(pad, y + 8, Math.Max(120, stateRight - pad), 24);

            ClientSize = new Size(ClientSize.Width, y + 36 + 22);
        }

        private void OnKeyChosen(int id)
        {
            _note = null;

            if (_pendingSrc == 0)
            {
                _pendingSrc = id;
                _board.SourceId = id;
                _board.TargetId = 0;
                _board.Invalidate();
                UpdateUi();
                return;
            }

            if (id == _pendingSrc)
            {
                _note = "源键和目标键不能是同一个";
                UpdateUi();
                return;
            }

            CommitPair(id);
        }

        private void CommitPair(int dst)
        {
            int src = _pendingSrc;
            if (src == 0) return;

            if (Commit != null) Commit(src, dst);

            _added++;
            _lastDesc = dst == KeyCode.BLOCK
                ? KeyCode.Name(src) + " 已屏蔽"
                : KeyCode.Name(src) + " → " + KeyCode.Name(dst);

            // 保留刚完成这条的蓝/绿标记，直到用户点下一个键，作为"已经设好"的视觉确认
            _pendingSrc = 0;
            _board.SourceId = src;
            _board.TargetId = dst == KeyCode.BLOCK ? 0 : dst;
            _board.Invalidate();
            UpdateUi();
        }

        private void SetBlock()
        {
            if (_pendingSrc == 0)
            {
                _note = "先点一下要被屏蔽的键";
                UpdateUi();
                return;
            }
            CommitPair(KeyCode.BLOCK);
        }

        private void UndoLast()
        {
            if (_added <= 0)
            {
                _note = "还没有添加过规则";
                UpdateUi();
                return;
            }
            if (Undo != null && !Undo())
            {
                _note = "撤销失败";
                UpdateUi();
                return;
            }
            _added--;
            _lastDesc = null;
            _note = "已撤销上一条";
            _pendingSrc = 0;
            _board.SourceId = 0;
            _board.TargetId = 0;
            _board.Invalidate();
            UpdateUi();
        }

        private void UpdateUi()
        {
            if (_note != null)
            {
                _lblState.Text = _note;
                _lblState.ForeColor = C_WARN;
            }
            else if (_pendingSrc == 0)
            {
                if (_added == 0)
                {
                    _lblState.Text = "点一下要被改掉的键";
                }
                else
                {
                    _lblState.Text = "已添加 " + _added.ToString() + " 条 · 最近：" + _lastDesc + " · 接着点下一个键";
                }
                _lblState.ForeColor = C_TEXT;
            }
            else
            {
                _lblState.Text = KeyCode.Name(_pendingSrc) + "  →  再点一下它要变成的键";
                _lblState.ForeColor = C_ACCENT;
            }

            _btnBlock.Enabled = _pendingSrc != 0;
            _btnUndo.Enabled = _added > 0;
            _btnBlock.ForeColor = _btnBlock.Enabled ? C_TEXT : Color.FromArgb(0xA8, 0xAE, 0xBA);
            _btnUndo.ForeColor = _btnUndo.Enabled ? C_TEXT : Color.FromArgb(0xA8, 0xAE, 0xBA);

            // 等待源键时不清标记，让刚完成的那条保持蓝+绿可见
            if (_pendingSrc != 0)
            {
                _board.SourceId = _pendingSrc;
                _board.TargetId = 0;
            }
        }
    }


    // =====================================================================
    // 自检：不装钩子，只验证纯逻辑
    // =====================================================================
    internal static class SelfTest
    {
        public static int Run()
        {
            List<string> log = new List<string>();
            int fail = 0;

            Action<string, bool> check = delegate(string name, bool ok)
            {
                log.Add((ok ? "[PASS] " : "[FAIL] ") + name);
                if (!ok) fail++;
            };

            check("字母键名 A", KeyCode.Name(0x41) == "A");
            check("数字键名 5", KeyCode.Name(0x35) == "5");
            check("F5 键名", KeyCode.Name(0x74) == "F5");
            check("屏蔽名", KeyCode.Name(KeyCode.BLOCK).Length > 0);

            check("左 Shift 规范化", KeyCode.Normalize(0x10, 0x2A, 0) == 0xA0);
            check("右 Shift 规范化", KeyCode.Normalize(0x10, 0x36, 0) == 0xA1);
            check("左 Ctrl 规范化", KeyCode.Normalize(0x11, 0x1D, 0) == 0xA2);
            check("右 Ctrl 规范化", KeyCode.Normalize(0x11, 0x1D, 1) == 0xA3);
            check("左 Alt 规范化", KeyCode.Normalize(0x12, 0x38, 0) == 0xA4);
            check("右 Alt 规范化", KeyCode.Normalize(0x12, 0x38, 1) == 0xA5);
            check("方向键带扩展位", KeyCode.Normalize(0x26, 0x48, 1) == 0x126);
            check("方向键名称", KeyCode.Name(0x126) == "↑");
            check("键盘图 ← 绑定 0x125", KeyboardBoard.IdOfLabel("←") == 0x125);
            check("键盘图 ↑ 绑定 0x126", KeyboardBoard.IdOfLabel("↑") == 0x126);
            check("键盘图 → 绑定 0x127", KeyboardBoard.IdOfLabel("→") == 0x127);
            check("键盘图 ↓ 绑定 0x128", KeyboardBoard.IdOfLabel("↓") == 0x128);
            check("主 Enter 与小键盘 Enter 区分",
                KeyCode.Normalize(0x0D, 0x1C, 0) != KeyCode.Normalize(0x0D, 0x1C, 1));
            check("CapsLock 名称", KeyCode.Name(KeyCode.Normalize(0x14, 0x3A, 0)) == "CapsLock");
            check("鼠标键判定", KeyCode.IsMouse(KeyCode.MOUSE_X1) && !KeyCode.IsMouse(0x41));
            check("滚轮判定", KeyCode.IsWheel(KeyCode.WHEEL_UP) && !KeyCode.IsWheel(KeyCode.MOUSE_L));
            check("鼠标键名非空", KeyCode.Name(KeyCode.WHEEL_DOWN).Length > 0);

            check("INPUT 结构大小", Marshal.SizeOf(typeof(Win32.INPUT)) == (IntPtr.Size == 8 ? 40 : 28));

            // 状态往返
            string tmp = Path.Combine(Path.GetTempPath(), "kml_selftest_" + Guid.NewGuid().ToString("N") + ".txt");
            List<Rule> src = new List<Rule>();
            src.Add(new Rule(0x14, 0x1B, true));
            src.Add(new Rule(KeyCode.MOUSE_X1, 0x0D, false));
            src.Add(new Rule(0x41, KeyCode.BLOCK, true));

            StateStore.Save(tmp, true, true, src);

            bool paused = false;
            bool passThrough = false;
            List<Rule> back = new List<Rule>();
            bool okLoad = StateStore.Load(tmp, out paused, out passThrough, back);

            check("状态文件可读", okLoad);
            check("暂停位往返", paused);
            check("输出模式往返", passThrough);
            check("规则条数往返", back.Count == 3);
            check("规则内容往返",
                back.Count == 3 &&
                back[0].Src == 0x14 && back[0].Dst == 0x1B && back[0].Enabled &&
                back[1].Src == KeyCode.MOUSE_X1 && back[1].Dst == 0x0D && !back[1].Enabled &&
                back[2].Dst == KeyCode.BLOCK && back[2].Enabled);

            try { File.Delete(tmp); } catch { }

            // 缺失文件不应崩
            bool p2;
            bool p3;
            List<Rule> r2 = new List<Rule>();
            check("缺失文件安全返回", !StateStore.Load(tmp, out p2, out p3, r2) && r2.Count == 0);

            string dir = Path.GetDirectoryName(Path.GetFullPath(
                System.Reflection.Assembly.GetExecutingAssembly().Location));
            string outPath = Path.Combine(dir, "selftest-result.txt");

            StringBuilder sb = new StringBuilder();
            foreach (string line in log) sb.AppendLine(line);
            sb.AppendLine(fail == 0 ? "RESULT=OK" : "RESULT=FAIL count=" + fail.ToString());

            try { File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false)); }
            catch { }

            return fail == 0 ? 0 : 1;
        }
    }
}
