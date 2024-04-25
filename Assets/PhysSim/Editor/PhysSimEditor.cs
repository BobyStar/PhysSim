using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

public static class PhysSimEditor
{
    public static bool isRunning;
    public static bool isQuickSim;

    public static GameObject[] simObjects;

    private static Rigidbody[] sceneRbs;
    private static List<MeshCollider> addedMCols;
    private static List<Rigidbody> addedRbs;
    private static bool[] wereKinematics;

    [MenuItem("GameObject/PhysSim/Quick Simulation", false, 0)]
    public static void StartQuickSim()
    {
        if (isRunning) return;

        Debug.Log("Starting Quick Sim");

        isRunning = true;
        isQuickSim = true;
        simObjects = Selection.gameObjects;

        SetupSelectedSimulation();

        EditorCoroutineUtility.StartCoroutineOwnerless(PhysicsUpdate());
    }

    [MenuItem("GameObject/PhysSim/Quick Simulation", true)]
    public static bool Validate_StartQuickSim()
    {
        return !isRunning && Selection.count > 0;
    }

    private static void SetupSelectedSimulation()
    {
        sceneRbs = Object.FindObjectsOfType<Rigidbody>();

        wereKinematics = new bool[sceneRbs.Length];

        int i = -1;
        foreach (Rigidbody rb in sceneRbs)
        {
            i++;
            wereKinematics[i] = rb.isKinematic;

            if (simObjects.Contains(rb.gameObject)) continue;

            rb.isKinematic = true;
        }

        addedMCols = new List<MeshCollider>();
        addedRbs = new List<Rigidbody>();
        foreach (GameObject gO in simObjects)
        {
            if (!gO.TryGetComponent(out Collider col))
            {
                if (!gO.TryGetComponent(out MeshFilter mFilter))
                    continue;

                addedMCols.Add(gO.AddComponent<MeshCollider>());
                addedMCols[^1].sharedMesh = mFilter.sharedMesh;
                addedMCols[^1].convex = true;
            }

            if (gO.TryGetComponent(out Rigidbody rb))
            {
                addedRbs.Add(rb);
                rb.isKinematic = false;
            }
            else
            {
                addedRbs.Add(gO.AddComponent<Rigidbody>());
            }
        }
    }

    private static void EndSelectedSimulation()
    {
        for (int i = 0; i < sceneRbs.Length; i++)
        {
            sceneRbs[i].isKinematic = wereKinematics[i];
        }

        foreach (MeshCollider mCol in addedMCols)
            Object.DestroyImmediate(mCol);

        foreach (Rigidbody rb in addedRbs)
            Object.DestroyImmediate(rb);
    }

    public static void EndSimulation()
    {
        Debug.Log("Ending Simulation");

        isRunning = false;
        EndSelectedSimulation();
    }

    public static IEnumerator PhysicsUpdate()
    {
        Debug.Log("Starting Simulation.");
        bool autoSim = Physics.autoSimulation;
        Physics.autoSimulation = false;

        yield return null;

        float timer = 10;
        while (timer > 0 && isRunning)
        {
            timer -= isQuickSim ? Time.fixedDeltaTime : Time.unscaledDeltaTime;

            Physics.Simulate(Time.fixedDeltaTime);

            Debug.Log($"Time left: {timer}.");
            yield return isQuickSim ? null : new WaitForSecondsRealtime(Time.fixedDeltaTime);
        }

        Physics.autoSimulation = autoSim;

        if (isRunning)
            EndSimulation();
    }
}