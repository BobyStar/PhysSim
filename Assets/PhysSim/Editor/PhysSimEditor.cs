using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class PhysSimEditor
{
    [MenuItem("GameObject/PhysSim/Quick Simulation", false, 0)]
    public static void StartQuickSim()
    {
        Debug.Log($"Starting Quick Sim with {Selection.count} objects.");
    }

    [MenuItem("GameObject/PhysSim/Quick Simulation", true)]
    public static bool Validate_StartQuickSim()
    {
        return Selection.count > 0;
    }
}