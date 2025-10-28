using UnityEngine;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Box : MonoBehaviour
{
    public string pythonPath = @"C:\Users\CSC5030Z\AppData\Local\Programs\Python\Python312\python.exe";
    public string scriptPath = @"C:\Users\CSC5030Z\Unity\projects\kkklll\The Engineer\Circuit Builder\Assets\solver.py";

    private string[,] legsArray = new string[10, 18];
    private const int Rows = 10;
    private const int Cols = 18;

    public bool circuit_status = false;

    [SerializeField] private Material onMaterialRed;
    [SerializeField] private Material originalMaterialRed;
    [SerializeField] private Material onMaterialBlue;
    [SerializeField] private Material originalMaterialBlue;
    [SerializeField] private Material onMaterialGreen;
    [SerializeField] private Material originalMaterialGreen;

    public async void buttonPress()
    {
        circuit_status = !circuit_status;
        if (circuit_status)
        {
            await SolveWithManualChanges(null, null);
        }
        else
        {
            TurnOffAllLEDs();
        }
    }

    public async void solve()
    {
        await SolveWithManualChanges(null, null);
    }

    public async Task SolveWithManualChanges(HashSet<GameObject> collidersToClear, Dictionary<GameObject, string> collidersToAdd)
    {
        if (!circuit_status) return;

        await SolveCircuitAsync(collidersToClear, collidersToAdd);
    }


    void PrintArray()
    {
        for (int i = 0; i < Rows; i++)
        {
            string rowString = "";
            for (int j = 0; j < Cols; j++)
            {
                rowString += string.IsNullOrEmpty(legsArray[i, j]) ? "[ ]" : $"[{legsArray[i, j]}]";
            }
            UnityEngine.Debug.Log($"Row {i}: {rowString}");
        }
    }

    private async Task SolveCircuitAsync(HashSet<GameObject> collidersToClear, Dictionary<GameObject, string> collidersToAdd)
    {
        UnityEngine.Debug.Log("Starting Python script with manual data correction...");


        FillLegsArray();

        if (collidersToClear != null)
        {
            foreach (var colObj in collidersToClear)
            {
                if (TryParseColliderName(colObj.name, out int row, out int colIndex))
                {
                    if (row >= 0 && row < Rows && colIndex >= 0 && colIndex < Cols)
                    {
                        legsArray[row, colIndex] = ""; // Force empty
                    }
                }
            }
        }

        if (collidersToAdd != null)
        {
            foreach (var pair in collidersToAdd)
            {
                GameObject colObj = pair.Key;
                string legName = pair.Value;
                if (TryParseColliderName(colObj.name, out int row, out int colIndex))
                {
                    if (row >= 0 && row < Rows && colIndex >= 0 && colIndex < Cols)
                    {
                        legsArray[row, colIndex] = legName; // Force add leg
                    }
                }
            }
        }
        PrintArray(); 

        string json = JsonConvert.SerializeObject(legsArray);
        string tempFile = Path.Combine(Application.dataPath, "board.json");
        File.WriteAllText(tempFile, json);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" \"{tempFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();

            await Task.Run(() => process.WaitForExit());

            string output = await process.StandardOutput.ReadToEndAsync();
            string errors = await process.StandardError.ReadToEndAsync();

            if (!string.IsNullOrEmpty(errors)) UnityEngine.Debug.LogWarning("Python Errors:\n" + errors);
            if (!string.IsNullOrWhiteSpace(output)) UpdateLEDMaterials(output);
        }
    }


    private bool TryParseColliderName(string name, out int row, out int col)
    {
        row = 0;
        col = 0;
        var parts = name.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out row) && int.TryParse(parts[1], out col))
        {
            return true;
        }
        return false;
    }


    void TurnOffAllLEDs()
    {
        GameObject[] leds = GameObject.FindGameObjectsWithTag("led");
        UnityEngine.Debug.Log("Turn Off");

        foreach (GameObject led in leds)
        {
            Renderer rend = led.GetComponent<Renderer>();
            if (rend != null && rend.materials.Length > 0)
            {
                if (led.name.Contains("L1") || led.name.ToLower().Contains("red"))
                    rend.materials[0] = originalMaterialRed;
                else if (led.name.Contains("L2") || led.name.ToLower().Contains("blue"))
                    rend.materials[0] = originalMaterialBlue;
                else if (led.name.Contains("L3") || led.name.ToLower().Contains("green"))
                    rend.materials[0] = originalMaterialGreen;
            }

            Transform childLight = led.transform.Find("P" + led.name);
            if (childLight != null)
            {
                Light lightComp = childLight.GetComponent<Light>();
                if (lightComp != null)
                    lightComp.enabled = false;
            }
        }
    }

    void UpdateLEDMaterials(string jsonOutput)
    {
        TurnOffAllLEDs();
        var ledStates = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonOutput);

        foreach (var kvp in ledStates)
        {
            string ledName = kvp.Key;
            string state = kvp.Value;

            GameObject led = GameObject.Find(ledName);
            if (led == null) continue;

            Renderer rend = led.GetComponent<Renderer>();
            if (rend != null && rend.materials.Length > 0)
            {
                if (state.ToUpper() == "ON")
                {
                    if (led.name.Contains("L1") || led.name.ToLower().Contains("red"))
                        rend.materials[0] = onMaterialRed;
                    else if (led.name.Contains("L2") || led.name.ToLower().Contains("blue"))
                        rend.materials[0] = onMaterialBlue;
                    else if (led.name.Contains("L3") || led.name.ToLower().Contains("green"))
                        rend.materials[0] = onMaterialGreen;
                }
            }

            Transform childLight = led.transform.Find("P" + led.name);
            if (childLight != null)
            {
                Light lightComp = childLight.GetComponent<Light>();
                if (lightComp != null)
                    lightComp.enabled = state.ToUpper() == "ON";
            }
        }
        UnityEngine.Debug.Log("LED materials and lights updated from JSON output.");
    }

    void FillLegsArray()
    {
        legsArray = new string[Rows, Cols];
        GameObject[] colliders = GameObject.FindGameObjectsWithTag("colliders");

        foreach (GameObject colObj in colliders)
        {
            Collider col = colObj.GetComponent<Collider>();
            if (col == null) continue;

            string[] parts = colObj.name.Split('_');
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int colIndex))
            {
                Collider[] hits = Physics.OverlapBox(col.bounds.center, col.bounds.extents);
                string legName = "";
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("legs"))
                    {
                        legName = hit.name;
                        break;
                    }
                }

                if (row >= 0 && row < Rows && colIndex >= 0 && colIndex < Cols)
                {
                    legsArray[row, colIndex] = legName;
                }
            }
        }
    }
}
