using UnityEngine;
using UnityEngine.InputSystem;

public class solve : MonoBehaviour
{
    private const int Rows = 10;
    private const int Cols = 18;
    public string[,] legsArray = new string[Rows, Cols];

    void Update()
    {
        // Run when the spacebar is pressed
        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{
        //    FillLegsArray();
        //    PrintArray();
        //}
    }

    void FillLegsArray()
    {
        // Clear array before refilling
        legsArray = new string[Rows, Cols];

        // Find all objects tagged "colliders"
        GameObject[] colliders = GameObject.FindGameObjectsWithTag("colliders");

        foreach (GameObject colObj in colliders)
        {
            Collider col = colObj.GetComponent<Collider>();
            if (col == null)
                continue;

            // Parse collider name into row and column (e.g., "3_7")
            string[] parts = colObj.name.Split('_');
            if (parts.Length != 2)
                continue;

            if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int colIndex))
            {
                // Find overlapping "legs" objects
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

                // Store leg name if indices are within array bounds
                if (row >= 0 && row < Rows && colIndex >= 0 && colIndex < Cols)
                {
                    legsArray[row, colIndex] = legName;
                }
            }
        }
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
            Debug.Log($"Row {i}: {rowString}");
        }
    }

}
