using System.Collections.Generic;
using UnityEngine;

public abstract class Prototype3D<T> : ScriptableObject
{
    public string description;
    public T tile;
    public int weight;
    public int rotation;
    public string posX;
    public string negX;
    public string posZ;
    public string negZ;
    public string posY;
    public string negY;
    public IEnumerable<Prototype3D<T>> posXNeighbors;
    public IEnumerable<Prototype3D<T>> negXNeighbors;
    public IEnumerable<Prototype3D<T>> posZNeighbors;
    public IEnumerable<Prototype3D<T>> negZNeighbors;
    public IEnumerable<Prototype3D<T>> posYNeighbors;
    public IEnumerable<Prototype3D<T>> negYNeighbors;
}

public abstract class Prototype2D<T> : ScriptableObject 
{
    public string description;
    public T tile;
    public int weight;
    public string posX;
    public string negX;
    public string posY;
    public string negY;
    public IEnumerable<Prototype3D<T>> posXNeighbors;
    public IEnumerable<Prototype3D<T>> negXNeighbors;
    public IEnumerable<Prototype3D<T>> posYNeighbors;
    public IEnumerable<Prototype3D<T>> negYNeighbors;
}
