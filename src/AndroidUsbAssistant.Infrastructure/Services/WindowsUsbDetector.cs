using System.Runtime.InteropServices;
using System.Windows.Forms;
using AndroidUsbAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Infrastructure.Services;

public class WindowsUsbDetector : NativeWindow, IUsbDetector, IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

    // GUID for USB Device Interface Class: {A5DCBF10-6530-11D2-901F-00C04FB951ED}
    private static readonly Guid GuidDevinterfaceUsbDevice = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    private readonly ILogger<WindowsUsbDetector> _logger;
    private IntPtr _notificationHandle = IntPtr.Zero;
    private bool _disposed;

    public event EventHandler? DeviceChanged;

    public WindowsUsbDetector(ILogger<WindowsUsbDetector> logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        if (Handle != IntPtr.Zero)
        {
            _logger.LogWarning("Windows USB Detector is already started.");
            return;
        }

        _logger.LogInformation("Starting Windows USB Detector.");

        try
        {
            var cp = new CreateParams
            {
                Caption = "AndroidUsbAssistant_UsbDetectorWindow"
            };

            CreateHandle(cp);
            RegisterUsbNotification(Handle);
            _logger.LogInformation("Windows USB Detector started successfully (HWND: {Handle}).", Handle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Windows USB Detector.");
        }
    }

    public void Stop()
    {
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        _logger.LogInformation("Stopping Windows USB Detector.");
        UnregisterUsbNotification();
        DestroyHandle();
    }

    private void RegisterUsbNotification(IntPtr hWnd)
    {
        var dbcc = new DEV_BROADCAST_DEVICEINTERFACE
        {
            dbcc_size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
            dbcc_reserved = 0,
            dbcc_classguid = GuidDevinterfaceUsbDevice,
            dbcc_name = char.MinValue
        };

        var buffer = Marshal.AllocHGlobal(dbcc.dbcc_size);
        try
        {
            Marshal.StructureToPtr(dbcc, buffer, false);
            _notificationHandle = RegisterDeviceNotification(hWnd, buffer, 0);
            
            if (_notificationHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("RegisterDeviceNotification failed with system error code: {Error}", error);
            }
            else
            {
                _logger.LogInformation("Successfully registered for USB device notifications.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering for USB device notifications.");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void UnregisterUsbNotification()
    {
        if (_notificationHandle != IntPtr.Zero)
        {
            if (UnregisterDeviceNotification(_notificationHandle))
            {
                _logger.LogInformation("Unregistered USB device notifications successfully.");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogWarning("UnregisterDeviceNotification returned error code: {Error}", error);
            }
            _notificationHandle = IntPtr.Zero;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DEVICECHANGE)
        {
            var eventType = m.WParam.ToInt32();
            if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
            {
                var action = eventType == DBT_DEVICEARRIVAL ? "Arrival" : "Removal";
                _logger.LogInformation("USB Device change detected: {Action} (WParam: {WParam}).", action, eventType);
                DeviceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
            }
            _disposed = true;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterDeviceNotification(IntPtr Handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public char dbcc_name;
    }
}
