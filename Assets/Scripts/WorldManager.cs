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
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WorldManager : MonoBehaviour
{
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

    private bool _generating = false;
    public bool Generating { get { return _generating; } }

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
        GenerateTerrain();
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region generation methods
    private async void GenerateTerrain()
    {
        transform.position = new Vector3((float)Width / 2, (float)Height / 2, (float)Depth / 2);
        Prototype[,,] generatedPrototypes = new Prototype[Width, Depth, Height];
        await Task.Run(() =>
        {
            _generating = true;
            generatedPrototypes = WaveFunctionCollapse.GenerateRecursiveCollapse(Prototypes, PropagationDepth, RetryCount, Width, Depth, Height);
            _generating = false;
        });

        CombineInstance[] combineList = new CombineInstance[Width * Depth * Height];

        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (generatedPrototypes[x, z, y] != null && generatedPrototypes[x, z, y].model != null)
                    {
                        combineList[x + y * Width + z * Width * Height].mesh = generatedPrototypes[x, z, y].model;
                        Matrix4x4 mat = Matrix4x4.identity;
                        mat.SetTRS(new Vector3(x - ((float)Width / 2), y - ((float)Height / 2), z - ((float)Depth / 2)), Quaternion.Euler(0, 90 * generatedPrototypes[x, z, y].rotation, 0), Vector3.one);
                        combineList[x + y * Width + z * Width * Height].transform = mat;
                    }
                    else
                        combineList[x + y * Width + z * Width * Height].mesh = new Mesh();
                }
            }
        }
        _meshFilter.sharedMesh = new Mesh();
        _meshFilter.sharedMesh.CombineMeshes(combineList);
    }

    public void RegenerateTerrain(int width, int height, int depth)
    {
        Width = width; Height = height; Depth = depth;
        GenerateTerrain();
    }
    #endregion
}
