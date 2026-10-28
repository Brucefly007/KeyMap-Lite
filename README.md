# KeyMap Lite

单文件 Windows 临时改键工具：打开即生效，关掉即还原。

- 零第三方依赖，C# / .NET Framework 4.x
- 系统托盘运行，支持按键映射与暂停
- 附带已编译成品 `KeyMapLite.exe`

## 直接使用（成品）

1. 下载本仓库中的 `KeyMapLite.exe`
2. 双击运行（可能需管理员权限，视系统策略而定）
3. 关闭程序后映射自动还原

截图见 `preview.png`、`picker.png`。

## 从源码编译

环境：Windows，已安装 .NET Framework 4.x（自带 `csc.exe`）。

```bat
build.cmd
```

或手动：

```bat
csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:KeyMapLite.exe /win32icon:app.ico ^
  /reference:System.dll /reference:System.Core.dll ^
  /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  KeyMapLite.cs
```

## 文件说明

| 文件 | 说明 |
|------|------|
| `KeyMapLite.cs` | 全部源码（单文件） |
| `KeyMapLite.exe` | 已编译成品 |
| `build.cmd` | 一键编译脚本 |
| `app.ico` | 程序图标 |
| `state.txt` | 运行时状态（程序自动读写；仓库内为示例） |

## License

MIT
