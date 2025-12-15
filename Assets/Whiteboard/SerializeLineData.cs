using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LineData
{
    public Vector3[] points;
    public Color color;
    public float width;
    public AnimationCurve widthCurve; 
}

[System.Serializable]
public class LineCollection
{
    public List<LineData> lines = new List<LineData>();
}
