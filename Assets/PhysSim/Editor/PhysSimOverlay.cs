using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "PhysSim Control", true)]
public class PhysSimOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement() { name = "PhysSim" };
        root.Add(new Label() { text = "TEST" });
        return root;
    }
}