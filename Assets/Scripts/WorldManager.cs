/**
 * WorldManager is responsible for keeping track of 
 * the current state of the world at large, generating
 * new worlds, and applying any changes to the world.
 * 
 * @author Garrett Bowers
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WorldManager : MonoBehaviour
{
    public bool GenerateWorld;
    public bool GenerateNeighbors;
    [Min(-1)]
    public int PropagationDepth;
    [Min(1)]
    public int RetryCount;

    [Min(1)]
    public int Width = 1;
    [Min(1)]
    public int Depth = 1;
    [Min(1)]
    public int Height = 1;

    public List<Prototype> Prototypes;

    public delegate void GenerationFinishedEvent();
    public event GenerationFinishedEvent GenerationFinished;

    private Prototype[,,] _generatedWorldData;
    private bool _generating = false;
    private MeshFilter _meshFilter;

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        #region Neighbor Generation
        /** TODO: This block of code allows for neighbor lists to
         * be pre-generated and saved to the scriptable object
         * file. Comment out or remove once done.
         * 
         * Note: This saves the neighbor data to the prototype only
         * when running in the Unity editor.
         */
        if (GenerateNeighbors)
        {
            // Build Prototype neighbor lists
            foreach (Prototype prototype in Prototypes)
            {

                // Clear lists
                prototype.posXNeighbors.Clear();
                prototype.negXNeighbors.Clear();
                prototype.posZNeighbors.Clear();
                prototype.negZNeighbors.Clear();
                prototype.posYNeighbors.Clear();
                prototype.negYNeighbors.Clear();

                foreach (Prototype comparer in Prototypes)
                {
                    // Positive X to Negative X
                    if (prototype.posX.Contains("F")) // if this prototype is flipped
                    {
                        if (prototype.posX.Equals(comparer.negX + "F"))
                            prototype.posXNeighbors.Add(comparer);
                    }
                    else if (prototype.posX.Contains("S") || prototype.posX.Contains("-")) // if this prototype is symmetrical
                    {
                        if (prototype.posX.Equals(comparer.negX))
                            prototype.posXNeighbors.Add(comparer);
                    }
                    else
                    {
                        if ((prototype.posX + "F").Equals(comparer.negX))
                            prototype.posXNeighbors.Add(comparer);
                    }

                    // Negative X to Positive X
                    if (prototype.negX.Contains("F")) // if this prototype is flipped
                    {
                        if (prototype.negX.Equals(comparer.posX + "F"))
                            prototype.negXNeighbors.Add(comparer);
                    }
                    else if (prototype.negX.Contains("S") || prototype.negX.Contains("-")) // if this prototype is symmetrical
                    {
                        if (prototype.negX.Equals(comparer.posX))
                            prototype.negXNeighbors.Add(comparer);
                    }
                    else
                    {
                        if ((prototype.negX + "F").Equals(comparer.posX))
                            prototype.negXNeighbors.Add(comparer);
                    }

                    // Positive Z to Negative Z
                    if (prototype.posZ.Contains("F")) // if this prototype is flipped
                    {
                        if (prototype.posZ.Equals(comparer.negZ + "F"))
                            prototype.posZNeighbors.Add(comparer);
                    }
                    else if (prototype.posZ.Contains("S") || prototype.posZ.Contains("-")) // if this prototype is symmetrical
                    {
                        if (prototype.posZ.Equals(comparer.negZ))
                            prototype.posZNeighbors.Add(comparer);
                    }
                    else
                    {
                        if ((prototype.posZ + "F").Equals(comparer.negZ))
                            prototype.posZNeighbors.Add(comparer);
                    }

                    // Negative Z to Positive Z
                    if (prototype.negZ.Contains("F")) // if this prototype is flipped
                    {
                        if (prototype.negZ.Equals(comparer.posZ + "F"))
                            prototype.negZNeighbors.Add(comparer);
                    }
                    else if (prototype.negZ.Contains("S") || prototype.negZ.Contains("-")) // if this prototype is symmetrical
                    {
                        if (prototype.negZ.Equals(comparer.posZ))
                            prototype.negZNeighbors.Add(comparer);
                    }
                    else
                    {
                        if ((prototype.negZ + "F").Equals(comparer.posZ))
                            prototype.negZNeighbors.Add(comparer);
                    }

                    // Positive Y to Negative Y
                    if (prototype.posY.Equals(comparer.negY))
                        prototype.posYNeighbors.Add(comparer);

                    // Negative Y to Positive Y
                    if (prototype.negY.Equals(comparer.posY))
                        prototype.negYNeighbors.Add(comparer);
                }
            }
        }
        #endregion
    }

    // Start is called before the first frame update
    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (GenerateWorld)
            StartCoroutine(GenerateTerrain());
        else
            GenerateFlatTerrain();
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region generation methods
    private void GenerateFlatTerrain()
    {
        transform.position = new Vector3((float)Width / 2, (float)Height / 2, (float)Depth / 2);

        Mesh flat = Prototypes.FirstOrDefault(prototype => prototype.description.Equals("Flat")).model;
        Mesh edgeDown = Prototypes.FirstOrDefault(prototype => prototype.description.Equals("Edge_Down_0")).model;
        Mesh cornerEdgeDown = Prototypes.FirstOrDefault(prototype => prototype.description.Equals("Corner_Edge_Down_0")).model;
        Mesh vertical = Prototypes.FirstOrDefault(prototype => prototype.description.Equals("Vertical_0")).model;
        Mesh verticalCorner = Prototypes.FirstOrDefault(prototype => prototype.description.Equals("Vertical_Outer_Corner_0")).model;

        CombineInstance[] worldData = new CombineInstance[Width * Depth * Height];
        for (int i = 0; i < worldData.Length; i++)
        {
            worldData[i].mesh = new();
        }

        // Fill top center cells with flat
        for (int x = 1; x < Width - 1; x++)
        {
            for (int z = 1; z < Depth - 1; z++)
            {
                worldData[x + (Height - 1) * Width + z * Width * Height].mesh = flat;
                Matrix4x4 mat = Matrix4x4.identity;
                mat.SetTRS(new Vector3(x - ((float)Width / 2), ((float)Height - 1) / 2, z - ((float)Depth / 2)), Quaternion.Euler(0, 0, 0), Vector3.one);
                worldData[x + (Height - 1) * Width + z * Width * Height].transform = mat;
            }
        }

        // Fill top z edges
        for (int x = 1; x < Width - 1; x++)
        {
            // Max Z
            worldData[x + (Height - 1) * Width + (Depth - 1) * Width * Height].mesh = edgeDown;
            Matrix4x4 matMax = Matrix4x4.identity;
            matMax.SetTRS(new Vector3(x - ((float)Width / 2), ((float)Height - 1) / 2, ((float)Depth - 1) / 2), Quaternion.Euler(0, 0, 0), Vector3.one);
            worldData[x + (Height - 1) * Width + (Depth - 1) * Width * Height].transform = matMax;

            // Min Z
            worldData[x + (Height - 1) * Width].mesh = edgeDown;
            Matrix4x4 matMin = Matrix4x4.identity;
            matMin.SetTRS(new Vector3(x - ((float)Width / 2), ((float)Height - 1) / 2, 0 - ((float)Depth / 2)), Quaternion.Euler(0, 180, 0), Vector3.one);
            worldData[x + (Height - 1) * Width].transform = matMin;
        }
        worldData[(Width - 1) + (Height - 1) * Width + (Depth - 1) * Width * Height].mesh = cornerEdgeDown;
        worldData[(Height - 1) * Width].mesh = cornerEdgeDown;
        {
            Matrix4x4 matMax = Matrix4x4.identity;
            matMax.SetTRS(new Vector3(((float)Width - 1) / 2, ((float)Height - 1) / 2, ((float)Depth - 1) / 2), Quaternion.Euler(0, 0, 0), Vector3.one);
            worldData[(Width - 1) + (Height - 1) * Width + (Depth - 1) * Width * Height].transform = matMax;

            Matrix4x4 matMin = Matrix4x4.identity;
            matMin.SetTRS(new Vector3(0 - ((float)Width / 2), ((float)Height - 1) / 2, 0 - ((float)Depth / 2)), Quaternion.Euler(0, 180, 0), Vector3.one);
            worldData[(Height - 1) * Width].transform = matMin;
        }

        // Fill top x edges
        for (int z = 1; z < Depth - 1; z++)
        {
            // Max X
            worldData[(Width - 1) + (Height - 1) * Width + z * Width * Height].mesh = edgeDown;
            Matrix4x4 matMax = Matrix4x4.identity;
            matMax.SetTRS(new Vector3(((float)Width - 1) / 2, ((float)Height - 1) / 2, z - ((float)Depth / 2)), Quaternion.Euler(0, 90, 0), Vector3.one);
            worldData[(Width - 1) + (Height - 1) * Width + z * Width * Height].transform = matMax;

            // Min X
            worldData[(Height - 1) * Width + z * Width * Height].mesh = edgeDown;
            Matrix4x4 matMin = Matrix4x4.identity;
            matMin.SetTRS(new Vector3(0 - ((float)Width / 2), ((float)Height - 1) / 2, z - ((float)Depth / 2)), Quaternion.Euler(0, 270, 0), Vector3.one);
            worldData[(Height - 1) * Width + z * Width * Height].transform = matMin;
        }
        worldData[(Width - 1) + (Height - 1) * Width].mesh = cornerEdgeDown;
        worldData[(Height - 1) * Width + (Depth - 1) * Width * Height].mesh = cornerEdgeDown;
        {
            Matrix4x4 matMax = Matrix4x4.identity;
            matMax.SetTRS(new Vector3(((float)Width - 1) / 2, ((float)Height - 1) / 2, 0 - ((float)Depth / 2)), Quaternion.Euler(0, 90, 0), Vector3.one);
            worldData[(Width - 1) + (Height - 1) * Width].transform = matMax;

            Matrix4x4 matMin = Matrix4x4.identity;
            matMin.SetTRS(new Vector3(0 - ((float)Width / 2), ((float)Height - 1) / 2, ((float)Depth - 1) / 2), Quaternion.Euler(0, 270, 0), Vector3.one);
            worldData[(Height - 1) * Width + (Depth - 1) * Width * Height].transform = matMin;
        }

        // Fill Y
        for (int y = Height - 2; y >= 0; y--)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                worldData[x + y * Width + (Depth - 1) * Width * Height].mesh = vertical;
                Matrix4x4 matMax = Matrix4x4.identity;
                matMax.SetTRS(new Vector3(x - ((float)Width / 2), y - ((float)Height / 2), ((float)Depth - 1) / 2), Quaternion.Euler(0, 0, 0), Vector3.one);
                worldData[x + y * Width + (Depth - 1) * Width * Height].transform = matMax;

                worldData[x + y * Width].mesh = vertical;
                Matrix4x4 matMin = Matrix4x4.identity;
                matMin.SetTRS(new Vector3(x - ((float)Width / 2), y - ((float)Height / 2), 0 - ((float)Depth / 2)), Quaternion.Euler(0, 180, 0), Vector3.one);
                worldData[x + y * Width].transform = matMin;
            }
            worldData[(Width - 1) + y * Width + (Depth - 1) * Width * Height].mesh = verticalCorner;
            worldData[y * Width].mesh = verticalCorner;
            {
                Matrix4x4 matMax = Matrix4x4.identity;
                matMax.SetTRS(new Vector3(((float)Width - 1) / 2, y - ((float)Height / 2), ((float)Depth - 1) / 2), Quaternion.Euler(0, 0, 0), Vector3.one);
                worldData[(Width - 1) + y * Width + (Depth - 1) * Width * Height].transform = matMax;

                Matrix4x4 matMin = Matrix4x4.identity;
                matMin.SetTRS(new Vector3(0 - ((float)Width / 2), y - ((float)Height / 2), 0 - ((float)Depth / 2)), Quaternion.Euler(0, 180, 0), Vector3.one);
                worldData[y * Width].transform = matMin;
            }

            for (int z = 1; z < Depth - 1; z++)
            {
                worldData[(Width - 1) + y * Width + z * Width * Height].mesh = vertical;
                Matrix4x4 matMax = Matrix4x4.identity;
                matMax.SetTRS(new Vector3(((float)Width - 1) / 2, y - ((float)Height / 2), z - ((float)Depth / 2)), Quaternion.Euler(0, 90, 0), Vector3.one);
                worldData[(Width - 1) + y * Width + z * Width * Height].transform = matMax;

                worldData[y * Width + z * Width * Height].mesh = vertical;
                Matrix4x4 matMin = Matrix4x4.identity;
                matMin.SetTRS(new Vector3(0 - ((float)Width / 2), y - ((float)Height / 2), z - ((float)Depth / 2)), Quaternion.Euler(0, 270, 0), Vector3.one);
                worldData[y * Width + z * Width * Height].transform = matMin;
            }
            worldData[(Width - 1) + y * Width].mesh = verticalCorner;
            worldData[y * Width + (Depth - 1) * Width * Height].mesh = verticalCorner;
            {
                Matrix4x4 matMax = Matrix4x4.identity;
                matMax.SetTRS(new Vector3(((float)Width - 1) / 2, y - ((float)Height / 2), 0 - ((float)Depth / 2)), Quaternion.Euler(0, 90, 0), Vector3.one);
                worldData[(Width - 1) + y * Width].transform = matMax;

                Matrix4x4 matMin = Matrix4x4.identity;
                matMin.SetTRS(new Vector3(0 - ((float)Width / 2), y - ((float)Height / 2), ((float)Depth - 1) / 2), Quaternion.Euler(0, 270, 0), Vector3.one);
                worldData[y * Width + (Depth - 1) * Width * Height].transform = matMin;
            }
        }

        _meshFilter.mesh = new Mesh();
        _meshFilter.mesh.CombineMeshes(worldData);

        if (GenerationFinished != null) GenerationFinished(); 
    }

    private IEnumerator GenerateTerrain()
    {
        transform.position = new Vector3((float)Width / 2, (float)Height / 2, (float)Depth / 2);
        _generatedWorldData = new Prototype[Width, Depth, Height];

        // Wave Function Collapse
        StartCoroutine(ExecuteWaveFunctionCollapse());

        while (_generating) {
            // Mesh Generation
            CombineInstance[] combineList = new CombineInstance[Width * Depth * Height];
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_generatedWorldData[x, z, y] != null && _generatedWorldData[x, z, y].model != null)
                        {
                            combineList[x + y * Width + z * Width * Height].mesh = _generatedWorldData[x, z, y].model;
                            Matrix4x4 mat = Matrix4x4.identity;
                            mat.SetTRS(new Vector3(x - ((float)Width / 2), y - ((float)Height / 2), z - ((float)Depth / 2)), Quaternion.Euler(0, 90 * _generatedWorldData[x, z, y].rotation, 0), Vector3.one);
                            combineList[x + y * Width + z * Width * Height].transform = mat;
                        }
                        else
                            combineList[x + y * Width + z * Width * Height].mesh = new Mesh();
                    }
                }
            }
            _meshFilter.mesh = new Mesh();
            _meshFilter.mesh.CombineMeshes(combineList);

            yield return null;

            if (!_generating && _meshFilter.mesh.vertexCount < 1)
            {
                StartCoroutine(ExecuteWaveFunctionCollapse());
            }
        }
        if (GenerationFinished != null) GenerationFinished.Invoke();
    }

    private IEnumerator ExecuteWaveFunctionCollapse()
    {
        _generating = true;
        WaveFunctionCollapse.GenerateCollapse(ref _generatedWorldData, Prototypes, PropagationDepth, RetryCount, Width, Depth, Height);
        yield return null;
        _generating = false;
    }

    public void RegenerateTerrain(int width, int height, int depth)
    {
        Width = width; Height = height; Depth = depth;
        StartCoroutine(GenerateTerrain());
    }
    #endregion
}
