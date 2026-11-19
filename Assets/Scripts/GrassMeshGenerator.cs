
using UnityEngine;
using System.Collections.Generic;

public static class GrassMeshGenerator
{
    public static Mesh Generate(
        float width = 0.6f,
        float height = 1f,
        int segments = 5)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Ensure at least one segment per quad
        segments = Mathf.Max(1, segments);

        // Generate three intersecting grass planes
        for (int i = 0; i < 3; i++)
        {
            // Rotate each plane by 60 degrees around the Y axis
            float angle = i * Mathf.PI / 3f;

            // Calculate the horizontal direction using sine and cosine
            Vector3 direction = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            );

            // Calculate the left and right edges of the plane
            Vector3 left = -direction * width * 0.5f;
            Vector3 right = direction * width * 0.5f;

            // Store the starting vertex index for the current plane
            int start = vertices.Count;

            // Generate pairs of vertices along the height of the plane
            for (int j = 0; j <= segments; j++)
            {
                // Normalized height from 0 (bottom) to 1 (top)
                float t = (float)j / segments;
                float y = t * height;

                // Add left and right vertices at the current height
                vertices.Add(left + Vector3.up * y);
                vertices.Add(right + Vector3.up * y);

                // Assign UV coordinates for texture mapping and wind animation
                uvs.Add(new Vector2(0, t));
                uvs.Add(new Vector2(1, t));

                // Use upward-facing normals for consistent grass lighting
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
            }

            // Connect consecutive vertex rows with two triangles
            for (int j = 0; j < segments; j++)
            {
                int bottomLeft = start + j * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = bottomLeft + 3;

                // First triangle
                triangles.Add(bottomLeft);
                triangles.Add(topLeft);
                triangles.Add(bottomRight);

                // Second triangle
                triangles.Add(bottomRight);
                triangles.Add(topLeft);
                triangles.Add(topRight);
            }
        }

        // Create the final grass mesh
        Mesh mesh = new Mesh();
        mesh.name = "Segmented Grass Cluster";

        // Assign generated geometry data
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);

        // Calculate mesh bounds for visibility checks
        mesh.RecalculateBounds();

        return mesh;
    }
}
