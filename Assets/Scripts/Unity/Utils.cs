using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Utils
{
    // Vector3
    public static Vector3 ClampVector3(Vector3 vector, float min, float max)
    {
        return new Vector3(
            Mathf.Clamp(vector.x, min, max),
            Mathf.Clamp(vector.y, min, max),
            Mathf.Clamp(vector.z, min, max)
        );
    }


    // Array related
    public static T[] ConcatenateArray<T>(T[] array1, T[] array2)
    {
        // Create a new container and copy all values
        T[] newArray = new T[array1.Length + array2.Length];
        array1.CopyTo(newArray, 0);
        array2.CopyTo(newArray, array1.Length);
        return newArray;
    }

    public static int ArgMinArray<T>(T[] array)
    {
        if (array.Length == 0)
            return -1;

        // Get smallest value and find the index
        T minValue = array.Min();
        return array.ToList().IndexOf(minValue);
    }

    public static T[][] TransposeArray<T>(T[][] Array2D)
    {
        // Assume all rows have the same length
        // new dimension
        int colLength = Array2D.Length;
        int rowLength = Array2D[0].Length;
        // assign values
        T[][] newArray = new T[rowLength][];
        for (int i = 0; i < rowLength; ++i)
        {
            newArray[i] = new T[colLength];
            for (int j = 0; j < colLength; ++j)
                newArray[i][j] = Array2D[j][i];
        }
        return newArray;
    }

    public static T[] Shuffle<T>(System.Random random, T[] array)
    {
        // Knuth Shuffle
        int n = array.Length;
        while (n > 1)
        {
            int k = random.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
        return array;
    }

    public static T[][] RandomizeRow<T>(System.Random random, T[][] Array2D)
    {
        // Randomize elements in each row
        for (int i = 0; i < Array2D.Length; ++i)
        {
            T[] temp = Shuffle(random, Array2D[i]);
            Array2D[i] = temp;
        }
        return Array2D;
    }

    public static string ArrayToCSVLine<T>(T[] array)
    {
        string line = "";
        // Add value to line
        foreach (T value in array)
        {
            if (value is float || value is int)
                line += string.Format("{0:0.000}", value) + ",";
            else if (value is string)
                line += value + ",";
        }
        // Remove "," in the end
        if (line.Length > 0)
            line.Remove(line.Length - 1);
        return line;
    }


    // GameObject
    public static void SetGameObjectLayer(
        GameObject obj, string name, bool applyToChild = true)
    {
        // game object
        int layer = LayerMask.NameToLayer(name);
        obj.layer = layer;
        // child
        if (applyToChild)
        {
            for (int i = 0; i < obj.transform.childCount; ++i)
            {
                obj.transform.GetChild(i).gameObject.layer = layer;
            }
        }
    }


    // Transform
    // from RUF (Unity) to FLU (regular)
    public static Vector3 ToFLU(Vector3 v)
    {
        return new Vector3(v.z, -v.x, v.y);
    }
    public static Quaternion ToFLU(Quaternion q)
    {
        return new Quaternion(q.z, -q.x, q.y, -q.w);
    }
    // from FLU (regular) to RUF (Unity)
    public static Vector3 FromFLU(Vector3 v)
    {
        return new Vector3(-v.y, v.z, v.x);
    }
    public static Quaternion FromFLU(Quaternion q)
    {
        return new Quaternion(-q.y, q.z, q.x, -q.w);
    }


    // Angles
    public static float WrapToPi(float angle)
    {
        return (angle + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    }
    public static float WrapToTwoPi(float angle)
    {
        return (angle + 2f * Mathf.PI) % (2f * Mathf.PI);
    }
}
