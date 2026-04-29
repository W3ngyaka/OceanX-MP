using System.Collections.Generic;
using UnityEngine;

public class Drawer : MonoBehaviour
{
    public Material lineMaterial;

    struct Line
    {
        public Vector3 from;
        public Vector3 to;
        public Color color;

        public Line(Vector3 from, Vector3 to, Color color)
        {
            this.from = from;
            this.to = to;
            this.color = color;
        }
    }

    static List<Line> lines = new List<Line>();

    static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");

        Material mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZWrite", 0);

        return mat;
    }

    void Awake()
    {
        if (lineMaterial == null)
            lineMaterial = CreateLineMaterial();
    }

    void OnPostRender()
    {
        if (lineMaterial == null)
            lineMaterial = CreateLineMaterial();

        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);

        foreach (var l in lines)
        {
            GL.Color(l.color);
            GL.Vertex3(l.from.x, l.from.y, l.from.z);
            GL.Vertex3(l.to.x, l.to.y, l.to.z);
        }

        GL.End();
    }

    void FixedUpdate()
    {
        lines.Clear();
    }

    public static void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        lines.Add(new Line(from, to, color));
    }

    public static void DrawRay(Vector3 from, Vector3 direction, Color color)
    {
        lines.Add(new Line(from, from + direction, color));
    }
}
