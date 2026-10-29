
using UnityEngine;
using System.Collections.Generic;

public static class GrassMeshGenerator
{
    public static Mesh Generate(float width = 0.6f, float height = 1f)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI / 3f;

            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            );

            Vector3 left = -direction * width * 0.5f;
            Vector3 right = direction * width * 0.5f;

            int start = vertices.Count;

            vertices.Add(left);
            vertices.Add(right);
            vertices.Add(left + Vector3.up * height);
            vertices.Add(right + Vector3.up * height);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            for (int j = 0; j < 4; j++)
                normals.Add(Vector3.up);

            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 1);

            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Grass Cluster";

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateBounds();

        return mesh;
    }
}
