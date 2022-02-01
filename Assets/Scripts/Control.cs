using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

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
    [SerializeField] float foodChancePercentage;
    [SerializeField] int maximumModifier;
    [SerializeField] int foodRange;
    
    // Agent things
    private int wanderRange = 10;
    private int speedMult   = 3;
    private float deathMult   = 1/20f;
    private int senseMult   = 5;
    //private float offspringVarience = .2f;
    
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
        if (!cycleComplete) {UpdateAgents();}
        
        if (cycleComplete)  {NewAgentCycle();}
    }


    void UpdateAgents()
    {
        foreach (Agent agent in agents)
        {
            if (agent.done)
                continue;
            
            // Energy
            agent.energy -= Time.deltaTime * (agent.speed + agent.sense) * deathMult;
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
            
            // Trying to get food, checking if a wall is still reachable in time and if theres any food left
            if (agent.foodGotten < 1 && !agent.gettingFood)
                GetFood(agent);
            if (WallAttainable(agent) && agent.foodGotten < 2 && foodPos.Count > 0 && !agent.gettingFood && !agent.goingHome)
                GetFood(agent);

            // Going home
            if (agent.foodGotten >= 2 && !agent.goingHome)
                Home(agent);
            if(agent.foodGotten < 2 && !WallAttainable(agent))
                Home(agent);
            
            // Wander if close to end of last 
            NavMeshAgent agentNav = agent.obj.GetComponent<NavMeshAgent>();
            if (agentNav.remainingDistance <= agentNav.stoppingDistance && !agent.gettingFood && !agent.goingHome) 
            {
                Wander(agent.obj);
                //Debug.Log(agent.id + " is wandering");
            }
        
            

            // Home?
            if ((Mathf.Abs(agent.obj.transform.position.x) >= 19 || Mathf.Abs(agent.obj.transform.position.z) >= 19) && agent.foodGotten >= 1) 
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
        foreach (Agent agent in agents) 
        {
            agent.position = agent.obj.transform.position;
            Destroy(agent.obj);
        }
        
        
        // Transfer surviving agents
        List<Agent> newAgents = new List<Agent>();
        foreach (Agent agent in agents)
        {
            if (agent.foodGotten > 0 && agent.safe)
            {
                // Surviving
                newAgents.Add(new Agent());
                newAgents[newAgents.Count - 1].size     = agent.size;
                newAgents[newAgents.Count - 1].speed    = agent.speed;
                newAgents[newAgents.Count - 1].sense    = agent.sense;
                newAgents[newAgents.Count - 1].position = agent.position; 
                
                // Offspring
                if (agent.foodGotten > 1)
                {
                    newAgents.Add(new Agent());
                    newAgents[newAgents.Count - 1].size     = agent.size;
                    newAgents[newAgents.Count - 1].speed    = agent.speed; // + Random.Range(-offspringVarience, offspringVarience);
                    newAgents[newAgents.Count - 1].sense    = agent.sense; // + Random.Range(-offspringVarience, offspringVarience);
                    newAgents[newAgents.Count - 1].position = agent.position;
                }
            }
        }

        agents.Clear();
        agents.TrimExcess();

        foreach (Agent agent in newAgents) {agents.Add(agent);} 

        // Recreating the objecters for each agent
        foreach (Agent agent in agents)
        {
            // The agent object
            // Initiate with agent prefab
            GameObject agentObj = Instantiate(agentPrefab);

            agentObj.transform.position = agent.position;
                        
            // Set the agent parent for organization
            agentObj.transform.SetParent(agentParent, false);

            agent.obj = agentObj;

            // Nav Setup
            agent.obj.GetComponent<NavMeshAgent>().speed = agent.speed * speedMult;
            Wander(agent.obj);

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

        cycleComplete = false;
    }

    void SpawnFood()
    {
        foodObj.Clear();
        foodPos.Clear();
        // Spawn Food
        for (int x = 0; x < 36; x+=4) {
            for (int y = 0; y < 36; y+=4) {
                if (Random.Range(0f, 1f) < foodChancePercentage / 100) {
                    // Initiate with food prefab
                    GameObject food = Instantiate(foodPrefab);
                    // Random location across the floor
                    float xOffset = Random.Range(-1f, 1f);
                    float yOffset = Random.Range(-1f, 1f);
                    food.transform.position = new Vector3(x-18+xOffset, .25f, y-18+yOffset);
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
        float timeRemaining = agent.energy / (agent.speed + agent.sense) * deathMult - 1;
        
        float distToXpos = objPos.x - 20;
        float distToXneg = objPos.x + 20;
        float distToZpos = objPos.z - 20;
        float distToZneg = objPos.z + 20;
        
        if (distToXpos < distToXpos * speed * timeRemaining &&
            distToXneg < distToXneg * speed * timeRemaining &&
            distToZpos < distToZpos * speed * timeRemaining &&
            distToZneg < distToZneg * speed * timeRemaining)
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
