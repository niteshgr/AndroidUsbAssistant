using System;
using System.Collections.Concurrent;

namespace AndroidUsbAssistant.App.Services;

public class DeviceDisconnectTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _manualDisconnectTimes = new();

    public void RecordManualDisconnect(string serial)
    {
        _manualDisconnectTimes[serial] = DateTime.UtcNow;
    }

    public bool IsRecentManualDisconnect(string serial)
    {
        if (_manualDisconnectTimes.TryGetValue(serial, out var disconnectTime))
        {
            if (DateTime.UtcNow - disconnectTime < TimeSpan.FromSeconds(8))
            {
                return true;
            }
            _manualDisconnectTimes.TryRemove(serial, out _);
        }
        return false;
    }
}
