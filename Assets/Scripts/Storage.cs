using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Storage
{
    public static List<GameObject>  FoodObj = new List<GameObject>();
    public static List<Vector3>     FoodPos = new List<Vector3>();
    public static List<Agent>       Agents  = new List<Agent>();

}

// Agent data
public class Agent {
    public int id;
    public float energy  = 1;
    public float size    = 1;
    public float speed   = 1;
    public float sense   = 1;
    public int foodGotten = 0;

    // States
    public bool wanderingForFood = true;
    public bool goingHome        = false;

    // Food stuff
    public Vector3 foodDestination;
    public bool resetWander = true;

    public bool safe = false;
    public bool done = false;
    public GameObject obj;
    public Vector3 position;
}
