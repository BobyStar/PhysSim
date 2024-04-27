using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace PhysSim
{
    public static class PhysSimEditor
    {
        public class PhysTransformData
        {
            public Vector3 position;
            public Quaternion rotation;

            public PhysTransformData(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }
        }

        public static bool isRunning;
        public static bool isQuickSim;

        public static GameObject[] simObjects;

        private static PhysTransformData[] startData;
        private static Rigidbody[] sceneRbs;
        private static List<MeshCollider> addedMCols;
        private static List<Rigidbody> addedRbs;
        private static List<Rigidbody> simRbs;
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

        private static void StoreSelectedPhysTransformData()
        {
            startData = new PhysTransformData[simObjects.Length];

            for (int i = 0; i < simObjects.Length; i++)
            {
                startData[i] = new PhysTransformData(
                    simObjects[i].transform.position,
                    simObjects[i].transform.rotation);
            }
        }

        private static void WritePhysTransformDataToUndo()
        {
            PhysTransformData[] endData = new PhysTransformData[simObjects.Length];

            for (int i = 0; i < simObjects.Length; i++)
            {
                endData[i] = new PhysTransformData(
                    simObjects[i].transform.position,
                    simObjects[i].transform.rotation);

                simObjects[i].transform.SetPositionAndRotation(startData[i].position, startData[i].rotation);
            }


            for (int i = 0; i < simObjects.Length; i++)
            {
                Undo.RecordObject(simObjects[i].transform, $"PhysSim {(isQuickSim ? "Quick" : "Regular")} Simulation");

                simObjects[i].transform.SetPositionAndRotation(endData[i].position, endData[i].rotation);
            }
        }

        private static void SetupSelectedSimulation()
        {
            StoreSelectedPhysTransformData();

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
            simRbs = new List<Rigidbody>();
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
                    simRbs.Add(rb);
                    rb.isKinematic = false;
                }
                else
                {
                    addedRbs.Add(gO.AddComponent<Rigidbody>());
                    simRbs.Add(addedRbs[^1]);
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

            WritePhysTransformDataToUndo();
        }

        public static void EndSimulation()
        {
            Debug.Log("Ending Simulation.");

            isRunning = false;
            EndSelectedSimulation();
        }

        public static IEnumerator PhysicsUpdate()
        {
            Debug.Log("Starting Simulation.");
            bool autoSim = Physics.autoSimulation;
            Physics.autoSimulation = false;

            yield return null;

            bool allSleeping = false;
            while (isRunning && !allSleeping)
            {
                Physics.Simulate(Time.fixedDeltaTime);

                allSleeping = true;
                foreach (Rigidbody rb in simRbs)
                {
                    if (!rb.IsSleeping())
                    {
                        allSleeping = false;
                        break;
                    }
                }

                yield return isQuickSim ? null : new WaitForSecondsRealtime(Time.fixedDeltaTime);
            }

            Physics.autoSimulation = autoSim;

            if (isRunning)
                EndSimulation();
        }
    }
}