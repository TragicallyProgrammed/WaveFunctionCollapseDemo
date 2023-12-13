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
    public async void GenerateTerrain()
    {
        transform.position = new Vector3((float)Width / 2, (float)Height / 2, (float)Depth / 2);
        Prototype3D<Mesh>[,,] generatedPrototypes = new Prototype3D<Mesh>[Width, Depth, Height];
        /*await Task.Run(() =>
        {
            _generating = true;
            generatedPrototypes = WaveFunctionCollapse<Mesh>.GenerateStaticCollapse3D(Prototypes, Width, Depth, Height, propagationDepth: PropagationDepth, retryCount: RetryCount, constrainCellsDelegate: (cells, w, d, h) =>
            {
                for (int x = 1; x < w - 1; x++)
                {
                    for (int z = 1; z < d - 1; z++)
                    {
                        for (int y = 0; y < h - 1; y++)
                        {
                            cells[x, z, y].RemoveProbabilities(cells[x, z, y].ProbablePrototypes.Where(proto => proto.description.Contains("Vertical")).ToList());
                        }
                    }
                }

                // top cells
                for (int x = 1; x < w - 1; x++)
                {
                    for (int z = 1; z < d - 1; z++)
                    {
                        cells[x, z, h - 1].RemoveProbabilities(cells[x, z, Height - 1].ProbablePrototypes.Where(proto => !proto.posY.Equals("-1")).ToList());
                    }
                }
                // side cells
                for (int y = 0; y < h; y++)
                {
                    // z faces
                    for (int x = 1; x < w - 1; x++)
                    {
                        cells[x, d - 1, y].RemoveProbabilities(cells[x, d - 1, y].ProbablePrototypes.Where(proto => !proto.posZ.Equals("-1")).ToList());
                        cells[x, 0, y].RemoveProbabilities(cells[x, 0, y].ProbablePrototypes.Where(proto => !proto.negZ.Equals("-1")).ToList());
                    }
                    // x faces
                    for (int z = 1; z < Depth - 1; z++)
                    {
                        cells[w - 1, z, y].RemoveProbabilities(cells[w - 1, z, y].ProbablePrototypes.Where(proto => !proto.posX.Equals("-1")).ToList());
                        cells[0, z, y].RemoveProbabilities(cells[0, z, y].ProbablePrototypes.Where(proto => !proto.negX.Equals("-1")).ToList());
                    }
                    // corners
                    cells[Width - 1, Depth - 1, y].RemoveProbabilities(cells[Width - 1, Depth - 1, y].ProbablePrototypes.Where(proto => !(proto.posX.Equals("-1") && proto.posZ.Equals("-1"))).ToList());
                    cells[Width - 1, 0, y].RemoveProbabilities(cells[Width - 1, 0, y].ProbablePrototypes.Where(proto => !(proto.posX.Equals("-1") && proto.negZ.Equals("-1"))).ToList());
                    cells[0, 0, y].RemoveProbabilities(cells[0, 0, y].ProbablePrototypes.Where(proto => !(proto.negX.Equals("-1") && proto.negZ.Equals("-1"))).ToList());
                    cells[0, Depth - 1, y].RemoveProbabilities(cells[0, Depth - 1, y].ProbablePrototypes.Where(proto => !(proto.negX.Equals("-1") && proto.posZ.Equals("-1"))).ToList());
                }
                return cells;
            });
            _generating = false;
        });*/

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
    #endregion
}
