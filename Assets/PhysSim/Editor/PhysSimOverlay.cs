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

            PhysSimEditor.toggleQuickSim = new Toggle { name = "Toggle_QuickSim", label = "Quick Sim" };
            PhysSimEditor.toggleQuickSim.RegisterValueChangedCallback(e => DoSetSimSpeed(e.newValue));
            root.Add(PhysSimEditor.toggleQuickSim);

            // TODO: Change to proper slider label that doesn't make the slider area tiny!
            root.Add(new Label { text = "Radius" });
            PhysSimEditor.sliderRadius = new Slider
            {
                name = "Slider_Radius", lowValue = 0, highValue = 50, value = PhysSimEditor.toolForceRadius,
                showInputField = true,
            };
            PhysSimEditor.sliderRadius.RegisterValueChangedCallback(e => DoSetRadius(e.newValue));
            root.Add(PhysSimEditor.sliderRadius);

            // TODO: Change to proper slider label that doesn't make the slider area tiny!
            root.Add(new Label { text = "Power" });
            PhysSimEditor.sliderPower = new Slider
            {
                name = "Slider_Power", lowValue = 0, highValue = 20, value = PhysSimEditor.toolForcePower,
                showInputField = true
            };
            PhysSimEditor.sliderPower.RegisterValueChangedCallback(e => DoSetPower(e.newValue));
            root.Add(PhysSimEditor.sliderPower);

            Button buttonEndSim = new Button { text = "End Simulation" };
            buttonEndSim.clicked += DoEndSimulation;
            root.Add(buttonEndSim);

            return root;
        }

        public void DoSetSimSpeed(bool isQuickSim)
        {
            PhysSimEditor.isQuickSim = isQuickSim;
        }

        public void DoSetRadius(float radius)
        {
            PhysSimEditor.toolForceRadius = radius;
        }

        public void DoSetPower(float power)
        {
            PhysSimEditor.toolForcePower = power;
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