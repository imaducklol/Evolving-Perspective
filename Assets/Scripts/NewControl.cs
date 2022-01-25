using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NewControl : MonoBehaviour
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

    // Start is called before the first frame update
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
            
            // Set the agent parent for organization
            agent.transform.SetParent(agentParent, false);
            
            // Add agent to list
            Agents.Add(new Agent());
            Agents[i].obj = agent;
            Agents[i].id = i;
            agent.GetComponent<PerAgentControl>().localID = i;
            
            // Place the agent on one of the four edges
            switch (Random.Range(0, 4))
            {
                case 0:
                    Agents[i].obj.transform.position = new Vector3(20, 1, Random.Range(-20, 20));
                    break;
                case 1:
                    Agents[i].obj.transform.position = new Vector3(-20, 1, Random.Range(-20, 20));
                    break;
                case 2:
                    Agents[i].obj.transform.position = new Vector3(Random.Range(-20, 20), 1, 20);
                    break;
                case 3:
                    Agents[i].obj.transform.position = new Vector3(Random.Range(-20, 20), 1, -20);
                    break;
            }
            
            // Nav Setup
            Agents[i].obj.GetComponent<NavMeshAgent>().speed = Agents[i].speed * speedMult;
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Spawn the food
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
}
