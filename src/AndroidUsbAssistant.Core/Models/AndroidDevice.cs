namespace AndroidUsbAssistant.Core.Models;

public class AndroidDevice
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // e.g. "device", "unauthorized", "offline"

    public bool IsAuthorized => string.Equals(State, "device", StringComparison.OrdinalIgnoreCase);

    public string DisplayName
    {
        get
        {
            if (!IsAuthorized)
            {
                return $"{SerialNumber} ({State})";
            }

            var manufacturerPart = string.IsNullOrWhiteSpace(Manufacturer)
                ? string.Empty
                : char.ToUpper(Manufacturer[0]) + Manufacturer[1..];

            var modelPart = string.IsNullOrWhiteSpace(Model) ? "Device" : Model;

            return string.IsNullOrWhiteSpace(manufacturerPart)
                ? $"{modelPart} ({SerialNumber})"
                : $"{manufacturerPart} {modelPart} ({SerialNumber})";
        }
    }
}
