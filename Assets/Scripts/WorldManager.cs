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
    [Min(-1)]
    public int RetryCount;

    [Min(1)]
    public int Width = 1;
    [Min(1)]
    public int Depth = 1;
    [Min(1)]
    public int Height = 1;

    public List<MeshPrototype> Prototypes;

    public delegate void GenerationFinishedEvent();
    public event GenerationFinishedEvent GenerationFinished;

    private bool _generating = false;
    public bool Generating { get { return _generating; } }

    private MeshFilter _meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        GenerateTerrain();
    }

    #region generation methods
    private async void GenerateTerrain()
    {
        transform.position = new Vector3((float)Width / 2, (float)Height / 2, (float)Depth / 2);
        Prototype3D<Mesh>[,,] generatedPrototypes = new Prototype3D<Mesh>[Width, Depth, Height];
        await Task.Run(() =>
        {
            _generating = true;
            generatedPrototypes = WaveFunctionCollapse<Mesh>.GenerateRecursiveCollapse3D(Prototypes, Width, Depth, Height, propagationDepth: PropagationDepth, retryCount: RetryCount, constrainCellsDelagate: (prototypes) =>
            {
                WaveFunctionCollapse<Mesh>.Cell[,,] cells = new WaveFunctionCollapse<Mesh>.Cell[Width, Depth, Height];
                for (int x = 1; x < Width - 1; x++)
                {
                    for (int z = 1; z < Depth - 1; z++)
                    {
                        for (int y = 0; y < Height - 1; y++)
                        {
                            cells[x, z, y] = new (prototypes.Where(proto => !proto.description.Contains("Vertical")), x, z, y);
                        }
                    }
                }

                // top cells
                for (int x = 1; x < Width - 1; x++)
                {
                    for (int z = 1; z < Depth - 1; z++)
                    {
                        cells[x, z, Height - 1] = new (prototypes.Where(proto => proto.posY.Equals("-1")), x, z, Height - 1);
                    }
                }
                // side cells
                for (int y = 0; y < Height; y++)
                {
                    // z faces
                    for (int x = 1; x < Width - 1; x++)
                    {
                        cells[x, Depth - 1, y] = new (prototypes.Where(proto => proto.posZ.Equals("-1")), x, Depth - 1, y);
                        cells[x, 0, y] = new (prototypes.Where(proto => proto.negZ.Equals("-1")), x, 0, y);
                    }
                    // x faces
                    for (int z = 1; z < Depth - 1; z++)
                    {
                        cells[Width - 1, z, y] = new (prototypes.Where(proto => proto.posX.Equals("-1")), Width - 1, z, y);
                        cells[0, z, y] = new (prototypes.Where(proto => proto.negX.Equals("-1")), 0, z, y);
                    }
                    // corners
                    cells[Width - 1, Depth - 1, y] = new(prototypes.Where(proto => proto.posX.Equals("-1") && proto.posZ.Equals("-1")), Width - 1, Depth - 1, y);
                    cells[Width - 1, 0, y] = new(prototypes.Where(proto => proto.posX.Equals("-1") && proto.negZ.Equals("-1")), Width - 1, 0, y);
                    cells[0, 0, y] = new (prototypes.Where(proto => proto.negX.Equals("-1") && proto.negZ.Equals("-1")), 0, 0, y);
                    cells[0, Depth - 1, y] = new (prototypes.Where(proto => proto.negX.Equals("-1") && proto.posZ.Equals("-1")), 0, Depth - 1, y);
                }
                return cells;
            });
            _generating = false;
        });

        CombineInstance[] combineList = new CombineInstance[Width * Depth * Height];

        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (generatedPrototypes[x, z, y] != null && generatedPrototypes[x, z, y].tile != null)
                    {
                        combineList[x + y * Width + z * Width * Height].mesh = generatedPrototypes[x, z, y].tile;
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
        if (GenerationFinished != null)
            GenerationFinished();
    }

    public void RegenerateTerrain(int width, int height, int depth)
    {
        Width = width; Height = height; Depth = depth;
        GenerateTerrain();
    }
    #endregion
}
