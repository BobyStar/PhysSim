using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace PhysSim {
    [Overlay(typeof(SceneView), "PhysSim", true)]
    public class PhysSimOverlay : Overlay
    {
        protected override Layout supportedLayouts => Layout.Panel;

        public override void OnCreated()
        {
            displayed = false;
        }

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement() { name = "PhysSim" };

            PhysSimEditor.toggleQuickSim = new Toggle { name = "Toggle_QuickSim",  label = "Quick Sim" };
            PhysSimEditor.toggleQuickSim.RegisterValueChangedCallback(e => DoSetSimSpeed(e.newValue));
            root.Add(PhysSimEditor.toggleQuickSim);

            Button buttonEndSim = new Button { text = "End Simulation" };
            buttonEndSim.clicked += DoEndSimulation;
            root.Add(buttonEndSim);

            return root;
        }

        public void DoSetSimSpeed(bool isQuickSim)
        {
            PhysSimEditor.isQuickSim = isQuickSim;
        }

        public void DoEndSimulation()
        {
            PhysSimEditor.EndSimulation();
        }

        public override void OnWillBeDestroyed()
        {
            PhysSimEditor.toggleQuickSim = null;
        }
    }
}