/**
* This file will be responsible for implementing
* the Wave Function Collapse Algorithm.
* Because the algorithm doesn't store any
* data, it will be implemented with static
* functions.
* 
* This implementation of the Wave Function
* Collapse Algorithm is based off of the 
* explanation of the algorithm by 
* Martin Donald in this video: https://www.youtube.com/watch?v=2SuvO4Gi7uY&t=565s
* 
* @author Garrett Bowers
*/
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WaveFunctionCollapse
{
    private static readonly System.Random _rand = new();

    public static Prototype[,,] GenerateRecursiveCollapse(IEnumerable<Prototype> prototypes, int propagationDepth, int retryCount, int w, int d, int h)
    {
        int count = 0;
        // Shuffle Prototypes
        prototypes = prototypes.OrderBy(_ => _rand.Next()).ToList();

        // Allocate 3D grid of cells
        Cell[,,] cells = AllocateConstrainedCells(prototypes, w, d, h);

        // Start with truly random cell (Due to constrained cells having lower entropy by default)
        Cell selectedCell = cells[_rand.Next(w), _rand.Next(d), _rand.Next(h)];
        selectedCell.CollapseCell();
        PropagateCollapse(ref cells, selectedCell, propagationDepth, w, d, h, 0);

        while (!IsFinished(cells, w, d, h))
        {
            if (count >= retryCount) 
            {
                throw new Exception(String.Format("Retry Count Exceeded! Total number of retries: {0}", count));
            }

            // Randomly select cell to collapse
            selectedCell = cells[_rand.Next(w), _rand.Next(d), _rand.Next(h)];
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
                PropagateCollapse(ref cells, selectedCell, propagationDepth, w, d, h, 0);
            }
            catch (Exception e)
            {
                count++;
                Debug.LogError(e.Message);
                cells = AllocateConstrainedCells(prototypes, w, d, h);
                selectedCell = cells[_rand.Next(w), _rand.Next(d), _rand.Next(h)];
                selectedCell.CollapseCell();
                PropagateCollapse(ref cells, selectedCell, propagationDepth, w, d, h, 0);
            }
        }
        Prototype[,,] generatedPrototypes = new Prototype[w, d, h];
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

    #region Propagation
    private static void PropagateCollapse(ref Cell[,,] cells, Cell collapsingCell, int propagationDepth, int w, int d, int h, int count)
    {
        if (count < propagationDepth || propagationDepth == -1)
        {
            count++;
            IEnumerable<Prototype> posXComparison = new List<Prototype>();
            IEnumerable<Prototype> negXComparison = new List<Prototype>();
            IEnumerable<Prototype> posZComparison = new List<Prototype>();
            IEnumerable<Prototype> negZComparison = new List<Prototype>();
            IEnumerable<Prototype> posYComparison = new List<Prototype>();
            IEnumerable<Prototype> negYComparison = new List<Prototype>();
            HashSet<Prototype> invalidPrototypes = new();
            // positive x
            if (collapsingCell.x + 1 < w)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y].NegXNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posXComparison = cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.PosXNeighbors);
            }
            // negative x
            if (collapsingCell.x - 1 > -1)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y].PosXNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negXComparison = cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.NegXNeighbors);
            }

            // positive z
            if (collapsingCell.z + 1 < d)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y].NegZNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posZComparison = cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.PosZNeighbors);
            }
            // negative z
            if (collapsingCell.z - 1 > -1)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y].PosZNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negZComparison = cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y].ProbablePrototypes.Except(collapsingCell.NegZNeighbors);
            }

            // positive y
            if (collapsingCell.y + 1 < h)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1].NegYNeighbors);
                invalidPrototypes.UnionWith(comparison);
                posYComparison = cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1].ProbablePrototypes.Except(collapsingCell.PosYNeighbors);
            }
            // negative y
            if (collapsingCell.y - 1 > -1)
            {
                IEnumerable<Prototype> comparison = collapsingCell.ProbablePrototypes.Except(cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1].PosYNeighbors);
                invalidPrototypes.UnionWith(comparison);
                negYComparison = cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1].ProbablePrototypes.Except(collapsingCell.NegYNeighbors);

            }

            if (invalidPrototypes.Any())
            {
                collapsingCell.RemoveProbabilities(invalidPrototypes.ToList());
            }

            if (posXComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x + 1, collapsingCell.z, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (negXComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x - 1, collapsingCell.z, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (posZComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x, collapsingCell.z + 1, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (negZComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x, collapsingCell.z - 1, collapsingCell.y], propagationDepth, w, d, h, count);
            }
            if (posYComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x, collapsingCell.z, collapsingCell.y + 1], propagationDepth, w, d, h, count);
            }
            if (negYComparison.Any())
            {
                PropagateCollapse(ref cells, cells[collapsingCell.x, collapsingCell.z, collapsingCell.y - 1], propagationDepth, w, d, h, count);
            }
        }
    }
    #endregion

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

    private static Cell[,,] AllocateCells(IEnumerable<Prototype> prototypes, int w, int d, int h)
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

    private static Cell[,,] AllocateConstrainedCells(IEnumerable<Prototype> prototypes, int w, int d, int h)
    {
        Cell[,,] cells = new Cell[w, d, h];
        // fill interior cells with all prototypes except the vertical prototypes
        for (int x = 1; x < w - 1; x++)
        {
            for (int z = 1; z < d - 1; z++)
            {
                for (int y = 0; y < h - 1; y++)
                {
                    cells[x, z, y] = new Cell(prototypes, x, z, y);
                }
            }
        }
        
        // top cells
        for (int x = 1; x < w - 1; x++)
        {
            for (int z = 1; z < d - 1; z++)
            {
                cells[x, z, h - 1] = new Cell(prototypes.Where(proto => proto.posY.Equals("-1")), x, z, h - 1); 
            }
        }
        // side cells
        for (int y = 0; y < h; y++)
        {
            // z faces
            for (int x = 1; x < w - 1 ; x++)
            {
                cells[x, d - 1, y] = new Cell(prototypes.Where(proto => proto.posZ.Equals("-1")), x, d - 1, y);
                cells[x, 0, y] = new Cell(prototypes.Where(proto => proto.negZ.Equals("-1")), x, 0, y);
            }
            // x faces
            for (int z = 1; z < d - 1; z++)
            {
                cells[w - 1, z, y] = new Cell(prototypes.Where(proto => proto.posX.Equals("-1")), w - 1, z, y);
                cells[0, z, y] = new Cell(prototypes.Where(proto => proto.negX.Equals("-1")), 0, z, y);
            }
            // corners
            cells[w - 1, d - 1, y] = new Cell(prototypes.Where(proto => proto.posX.Equals("-1") && proto.posZ.Equals("-1")), w - 1, d - 1, y);
            cells[w - 1, 0, y] = new Cell(prototypes.Where(proto => proto.posX.Equals("-1") && proto.negZ.Equals("-1")), w - 1, 0, y);
            cells[0, 0, y] = new Cell(prototypes.Where(proto => proto.negX.Equals("-1") && proto.negZ.Equals("-1")), 0, 0, y);
            cells[0, d - 1, y] = new Cell(prototypes.Where(proto => proto.negX.Equals("-1") && proto.posZ.Equals("-1")), 0, d - 1, y);
        }
        
        return cells;
    }

    #region Cell
    private class Cell
    {
        private readonly HashSet<Prototype> _probablePrototypes;
        private readonly HashSet<Prototype> _posXNeighbors = new();
        private readonly HashSet<Prototype> _negXNeighbors = new();
        private readonly HashSet<Prototype> _posZNeighbors = new();
        private readonly HashSet<Prototype> _negZNeighbors = new();
        private readonly HashSet<Prototype> _posYNeighbors = new();
        private readonly HashSet<Prototype> _negYNeighbors = new();
        private Prototype? _collapsedPrototype;
        private double _entropy;

        public IEnumerable<Prototype> ProbablePrototypes { get { return _probablePrototypes; } }
        public IEnumerable<Prototype> PosXNeighbors { get { return _posXNeighbors; } }
        public IEnumerable<Prototype> NegXNeighbors { get { return _negXNeighbors; } }
        public IEnumerable<Prototype> PosZNeighbors { get { return _posZNeighbors; } }
        public IEnumerable<Prototype> NegZNeighbors { get { return _negZNeighbors; } }
        public IEnumerable<Prototype> PosYNeighbors { get { return _posYNeighbors; } }
        public IEnumerable<Prototype> NegYNeighbors { get { return _negYNeighbors; } }
        public Prototype? CollapsedPrototype { get { return _collapsedPrototype; } }
        public double Entropy { get { return _entropy; } }
        public readonly int x, z, y;

        public Cell(IEnumerable<Prototype> prototypes, int x_index, int z_index, int y_index)
        {
            _probablePrototypes = new(prototypes);
            x = x_index;
            z = z_index;
            y = y_index;
            RecalculateEntropy();
            RecalculateNeighbors();
        }

        public bool RemoveProbabilities(IEnumerable<Prototype> prototypes)
        {
            _probablePrototypes.ExceptWith(prototypes);
            if (_probablePrototypes.Count() == 1)
            {
                CollapseCell();
                return true;
            }
            else
            {
                RecalculateEntropy();
                RecalculateNeighbors();
            }
            return false;
        }

        public void CollapseCell()
        {
            if (_probablePrototypes.Count == 0)
            {
                throw new InvalidCellException(String.Format("Invalid Cell At Position: x:{0} z:{1} y:{2}", x, z, y));
            }
            if (_probablePrototypes.Count > 1)
            {
                int totalWeight = _probablePrototypes.Sum(prototype => prototype.weight);
                int randWeightVal = _rand.Next(totalWeight) + 1;
                int processedWeight = 0;
                foreach (Prototype prototype in _probablePrototypes)
                {
                    processedWeight += prototype.weight;
                    if (randWeightVal <= processedWeight)
                    {
                        CollapseCell(prototype);
                        return;
                    }
                }
            }
            CollapseCell(_probablePrototypes.ElementAt(0));
        }

        public Prototype CollapseCell(Prototype prototype)
        {
            _collapsedPrototype = prototype;
            _probablePrototypes.Clear();
            RecalculateEntropy();
            SetNeighbors(prototype);
            return prototype;
        }

        private void RecalculateEntropy()
        {
            if (_probablePrototypes.Count == 0)
            {
                _entropy = 0;
            }
            else
            {
                // Based on Shannon Entropy For Square from this article: https://robertheaton.com/2018/12/17/wavefunction-collapse-algorithm/
                int totalWeight = _probablePrototypes.Sum(prototype => prototype.weight);
                _entropy = Math.Log(totalWeight) - (_probablePrototypes.Sum(prototype => prototype.weight * Math.Log(prototype.weight)) / totalWeight);
            }
        }

        private void RecalculateNeighbors()
        {
            _posXNeighbors.Clear();
            _negXNeighbors.Clear();
            _posZNeighbors.Clear();
            _negZNeighbors.Clear();
            _posYNeighbors.Clear();
            _negYNeighbors.Clear();
            foreach (Prototype prototype in _probablePrototypes)
            {
                prototype.posXNeighbors.ForEach(neighbor => _posXNeighbors.Add(neighbor));
                prototype.negXNeighbors.ForEach(neighbor => _negXNeighbors.Add(neighbor));
                prototype.posZNeighbors.ForEach(neighbor => _posZNeighbors.Add(neighbor));
                prototype.negZNeighbors.ForEach(neighbor => _negZNeighbors.Add(neighbor));
                prototype.posYNeighbors.ForEach(neighbor => _posYNeighbors.Add(neighbor));
                prototype.negYNeighbors.ForEach(neighbor => _negYNeighbors.Add(neighbor));
            }
        }

        private void SetNeighbors(Prototype prototype)
        {
            _posXNeighbors.Clear();
            _negXNeighbors.Clear();
            _posZNeighbors.Clear();
            _negZNeighbors.Clear();
            _posYNeighbors.Clear();
            _negYNeighbors.Clear();
            prototype.posXNeighbors.ForEach(neighbor => _posXNeighbors.Add(neighbor));
            prototype.negXNeighbors.ForEach(neighbor => _negXNeighbors.Add(neighbor));
            prototype.posZNeighbors.ForEach(neighbor => _posZNeighbors.Add(neighbor));
            prototype.negZNeighbors.ForEach(neighbor => _negZNeighbors.Add(neighbor));
            prototype.posYNeighbors.ForEach(neighbor => _posYNeighbors.Add(neighbor));
            prototype.negYNeighbors.ForEach(neighbor => _negYNeighbors.Add(neighbor));
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