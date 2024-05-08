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
            if (!PhysSimEditor.isRunning)
                Debug.Log("The PhysSim Force Tool must have a simulation running to be used!");
        }

        private bool isMouseHeld;

        public override void OnToolGUI(EditorWindow window)
        {
            //EditorGUIUtility.AddCursorRect(window.position, MouseCursor.Zoom);

            if (!PhysSimEditor.isRunning)
                return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            //Debug.Log(Event.current.mousePosition);
            
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                isMouseHeld = true;
            else if (e.type == EventType.MouseUp && e.button == 0 || e.button != 0)
                isMouseHeld = false;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, -1, QueryTriggerInteraction.Ignore))
            {
                Vector3 sceneFwd = SceneView.lastActiveSceneView.camera.transform.forward;
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hit.point, -sceneFwd, 1);

                if (isMouseHeld && e.button == 0)
                {
                    foreach (Collider col in Physics.OverlapSphere(hit.point, 1, -1, QueryTriggerInteraction.Ignore))
                    {
                        if (col.TryGetComponent(out Rigidbody rb))
                        {
                            Vector3 d = rb.position - hit.point;
                            if (col.transform == hit.transform)
                            {
                                d = Vector3.ProjectOnPlane(d, sceneFwd);
                                rb.velocity -= sceneFwd * Vector3.Dot(rb.velocity, sceneFwd);
                            }
                            Debug.DrawRay(hit.point, d, Color.Lerp(Color.yellow, Color.clear, Mathf.Clamp01(d.magnitude)), 0, true);

                            
                            rb.AddForceAtPosition(Mathf.Clamp01(d.magnitude) * -10 * d.normalized, col.ClosestPoint(hit.point));
                        }
                    }
                }
            }
        }
    }
}