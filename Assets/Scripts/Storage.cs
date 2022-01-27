using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Storage
{
    public static List<GameObject>  foodObj = new List<GameObject>();
    public static List<Vector3>     foodPos = new List<Vector3>();
    public static List<Agent>       agents  = new List<Agent>();

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
    public bool goingHome = false;
    public bool gettingFood;

    // Food stuff
    public Vector3 foodDestination;
    public bool resetWander = false;

    public bool safe = false;
    public bool done = false;
    public GameObject obj;
    public Vector3 position;
}
