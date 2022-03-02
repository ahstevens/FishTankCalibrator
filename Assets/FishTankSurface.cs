using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FishTankSurface : MonoBehaviour
{
    public int screenNumber;
    public Vector3 topLeft;
    public Vector3 topRight;
    public Vector3 bottomRight;
    public Vector3 bottomLeft;

    public FishTankSurface(Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft)
    {
        this.topLeft = topLeft;
        this.topRight = topRight;
        this.bottomRight = bottomRight;
        this.bottomLeft = bottomLeft;
    }
}

// A tiny custom editor for ExampleScript component
[CustomEditor(typeof(FishTankSurface))]
public class FishTankSurfaceVisualizaer : Editor
{
    // Custom in-scene UI for when ExampleScript
    // component is selected.
    public void OnSceneGUI()
    {
        var t = target as FishTankSurface;
        var tr = t.transform;
        var center = ((t.topLeft - t.bottomLeft) + (t.bottomRight - t.bottomLeft)) * 0.5f;
        tr.position = center;
        // display an orange disc where the object is
        var color = new Color(1, 0.8f, 0.4f, 1);
        Handles.color = color;
        Handles.DrawPolyLine(t.topLeft, t.topRight, t.bottomRight, t.bottomLeft, t.topLeft);
        // display object "value" in scene
        GUI.color = color;
        Handles.Label(center, "Screen " + t.screenNumber.ToString());
    }
}