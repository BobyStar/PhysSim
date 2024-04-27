using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "PhysSim Control", true)]
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
        root.Add(new Label() { text = "TEST" });
        return root;
    }
}