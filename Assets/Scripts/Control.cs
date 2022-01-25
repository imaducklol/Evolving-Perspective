using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Control : MonoBehaviour
{
    public List<GameObject> FoodObj = Storage.FoodObj;
    public List<Vector3>    FoodPos = Storage.FoodPos;
    public List<Agent>      Agents  = Storage.Agents;

    [SerializeField] GameObject agentPrefab;
    [SerializeField] GameObject foodPrefab;
    [SerializeField] Transform agentParent;
    [SerializeField] Transform foodParent;    
    [SerializeField] int initialAgentQuantity;
    [SerializeField] int initialFoodQuantity;
    [SerializeField] int maximumModifier;
    [SerializeField] int foodRange;
    
    // Agent things
    private int wanderRange = 10;
    private int speedMult   = 3;
    private int deathMult   = 10;
    private int senseMult   = 5;
    private float offspringVarience = .2f;
    
    private bool cycleComplete = false;


    void Start()
    {
        SpawnFood();
        
        // Spawn Initial Agents
        for (int i = 0; i < initialAgentQuantity; i++)
        {
            // The agent
            GameObject agent;

            // Initiate with agent prefab
            agent = Instantiate(agentPrefab);
            
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
            Agents.Add(new Agent());
            Agents[i].obj = agent;
            Agents[i].id = i;
            agent.GetComponent<PerAgentControl>().localID = i;
            
            
            // Nav Setup
            agent.GetComponent<NavMeshAgent>().speed = Agents[Agents.Count - 1].speed * speedMult;
            Wander(Agents[Agents.Count-1].obj);
            
        }
    }

    void Update()
    {
        if (!cycleComplete) {UpdateAgents();}
        
        if (cycleComplete)  {NewAgentCycle();}
    }


    void UpdateAgents()
    {
        foreach (Agent agent in Agents)
        {
            //Debug.Log(agent.foodGotten);

            NavMeshAgent agentNav = agent.obj.GetComponent<NavMeshAgent>();
            
            // Energy
            agent.energy -= Time.deltaTime * (agent.speed + agent.sense) / deathMult;
            //Debug.Log(agent.energy);
            if (agent.energy <= 0 && !agent.safe) 
            {
                agent.obj.SetActive(false);
                agent.done = true;
                continue;
            }

            // Wander if close to end of last 
            if (agentNav.remainingDistance <= agentNav.stoppingDistance && agent.wanderingForFood) 
            {
                agent.wanderingForFood = true;
                agent.obj.GetComponent<PerAgentControl>().wanderingForFood = true;
                Wander(agent.obj);
            }
        
            // Looking for target
            if (FoodPos.Count > 0 && agent.wanderingForFood && !agent.goingHome) 
            {
                Vector3 posibleFood = GetClosestFood(FoodPos, agent.obj.transform.position);
                if ((agent.obj.transform.position - posibleFood).magnitude < agent.sense * senseMult)
                {
                    agentNav.SetDestination(posibleFood);
                    agent.foodDestination = posibleFood;
                    agent.wanderingForFood = false;
                    agent.obj.GetComponent<PerAgentControl>().wanderingForFood = false;            
                }
            }
            // If another agent gets to the food first
            if (agent.resetWander) 
            {
                agent.resetWander = false;
                Wander(agent.obj);
                agent.wanderingForFood = true;
                agent.obj.GetComponent<PerAgentControl>().wanderingForFood = true;

            }

            // Home
            if (agent.foodGotten >= 1 && !agent.goingHome) 
            {
                //Debug.Log(agent.id);
                agent.goingHome = true;
                agent.obj.GetComponent<PerAgentControl>().goingHome = true;
                agent.wanderingForFood = false;
                agent.obj.GetComponent<PerAgentControl>().wanderingForFood = false;
                Home(agent.obj);
            }

            // Home?
            if ((Mathf.Abs(agent.obj.transform.position.x) >= 18 || Mathf.Abs(agent.obj.transform.position.z) >= 18) && agent.foodGotten >= 1 && !agent.safe) 
            {
                agent.safe = true; 
                agent.obj.GetComponent<PerAgentControl>().safe = true;
                agent.done = true;
                agent.obj.GetComponent<PerAgentControl>().done = true;
            }            
        }

        CheckCompletion();
    }
    
    void NewAgentCycle()
    {
        foreach (Agent agent in Agents) 
            {
                agent.position = agent.obj.transform.position;
                Destroy(agent.obj);
            }
            
            
            // Transfer surviving agents
            List<Agent> NewAgents = new List<Agent>();
            for (int i = 0; i < Agents.Count; i++)
            {
                if (Agents[i].foodGotten > 0 && Agents[i].safe) 
                {
                    // Surviving
                    NewAgents.Add(new Agent());
                    NewAgents[NewAgents.Count-1].energy = Agents[i].energy;
                    NewAgents[NewAgents.Count-1].size = Agents[i].size;
                    NewAgents[NewAgents.Count-1].speed = Agents[i].speed;
                    NewAgents[NewAgents.Count-1].sense = Agents[i].sense;
                    NewAgents[NewAgents.Count-1].position = Agents[i].position;
                    
                    // Offspring
                    if (Agents[i].foodGotten > 1)
                    {
                        NewAgents.Add(new Agent());
                        NewAgents[NewAgents.Count-1].energy = Agents[i].energy;
                        NewAgents[NewAgents.Count-1].size  = Agents[i].size ;
                        NewAgents[NewAgents.Count-1].speed = Agents[i].speed + Random.Range(-offspringVarience, offspringVarience);
                        NewAgents[NewAgents.Count-1].sense = Agents[i].sense + Random.Range(-offspringVarience, offspringVarience);
                        NewAgents[NewAgents.Count-1].position = Agents[i].position;
                    }
                }

            }
            
            Agents.Clear();
            Agents.TrimExcess();

            foreach (Agent agent in NewAgents) {Agents.Add(agent);} 

            // Recreating the objecters for each agent
            foreach (Agent agent in Agents)
            {
                // The agent object
                GameObject agentObj;
                // Initiate with agent prefab
                agentObj = Instantiate(agentPrefab);

                agentObj.transform.position = agent.position;
                /*            
                // Place the agent on one of the four edges
                switch (Random.Range(0, 4))
                {
                    case 0:
                        agentObj.transform.position = new Vector3(20, 1, Random.Range(-20, 20));
                        break;
                    case 1:
                        agentObj.transform.position = new Vector3(-20, 1, Random.Range(-20, 20));
                        break;
                    case 2:
                        agentObj.transform.position = new Vector3(Random.Range(-20, 20), 1, 20);
                        break;
                    case 3:
                        agentObj.transform.position = new Vector3(Random.Range(-20, 20), 1, -20);
                        break;
                }*/
                            
                // Set the agent parent for organization
                agentObj.transform.SetParent(agentParent, false);

                agent.obj = agentObj;
                //agent.obj.GetComponent<PerAgentControl>().localID = agent.id;

                // Nav Setup
                agent.obj.GetComponent<NavMeshAgent>().speed = agent.speed * speedMult;
                Wander(agent.obj);

                agent.obj.SetActive(true);
            }
            
            // ID shenanigins
            for (int i = 0; i < Agents.Count; i++) 
            {
                Agents[i].id = i;
                Agents[i].obj.name = i.ToString();
                Agents[i].obj.GetComponent<PerAgentControl>().localID = i;
            }

            //foreach (Agent agent in Agents) {ResetAgent(agent);}

            // Destroy all the old deactivated food
            foreach (GameObject food in FoodObj)
            {
                Destroy(food);
            }
            FoodPos.Clear();
            // Spawn new food
            SpawnFood();

            cycleComplete = false;
    }

    void SpawnFood()
    {
        FoodObj.Clear();
        FoodPos.Clear();
        // Spawn Food
        for (int i = 0; i < initialFoodQuantity; i++) {
            // The food
            GameObject food;
            // Initiate with food prefab
            food = Instantiate(foodPrefab);
            // Random location across the floor
            food.transform.position = new Vector3(Random.Range(-foodRange, foodRange), .5f, Random.Range(-foodRange, foodRange));
            // Set the food parent for organization
            food.transform.SetParent(foodParent, false);
            FoodPos.Add(food.transform.position);
            FoodObj.Add(food);
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

    void Home(GameObject obj)
    {
        Vector3 pos = obj.transform.position;
        Vector3 center = Vector3.zero;
        if (Mathf.Abs(center.x - pos.x) > Mathf.Abs(center.z - pos.z)) 
        {
            if (pos.x > 0) {pos.x = 20;} else {pos.x = -20;}
        } 
        else 
        {
            if (pos.z > 0) {pos.z = 20;} else {pos.z = -20;}
        }
        obj.GetComponent<NavMeshAgent>().SetDestination(pos);
    }

    Vector3 GetClosestFood(List<Vector3> foodPos, Vector3 agentPos)
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

    void CheckCompletion() 
    {
        bool isComplete = true;
        foreach (Agent agent in Agents) 
        {
            if (!agent.done) {isComplete = false;}
        }
        if (isComplete) {Debug.Log("complete");}
        cycleComplete = isComplete;
    }

    void ResetAgent(Agent agent)
    {
        agent.energy           = 1;
        agent.foodGotten       = 0;
        agent.wanderingForFood = true;
        agent.goingHome        = false;
        agent.foodDestination  = Vector3.positiveInfinity;
        agent.resetWander      = false;
        agent.safe             = false;
        agent.done             = false;
    }
}
