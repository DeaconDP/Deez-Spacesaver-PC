using System.Diagnostics;
using Spacesaver.Models;
using Windows.System;

namespace Spacesaver.Services;

public sealed class WindowsLauncherService
{
    public async Task<bool> LaunchAsync(LaunchAction action)
    {
        return action.Type switch
        {
            LaunchActionType.SettingsUri => await LaunchUriAsync(action.Target),
            LaunchActionType.BrowserUri => await LaunchUriAsync(action.Target),
            LaunchActionType.Executable => LaunchExecutable(action.Target, action.Arguments),
            LaunchActionType.ExplorerPath => LaunchExplorer(action.Target),
            LaunchActionType.ControlPanel => LaunchExecutable(action.Target),
            LaunchActionType.InstructionsOnly => false,
            _ => false
        };
    }

    public static async Task<bool> CopyToClipboardAsync(string text)
    {
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage
        {
            RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
        };
        package.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        return await Task.FromResult(true);
    }

    private static async Task<bool> LaunchUriAsync(string uri)
    {
        try
        {
            return await Launcher.LaunchUriAsync(new Uri(uri));
        }
        catch
        {
            return false;
        }
    }

    private static bool LaunchExecutable(string path, string? arguments = null)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(arguments))
                startInfo.Arguments = arguments;

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool LaunchExplorer(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        return LaunchExecutable("explorer.exe", expanded);
    }

    /// <summary>Opens a folder path in File Explorer. Caller should verify existence for UX messaging.</summary>
    public (bool Ok, string? Error) OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
