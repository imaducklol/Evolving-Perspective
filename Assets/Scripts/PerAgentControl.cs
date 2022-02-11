using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerAgentControl : MonoBehaviour
{
    public int  localID;
    public int  collectedFood;
    
    public float timeRemaining;
    public float distToXpos;
    public float distToXneg;
    public float distToZpos;
    public float distToZneg;

    public bool gettingFood;
    public bool goingHome;
    public bool safe;
    public bool done;

    public float size;
    public float speed;
    public float sense;

    private void OnTriggerEnter(Collider other)
    {
        Storage.foodPos.Remove(other.transform.position);
        foreach (Agent agent in Storage.agents) 
        {
            if (agent.foodDestination == other.transform.position) 
            {
                agent.resetWander = true;
            }

            Storage.agents[localID].resetWander = false;
        }
        other.gameObject.SetActive(false);
        Storage.agents[localID].foodGotten += 1;
        Storage.agents[localID].gettingFood = false;
        gettingFood = false;
        collectedFood += 1;
        //Debug.Log(localID  + " got food");
    }
}