using Aimmy.Core.Config;
using Aimmy.Core.Capture;
using Aimmy.Core.Models;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Aimmy.Platform.Linux.X11.Overlay;

public sealed class X11OverlayBackend : IOverlayBackend
{
    private const string TransparentBackgroundColor = "#010203";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly ICommandRunner _commandRunner;
    private readonly Func<string, string?> _environmentVariableReader;
    private readonly object _sync = new();

    private readonly int _captureOffsetX;
    private readonly int _captureOffsetY;
    private readonly int _displayOriginX;
    private readonly int _displayOriginY;
    private readonly int _displayWidth;
    private readonly int _displayHeight;
    private readonly float _dpiScaleX;
    private readonly float _dpiScaleY;

    private readonly string _stateFilePath;
    private OverlayState _state;
    private Process? _overlayProcess;
    private bool _disposed;
    private bool _overlayFaulted;

    public X11OverlayBackend(
        AimmyConfig config,
        ICommandRunner? commandRunner = null,
        Func<string, string?>? environmentVariableReader = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;

        var geometry = CaptureGeometryResolver.Resolve(config.Capture);
        _displayOriginX = geometry.DisplayOriginX;
        _displayOriginY = geometry.DisplayOriginY;
        _displayWidth = geometry.DisplayWidth;
        _displayHeight = geometry.DisplayHeight;
        _captureOffsetX = geometry.CaptureX;
        _captureOffsetY = geometry.CaptureY;
        _dpiScaleX = geometry.DpiScaleX;
        _dpiScaleY = geometry.DpiScaleY;

        _stateFilePath = Path.Combine(Path.GetTempPath(), $"aimmylinux_overlay_state_{Guid.NewGuid():N}.json");
        _state = OverlayState.CreateInitial(config);
        PersistStateUnsafe();
    }

    public string Name => "x11-overlay(tkinter-fov-esp)";

    public static bool IsSupported(
        ICommandRunner commandRunner,
        Func<string, string?> environmentVariableReader,
        out string reason)
    {
        var sessionType = environmentVariableReader("XDG_SESSION_TYPE") ?? string.Empty;
        var display = environmentVariableReader("DISPLAY") ?? string.Empty;
        var isX11 = string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(display);

        if (!isX11)
        {
            reason = "X11 session not detected.";
            return false;
        }

        if (!commandRunner.CommandExists("python3"))
        {
            reason = "python3 is required for the overlay renderer.";
            return false;
        }

        reason = "X11 overlay prerequisites satisfied.";
        return true;
    }

    public Task ShowFovAsync(int size, string style, string color, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!CanProceedLocked())
            {
                return Task.CompletedTask;
            }

            _state.ShowFov = size > 0;
            _state.FovSize = Math.Max(10, size);
            _state.FovStyle = NormalizeStyle(style);
            _state.FovColor = NormalizeColor(color);

            if (!EnsureOverlayProcessLocked())
            {
                return Task.CompletedTask;
            }

            PersistStateUnsafe();
        }

        return Task.CompletedTask;
    }

    public Task HideFovAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _state.ShowFov = false;
            PersistStateUnsafe();
        }

        return Task.CompletedTask;
    }

    public Task ShowDetectionsAsync(IReadOnlyList<Detection> detections, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!CanProceedLocked())
            {
                return Task.CompletedTask;
            }

            _state.Detections = detections
                .Select(MapDetectionToOverlayRect)
                .ToList();

            if (!EnsureOverlayProcessLocked())
            {
                return Task.CompletedTask;
            }

            PersistStateUnsafe();
        }

        return Task.CompletedTask;
    }

    public Task ClearDetectionsAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _state.Detections = new List<OverlayDetection>();
            PersistStateUnsafe();
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            StopOverlayProcessLocked();
            DeleteStateFileQuietly(_stateFilePath);
        }

        return ValueTask.CompletedTask;
    }

    public static OverlayDetectionRect ProjectDetectionToScreen(
        Detection detection,
        int captureOffsetX,
        int captureOffsetY,
        int displayWidth,
        int displayHeight,
        float dpiScaleX = 1f,
        float dpiScaleY = 1f,
        int displayOriginX = 0,
        int displayOriginY = 0)
    {
        var minX = displayOriginX;
        var minY = displayOriginY;
        var maxX = displayOriginX + Math.Max(1, displayWidth) - 1;
        var maxY = displayOriginY + Math.Max(1, displayHeight) - 1;

        var x1 = Clamp((detection.Left * dpiScaleX) + captureOffsetX, minX, maxX);
        var y1 = Clamp((detection.Top * dpiScaleY) + captureOffsetY, minY, maxY);
        var x2 = Clamp((detection.Right * dpiScaleX) + captureOffsetX, minX, maxX);
        var y2 = Clamp((detection.Bottom * dpiScaleY) + captureOffsetY, minY, maxY);

        if (x2 <= x1)
        {
            x2 = Math.Min(maxX, x1 + 1);
        }

        if (y2 <= y1)
        {
            y2 = Math.Min(maxY, y1 + 1);
        }

        return new OverlayDetectionRect(x1, y1, x2, y2);
    }

    private bool CanProceedLocked()
    {
        if (_disposed || _overlayFaulted)
        {
            return false;
        }

        if (!IsSupported(_commandRunner, _environmentVariableReader, out var reason))
        {
            _overlayFaulted = true;
            Console.Error.WriteLine($"Overlay backend unavailable: {reason}");
            return false;
        }

        return true;
    }

    private bool EnsureOverlayProcessLocked()
    {
        if (_overlayProcess is { HasExited: false })
        {
            return true;
        }

        StopOverlayProcessLocked();

        try
        {
            _overlayProcess = StartOverlayProcess();
            return true;
        }
        catch (Exception ex)
        {
            _overlayFaulted = true;
            Console.Error.WriteLine($"Overlay backend error: failed to start overlay process ({ex.Message}).");
            return false;
        }
    }

    private Process StartOverlayProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = "-",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment["AIMMY_OVERLAY_STATE_PATH"] = _stateFilePath;
        startInfo.Environment["AIMMY_OVERLAY_BG"] = TransparentBackgroundColor;
        startInfo.Environment["AIMMY_DISPLAY_ORIGIN_X"] = _displayOriginX.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["AIMMY_DISPLAY_ORIGIN_Y"] = _displayOriginY.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["AIMMY_DISPLAY_WIDTH"] = _displayWidth.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["AIMMY_DISPLAY_HEIGHT"] = _displayHeight.ToString(CultureInfo.InvariantCulture);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                Console.Error.WriteLine($"Overlay stderr: {eventArgs.Data}");
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start python3 overlay process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Write(BuildOverlayScript());
        process.StandardInput.Close();
        return process;
    }

    private static string BuildOverlayScript()
    {
        var script = new StringBuilder();
        script.AppendLine("import json");
        script.AppendLine("import os");
        script.AppendLine("import signal");
        script.AppendLine("import tkinter as tk");
        script.AppendLine();
        script.AppendLine("state_path = os.environ.get('AIMMY_OVERLAY_STATE_PATH')");
        script.AppendLine("bg = os.environ.get('AIMMY_OVERLAY_BG', '#010203')");
        script.AppendLine();
        script.AppendLine("root = tk.Tk()");
        script.AppendLine("root.overrideredirect(True)");
        script.AppendLine("root.attributes('-topmost', True)");
        script.AppendLine("root.configure(bg=bg)");
        script.AppendLine("try:");
        script.AppendLine("    root.wm_attributes('-transparentcolor', bg)");
        script.AppendLine("except tk.TclError:");
        script.AppendLine("    pass");
        script.AppendLine();
        script.AppendLine("screen_w = root.winfo_screenwidth()");
        script.AppendLine("screen_h = root.winfo_screenheight()");
        script.AppendLine("display_origin_x = int(os.environ.get('AIMMY_DISPLAY_ORIGIN_X', '0'))");
        script.AppendLine("display_origin_y = int(os.environ.get('AIMMY_DISPLAY_ORIGIN_Y', '0'))");
        script.AppendLine("display_w = max(1, int(os.environ.get('AIMMY_DISPLAY_WIDTH', str(screen_w))))");
        script.AppendLine("display_h = max(1, int(os.environ.get('AIMMY_DISPLAY_HEIGHT', str(screen_h))))");
        script.AppendLine("display_center_x = display_origin_x + (display_w / 2.0)");
        script.AppendLine("display_center_y = display_origin_y + (display_h / 2.0)");
        script.AppendLine("root.geometry(f'{screen_w}x{screen_h}+0+0')");
        script.AppendLine();
        script.AppendLine("canvas = tk.Canvas(root, width=screen_w, height=screen_h, bg=bg, highlightthickness=0)");
        script.AppendLine("canvas.pack(fill='both', expand=True)");
        script.AppendLine();
        script.AppendLine("def norm_color(value, fallback):");
        script.AppendLine("    if not value:");
        script.AppendLine("        return fallback");
        script.AppendLine("    value = str(value).strip()");
        script.AppendLine("    return value if value else fallback");
        script.AppendLine();
        script.AppendLine("def tracer_origin(position):");
        script.AppendLine("    pos = str(position or 'bottom').lower()");
        script.AppendLine("    if pos == 'top':");
        script.AppendLine("        return (display_center_x, float(display_origin_y))");
        script.AppendLine("    if pos == 'left':");
        script.AppendLine("        return (float(display_origin_x), display_center_y)");
        script.AppendLine("    if pos == 'right':");
        script.AppendLine("        return (float(display_origin_x + display_w), display_center_y)");
        script.AppendLine("    if pos == 'center':");
        script.AppendLine("        return (display_center_x, display_center_y)");
        script.AppendLine("    return (display_center_x, float(display_origin_y + display_h))");
        script.AppendLine();
        script.AppendLine("def render(state):");
        script.AppendLine("    canvas.delete('all')");
        script.AppendLine("    try:");
        script.AppendLine("        alpha = float(state.get('Opacity', 1.0))");
        script.AppendLine("        alpha = min(1.0, max(0.15, alpha))");
        script.AppendLine("        root.attributes('-alpha', alpha)");
        script.AppendLine("    except Exception:");
        script.AppendLine("        pass");
        script.AppendLine();
        script.AppendLine("    if state.get('ShowFov', False):");
        script.AppendLine("        fov_size = int(state.get('FovSize', 0) or 0)");
        script.AppendLine("        if fov_size > 0:");
        script.AppendLine("            color = norm_color(state.get('FovColor'), '#ff0000')");
        script.AppendLine("            style = str(state.get('FovStyle', 'circle')).lower()");
        script.AppendLine("            cx = display_center_x");
        script.AppendLine("            cy = display_center_y");
        script.AppendLine("            half = fov_size / 2.0");
        script.AppendLine("            x1 = cx - half");
        script.AppendLine("            y1 = cy - half");
        script.AppendLine("            x2 = cx + half");
        script.AppendLine("            y2 = cy + half");
        script.AppendLine("            if style == 'square':");
        script.AppendLine("                canvas.create_rectangle(x1, y1, x2, y2, outline=color, width=2)");
        script.AppendLine("            else:");
        script.AppendLine("                canvas.create_oval(x1, y1, x2, y2, outline=color, width=2)");
        script.AppendLine();
        script.AppendLine("    detections = state.get('Detections') or []");
        script.AppendLine("    show_conf = bool(state.get('ShowConfidence', False))");
        script.AppendLine("    show_tracers = bool(state.get('ShowTracers', False))");
        script.AppendLine("    tracer_x, tracer_y = tracer_origin(state.get('TracerPosition'))");
        script.AppendLine();
        script.AppendLine("    for det in detections:");
        script.AppendLine("        x1 = float(det.get('X1', 0.0))");
        script.AppendLine("        y1 = float(det.get('Y1', 0.0))");
        script.AppendLine("        x2 = float(det.get('X2', 0.0))");
        script.AppendLine("        y2 = float(det.get('Y2', 0.0))");
        script.AppendLine("        conf = float(det.get('Confidence', 0.0))");
        script.AppendLine("        label = str(det.get('Label', 'Enemy'))");
        script.AppendLine();
        script.AppendLine("        canvas.create_rectangle(x1, y1, x2, y2, outline='#00FF80', width=2)");
        script.AppendLine("        if show_conf:");
        script.AppendLine("            text = f'{label} {conf:.2f}'");
        script.AppendLine("        else:");
        script.AppendLine("            text = label");
        script.AppendLine("        canvas.create_text(x1 + 4, max(10, y1 - 10), text=text, anchor='w', fill='#00FF80', font=('Sans', 10, 'bold'))");
        script.AppendLine();
        script.AppendLine("        if show_tracers:");
        script.AppendLine("            tx = (x1 + x2) / 2.0");
        script.AppendLine("            ty = (y1 + y2) / 2.0");
        script.AppendLine("            canvas.create_line(tracer_x, tracer_y, tx, ty, fill='#00FF80', width=1)");
        script.AppendLine();
        script.AppendLine("last_mtime = None");
        script.AppendLine("def tick():");
        script.AppendLine("    global last_mtime");
        script.AppendLine("    try:");
        script.AppendLine("        mtime = os.path.getmtime(state_path)");
        script.AppendLine("    except Exception:");
        script.AppendLine("        root.after(33, tick)");
        script.AppendLine("        return");
        script.AppendLine();
        script.AppendLine("    if last_mtime is None or mtime != last_mtime:");
        script.AppendLine("        try:");
        script.AppendLine("            with open(state_path, 'r', encoding='utf-8') as f:");
        script.AppendLine("                state = json.load(f)");
        script.AppendLine("            render(state)");
        script.AppendLine("            last_mtime = mtime");
        script.AppendLine("        except Exception:");
        script.AppendLine("            pass");
        script.AppendLine();
        script.AppendLine("    root.after(33, tick)");
        script.AppendLine();
        script.AppendLine("def _shutdown(*_args):");
        script.AppendLine("    try:");
        script.AppendLine("        root.destroy()");
        script.AppendLine("    except Exception:");
        script.AppendLine("        pass");
        script.AppendLine();
        script.AppendLine("signal.signal(signal.SIGTERM, _shutdown)");
        script.AppendLine("signal.signal(signal.SIGINT, _shutdown)");
        script.AppendLine("tick()");
        script.AppendLine("root.mainloop()");
        return script.ToString();
    }

    private OverlayDetection MapDetectionToOverlayRect(Detection detection)
    {
        var projected = ProjectDetectionToScreen(
            detection,
            _captureOffsetX,
            _captureOffsetY,
            _displayWidth,
            _displayHeight,
            _dpiScaleX,
            _dpiScaleY,
            _displayOriginX,
            _displayOriginY);

        return new OverlayDetection(
            projected.X1,
            projected.Y1,
            projected.X2,
            projected.Y2,
            detection.Confidence,
            string.IsNullOrWhiteSpace(detection.ClassName) ? "Enemy" : detection.ClassName);
    }

    private void PersistStateUnsafe()
    {
        try
        {
            var tempPath = $"{_stateFilePath}.tmp";
            var json = JsonSerializer.Serialize(_state, JsonOptions);
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            File.Move(tempPath, _stateFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Overlay backend warning: failed to update overlay state ({ex.Message}).");
        }
    }

    private static string NormalizeStyle(string style)
    {
        return string.Equals(style, "square", StringComparison.OrdinalIgnoreCase)
            ? "square"
            : "circle";
    }

    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#ff0000";
        }

        var value = color.Trim();
        if (!value.StartsWith('#'))
        {
            return value;
        }

        if (value.Length == 9)
        {
            // Convert #AARRGGBB -> #RRGGBB for tkinter compatibility.
            return $"#{value[3..]}";
        }

        return value;
    }

    private void StopOverlayProcessLocked()
    {
        if (_overlayProcess is null)
        {
            return;
        }

        try
        {
            if (!_overlayProcess.HasExited)
            {
                _overlayProcess.Kill(entireProcessTree: true);
                _overlayProcess.WaitForExit(1000);
            }
        }
        catch
        {
            // Best effort shutdown.
        }
        finally
        {
            _overlayProcess.Dispose();
            _overlayProcess = null;
        }
    }

    private static void DeleteStateFileQuietly(string stateFilePath)
    {
        try
        {
            if (File.Exists(stateFilePath))
            {
                File.Delete(stateFilePath);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private sealed class OverlayState
    {
        public bool ShowFov { get; set; }
        public int FovSize { get; set; }
        public string FovStyle { get; set; } = "circle";
        public string FovColor { get; set; } = "#ff8080";
        public bool ShowConfidence { get; set; }
        public bool ShowTracers { get; set; }
        public string TracerPosition { get; set; } = "bottom";
        public double Opacity { get; set; } = 1.0;
        public List<OverlayDetection> Detections { get; set; } = new();

        public static OverlayState CreateInitial(AimmyConfig config)
        {
            return new OverlayState
            {
                ShowFov = false,
                FovSize = Math.Max(10, config.Fov.Size),
                FovStyle = NormalizeStyle(config.Fov.Style),
                FovColor = NormalizeColor(config.Fov.Color),
                ShowConfidence = config.Overlay.ShowConfidence,
                ShowTracers = config.Overlay.ShowTracers,
                TracerPosition = string.IsNullOrWhiteSpace(config.Overlay.TracerPosition)
                    ? "bottom"
                    : config.Overlay.TracerPosition.Trim(),
                Opacity = Math.Clamp(config.Overlay.Opacity, 0.15, 1.0)
            };
        }
    }

    private sealed record OverlayDetection(
        float X1,
        float Y1,
        float X2,
        float Y2,
        float Confidence,
        string Label);
}

public readonly record struct OverlayDetectionRect(
    float X1,
    float Y1,
    float X2,
    float Y2);
