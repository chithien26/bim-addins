using System.Diagnostics;
using System.Text;

namespace AddinStore;

internal static class UpdateLauncher
{
    public static void Start(string productName, int processId, string setupPath, string? revitPath, string? documentPath)
    {
        var helper = Path.Combine(Path.GetTempPath(), productName + "-update.cmd");
        var script = new StringBuilder();
        script.AppendLine("@echo off");
        script.AppendLine("setlocal");
        script.AppendLine(":wait");
        script.AppendLine("tasklist /FI \"PID eq " + processId + "\" | findstr /I \"" + processId + "\" >nul");
        script.AppendLine("if not errorlevel 1 (");
        script.AppendLine("  timeout /t 2 /nobreak >nul");
        script.AppendLine("  goto wait");
        script.AppendLine(")");
        script.AppendLine("timeout /t 2 /nobreak >nul");
        script.AppendLine("start /wait \"\" \"" + setupPath + "\" /VERYSILENT /NORESTART /SUPPRESSMSGBOXES");
        if (!string.IsNullOrEmpty(revitPath) && File.Exists(revitPath))
        {
            if (!string.IsNullOrEmpty(documentPath) && File.Exists(documentPath))
            {
                script.AppendLine("start \"\" \"" + revitPath + "\" \"" + documentPath + "\"");
            }
            else
            {
                script.AppendLine("start \"\" \"" + revitPath + "\"");
            }
        }

        File.WriteAllText(helper, script.ToString(), Encoding.Default);
        Process.Start(new ProcessStartInfo
        {
            FileName = helper,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });
    }
}
