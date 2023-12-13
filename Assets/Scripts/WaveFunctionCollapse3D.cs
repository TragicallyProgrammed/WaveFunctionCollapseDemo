#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** TODO: Update
 * <summary>
 * This static class exists as a container for the functions that execute the wave function collapse algorithm
 * and the Cell class that is used as a container for probable prototypes at any given position.
 * 
 * The Algorithm is as follows:
 * <list type="number">
 * <item>A 3d array of cells, which contains a list of prototypes that they can 'collapse' to is instantiated.</item>
 * <item>The lowest entropy cell, or a random cell if all entropy values are equal, is selected to be collapsed to a random prototype.</item>
 * <item>The collapse is propagated through the array by starting at the cell that has collapsed, and trimming down all it's neighbor's probable prototypes to only contain prototypes that are valid neighbors in that direction for the selected cell. Each cell changed like this is then propagated to.</item>
 * <item>Once all cells that should have their probabilities trimmed down, the process is repeated until all cells have collapsed into a prototype.</item>
 * </list>
 * </summary>
 */
public class WaveFunctionCollapse3D
{
    public readonly int Width;
    public readonly int Height;
    public readonly int Depth;

    private readonly System.Random _rand;
    private readonly IEnumerable<MeshPrototype> _prototypes;
    private readonly int _flatLength;

    public WaveFunctionCollapse3D(IEnumerable<MeshPrototype> prototypes, int w, int h, int d)
    {
        _rand = new();
        _prototypes = prototypes;
        Width = w;
        Height = h;
        Depth = d;
        _flatLength = Width * Height * Depth;
        CalculatePrototypeNeighbors();
    }

    /** TODO: Update
     * <summary>
     * Executes the wave function collapse algorithm with recursive propagation.
     * </summary>
     * <param name="prototypes">List of prototypes to populate the cells with.</param>
     * <param name="w">Width of the volume that is being generated.</param>
     * <param name="d">Depth of the volume that is being generated.</param>
     * <param name="h">Height of the volume that is being generated.</param>
     * <param name="propagationDepth">Maximum amount of propagations after a cell is selected and collapsed.</param>
     * <param name="retryCount">Maximum number of retries to find a valid configuration of prototypes.</param>
     * <param name="constrainCellsDelegate">Allows for cells with specific constrains to be generated. Passes in the array of unconstrained cells and expects the newly constrained array of cells to be returned.</param>
     */
    public Mesh GenerateStaticMesh(int propagationDepth = -1, int retryCount = -1, Func<List<MeshPrototype>[], List<MeshPrototype>[]>? constrainCells = null)
    {
        List<MeshPrototype>[] cells = new List<MeshPrototype>[_flatLength];
        if (constrainCells != null)
            cells = constrainCells(cells);

        CombineInstance[] combineList = new CombineInstance[_flatLength];

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combineList);
        return finalMesh;
    }

    /*
     * 
     */
    private void PropagateCollapse3D()
    {
        
    }

    /*
     * 
     */
    private bool IsFinished()
    {
        return false;
    }

    /*
     * 
     */
    private void AllocateCells(ref List<MeshPrototype>[] cells)
    {
        for (int i = 0; i < _flatLength; i++)
        {
            cells[i] = new List<MeshPrototype>(_prototypes);
        }
    }

    /*
     * 
     */
    private void CalculatePrototypeNeighbors()
    {
        // Build Prototype neighbor lists
        foreach (MeshPrototype prototype in _prototypes)
        {

            // Clear lists
            List<MeshPrototype> posXNeighbors = new();
            List<MeshPrototype> negXNeighbors = new();
            List<MeshPrototype> posZNeighbors = new();
            List<MeshPrototype> negZNeighbors = new();
            List<MeshPrototype> posYNeighbors = new();
            List<MeshPrototype> negYNeighbors = new();

            foreach (MeshPrototype comparer in _prototypes)
            {
                // Positive X to Negative X
                if (prototype.posX.Contains("F")) // if this prototype is flipped
                {
                    if (prototype.posX.Equals(comparer.negX + "F"))
                        posXNeighbors.Add(comparer);
                }
                else if (prototype.posX.Contains("S") || prototype.posX.Contains("-")) // if this prototype is symmetrical
                {
                    if (prototype.posX.Equals(comparer.negX))
                        posXNeighbors.Add(comparer);
                }
                else
                {
                    if ((prototype.posX + "F").Equals(comparer.negX))
                        posXNeighbors.Add(comparer);
                }

                // Negative X to Positive X
                if (prototype.negX.Contains("F")) // if this prototype is flipped
                {
                    if (prototype.negX.Equals(comparer.posX + "F"))
                        negXNeighbors.Add(comparer);
                }
                else if (prototype.negX.Contains("S") || prototype.negX.Contains("-")) // if this prototype is symmetrical
                {
                    if (prototype.negX.Equals(comparer.posX))
                        negXNeighbors.Add(comparer);
                }
                else
                {
                    if ((prototype.negX + "F").Equals(comparer.posX))
                        negXNeighbors.Add(comparer);
                }

                // Positive Z to Negative Z
                if (prototype.posZ.Contains("F")) // if this prototype is flipped
                {
                    if (prototype.posZ.Equals(comparer.negZ + "F"))
                        posZNeighbors.Add(comparer);
                }
                else if (prototype.posZ.Contains("S") || prototype.posZ.Contains("-")) // if this prototype is symmetrical
                {
                    if (prototype.posZ.Equals(comparer.negZ))
                        posZNeighbors.Add(comparer);
                }
                else
                {
                    if ((prototype.posZ + "F").Equals(comparer.negZ))
                        posZNeighbors.Add(comparer);
                }

                // Negative Z to Positive Z
                if (prototype.negZ.Contains("F")) // if this prototype is flipped
                {
                    if (prototype.negZ.Equals(comparer.posZ + "F"))
                        negZNeighbors.Add(comparer);
                }
                else if (prototype.negZ.Contains("S") || prototype.negZ.Contains("-")) // if this prototype is symmetrical
                {
                    if (prototype.negZ.Equals(comparer.posZ))
                        negZNeighbors.Add(comparer);
                }
                else
                {
                    if ((prototype.negZ + "F").Equals(comparer.posZ))
                        negZNeighbors.Add(comparer);
                }

                // Positive Y to Negative Y
                if (prototype.posY.Equals(comparer.negY))
                    posYNeighbors.Add(comparer);

                // Negative Y to Positive Y
                if (prototype.negY.Equals(comparer.posY))
                    negYNeighbors.Add(comparer);
            }
            prototype.posXNeighbors = posXNeighbors;
            prototype.negXNeighbors = negXNeighbors;
            prototype.posZNeighbors = posZNeighbors;
            prototype.negZNeighbors = negZNeighbors;
            prototype.posYNeighbors = posYNeighbors;
            prototype.negYNeighbors = negYNeighbors;
        }
    }

    #region Indexing
    /*
     * 
     */
    public int Index(int x, int y, int z)
    {
        return x + y * Width + z * Width * Height;
    }

    /*
     * 
     */
    public int[] Index(int index)
    {
        int z = index / (Width * Height);
        index -= z * Width * Height;
        int y = index / Width;
        int x = index % Width;
        return new int[]{ x, y, z };
    }
    #endregion
}

class InvalidCellException : Exception
{
    public InvalidCellException() { }
    public InvalidCellException(string message) : base(message) { }
    public InvalidCellException(string message, Exception inner) : base(message, inner) { }
}