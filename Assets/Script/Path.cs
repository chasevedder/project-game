using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Path : MonoBehaviour
{
    
    public bool[,] grid;
    public int gridWidth;
    public int gridHeight;

    public Vector2 start;
    public Vector2 end;

    public List<Vector2> path = null;

    private bool enabling;
    private bool drawing;

    // Start is called before the first frame update
    void Start()
    {
        Random rnd = new Random();
        grid = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (drawing)
        {
            int x, y;
            Vector2 mousePos = Pointer.current.position.ReadValue();
            x = (int)(mousePos.x / (Screen.width / 32.0f));
            y = (int)(mousePos.y / (Screen.height / 18.0f));
            ToggleWall(x, y);
        }
    }

    public void StartPathing(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed)
        {
            return;
        }

        List<Vector2> path = FindPathTo(start, end);
        StringBuilder pathString = new StringBuilder();
        foreach (Vector2 node in path)
        {
            pathString.Append(node + " -> ");
        }

        this.path = path;

        Debug.Log(pathString.ToString());
    }

    public void ToggleWall(InputAction.CallbackContext context)
    {
        
        int x, y;
        Vector2 mousePos = Pointer.current.position.ReadValue();
        x = (int)(mousePos.x / (Screen.width / 32.0f));
        y = (int)(mousePos.y / (Screen.height / 18.0f));
        if (context.phase == InputActionPhase.Started)
        {
            enabling = true;
            drawing = true;
            ToggleWall(x, y);
        }
        if (context.phase == InputActionPhase.Canceled)
        {
            drawing = false;
        }
        
    }

    public void EraseWall(InputAction.CallbackContext context)
    {
        int x, y;
        Vector2 mousePos = Pointer.current.position.ReadValue();
        x = (int)(mousePos.x / (Screen.width / 32.0f));
        y = (int)(mousePos.y / (Screen.height / 18.0f));
        if (context.phase == InputActionPhase.Started)
        {
            enabling = false;
            drawing = true;
            ToggleWall(x, y);
        }
        if (context.phase == InputActionPhase.Canceled)
        {
            drawing = false;
        }
    }

    public void ToggleWall(int x, int y)
    {
        grid[x, y] = enabling;
    }

    public List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        List<Vector2> currentPath = new List<Vector2>();
        currentPath.Add(current);
        while (cameFrom.Keys.Contains(current))
        {
            current = cameFrom[current];
            currentPath.Insert(0, current);
        }

        return currentPath;
    }

    public List<Vector2> FindPathTo(Vector2 startPos, Vector2 endPos)
    {
        List<Vector2> openSet = new List<Vector2>();
        openSet.Add(startPos);

        List<Vector2> closedSet = new List<Vector2>();

        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();

        Dictionary<Vector2, int> gScore = new Dictionary<Vector2, int>();
        gScore[startPos] = 0;

        Dictionary<Vector2, int> fScore = new Dictionary<Vector2, int>();
        fScore[startPos] = SimpleHeuristic(startPos, endPos);

        while (openSet.Count > 0)
        {
            Vector2 node = openSet[0];
            Debug.Log("Visiting: " + node);
            if (node.Equals(endPos))
            {
                return ReconstructPath(cameFrom, node);
            }

            openSet.Remove(node);
            foreach (Vector2 neighbor in GetNeighbors(node))
            {
                if (!grid[(int)neighbor.x, (int)neighbor.y])
                {
                    continue;
                }

                if (closedSet.Contains(neighbor))
                {
                    Debug.Log(neighbor + " was already closed");
                    continue;
                }

                int tentativeGScore = gScore[node] + 1;
                if (!gScore.Keys.Contains(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = node;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + SimpleHeuristic(neighbor, endPos);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                        Debug.Log("Adding: " + neighbor);
                    }
                }
            }
            closedSet.Add(node);
        }

        return null;
    }

    private int SimpleHeuristic(Vector2 node, Vector2 goal)
    {
        return (int) Math.Min(Math.Abs(goal.x - node.x), Math.Abs(goal.y - node.y));
    }

    private List<Vector2> GetNeighbors(Vector2 node)
    {
        List<Vector2> neighbors = new List<Vector2>();
        for (int i = -1; i <= 1; i += 2)
        {
            int x = (int)node.x + i;
            int y = (int)node.y;
            if (x < 0 || x >= gridWidth)
            {
                continue;
            }
            neighbors.Add(new Vector2(x, y));
        }

        for (int j = -1; j <= 1; j += 1)
        {
            int x = (int)node.x;
            int y = (int)node.y + j;
            if (y < 0 || y >= gridHeight)
            {
                continue;
            }
            neighbors.Add(new Vector2(x, y));
        }

        return neighbors;
    }

    private void OnDrawGizmos()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!grid[x, y])
                {
                    Gizmos.color = new Color(1.0f, 0, 0, 0.5f);
                }
                else if (path == null || !path.Contains(new Vector2(x, y)))
                {
                    Gizmos.color = new Color(0, 0, 0, 0.5f);
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                

                Vector3 startPos = Camera.main.ScreenToWorldPoint(new Vector3(0, 0));
                Vector3 endPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height));
                float width = endPos.x - startPos.x;
                float height = endPos.y - startPos.y;

                Gizmos.DrawCube(new Vector3(startPos.x + (x * (width / 32.0f)) + (width / 32.0f / 2.0f), startPos.y + (y * (height / 18.0f)) + (height / 18.0f / 2.0f)), new Vector3(width / 32.0f * 0.75f, height / 18.0f * 0.75f));
            }
        } 
    }
}
