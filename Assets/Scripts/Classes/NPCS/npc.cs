﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Path;
using System.Threading;
using RPG;

[System.Serializable]
public class npc : MonoBehaviour{
    public static List<npc> allNPCs = new List<npc>();
	
    //protected => this scope allows the variable to be accessed from within this class and any sub-class
    protected int age;
    protected int income;
    protected string description;
    protected SpriteRenderer skin;
    [Range(0f, 1f)]//Creates a slider in Unity Inspector that will let you sett the value from 0 to 1
    protected float health;
    [Range(0f, 1f)]
    protected float sanity;
    [Range(0f, 1f)]
    protected float rest;
    protected bool male;
    [SerializeField]protected Task myTask;

    protected delegate void Trigger();
    Trigger taskChanged;
    Trigger targetReached;

	public Tile target;//Where does the npc want to go
    public Tile position;//Where is the npc
    public float speed = 0.25f;//How long does it take the npc to move one tile;
    public List<Tile> path = new List<Tile>();

    protected virtual void Awake()
	{
        allNPCs.Add(this);
        taskChanged += OnTaskChanged;
        targetReached += OnTargetReached;
        position = TileGenerator.map[Mathf.RoundToInt(transform.position.x)][Mathf.RoundToInt(transform.position.y)]; //TO DO: SET THE POSITION ON CREATION
		skin = GetComponent<SpriteRenderer> (); // Get the refference for Sprite Rendere so thatwe can change the colour
        speed = 0.25f;
    }

    public npc()
    {
        age = 30;
        income = 100;
        description = "Empty";
        health = 1f;     
        sanity = 1f;
        rest = 1f;
        male = false;
    }
    public npc(int _age, int _income, string _description, float _health, float _sanity, float _rest, bool _male)
    {
        age = _age;
        income = _income;
        description = _description;
        health = _health;
        sanity = _sanity;
        rest = _rest;
        male = _male;
    }

	public void ChangeColour(Color newCol)
	{
		skin.color = newCol;
	}

	void OnMouseDown()
	{
        UIController.selectedNPCgo = this.gameObject;
	}

    #region Movement
    public void SetTargetLocation(int x = 0, int y = 0)
	{
        StopAllCoroutines();
        if (!TileGenerator.map[x][y].isWalkable) { Debug.LogWarning("You're goal is not walkable."); targetReached();  return; }
		target = TileGenerator.map [x][y];

        StartCoroutine(WaitForMyThread());

        //MoveTo(target);
	}

    IEnumerator WaitForMyThread()
    {
        Thread pathfinding = new Thread(MoveTo);
        pathfinding.Start();
        while (pathfinding.IsAlive)
        {
            yield return null;
        }
        if(path.Count == 0)
        {
            //Debug.Log("Couldn't find a path to my target.");
            targetReached(); //TODO: Should actually dump the target back to task manager, probably
        }
        MoveTo2();
    }

    public void MoveTo()
    {

        path = Pathfinding.AStar(TileGenerator.map, position, target);
        //Debug.Log("Our path is this long: " + path.Count);
        
        //Debug.Log(dist[target]);
    }

    public void MoveTo2()
    {
        Debug.Log("On our way");
        if (path.Count > 0)
        {
            StartCoroutine(TileByTile(path));
        }
        else
        {
            Debug.LogWarning("There is no way to reach your target.");
        }
    }

    //Generates a list of tiles the NPC has to go through in order to get to his target location
    //We cannot simply go through a dictionary therefore we made this helper function
    private List<Tile> TileList(Dictionary<Tile, Tile> prev, Tile target)
    {
        List<Tile> temp = new List<Tile>();
        if(position == target)
        {
            temp.Add(target);
            return temp;
        }
    
        Tile key = target;
        temp.Add(target);
        while (key != position)
        {
            Tile t = prev[key];
            key = t;
            temp.Add(t);
        }
        
        return temp;
    }

    //Coroutine that moves the NPC by one tile and waits some time between the steps
    //Coroutines are functions that can pause their execution for a certain amount of time and then continue
    IEnumerator TileByTile(List<Tile> list)
    {
        for (int i = list.Count - 1; i >= 0 ; i--)
        {
            StartCoroutine(MoveObjectLerp(transform.position, list[i].transform.position, speed * position.difficulty));
            position = list[i];
            yield return new WaitForSeconds(speed*position.difficulty);
        }
        targetReached();
    }

    //interpolates every step to create smooth motion
    IEnumerator MoveObjectLerp(Vector3 source, Vector3 target, float overTime)
    {
        float startTime = Time.time;
        while (Time.time < startTime + overTime)
        {
            transform.position = Vector3.Lerp(source, target, (Time.time - startTime) / overTime);
            yield return null;
        }
        transform.position = target;
    }
    #endregion
    
    public Task GetTask()
    {
        return myTask;
    }

    public void GiveTask(Task newTask)
    {
        myTask = newTask;
        taskChanged();
    }

    void OnTaskChanged()
    {
        SetTargetLocation(Mathf.RoundToInt(myTask.target.x), Mathf.RoundToInt(myTask.target.y));
    }

    void OnTargetReached()
    {
        //Debug.Log("I've reached my target. Love, " + this.gameObject);
        myTask = null;
    }
}
