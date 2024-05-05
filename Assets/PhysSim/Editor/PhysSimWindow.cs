using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PhysSim
{
    public class PhysSimWindow : EditorWindow
    {
        private Vector2 gui_scrollLeft;
        private Vector2 gui_scrollRight;

        public bool[] gui_selectedLeft;
        public string[] gui_namesLeft;

        public bool[] gui_selectedRight;
        public string[] gui_namesRight;

        public void CreateGUI()
        {
            minSize = new Vector2(512, 152);
        }

        public void OnGUI()
        {
            GUIStyle centeredLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle windows = new GUIStyle(GUI.skin.window) { padding = GUI.skin.box.padding };

            GUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * .45f));
            EditorGUILayout.LabelField("Fell Asleep", centeredLabel);
            gui_scrollLeft = EditorGUILayout.BeginScrollView(gui_scrollLeft, windows);

            for (int i = 0; i < gui_selectedLeft.Length; i++)
            {
                gui_selectedLeft[i] = EditorGUILayout.ToggleLeft(gui_namesLeft[i], gui_selectedLeft[i]);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * .45f));
            EditorGUILayout.LabelField("Stayed Awake", centeredLabel);
            gui_scrollRight = EditorGUILayout.BeginScrollView(gui_scrollRight, windows);

            for (int i = 0; i < gui_selectedRight.Length; i++)
            {
                gui_selectedRight[i] = EditorGUILayout.ToggleLeft(gui_namesRight[i], gui_selectedRight[i]);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Rigidbodies checked will be marked asleep and kept in their positions after the simulation.\n" +
                "Rigidbodies unchecked will be kept at their settings and be reverted to their starting positions before the simulation.",
                MessageType.None);
            EditorGUILayout.Space();
            if (GUILayout.Button("Mark Asleep and Revert Others"))
            {
                PhysSimEditor.MarkSleepingAndRevertOthers(gui_selectedLeft, gui_selectedRight);
                Close();
            }
        }

        private void OnDestroy()
        {
            if (PhysSimEditor.isSleepBake)
                PhysSimEditor.isSleepBake = false;
        }
    }
}