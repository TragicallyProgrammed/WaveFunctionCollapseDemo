#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
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
public static class WaveFunctionCollapse<T>
{
    private static readonly System.Random _rand = new();

    /**
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
    public static Prototype3D<T>[,,] GenerateRecursiveCollapse3D(IEnumerable<Prototype3D<T>> prototypes, int w, int d, int h, int propagationDepth = -1, int retryCount = -1, Func<Cell[,,], int, int, int, Cell[,,]>? constrainCellsDelegate = null)
    {
        prototypes = CalculatePrototypeNeighbors(prototypes);

        int count = 0;

        Cell[,,] cells = AllocateCells(prototypes, w, d, h);
        if (constrainCellsDelegate != null)
        {
            cells = constrainCellsDelegate(cells, w, d, h);
        }

        while (!IsFinished(cells, w, d, h))
        {
            if (retryCount != -1 && count >= retryCount) 
            {
                throw new Exception(string.Format("Retry Count Exceeded! Total number of retries: {0}", count));
            }

            // Randomly select cell to collapse
            Cell selectedCell = cells[_rand.Next(w), _rand.Next(d), _rand.Next(h)];
            while (selectedCell.Entropy == 0)
            {
                selectedCell = cells[_rand.Next(w), _rand.Next(d), _rand.Next(h)];
            }
            for (int x_index = 0; x_index < w; x_index++)
            {
                for (int z_index = 0; z_index < d; z_index++)
                {
                    for (int y_index = 0; y_index < h; y_index++)
                    {
                        if (cells[x_index, z_index, y_index].Entropy > 0 && cells[x_index, z_index, y_index].Entropy < selectedCell.Entropy)
                            selectedCell = cells[x_index, z_index, y_index];
                    }
                }
            }

            try
            {
                selectedCell.CollapseCell();
                PropagateCollapse3D(ref cells, selectedCell, propagationDepth, w, d, h);
                count++;
            }
            catch (Exception e)
            {
                //count++;
                Debug.LogError(e.Message);
                cells = AllocateCells(prototypes, w, d, h);
                if (constrainCellsDelegate != null)
                {
                    cells = constrainCellsDelegate(cells, w, d, h);
                }
            }
        }
        Prototype3D<T>[,,] generatedPrototypes = new Prototype3D<T>[w, d, h];
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < d; z++)
            {
                for (int y = 0; y < h; y++)
                {
                    generatedPrototypes[x, z, y] = cells[x, z, y].CollapsedPrototype!;
                }
            }
        }
        return generatedPrototypes;
    }

    /*
     * 
     */
    private static void PropagateCollapse3D(ref Cell[,,] cells, Cell collapsingCell, int propagationDepth, int w, int d, int h, int count = 0)
    {
        if (count < propagationDepth || propagationDepth == -1)
        {
            count++;
            IEnumerable<Prototype3D<T>> posXComparison = new List<Prototype3D<T>>();
            IEnumerable<Prototype3D<T>> negXComparison = new List<Prototype3D<T>>();
            IEnumerable<Prototype3D<T>> posZComparison = new List<Prototype3D<T>>();
            IEnumerable<Prototype3D<T>> negZComparison = new List<Prototype3D<T>>();
            IEnumerable<Prototype3D<T>> posYComparison = new List<Prototype3D<T>>();
            IEnumerable<Prototype3D<T>> negYComparison = new List<Prototype3D<T>>();
            HashSet<Prototype3D<T>> invalidPrototypes = new();
            // positive x
            if (collapsingCell.x + 1 < w)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y].NegXNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posXComparison = cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.PosXNeighbors);
            }
            // negative x
            if (collapsingCell.x - 1 > -1)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y].PosXNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negXComparison = cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.NegXNeighbors);
            }

            // positive z
            if (collapsingCell.z + 1 < d)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y].NegZNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posZComparison = cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.PosZNeighbors);
            }
            // negative z
            if (collapsingCell.z - 1 > -1)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y].PosZNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negZComparison = cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.NegZNeighbors);
            }

            // positive y
            if (collapsingCell.y + 1 < h)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1].NegYNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posYComparison = cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1].ProbablePrototypes.Except(collapsingCell.PosYNeighbors);
            }
            // negative y
            if (collapsingCell.y - 1 > -1)
            {
                IEnumerable<Prototype3D<T>> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1].PosYNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negYComparison = cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1].ProbablePrototypes.Except(collapsingCell.NegYNeighbors);

            }

            if (invalidPrototypes.Any())
            {
                collapsingCell.RemoveProbabilities(invalidPrototypes.ToList());
            }

            if (posXComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (negXComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (posZComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (negZComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (posYComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1], propagationDepth, w, d, h, count);
            }
            if (negYComparison.Any())
            {
                PropagateCollapse3D(ref cells, cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1], propagationDepth, w, d, h, count);
            }
        }
    }

    private static bool IsFinished(Cell[,,] cells, int w, int d, int h)
    {
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < d; z++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (cells[x, z, y].Entropy != 0)
                        return false;
                }
            }
        }
        return true;
    }

    private static Cell[,,] AllocateCells(IEnumerable<Prototype3D<T>> prototypes, int w, int d, int h)
    {
        Cell[,,] cells = new Cell[w, d, h];

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < d; z++)
            {
                for (int y = 0; y < h; y++)
                {
                    cells[x, z, y] = new Cell(prototypes, x, z, y);
                }
            }
        }
        return cells;
    }

    private static IEnumerable<Prototype3D<T>> CalculatePrototypeNeighbors(IEnumerable<Prototype3D<T>> prototypes)
    {
        // Build Prototype neighbor lists
        foreach (Prototype3D<T> prototype in prototypes)
        {

            // Clear lists
            List<Prototype3D<T>> posXNeighbors = new();
            List<Prototype3D<T>> negXNeighbors = new();
            List<Prototype3D<T>> posZNeighbors = new();
            List<Prototype3D<T>> negZNeighbors = new();
            List<Prototype3D<T>> posYNeighbors = new();
            List<Prototype3D<T>> negYNeighbors = new();

            foreach (Prototype3D<T> comparer in prototypes)
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
        return prototypes;
    }

    #region Cell
    /**
     * 
     */
    public class Cell
    {
        private readonly HashSet<Prototype3D<T>> _probablePrototypes;
        private readonly HashSet<Prototype3D<T>> _posXNeighbors = new();
        private readonly HashSet<Prototype3D<T>> _negXNeighbors = new();
        private readonly HashSet<Prototype3D<T>> _posZNeighbors = new();
        private readonly HashSet<Prototype3D<T>> _negZNeighbors = new();
        private readonly HashSet<Prototype3D<T>> _posYNeighbors = new();
        private readonly HashSet<Prototype3D<T>> _negYNeighbors = new();
        private double _entropy;

        public IEnumerable<Prototype3D<T>> ProbablePrototypes { get { return _probablePrototypes; } }
        public IEnumerable<Prototype3D<T>> PosXNeighbors { get { return _posXNeighbors; } }
        public IEnumerable<Prototype3D<T>> NegXNeighbors { get { return _negXNeighbors; } }
        public IEnumerable<Prototype3D<T>> PosZNeighbors { get { return _posZNeighbors; } }
        public IEnumerable<Prototype3D<T>> NegZNeighbors { get { return _negZNeighbors; } }
        public IEnumerable<Prototype3D<T>> PosYNeighbors { get { return _posYNeighbors; } }
        public IEnumerable<Prototype3D<T>> NegYNeighbors { get { return _negYNeighbors; } }
        public Prototype3D<T>? CollapsedPrototype { get { if (_entropy == 0) return _probablePrototypes.ElementAt(0); return null; } }
        public double Entropy { get { return _entropy; } }
        public readonly int x, z, y;

        public Cell(IEnumerable<Prototype3D<T>> prototypes, int x_index, int z_index, int y_index)
        {
            _probablePrototypes = new(prototypes);
            // Shuffle Prototypes. This isn't required, but makes a nicer distribution when randomly selecting a cell
            _probablePrototypes = _probablePrototypes.OrderBy(_ => _rand.Next()).ToHashSet();
            x = x_index;
            z = z_index;
            y = y_index;
            RecalculateEntropy();
            RecalculateNeighbors();
        }

        public bool RemoveProbabilities(IEnumerable<Prototype3D<T>> prototypes)
        {
            IEnumerable<Prototype3D<T>> currentPrototypes = _probablePrototypes;
            HashSet<Prototype3D<T>> currentPrototypesSet = currentPrototypes.ToHashSet();
            currentPrototypesSet.ExceptWith(prototypes);
            if (currentPrototypesSet.Count() == 0)
            {
                throw new InvalidCellException(string.Format("Invalid Cell At Position: x:{0} z:{1} y:{2}", x, z, y));
            }
            _probablePrototypes.ExceptWith(prototypes);
            /*
            if (_probablePrototypes.Count == 0)
            {
                throw new InvalidCellException(string.Format("Invalid Cell At Position: x:{0} z:{1} y:{2}", x, z, y));
            }
            */
            RecalculateEntropy();
            RecalculateNeighbors();
            if (_probablePrototypes.Count() == 1)
            {
                return true;
            }
            return false;
        }

        public void CollapseCell()
        {
            if (_probablePrototypes.Count > 1)
            {
                int totalWeight = _probablePrototypes.Sum(prototype => prototype.weight);
                int randWeightVal = _rand.Next(totalWeight) + 1;
                int processedWeight = 0;
                foreach (Prototype3D<T> prototype in _probablePrototypes)
                {
                    processedWeight += prototype.weight;
                    if (randWeightVal <= processedWeight)
                    {
                        _probablePrototypes.Clear();
                        _probablePrototypes.Add(prototype);
                        break;
                    }
                }
            }
            RecalculateEntropy();
            RecalculateNeighbors();
        }

        private void RecalculateEntropy()
        {
            // Based on Shannon Entropy For Square from this article: https://robertheaton.com/2018/12/17/wavefunction-collapse-algorithm/
            int totalWeight = _probablePrototypes.Sum(prototype => prototype.weight);
            _entropy = Math.Log(totalWeight) - (_probablePrototypes.Sum(prototype => prototype.weight * Math.Log(prototype.weight)) / totalWeight);
        }

        private void RecalculateNeighbors()
        {
            _posXNeighbors.Clear();
            _negXNeighbors.Clear();
            _posZNeighbors.Clear();
            _negZNeighbors.Clear();
            _posYNeighbors.Clear();
            _negYNeighbors.Clear();
            foreach (Prototype3D<T> prototype in _probablePrototypes)
            {
                foreach (Prototype3D<T> neighbor in prototype.posXNeighbors) { _posXNeighbors.Add(neighbor); }
                foreach (Prototype3D<T> neighbor in prototype.negXNeighbors) { _negXNeighbors.Add(neighbor); }
                foreach (Prototype3D<T> neighbor in prototype.posZNeighbors) { _posZNeighbors.Add(neighbor); }
                foreach (Prototype3D<T> neighbor in prototype.negZNeighbors) { _negZNeighbors.Add(neighbor); }
                foreach (Prototype3D<T> neighbor in prototype.posYNeighbors) { _posYNeighbors.Add(neighbor); }
                foreach (Prototype3D<T> neighbor in prototype.negYNeighbors) { _negYNeighbors.Add(neighbor); }
            }
        }
    }
    #endregion
}

class InvalidCellException : Exception
{
    public InvalidCellException() { }
    public InvalidCellException(string message) : base(message) { }
    public InvalidCellException(string message, Exception inner) : base(message, inner) { }
}