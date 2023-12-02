/**
 * This class acts as a template for the prototype
 * scriptable objects.
 * Each prototype defines a socket that will then
 * be used to populate neighborLists with valid neighbors
 * that match the socket.
 * 
 * @author Garrett Bowers
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Prototype", menuName = "Project/New Prototype")]
public class Prototype : ScriptableObject
{
    public string description;
    public Mesh model;
    public int weight;
    public int rotation;
    public string posX;
    public string negX;
    public string posZ;
    public string negZ;
    public string posY;
    public string negY;
    public List<Prototype> posXNeighbors = new();
    public List<Prototype> negXNeighbors = new();
    public List<Prototype> posZNeighbors = new();
    public List<Prototype> negZNeighbors = new();
    public List<Prototype> posYNeighbors = new();
    public List<Prototype> negYNeighbors = new();
}
