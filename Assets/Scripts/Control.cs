using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Control : MonoBehaviour
{
    public List<GameObject> foodObj = Storage.foodObj;
    public List<Vector3>    foodPos = Storage.foodPos;
    private List<Agent>      agents  = Storage.agents;

    [SerializeField] GameObject agentPrefab;
    [SerializeField] GameObject foodPrefab;
    [SerializeField] Transform agentParent;
    [SerializeField] Transform foodParent;    
    [SerializeField] int initialAgentQuantity;
    [SerializeField, Range(0, 100)] float foodChancePercentage;
    [SerializeField] float offspringVarience;
    [SerializeField] float maximumModifier;
    [SerializeField] float minimumModifier;
    [SerializeField] int foodRange;
    
    [SerializeField, Range(0,1)] float FUCKTHISSHITENERGYVARIABLEGOOOOOOO;
    [SerializeField] private bool WallAttainableColorToggle;

    [SerializeField] private int cycleCount = 0;
    
    // Agent things
    private int wanderRange = 10;
    private int speedMult   = 3;
    private float deathMult = 1/20f;
    private int senseMult   = 5;
    
    private bool cycleComplete = false;
    
    
    void Start()
    {
        SpawnFood();
        
        // Spawn Initial Agents
        for (int i = 0; i < initialAgentQuantity; i++)
        {
            // Initiate with agent prefab
            GameObject agent = Instantiate(agentPrefab);
            
            // Place the agent on one of the four edges
            switch (Random.Range(0, 4))
            {
                case 0:
                    agent.transform.position = new Vector3(20, 1, Random.Range(-20, 20));
                    break;
                case 1:
                    agent.transform.position = new Vector3(-20, 1, Random.Range(-20, 20));
                    break;
                case 2:
                    agent.transform.position = new Vector3(Random.Range(-20, 20), 1, 20);
                    break;
                case 3:
                    agent.transform.position = new Vector3(Random.Range(-20, 20), 1, -20);
                    break;
            }
            
            // Set the agent parent for organization
            agent.transform.SetParent(agentParent, false);
            
            // Add agent to list
            agents.Add(new Agent());
            agents[i].obj = agent;
            agents[i].id = i;
            agents[i].obj.name = i.ToString();
            agent.GetComponent<PerAgentControl>().localID = i;
            
            
            // Nav Setup
            agent.GetComponent<NavMeshAgent>().speed = agents[agents.Count - 1].speed * speedMult;
            Wander(agents[agents.Count-1].obj);
            
        }
    }

    void Update()
    {
    }

    void FixedUpdate() 
    {
        if (!cycleComplete) {UpdateAgents();}
        //if (!cycleComplete) {WallattainableColor();}
        
        if (cycleComplete)  {NewAgentCycle();}
    }

    void WallAttainableColor ()
    {
        foreach (Agent agent in agents)
        {
            if (WallAttainable(agent))
                agent.obj.GetComponent<Renderer>().material.color = Color.green;
            else
                agent.obj.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void UpdateAgents()
    {
        foreach (Agent agent in agents)
        {
            if (agent.done)
                continue;
            
            // Energy
            agent.energy -= Time.deltaTime * (agent.speed + agent.sense) * deathMult;
            //agent.energy = FUCKTHISSHITENERGYVARIABLEGOOOOOOO;
            if (agent.energy <= 0 && !agent.safe) 
            {
                agent.obj.SetActive(false);
                agent.done = true;
                continue;
            }
            
            // If another agent gets to the food first
            if (agent.resetWander) 
            {
                agent.resetWander = false;
                agent.gettingFood = false;
                //Debug.Log(agent.id + " reset wander called");
                
            }

            if (WallAttainableColorToggle) WallAttainableColor();
            
            // Trying to get food, checking if a wall is still reachable in time and if theres any food left
            if (agent.foodGotten == 0 && !agent.gettingFood)
                GetFood(agent);
            if (WallAttainable(agent) && agent.foodGotten < 2 && 
                foodPos.Count > 0 && !agent.gettingFood && !agent.goingHome)
                GetFood(agent);

            // Going home
            if (agent.foodGotten >= 2 && !agent.goingHome)
                Home(agent);
            if(agent.foodGotten == 1 && !WallAttainable(agent) && !agent.goingHome)
                Home(agent);
            
            // Wander if close to end of last 
            NavMeshAgent agentNav = agent.obj.GetComponent<NavMeshAgent>();
            if (agentNav.remainingDistance <= agentNav.stoppingDistance && !agent.gettingFood && !agent.goingHome) 
            {
                Wander(agent.obj);
                //Debug.Log(agent.id + " is wandering");
            }
        
            

            // Home?
            if ((Mathf.Abs(agent.obj.transform.position.x) >= 19 || Mathf.Abs(agent.obj.transform.position.z) >= 19) && agent.goingHome) 
            {
                agent.safe = true; 
                agent.obj.GetComponent<PerAgentControl>().safe = true;
                agent.done = true;
                agent.obj.GetComponent<PerAgentControl>().done = true;
                agent.obj.GetComponent<NavMeshAgent>().velocity = Vector3.zero;
                agent.obj.GetComponent<NavMeshAgent>().speed = 0;
            }            
        }

        CheckCompletion();
    }

    void NewAgentCycle()
    {
        // Start off by destroying the old objects cause it makes my life easier
        foreach (Agent agent in agents) 
        {
            agent.position = agent.obj.transform.position;
            Destroy(agent.obj);
        }
        
        // Transfer surviving agents to the next day 
        List<Agent> newAgents = new List<Agent>();
        int index;
        foreach (Agent agent in agents)
        {
            if (agent.foodGotten > 0 && agent.safe)
            {
                // Copy agents that lived over
                newAgents.Add(new Agent());
                index = newAgents.Count - 1;
                newAgents[index].size     = agent.size;
                newAgents[index].speed    = agent.speed;
                newAgents[index].sense    = agent.sense;
                newAgents[index].position = agent.position; 
                
                // Using values from the parent agent, create offspring
                if (agent.foodGotten > 1)
                {
                    // Values from parents plus some bit of varience
                    newAgents.Add(new Agent());
                    index = newAgents.Count - 1;
                    newAgents[index].size     = agent.size;
                    newAgents[index].speed    = agent.speed + Random.Range(-offspringVarience, offspringVarience);
                    newAgents[index].sense    = agent.sense + Random.Range(-offspringVarience, offspringVarience);
                    newAgents[index].position = agent.position;

                    // Bounds
                    if (newAgents[index].size  < minimumModifier) newAgents[index].size  = minimumModifier;
                    if (newAgents[index].size  > maximumModifier) newAgents[index].size  = maximumModifier;
                    if (newAgents[index].speed < minimumModifier) newAgents[index].speed = minimumModifier;
                    if (newAgents[index].speed > maximumModifier) newAgents[index].speed = maximumModifier;
                    if (newAgents[index].sense < minimumModifier) newAgents[index].sense = minimumModifier;
                    if (newAgents[index].sense > maximumModifier) newAgents[index].sense = maximumModifier;
                }
            }
        }

        agents.Clear();
        agents.TrimExcess();

        foreach (Agent agent in newAgents) {agents.Add(agent);}
        

        // Recreating the gameobjects for each agent
        foreach (Agent agent in agents)
        {
            // Initiate new agent object with agent prefab
            GameObject agentObj = Instantiate(agentPrefab);
            
            // Set the object position to the previous generations position
            agentObj.transform.position = agent.position;
                        
            // Set the agent parent for organization
            agentObj.transform.SetParent(agentParent, false);

            // Assign the gameobject to the agent object
            agent.obj = agentObj;

            // Nav Setup and initial nav point set
            agent.obj.GetComponent<NavMeshAgent>().speed = agent.speed * speedMult;
            Wander(agent.obj);

            // Set object local values
            agent.obj.GetComponent<PerAgentControl>().size  = agent.size;
            agent.obj.GetComponent<PerAgentControl>().speed = agent.speed;
            agent.obj.GetComponent<PerAgentControl>().sense = agent.sense;
            
            
            agent.obj.SetActive(true);
        }
        
        // ID shenanigins
        for (int i = 0; i < agents.Count; i++) 
        {
            agents[i].id = i;
            agents[i].obj.name = i.ToString();
            agents[i].obj.GetComponent<PerAgentControl>().localID = i;
        }
        
        //foreach (Agent agent in Agents) {ResetAgent(agent);}

        // Destroy all the old deactivated food
        foreach (GameObject food in foodObj)
        {
            Destroy(food);
        }
        foodPos.Clear();
        // Spawn new food
        SpawnFood();

        cycleCount += 1;
        cycleComplete = false;
    }

    void SpawnFood()
    {
        foodObj.Clear();
        foodPos.Clear();
        // Spawn Food
        // Funky addition so it doesnt spawn on the walls
        for (int x = 0; x < 36; x+=4) {
            for (int y = 0; y < 34; y+=4) {
                if (Random.Range(0f, 1f) < foodChancePercentage / 100) {
                    // Initiate with food prefab
                    GameObject food = Instantiate(foodPrefab);
                    // Random location across the floor
                    float xOffset = Random.Range(-1f, 1f);
                    float yOffset = Random.Range(-1f, 1f);
                    food.transform.position = new Vector3(x-17+xOffset, .25f, y-17+yOffset);
                    // Set the food parent for organization
                    food.transform.SetParent(foodParent, false);
                    foodPos.Add(food.transform.position);
                    foodObj.Add(food);
                }
            }
        }
        /*for (int i = 0; i < foodQuantity; i++) {
            // The food
            // Initiate with food prefab
            GameObject food = Instantiate(foodPrefab);
            // Random location across the floor
            food.transform.position = new Vector3(Random.Range(-foodRange, foodRange), .5f, Random.Range(-foodRange, foodRange));
            // Set the food parent for organization
            food.transform.SetParent(foodParent, false);
            foodPos.Add(food.transform.position);
            foodObj.Add(food);
        }*/
    }
    
    void GetFood(Agent agent)
    {
        Vector3 posibleFood = GetClosestFood(agent.obj.transform.position);
        if ((agent.obj.transform.position - posibleFood).magnitude < agent.sense * senseMult)
        {
            agent.obj.GetComponent<NavMeshAgent>().SetDestination(posibleFood);
            agent.foodDestination = posibleFood;
            agent.gettingFood = true;
            agent.obj.GetComponent<PerAgentControl>().gettingFood = true;
            //Debug.Log(agent.id + " set food");
        }
    }

    void Wander(GameObject obj)
    {
        Vector3 finalPosition = obj.transform.position;
        Vector3 randomPosition = Random.insideUnitSphere * wanderRange;
        randomPosition += finalPosition;
        if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, wanderRange, 1))
        {
            obj.GetComponent<NavMeshAgent>().SetDestination(hit.position);
        }
    }

    void Home(Agent agent)
    {
        Vector3 pos = agent.obj.transform.position;
        Vector3 center = Vector3.zero;
        if (Mathf.Abs(center.x - pos.x) > Mathf.Abs(center.z - pos.z)) 
        {
            if (pos.x > 0) 
                pos.x = 20; 
            else
                pos.x = -20;
        }
        else
        {
            if (pos.z > 0) 
                pos.z = 20;
            else 
                pos.z = -20;
        }
        
        agent.obj.GetComponent<NavMeshAgent>().SetDestination(pos);
        
        agent.goingHome = true;
        agent.obj.GetComponent<PerAgentControl>().goingHome = true;
        //Debug.Log(agent.id + " going home");
    }

    Vector3 GetClosestFood(Vector3 agentPos)
    {
        // Initial Values
        Vector3 bestTarget = Vector3.zero;
        float ClosestDistanceSqr = Mathf.Infinity;

        // Distance to each target
        foreach (Vector3 potentialTarget in foodPos) 
        {
            Vector3 directionToTarget = potentialTarget - agentPos;
            float directionSqrToTarget = directionToTarget.sqrMagnitude;
            
            // Check if closest
            if (directionSqrToTarget < ClosestDistanceSqr)
            {
                ClosestDistanceSqr = directionSqrToTarget;
                bestTarget = potentialTarget;
            }
        }
        return bestTarget;
    }

    bool WallAttainable(Agent agent)
    {
        Vector3 objPos = agent.obj.transform.position;
        float speed = agent.obj.GetComponent<NavMeshAgent>().speed;
        // IDK if or why this works but it seems good. -1 to give them leeway
        float timeRemaining = agent.energy / ((agent.speed + agent.sense) * deathMult) - 2;
        agent.obj.GetComponent<PerAgentControl>().timeRemaining =  timeRemaining;

        float distToXneg = Mathf.Abs(objPos.x + 20);
        float distToXpos = Mathf.Abs(objPos.x - 20);
        float distToZneg = Mathf.Abs(objPos.z + 20);
        float distToZpos = Mathf.Abs(objPos.z - 20);

        agent.obj.GetComponent<PerAgentControl>().distToXpos = distToXpos;
        agent.obj.GetComponent<PerAgentControl>().distToXneg = distToXneg;
        agent.obj.GetComponent<PerAgentControl>().distToZpos = distToZpos;
        agent.obj.GetComponent<PerAgentControl>().distToZneg = distToZneg;
        
        if (distToXpos > speed * timeRemaining &&
            distToXneg > speed * timeRemaining &&
            distToZpos > speed * timeRemaining &&
            distToZneg > speed * timeRemaining)
            return false;
        return true;
    }
    
    void CheckCompletion() 
    {
        bool isComplete = true;
        foreach (Agent agent in agents) 
        {
            if (!agent.done) {isComplete = false;}
        }
        if (isComplete) {Debug.Log("complete");}
        cycleComplete = isComplete;
    }
}
