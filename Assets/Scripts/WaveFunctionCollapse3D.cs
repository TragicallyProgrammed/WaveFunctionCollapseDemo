#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * <summary>
 * Class for executing the Wave Function Collapse Algorithm for a 3D volume.
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
    private readonly IEnumerable<Prototype3D> _prototypes;

    /**
     * <param name="prototypes">Collection of 3D Prototypes to instantiate each cell with.</param>
     * <param name="w">Width of volume to generate.</param>
     * <param name="h">Height of volume to generate.</param>
     * <param name="d">Depth of volume to generate.</param>
     */
    public WaveFunctionCollapse3D(IEnumerable<Prototype3D> prototypes, int w, int h, int d)
    {
        _rand = new();
        _prototypes = prototypes;
        Width = w;
        Height = h;
        Depth = d;
        CalculatePrototypeNeighbors();
    }

    /**
     * <summary>
     * Executes the wave function collapse algorithm with recursive propagation.
     * </summary>
     * <param name="propagationDepth">Maximum amount of propagations after a cell is selected and collapsed.</param>
     * <param name="retryCount">Maximum number of retries to find a valid configuration of prototypes.</param>
     * <param name="constrainCells">Allows for cells with specific constrains to be generated. Passes in the array of unconstrained cells and expects the newly constrained array of cells to be returned where each cell is a List<Prototype3D>[].</param>
     */
    public Mesh GenerateStaticMesh(int propagationDepth = -1, int retryCount = -1, Func<List<Prototype3D>[], List<Prototype3D>[]>? constrainCells = null)
    {
        // Instantiate array for cells and an array to keep track of entropy
        List<Prototype3D>[] cells = new List<Prototype3D>[Width * Height * Depth];
        double[] entropyArray = new double[cells.Length];
        // Instantiate a new list for each element in array
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new(_prototypes);
        }
        // Execute contstraints on cells
        if (constrainCells != null)
            cells = constrainCells(cells);
        // Calculate Entropy for starting cells
        for (int i = 0; i < entropyArray.Length; i++)
        {
            entropyArray[i] = CalculateEntropy(cells[i]);
        }
        // Instantiate CombineInstance array for when a cell collapses
        CombineInstance[] combineList = new CombineInstance[cells.Length];

        #region algorithm
        // Begin WFC algorithm
        while (!IsFinished(entropyArray))
        {
            // Select random cell
            int selectedIndex = _rand.Next(cells.Length);
            while (entropyArray[selectedIndex] == 0)
            {
                selectedIndex = _rand.Next(cells.Length);
            }
            List<Prototype3D> selectedCell = cells[selectedIndex];
            Prototype3D collapsedPrototype = selectedCell.ElementAt(_rand.Next(selectedCell.Count()));
            selectedCell.RemoveAll(p => p.description.Equals(collapsedPrototype.description));
            Stack<int> propagationIndexStack = new Stack<int>();
            Vector3Int indexVector = Index(selectedIndex);
            propagationIndexStack.Push(Index(indexVector.x + 1, indexVector.y, indexVector.z));
            propagationIndexStack.Push(Index(indexVector.x - 1, indexVector.y, indexVector.z));
            propagationIndexStack.Push(Index(indexVector.x, indexVector.y, indexVector.z + 1));
            propagationIndexStack.Push(Index(indexVector.x, indexVector.y, indexVector.z - 1));
            propagationIndexStack.Push(Index(indexVector.x, indexVector.y + 1, indexVector.z));
            propagationIndexStack.Push(Index(indexVector.x, indexVector.y - 1, indexVector.z));
        }
        #endregion

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combineList);
        return finalMesh;
    }

    /*
     * 
     */
    private bool IsFinished(double[] entropyArray)
    {
        for (int i = 0; i < entropyArray.Length; i++)
        {
            if (entropyArray[i] != 0)
            {
                return false;
            }
        }
        return true;
    }

    /*
     * 
     */
    private void CalculatePrototypeNeighbors()
    {
        // Build Prototype neighbor lists
        foreach (Prototype3D prototype in _prototypes)
        {

            // Clear lists
            List<Prototype3D> posXNeighbors = new();
            List<Prototype3D> negXNeighbors = new();
            List<Prototype3D> posZNeighbors = new();
            List<Prototype3D> negZNeighbors = new();
            List<Prototype3D> posYNeighbors = new();
            List<Prototype3D> negYNeighbors = new();

            foreach (Prototype3D comparer in _prototypes)
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

    private double CalculateEntropy(IEnumerable<Prototype3D> prototypes)
    {
        int totalWeight = prototypes.Sum(prototype => prototype.weight);
        return Math.Log(totalWeight) - (prototypes.Sum(prototype => prototype.weight * Math.Log(prototype.weight)) / totalWeight);
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
    public Vector3Int Index(int index)
    {
        int z = index / (Width * Height);
        index -= z * Width * Height;
        int y = index / Width;
        int x = index % Width;
        return new Vector3Int(x, y, z);
    }
    #endregion
}

class InvalidCellException : Exception
{
    public InvalidCellException() { }
    public InvalidCellException(string message) : base(message) { }
    public InvalidCellException(string message, Exception inner) : base(message, inner) { }
}