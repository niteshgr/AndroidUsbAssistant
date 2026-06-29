namespace AndroidUsbAssistant.Core.Interfaces;

public interface IUsbDetector
{
    event EventHandler? DeviceChanged;
    void Start();
    void Stop();
}
