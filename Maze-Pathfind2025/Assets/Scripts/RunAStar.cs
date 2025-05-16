using System;
using System.Collections.Generic;
using UnityEngine;

// A* needs only a WeightedGraph and a location type L, and does *not*
// have to be a grid. However, in the example code I am using a grid.
public interface WeightedGraph<L>
{
    double Cost(Location a, Location b);
    IEnumerable<Location> Neighbors(Location id);
}


public struct Location
{
    // Implementation notes: I am using the default Equals but it can
    // be slow. You'll probably want to override both Equals and
    // GetHashCode in a real project.

    public readonly int x, y;
    public Location(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}


public class SquareGrid : WeightedGraph<Location>
{
    // Implementation notes: I made the fields public for convenience,
    // but in a real project you'll probably want to follow standard
    // style and make them private.

    public static readonly Location[] DIRS = new[]
        {
            new Location(1, 0),
            new Location(0, -1),
            new Location(-1, 0),
            new Location(0, 1)
        };

    public int width, height;
    public HashSet<Location> walls = new HashSet<Location>();
    public HashSet<Location> forests = new HashSet<Location>();

    public SquareGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public bool InBounds(Location id)
    {
        return 0 <= id.x && id.x < width
            && 0 <= id.y && id.y < height;
    }

    public bool Passable(Location id)
    {
        return !walls.Contains(id);
    }

    public double Cost(Location a, Location b)
    {
        return forests.Contains(b) ? 5 : 1;
    }

    public IEnumerable<Location> Neighbors(Location id)
    {
        foreach (var dir in DIRS)
        {
            Location next = new Location(id.x + dir.x, id.y + dir.y);
            if (InBounds(next) && Passable(next))
            {
                yield return next;
            }
        }
    }
}


// When I wrote this code in 2015, C# didn't have a PriorityQueue<>
// class. It was added in 2020; see
// https://github.com/dotnet/runtime/issues/14032
//
// This is a placeholder PriorityQueue<> that runs inefficiently but
// will allow you to run the sample code on older C# implementations.
//
// If you're using a version of C# that doesn't have PriorityQueue<>,
// consider using one of these fast libraries instead of my slow
// placeholder:
//
// * https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// * https://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx
// * http://xfleury.github.io/graphsearch.html
// * http://stackoverflow.com/questions/102398/priority-queue-in-net
public class PriorityQueue<TElement, TPriority>
{
    private List<Tuple<TElement, TPriority>> elements = new List<Tuple<TElement, TPriority>>();

    public int Count
    {
        get { return elements.Count; }
    }

    public void Enqueue(TElement item, TPriority priority)
    {
        elements.Add(Tuple.Create(item, priority));
    }

    public TElement Dequeue()
    {
        Comparer<TPriority> comparer = Comparer<TPriority>.Default;
        int bestIndex = 0;

        for (int i = 0; i < elements.Count; i++)
        {
            if (comparer.Compare(elements[i].Item2, elements[bestIndex].Item2) < 0)
            {
                bestIndex = i;
            }
        }

        TElement bestItem = elements[bestIndex].Item1;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}


/* NOTE about types: in the main article, in the Python code I just
 * use numbers for costs, heuristics, and priorities. In the C++ code
 * I use a typedef for this, because you might want int or double or
 * another type. In this C# code I use double for costs, heuristics,
 * and priorities. You can use an int if you know your values are
 * always integers, and you can use a smaller size number if you know
 * the values are always small. */

public class AStarSearch
{
    public Dictionary<Location, Location> cameFrom
        = new Dictionary<Location, Location>();
    public Dictionary<Location, double> costSoFar
        = new Dictionary<Location, double>();

    // Note: a generic version of A* would abstract over Location and
    // also Heuristic
    static public double Heuristic(Location a, Location b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }

    public AStarSearch(WeightedGraph<Location> graph, Location start, Location goal)
    {
        var frontier = new PriorityQueue<Location, double>();
        frontier.Enqueue(start, 0);

        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            if (current.Equals(goal))
            {
                break;
            }

            foreach (var next in graph.Neighbors(current))
            {
                double newCost = costSoFar[current]
                    + graph.Cost(current, next);
                if (!costSoFar.ContainsKey(next)
                    || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    double priority = newCost + Heuristic(next, goal);
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }
    }
}

public class RunAStar: MonoBehaviour
{
    static void DrawGrid(SquareGrid grid, AStarSearch astar)
    {
        // Print out the cameFrom array
        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                Location id = new Location(x, y);
                Location ptr = id;
                if (!astar.cameFrom.TryGetValue(id, out ptr))
                {
                    ptr = id;
                }
                if (grid.walls.Contains(id)) { Debug.Log("##"); }
                else if (ptr.x == x + 1) { Debug.Log("\u2192 "); }
                else if (ptr.x == x - 1) { Debug.Log("\u2190 "); }
                else if (ptr.y == y + 1) { Debug.Log("\u2193 "); }
                else if (ptr.y == y - 1) { Debug.Log("\u2191 "); }
                else { Debug.Log("* "); }
            }
            Console.WriteLine();
        }
    }

    void PrintGrid(SquareGrid grid)
    {
        for (int y = grid.height - 1; y >= 0; y--)
        {
            string line = "";
            for (int x = 0; x < grid.width; x++)
            {
                var loc = new Location(x, y);
                if (grid.walls.Contains(loc))
                {
                    line += "#";
                }
                else if (grid.forests.Contains(loc))
                {
                    line += "F";
                }
                else
                {
                    line += ".";
                }
            }
            Debug.Log(line);
        }
    }

    public SquareGrid grid = null;


    void InitializeGrid()
    {
        // Make "diagram 4" from main article
        grid = new SquareGrid(10, 10);

        for (var x = 1; x < 4; x++)
        {
            for (var y = 7; y < 9; y++)
            {
                grid.walls.Add(new Location(x, y));
            }
        }
        grid.forests = new HashSet<Location>
            {
                new Location(3, 4), new Location(3, 5),
                new Location(4, 1), new Location(4, 2),
                new Location(4, 3), new Location(4, 4),
                new Location(4, 5), new Location(4, 6),
                new Location(4, 7), new Location(4, 8),
                new Location(5, 1), new Location(5, 2),
                new Location(5, 3), new Location(5, 4),
                new Location(5, 5), new Location(5, 6),
                new Location(5, 7), new Location(5, 8),
                new Location(6, 2), new Location(6, 3),
                new Location(6, 4), new Location(6, 5),
                new Location(6, 6), new Location(6, 7),
                new Location(7, 3), new Location(7, 4),
                new Location(7, 5)
            };
    }

    private const int GRID_WIDTH = 10;
    private const int GRID_HEIGHT = 10;

    int[,] CreateGrid()
    {
        int[,] maze = new int[GRID_HEIGHT, GRID_WIDTH]
        {
            {  1,   1,   1,   1,   1,   1,   1,   1,   1,   1 },
            {  1,   2,   1,   2,   0,   0,   0,   0,   0,   1 },
            {  1,   2,   1,   2,   0,   2,   2,   2,   2,   1 },
            {  1,   2,   1,   2,   0,   0,   0,   0,   0,   1 },
            {  1,   2,   1,   2,   1,   0,   1,   1,   1,   1 },
            {  1,   2,   1,   2,   1,   0,   0,   0,   0,   1 },
            {  1,   2,   2,   2,   1,   0,   2,   2,   2,   1 },
            {  1,   2,   2,   2,   1,   0,   2,   2,   2,   1 },
            {  1,   2,   2,   2,   1,   0,   0,   0,   0,   1 },
            {  1,   1,   1,   1,   1,   1,   1,   1,   1,   1 }

        };


        grid = new SquareGrid(10, 10);

        for (int i = 0; i < GRID_HEIGHT; i++)
        {
            for (int j = 0; j < GRID_WIDTH; j++)
            {
                if (maze[i, j] == 1) //pedra
                {
                    grid.walls.Add(new Location(j, i));
                }
                else if (maze[i, j] == 2) //floresta
                {
                    grid.forests.Add(new Location(j, i));
                }
            }

        }
        return maze;
    }


    //void OnDrawGizmos()
    //{
       
    //    if (grid == null) InitializeGrid();  // Só desenha se já existir o grid

    //    for (int x = 0; x < grid.width; x++)
    //    {
    //        for (int y = 0; y < grid.height; y++)
    //        {
    //            Vector3 pos = new Vector3(x, 0, y);
    //            var loc = new Location(x, y);

    //            if (grid.walls.Contains(loc))
    //                Gizmos.color = Color.black;
    //            else if (grid.forests.Contains(loc))
    //                Gizmos.color = new Color(0.0f, 0.5f, 0.0f); // verde escuro para floresta
    //            else
    //                Gizmos.color = Color.white; // caminho normal

    //            Gizmos.DrawCube(pos, Vector3.one * 0.9f); // Cubo levemente menor para dar espaço entre os tiles
    //        }
    //    }
    //}

    int[,] GenerateMaze(List<Location> path = null)
    {
        int[,] maze = new int[GRID_HEIGHT, GRID_WIDTH];

        for (int y = grid.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < grid.width; x++)
            {
                var loc = new Location(x, y);
                if (grid.walls.Contains(loc))
                {
                    maze[y, x] = 1; //pedra
                }
                else if (grid.forests.Contains(loc))
                {
                    maze[y, x] = 2; //forest
                }
                else
                {
                    maze[y, x] = 0; //sand
                }
            }
        }

        if (path != null)
        { 
            for (int i = 0; i < path.Count; i++)
            {
                int x = path[i].x;
                int y = path[i].y;
                maze[y, x] = 3; //path

            }
        }
        
        return maze;
    }


    int[,] AddPathToMaze(List<Location> path, int[,] old_maze)
    {
        int[,] maze = old_maze;

        for (int i = 0; i < path.Count; i++)
        {
            int x = path[i].x;
            int y = path[i].y;
            maze[y, x] = 3; //path
        }

        return maze;
    }

    List<Location> ReconstructPath(Location start, Location goal, Dictionary<Location, Location> cameFrom)
    {
        List<Location> path = new List<Location>();
        Location current = goal;

        if (!cameFrom.ContainsKey(goal))
        {
            return path; // caminho não encontrado
        }

        while (!current.Equals(start))
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start); // opcional: adicionar o ponto de origem
        path.Reverse();
        return path;
    }




    public List<GameObject> tileset = new List<GameObject>();
    public GameObject rat, cheese;

    float tileWidth = 10.0f;
    float tileDepth = 10.0f;

    float xi = -25.0f;
    float zi = 25.0f;

    void Start()
    {
        //if (grid == null) InitializeGrid();

        int[,] maze = CreateGrid();


        //Location start = new Location(1, 4);
        Location start = new Location(1, 1);
        Location goal = new Location(8, 5);
        

        // Roda A*
        var astar = new AStarSearch(grid, start,
                                    goal);

        List<Location> path = ReconstructPath(start, goal, astar.cameFrom);
        // Desenhar a grid depois da aplicação do A*

        //Gerando a matriz de índices baseado na grid
        //int[,] maze = GenerateMaze();
        //int[,] maze = GenerateMaze(path);
        maze = AddPathToMaze(path, maze);

        for (int i = 0; i < GRID_HEIGHT; i++) //z
        {
            for (int j = 0; j < GRID_WIDTH; j++) //x
            {
                GameObject tilePrefab = tileset[maze[i, j]];
                Vector3 p = tilePrefab.transform.position;
                p.x = xi + j * tileWidth;
                p.z = zi - i * tileDepth;

                GameObject newTile = Instantiate(tilePrefab, p, Quaternion.identity) as GameObject;

            }
        }


        //DrawGrid(grid, astar);


        Vector3 posRat = new Vector3(xi + start.x * tileWidth, 0.34f, zi - start.y * tileDepth);
        Vector3 posCheese = new Vector3(xi + goal.x * tileWidth, 4.4f, zi - goal.y * tileDepth);
        
        rat.transform.position = posRat;
        cheese.transform.position = posCheese;  
        
   
       
    }

    





}