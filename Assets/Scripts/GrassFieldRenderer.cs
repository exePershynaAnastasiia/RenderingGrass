
using System.Collections.Generic;
using UnityEngine;

public class GrassFieldRenderer : MonoBehaviour
{
    [Header("Vegetation Types")]
    [SerializeField] private GrassType[] grassTypes;

    [Header("Field")]
    [SerializeField] private Vector2 fieldSize = new Vector2(8f, 8f);
    [SerializeField] private int randomSeed = 42;

    private const int BatchSize = 400;

    private class GrassBatch
    {
        public Mesh mesh;
        public Material material;
        public Matrix4x4[] matrices;
    }

    private readonly List<GrassBatch> batches = new List<GrassBatch>();
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();

    private void Start()
    {
        GenerateField();
    }

    private void GenerateField()
    {
        batches.Clear();

        Random.State previousState = Random.state;
        Random.InitState(randomSeed);

        foreach (GrassType type in grassTypes)
        {
            if (type == null || type.material == null || type.count <= 0)
                continue;

            // Generate one shared mesh for each vegetation type
            Mesh mesh = GrassMeshGenerator.Generate(
                type.width,
                type.height,
                type.segments
            );

            generatedMeshes.Add(mesh);

            Matrix4x4[] currentBatch = new Matrix4x4[BatchSize];
            int batchIndex = 0;

            for (int i = 0; i < type.count; i++)
            {
                // Generate a random position within the field
                Vector3 position = transform.position + new Vector3(
                    Random.Range(-fieldSize.x * 0.5f, fieldSize.x * 0.5f),
                    0f,
                    Random.Range(-fieldSize.y * 0.5f, fieldSize.y * 0.5f)
                );

                // Randomly rotate each grass cluster
                Quaternion rotation = Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                );

                // Generate non-uniform scale variation
                float widthScale = Random.Range(
                    type.widthRange.x,
                    type.widthRange.y
                );

                float heightScale = Random.Range(
                    type.heightRange.x,
                    type.heightRange.y
                );

                Vector3 scale = new Vector3(
                    widthScale,
                    heightScale,
                    widthScale
                );

                // Store the instance transformation matrix
                currentBatch[batchIndex] =
                    Matrix4x4.TRS(position, rotation, scale);

                batchIndex++;

                // Save a completed instancing batch
                if (batchIndex == BatchSize)
                {
                    batches.Add(new GrassBatch
                    {
                        mesh = mesh,
                        material = type.material,
                        matrices = currentBatch
                    });

                    currentBatch = new Matrix4x4[BatchSize];
                    batchIndex = 0;
                }
            }

            // Save the final incomplete batch
            if (batchIndex > 0)
            {
                Matrix4x4[] lastBatch = new Matrix4x4[batchIndex];

                System.Array.Copy(
                    currentBatch,
                    lastBatch,
                    batchIndex
                );

                batches.Add(new GrassBatch
                {
                    mesh = mesh,
                    material = type.material,
                    matrices = lastBatch
                });
            }
        }

        Random.state = previousState;
    }

    private void Update()
    {
        foreach (GrassBatch batch in batches)
        {
            RenderParams renderParams = new RenderParams(batch.material)
            {
                // Shared field bounds with extra space for deformation
                worldBounds = new Bounds(
                    transform.position,
                    new Vector3(
                        fieldSize.x + 8f,
                        10f,
                        fieldSize.y + 8f
                    )
                )
            };

            Graphics.RenderMeshInstanced(
                renderParams,
                batch.mesh,
                0,
                batch.matrices
            );
        }
    }

    private void OnDestroy()
    {
        // Release dynamically generated meshes
        foreach (Mesh mesh in generatedMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
