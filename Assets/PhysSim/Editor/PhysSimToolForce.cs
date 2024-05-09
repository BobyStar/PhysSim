using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor;

namespace PhysSim
{
    [EditorTool("PhysSim Force Tool")]
    public class PhysSimToolForce : EditorTool
    {
        public override void OnActivated()
        {
            if (!PhysSimEditor.isRunning && !Application.isPlaying)
                Debug.Log("The PhysSim Force Tool must have a simulation running to be used!");
        }

        private bool isMouseHeld;
        private bool hittingSimObject;
        private float hitSimObjDepth;

        public override void OnToolGUI(EditorWindow window)
        {
            if (!PhysSimEditor.isRunning && !Application.isPlaying)
                return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                isMouseHeld = true;
            else if ((e.type == EventType.MouseUp && e.button == 0) || e.button != 0)
                isMouseHeld = false;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
            {
                Vector3 scenePos = SceneView.lastActiveSceneView.camera.transform.position;
                Vector3 sceneFwd = SceneView.lastActiveSceneView.camera.transform.forward;

                Vector3 attractPoint = hit.point;
                if (hittingSimObject)
                    attractPoint = scenePos + (sceneFwd * hitSimObjDepth) + Vector3.ProjectOnPlane(attractPoint - scenePos, sceneFwd);

                Handles.color = Color.yellow;
                Handles.DrawWireDisc(attractPoint, -sceneFwd, PhysSimEditor.toolForceRadius);

                hittingSimObject = false;
                
                if (isMouseHeld && e.button == 0)
                {
                    foreach (Collider col in Physics.OverlapSphere(attractPoint, PhysSimEditor.toolForceRadius, -1, QueryTriggerInteraction.Ignore))
                    {
                        if (col.TryGetComponent(out Rigidbody rb))
                        {
                            Vector3 d = rb.position - attractPoint;

                            if (col.transform == hit.transform)
                            {
                                if (!hittingSimObject)
                                {
                                    hittingSimObject = true;
                                    hitSimObjDepth = Vector3.Dot(attractPoint - scenePos, sceneFwd);
                                }

                                d = Vector3.ProjectOnPlane(d, sceneFwd);
                                rb.velocity -= sceneFwd * Vector3.Dot(rb.velocity, sceneFwd);
                            }

                            rb.AddForceAtPosition(Mathf.Clamp01(d.magnitude) * -PhysSimEditor.toolForcePower * d.normalized, col.ClosestPoint(attractPoint));
                        }
                    }
                }
            }
        }
    }
}