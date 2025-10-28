using UnityEngine;
using UnityEngine.InputSystem;

public class solve : MonoBehaviour
{
    private const int Rows = 10;
    private const int Cols = 18;
    public string[,] legsArray = new string[Rows, Cols];

    public void getArray()
    {
        FillLegsArray();
        PrintArray();
  
    }

    void FillLegsArray()
    {
        legsArray = new string[Rows, Cols];

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
