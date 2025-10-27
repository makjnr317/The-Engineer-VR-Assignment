using UnityEngine;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Box : MonoBehaviour
{
    public string pythonPath = @"C:\Users\mkuhu\AppData\Local\Programs\Python\Python312\python.exe";
    public string scriptPath = @"C:\Users\mkuhu\Downloads\solver.py";

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

    void Update()
    {
        //if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        //{
            //circuit_status= !circuit_status;
            //if (circuit_status)
            //{
                //solve();
           // }
           // else
            //{
                //TurnOffAllLEDs();
            //}
       // }

        
    }

    public void solve()
    {
        FillLegsArray();

        string json = JsonConvert.SerializeObject(legsArray);

        RunPythonScript(json);
    }

    void RunPythonScript(string boardJson)
    {
        UnityEngine.Debug.Log("Board JSON: " + boardJson);

        // Save board JSON to temp file
        string tempFile = Path.Combine(Application.dataPath, "board.json");
        File.WriteAllText(tempFile, boardJson);

        // Prepare process
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" \"{tempFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process();
        process.StartInfo = psi;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string errors = process.StandardError.ReadToEnd();

        process.WaitForExit();

        UnityEngine.Debug.Log("Python Output:\n" + output);
        if (!string.IsNullOrEmpty(errors))
        {
            UnityEngine.Debug.LogError("Python Errors:\n" + errors);
        }

        // --- New part: Handle LED material updates ---
        if (!string.IsNullOrWhiteSpace(output))
        {
            try
            {
                UpdateLEDMaterials(output);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Error updating LED materials: " + ex.Message);
            }
        }
    }

    void TurnOffAllLEDs()
    {
        GameObject[] leds = GameObject.FindGameObjectsWithTag("led");

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

            // Turn off child light if exists
            Transform childLight = led.transform.Find("P" + led.name); // e.g., L1 → PL1
            if (childLight != null)
            {
                Light lightComp = childLight.GetComponent<Light>();
                if (lightComp != null)
                    lightComp.enabled = false;
            }
        }
    }

    // Function to update LED materials from JSON
    void UpdateLEDMaterials(string jsonOutput)
    {
        // First, turn off all LEDs
        TurnOffAllLEDs();

        // Parse JSON string into dictionary
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

            // Enable child light if ON
            Transform childLight = led.transform.Find("P" + led.name); // e.g., L1 → PL1
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
        // Clear array
        legsArray = new string[Rows, Cols];

        // Find all collider objects tagged "colliders"
        GameObject[] colliders = GameObject.FindGameObjectsWithTag("colliders");

        foreach (GameObject colObj in colliders)
        {
            Collider col = colObj.GetComponent<Collider>();
            if (col == null)
                continue;

            string[] parts = colObj.name.Split('_');
            if (parts.Length != 2)
                continue;

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
