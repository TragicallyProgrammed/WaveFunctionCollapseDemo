using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.MaterialProperty;

[CreateAssetMenu(fileName = "New Prototype", menuName = "Project/New 3D Prototype")]
public class Prototype3D : ScriptableObject
{
    public string description;
    public Mesh tile;
    public int weight;
    public int rotation;
    public string posX;
    public string negX;
    public string posZ;
    public string negZ;
    public string posY;
    public string negY;
    public List<Prototype3D> posXNeighbors = new();
    public List<Prototype3D> negXNeighbors = new();
    public List<Prototype3D> posZNeighbors = new();
    public List<Prototype3D> negZNeighbors = new();
    public List<Prototype3D> posYNeighbors = new();
    public List<Prototype3D> negYNeighbors = new();
}

[CreateAssetMenu(fileName = "New Prototype", menuName = "Project/New 2D Prototype")]
public class Prototype2D : ScriptableObject 
{
    public string description;
    public Texture2D tile;
    public int weight;
    public string posX;
    public string negX;
    public string posY;
    public string negY;
    public List<Prototype2D> posXNeighbors = new();
    public List<Prototype2D> negXNeighbors = new();
    public List<Prototype2D> posYNeighbors = new();
    public List<Prototype2D> negYNeighbors = new();
}
