using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Overlays;
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

        private static bool setupEndBeforeRecompile;
        public static bool isRunning;
        public static bool isQuickSim;
        public static bool isSleepBake;

        public static float toolForceRadius = 1;
        public static float toolForcePower = 10;

        private static Overlay physSimOverlay;
        public static Toggle toggleQuickSim;
        public static Slider sliderRadius;
        public static Slider sliderPower;

        public static GameObject[] simObjects;

        private static Dictionary<GameObject,PhysTransformData> startData;
        private static Rigidbody[] sceneRbs;
        private static List<MeshCollider> addedMCols;
        private static List<Rigidbody> addedRbs;
        private static List<Rigidbody> simRbs;
        private static List<MeshCollider> markedConvexMCols;
        private static bool[] wereKinematics;

        private static List<Rigidbody> sleptRbs;
        private static List<Rigidbody> awakeRbs;

        [MenuItem("GameObject/Run PhysSim", false, 0)]
        public static void StartQuickSim()
        {
            if (isRunning) return;

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
            if (isRunning || isSleepBake || Selection.count == 0)
                return false;

            foreach (GameObject gO in Selection.gameObjects)
            {
                if (gO.TryGetComponent(out MeshFilter mFilter) && mFilter.sharedMesh)
                    return true;

                if (gO.TryGetComponent(out Collider _)) return true;
            }

            return false;
        }

        [MenuItem("Tools/PhysSim/Bake Rigidbody Initalization")]
        public static void StartRigidbodyInitializationBake()
        {
            if (isRunning) return;

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

        [MenuItem("Tools/PhysSim/Bake Rigidbody Initalization", true)]
        public static bool Validate_StartRigidbodyInitializationBake()
        {
            if (isRunning || isSleepBake) return false;

            sceneRbs = Object.FindObjectsOfType<Rigidbody>();

            if (sceneRbs == null || sceneRbs.Length == 0) return false;

            foreach (Rigidbody rb in sceneRbs)
                if (!rb.isKinematic) return true;

            return false;
        }

        [MenuItem("Tools/PhysSim/Wake Up Scene Rigidbodies")]
        public static void WakeUpRigidbodiesInScene()
        {
            if (!EditorUtility.DisplayDialog("PhysSim Wake Up All Rigidbodies",
                "Are you sure you want to wake up all Rigidbodies in the scene?\n\n" +
                "This cannot be undone.", "Wake Up", "Cancel"))
            { return; }

            int count = 0;
            int sleeping = 0;
            foreach (Rigidbody rb in Object.FindObjectsOfType<Rigidbody>())
            {
                if (rb.IsSleeping())
                {
                    sleeping++;
                    rb.WakeUp();
                }
                count++;
            }

            if (sleeping > 0)
                Debug.Log($"Woke up {sleeping} out of {count} rigidbodies in the Scene.");
            else
                Debug.Log("All rigidbodies in Scene are already awake!");
        }

        [MenuItem("Tools/PhysSim/Wake Up Scene Rigidbodies", true)]
        public static bool Validate_WakeUpRigidbodiesInScene()
        {
            return Object.FindObjectsOfType<Rigidbody>().Length > 0;
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
                else Debug.LogWarning("Failed to find overlay.");
            }
            else Debug.LogError("No active scene view found! Control Overlay may have issues.");
        }

        private static void StoreSelectedPhysTransformData()
        {
            startData = new Dictionary<GameObject, PhysTransformData>();

            for (int i = 0; i < simObjects.Length; i++)
            {
                if (!simObjects[i]) continue;

                startData.Add(simObjects[i], new PhysTransformData(
                    simObjects[i].transform.position,
                    simObjects[i].transform.rotation));
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

                simObjects[i].transform.SetPositionAndRotation(startData[simObjects[i]].position, startData[simObjects[i]].rotation);
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
            UnityEditor.EditorTools.ToolManager.SetActiveTool(typeof(PhysSimToolForce));
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
            Physics.autoSimulation = true;
            isRunning = false;

            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(PhysSimToolForce))
                UnityEditor.EditorTools.ToolManager.RestorePreviousPersistentTool();

            if (physSimOverlay != null)
                physSimOverlay.displayed = false;

            if (isSleepBake)
            {
                OpenSleepWindow();
            }    
            else EndSelectedSimulation();

            foreach (Rigidbody rb in simRbs)
            {
                if (rb)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            WritePhysTransformDataToUndo();
        }

        private static void OpenSleepWindow()
        {
            PhysSimWindow window = EditorWindow.GetWindow<PhysSimWindow>();
            window.titleContent = new GUIContent("PhysSim Bake");

            sleptRbs = new List<Rigidbody>();
            awakeRbs = new List<Rigidbody>();

            foreach (Rigidbody rb in simRbs)
            {
                if (rb.IsSleeping())
                    sleptRbs.Add(rb);
                else
                    awakeRbs.Add(rb);
            }

            window.gui_selectedLeft = new bool[sleptRbs.Count];
            window.gui_namesLeft = new string[sleptRbs.Count];
            window.gui_selectedRight = new bool[awakeRbs.Count];
            window.gui_namesRight = new string[awakeRbs.Count];

            for (int i = 0; i < window.gui_namesLeft.Length; i++)
            {
                window.gui_namesLeft[i] = sleptRbs[i].name;
                window.gui_selectedLeft[i] = true;
            }

            for (int i = 0; i < window.gui_namesRight.Length; i++)
            {
                window.gui_namesRight[i] = awakeRbs[i].name;
                window.gui_selectedRight[i] = false;
            }
        }

        public static void MarkSleepingAndRevertOthers(bool[] markedSleep, bool[] markedAwake)
        {
            if (!isSleepBake)
                return;

            if (markedSleep.Length != sleptRbs.Count || markedAwake.Length != awakeRbs.Count)
                return;

            for (int i = 0; i < markedSleep.Length; i++)
            {
                if (!sleptRbs[i]) return;
                if (markedSleep[i])
                    sleptRbs[i].Sleep();
                else
                {
                    PhysTransformData ptData = startData[sleptRbs[i].gameObject];
                    sleptRbs[i].transform.SetPositionAndRotation(ptData.position, ptData.rotation);
                }
            }

            for (int i = 0; i < markedAwake.Length; i++)
            {
                if (!awakeRbs[i]) return;
                if (markedAwake[i])
                    awakeRbs[i].Sleep();
                else
                {
                    PhysTransformData ptData = startData[awakeRbs[i].gameObject];
                    awakeRbs[i].transform.SetPositionAndRotation(ptData.position, ptData.rotation);
                }
            }
        }

        public static IEnumerator PhysicsUpdate()
        {
            Physics.autoSimulation = false;

            if (!setupEndBeforeRecompile)
            {
                setupEndBeforeRecompile = true;
                AssemblyReloadEvents.beforeAssemblyReload += EndSimulation;

                EditorApplication.playModeStateChanged += PlayModeStateChanged;
            }

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

            Physics.autoSimulation = true;

            if (isRunning)
                EndSimulation();
        }

        private static void PlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && isRunning)
            {
                EditorApplication.ExitPlaymode();
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent("You need to end the current PhysSim simulation before switching to Play Mode!"));
            }
        }
    }
}