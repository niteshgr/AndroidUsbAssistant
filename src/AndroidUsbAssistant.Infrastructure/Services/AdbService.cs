using System.Diagnostics;
using System.Text;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace AndroidUsbAssistant.Infrastructure.Services;

public class AdbService : IAdbService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<AdbService> _logger;

    public AdbService(IConfigurationService configService, ILogger<AdbService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<bool> IsAdbAvailableAsync()
    {
        var adbPath = GetAdbPath();
        try
        {
            _logger.LogDebug("Testing ADB availability at: {AdbPath}", adbPath);
            var result = await RunProcessAsync(adbPath, "version", TimeSpan.FromSeconds(3));
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ADB is not available at: {AdbPath}", adbPath);
            return false;
        }
    }

    public async Task<string> ExecuteAdbCommandAsync(string arguments)
    {
        var adbPath = GetAdbPath();
        _logger.LogDebug("Executing ADB command: {AdbPath} {Arguments}", adbPath, arguments);
        
        var result = await RunProcessAsync(adbPath, arguments, TimeSpan.FromSeconds(10));
        if (result.ExitCode != 0)
        {
            var errMsg = string.IsNullOrWhiteSpace(result.StandardError) ? "Unknown error" : result.StandardError.Trim();
            throw new InvalidOperationException($"ADB command failed (Exit code: {result.ExitCode}): {errMsg}");
        }
        return result.StandardOutput;
    }

    public async Task<List<AndroidDevice>> GetConnectedDevicesAsync()
    {
        _logger.LogInformation("Scanning for connected Android devices via ADB.");
        var devices = new List<AndroidDevice>();

        try
        {
            var output = await ExecuteAdbCommandAsync("devices");
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Skip the first line: "List of devices attached"
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var serial = parts[0];
                    var state = parts[1];

                    var device = new AndroidDevice
                    {
                        SerialNumber = serial,
                        State = state
                    };

                    if (device.IsAuthorized)
                    {
                        await PopulateDevicePropertiesAsync(device);
                    }
                    else
                    {
                        _logger.LogWarning("Device {Serial} is in {State} state.", serial, state);
                    }

                    devices.Add(device);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for connected Android devices.");
        }

        return devices;
    }

    private async Task PopulateDevicePropertiesAsync(AndroidDevice device)
    {
        try
        {
            var modelTask = GetDevicePropertyAsync(device.SerialNumber, "ro.product.model");
            var manufacturerTask = GetDevicePropertyAsync(device.SerialNumber, "ro.product.manufacturer");

            await Task.WhenAll(modelTask, manufacturerTask);

            device.Model = modelTask.Result;
            device.Manufacturer = manufacturerTask.Result;

            _logger.LogInformation("Identified device: {DisplayName}", device.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read properties for device {Serial}.", device.SerialNumber);
        }
    }

    private async Task<string> GetDevicePropertyAsync(string serialNumber, string propertyName)
    {
        try
        {
            var output = await ExecuteAdbCommandAsync($"-s {serialNumber} shell getprop {propertyName}");
            return output.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query property {Property} for device {Serial}: {Message}", propertyName, serialNumber, ex.Message);
            return string.Empty;
        }
    }

    private string GetAdbPath()
    {
        var config = _configService.GetConfiguration();
        if (!string.IsNullOrWhiteSpace(config.AdbPath))
        {
            return config.AdbPath;
        }

        return "adb"; // Fallback to PATH environment
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(
        string fileName,
        string arguments,
        TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var outputWaitHandle = new SemaphoreSlim(0);
        using var errorWaitHandle = new SemaphoreSlim(0);

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                outputWaitHandle.Release();
            }
            else
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                errorWaitHandle.Release();
            }
            else
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var processTask = Task.Run(() => process.WaitForExit());
        var outputTask = outputWaitHandle.WaitAsync();
        var errorTask = errorWaitHandle.WaitAsync();

        var completedTask = await Task.WhenAny(processTask, Task.Delay(timeout));
        if (completedTask == processTask)
        {
            await Task.WhenAll(outputTask, errorTask);
            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        else
        {
            try
            {
                process.Kill();
            }
            catch { }
            throw new TimeoutException($"Process execution timed out: {fileName} {arguments}");
        }
    }
}
