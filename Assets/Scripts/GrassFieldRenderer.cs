
using System.Collections.Generic;
using UnityEngine;

public class GrassFieldRenderer : MonoBehaviour
{
    [Header("Grass")]
    [SerializeField] private Material grassMaterial;
    [SerializeField, Range(1, 10000)] private int grassCount = 1000;

    [Header("Field")]
    [SerializeField] private Vector2 fieldSize = new Vector2(8f, 8f);
    [SerializeField] private int randomSeed = 42;

    [Header("Variation")]
    [SerializeField] private Vector2 heightRange = new Vector2(0.6f, 1.2f);
    [SerializeField] private Vector2 widthRange = new Vector2(0.7f, 1.3f);

    private Mesh grassMesh;
    private readonly List<Matrix4x4[]> batches = new List<Matrix4x4[]>();

    private const int BatchSize = 400;

    private void Start()
    {
        grassMesh = GrassMeshGenerator.Generate();

        GenerateField();
    }

    private void GenerateField()
    {
        batches.Clear();

        Random.State previousState = Random.state;
        Random.InitState(randomSeed);

        Matrix4x4[] currentBatch = new Matrix4x4[BatchSize];
        int batchIndex = 0;

        for (int i = 0; i < grassCount; i++)
        {
            Vector3 position = transform.position + new Vector3(
                Random.Range(-fieldSize.x * 0.5f, fieldSize.x * 0.5f),
                0f,
                Random.Range(-fieldSize.y * 0.5f, fieldSize.y * 0.5f)
            );

            Quaternion rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );

            Vector3 scale = new Vector3(
                Random.Range(widthRange.x, widthRange.y),
                Random.Range(heightRange.x, heightRange.y),
                1f
            );

            currentBatch[batchIndex] =
                Matrix4x4.TRS(position, rotation, scale);

            batchIndex++;

            if (batchIndex == BatchSize)
            {
                batches.Add(currentBatch);
                currentBatch = new Matrix4x4[BatchSize];
                batchIndex = 0;
            }
        }

        if (batchIndex > 0)
        {
            Matrix4x4[] lastBatch = new Matrix4x4[batchIndex];
            System.Array.Copy(currentBatch, lastBatch, batchIndex);
            batches.Add(lastBatch);
        }

        Random.state = previousState;
    }

    private void Update()
    {
        if (grassMaterial == null || grassMesh == null)
            return;

        RenderParams renderParams = new RenderParams(grassMaterial)
        {
            worldBounds = new Bounds(
                transform.position,
                new Vector3(fieldSize.x + 4f, 5f, fieldSize.y + 4f)
            )
        };

        foreach (Matrix4x4[] batch in batches)
        {
            Graphics.RenderMeshInstanced(
                renderParams,
                grassMesh,
                0,
                batch
            );
        }
    }

    private void OnDestroy()
    {
        if (grassMesh != null)
            Destroy(grassMesh);
    }
}
