using UnityEngine;
using System.Diagnostics;
using System.IO;

public class RunPython : MonoBehaviour
{
    // Full path to your Python executable and script
    public string pythonPath = @"C:\Users\CSC5030Z\AppData\Local\Programs\Python\Python312\python.exe";
    public string scriptPath = @"C:\Users\CSC5030Z\Downloads\aaa.py";

    void Start()
    {
        RunPythonScript();
    }

    void RunPythonScript()
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = pythonPath;
        start.Arguments = $"\"{scriptPath}\""; 
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.CreateNoWindow = true;

        using (Process process = new Process())
        {
            process.StartInfo = start;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();

            process.WaitForExit();

            UnityEngine.Debug.Log("Python Output:\n" + output);
            if (!string.IsNullOrEmpty(errors))
                UnityEngine.Debug.LogError("Python Errors:\n" + errors);
        }
    }
}
