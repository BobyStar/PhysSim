using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
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
        public static bool isSleepBake;

        private static Overlay physSimOverlay;
        public static Toggle toggleQuickSim;

        public static GameObject[] simObjects;

        private static PhysTransformData[] startData;
        private static Rigidbody[] sceneRbs;
        private static List<MeshCollider> addedMCols;
        private static List<Rigidbody> addedRbs;
        private static List<Rigidbody> simRbs;
        private static List<MeshCollider> markedConvexMCols;
        private static bool[] wereKinematics;

        [MenuItem("GameObject/Run PhysSim", false, 0)]
        public static void StartQuickSim()
        {
            if (isRunning) return;

            Debug.Log("Starting PhysSim");

            isRunning = true;
            isQuickSim = true;
            simObjects = Selection.gameObjects;

            SetupSelectedSimulation();

            GetOverlay();

            EditorCoroutineUtility.StartCoroutineOwnerless(PhysicsUpdate());
        }

        [MenuItem("GameObject/Run PhysSim", true)]
        public static bool Validate_StartQuickSim()
        {
            if (isRunning || Selection.count == 0) 
                return false;

            foreach (GameObject gO in Selection.gameObjects)
            {
                if (gO.TryGetComponent(out MeshFilter mFilter) && mFilter.sharedMesh)
                    return true;

                if (gO.TryGetComponent(out Collider _)) return true;
            }

            return false;
        }

        [MenuItem("Tools/PhysSim Bake Rigidbody Initalization")]
        public static void StartRigidbodyInitializationBake()
        {
            if (isRunning) return;
            Debug.Log("Starting Rigidbody Initialization Bake");

            isRunning = true;
            isQuickSim = true;
            isSleepBake = true;

            sceneRbs = Object.FindObjectsOfType<Rigidbody>();
            simObjects = new GameObject[sceneRbs.Length];
            
            for (int i = 0; i < sceneRbs.Length; i++)
                simObjects[i] = sceneRbs[i].gameObject;

            SetupSelectedSimulation();
            GetOverlay();

            EditorCoroutineUtility.StartCoroutineOwnerless(PhysicsUpdate());
        }

        [MenuItem("Tools/PhysSim Bake Rigidbody Initalization", true)]
        public static bool Validate_StartRigidbodyInitializationBake()
        {
            if (isRunning) return false;

            sceneRbs = Object.FindObjectsOfType<Rigidbody>();

            if (sceneRbs == null || sceneRbs.Length == 0) return false;

            foreach (Rigidbody rb in sceneRbs)
                if (!rb.isKinematic) return true;

            return false;
        }

        private static void GetOverlay()
        {
            if (SceneView.lastActiveSceneView)
            {
                physSimOverlay = null;
                if (SceneView.lastActiveSceneView.TryGetOverlay("PhysSim", out physSimOverlay))
                {
                    physSimOverlay.displayed = true;
                    toggleQuickSim.SetValueWithoutNotify(isQuickSim);
                }
                else Debug.Log("Failed to find overlay.");
            }
            else Debug.LogError("No active scene view found! Control Overlay may have issues.");
        }

        private static void StoreSelectedPhysTransformData()
        {
            startData = new PhysTransformData[simObjects.Length];

            for (int i = 0; i < simObjects.Length; i++)
            {
                if (!simObjects[i]) continue;

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
                if (!simObjects[i]) continue;

                endData[i] = new PhysTransformData(
                    simObjects[i].transform.position,
                    simObjects[i].transform.rotation);

                simObjects[i].transform.SetPositionAndRotation(startData[i].position, startData[i].rotation);
            }

            string undoName = $"PhysSim ${simObjects.Length} objects.";
            for (int i = 0; i < simObjects.Length; i++)
            {
                if (!simObjects[i]) continue;

                Undo.RecordObject(simObjects[i].transform, undoName);

                simObjects[i].transform.SetPositionAndRotation(endData[i].position, endData[i].rotation);
            }
        }

        private static void SetupSelectedSimulation()
        {
            StoreSelectedPhysTransformData();

            if (!isSleepBake)
                sceneRbs = Object.FindObjectsOfType<Rigidbody>();

            wereKinematics = new bool[sceneRbs.Length];

            int i = -1;
            foreach (Rigidbody rb in sceneRbs)
            {
                i++;

                if (!rb) continue;

                wereKinematics[i] = rb.isKinematic;

                if (simObjects.Contains(rb.gameObject)) continue;

                rb.isKinematic = true;
            }

            if (!isSleepBake)
            {
                addedMCols = new List<MeshCollider>();
                addedRbs = new List<Rigidbody>();
                simRbs = new List<Rigidbody>();
                markedConvexMCols = new List<MeshCollider>();
                foreach (GameObject gO in simObjects)
                {
                    if (!gO) continue;

                    if (!gO.TryGetComponent(out Collider col))
                    {
                        if (!gO.TryGetComponent(out MeshFilter mFilter))
                            continue;

                        addedMCols.Add(gO.AddComponent<MeshCollider>());
                        addedMCols[^1].sharedMesh = mFilter.sharedMesh;
                        addedMCols[^1].convex = true;
                    }
                    else if (col.TryGetComponent(out MeshCollider mCol))
                    {
                        if (!mCol.convex)
                        {
                            markedConvexMCols.Add(mCol);
                            mCol.convex = true;
                        }
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
            else // isSleepBake
            {
                simRbs = sceneRbs.ToList();
            }
        }

        private static void EndSelectedSimulation()
        {
            for (int i = 0; i < sceneRbs.Length; i++)
            {
                if (!sceneRbs[i]) continue;
                sceneRbs[i].isKinematic = wereKinematics[i];
            }

            foreach (MeshCollider mCol in addedMCols)
                if (mCol) Object.DestroyImmediate(mCol);

            foreach (Rigidbody rb in addedRbs)
                if (rb) Object.DestroyImmediate(rb);

            foreach (MeshCollider mCol in markedConvexMCols)
                if (mCol) mCol.convex = false;
        }

        public static void EndSimulation()
        {
            if (!isRunning) return;
            Debug.Log("Ending PhysSim.");
            isRunning = false;

            if (physSimOverlay != null)
                physSimOverlay.displayed = false;

            if (isSleepBake)
            {
                foreach (Rigidbody rb in sceneRbs)
                {
                    if (rb.IsSleeping())
                        Debug.Log($"{rb.name} slept after the bake.");
                }
            }    
            else EndSelectedSimulation();

            WritePhysTransformDataToUndo();
        }

        public static IEnumerator PhysicsUpdate()
        {
            bool autoSim = Physics.autoSimulation;
            Physics.autoSimulation = false;

            yield return null;

            bool hasSwapped = false;
            while (isRunning)
            {
                Physics.Simulate(Time.fixedDeltaTime);

                if (!hasSwapped)
                {
                    if (!isQuickSim) hasSwapped = true;

                    bool allSleeping = true;
                    foreach (Rigidbody rb in simRbs)
                    {
                        if (!rb.IsSleeping())
                        {
                            allSleeping = false;
                            break;
                        }
                    }

                    if (allSleeping)
                    {
                        isQuickSim = false;
                        hasSwapped = true;
                        if (toggleQuickSim != null)
                            toggleQuickSim.SetValueWithoutNotify(false);

                        if (isSleepBake) break;
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