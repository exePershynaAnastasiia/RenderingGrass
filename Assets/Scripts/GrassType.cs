using System;
using UnityEngine;

[Serializable]
public class GrassType
{
    public string name;
    public Material material;

    [Min(0)]
    public int count = 100;

    public float width = 0.6f;
    public float height = 1f;

    [Min(1)]
    public int segments = 5;

    public Vector2 heightRange = new Vector2(0.8f, 1.2f);
    public Vector2 widthRange = new Vector2(0.8f, 1.2f);
}