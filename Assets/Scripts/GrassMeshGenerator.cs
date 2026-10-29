
using UnityEngine;
using System.Collections.Generic;

public static class GrassMeshGenerator
// Creates a procedural grass mesh consisting of three intersecting quads
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

            // because of the 3 quads, we need to rotate them by 60 degrees each time
            // sin and cos are used for coords because of 1 unit circle (check notes)

            Vector3 left = -direction * width * 0.5f;
            Vector3 right = direction * width * 0.5f;

            int start = vertices.Count;

            vertices.Add(left);
            vertices.Add(right);
            vertices.Add(left + Vector3.up * height);
            vertices.Add(right + Vector3.up * height);

            uvs.Add(new Vector2(0, 0)); // bottom left
            uvs.Add(new Vector2(1, 0)); // bottom right
            uvs.Add(new Vector2(0, 1)); // top left
            uvs.Add(new Vector2(1, 1)); // top right
            //created quad with vertices 0, 1, 2, 3

            for (int j = 0; j < 4; j++)
                normals.Add(Vector3.up);

            // triangle one with vertices 0, 2, 1
            // 0, 2, 1 since we want to make sure the triangles are facing the right way (clockwise)
            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 1);

            // triangle two with vertices 1, 2, 3
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
