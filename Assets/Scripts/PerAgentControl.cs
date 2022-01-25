using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerAgentControl : MonoBehaviour
{
    public int  localID;
    public int  collectedFood;
    public bool isNew;
    public bool wanderingForFood;
    public bool goingHome;
    public bool safe;
    public bool done;

    private void OnTriggerEnter(Collider other)
    {
        Storage.FoodPos.Remove(other.transform.position);
        foreach (Agent agent in Storage.Agents) 
        {
            if (agent.foodDestination == other.transform.position) 
            {
                agent.resetWander = true;
            } 
        }
        other.gameObject.SetActive(false);
        Storage.Agents[localID].foodGotten += 1;
        collectedFood += 1;
        Debug.Log(localID  + " got food");
        Debug.Log("food gotten in global storage " + Storage.Agents[localID].foodGotten);
    }
}