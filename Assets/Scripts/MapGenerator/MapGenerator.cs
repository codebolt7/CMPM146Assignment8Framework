using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MapGenerator : MonoBehaviour
{
    public List<Room> rooms;
    public Hallway vertical_hallway;
    public Hallway horizontal_hallway;
    public Room start;
    public Room target;

    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE;

    // set this to a high value when the generator works
    // for debugging it can be helpful to test with few rooms
    // and, say, a threshold of 100 iterations
    public int THRESHOLD;

    // keep the instantiated rooms and hallways here 
    private List<GameObject> generated_objects;

    // Moved occupied here for more convenience 
    Dictionary<Vector2Int, Room> occupied;

    int iterations;

    public void Generate()
    {
        // dispose of game objects from previous generation process
        foreach (var go in generated_objects)
        {
            Destroy(go);
        }
        generated_objects.Clear();
        
        generated_objects.Add(start.Place(new Vector2Int(0,0)));
        List<Door> doors = start.GetDoors();
        occupied = new Dictionary<Vector2Int, Room>(); // switched list to dictionary so its more efficient
        occupied[Vector2Int.zero] = start;
        iterations = 0;
        GenerateWithBacktracking(Vector2Int.zero, doors, 1);
    }


    bool GenerateWithBacktracking(Vector2Int roomCoord, List<Door> doors, int depth)
    {
        iterations++;
        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");

        // if no more doors left unattached
        if (doors.Count == 0) 
        {
            return depth >= 5; //return true only if min depth has been reached
        }

        
        List<Door> doorsToTry = new List<Door>(doors);

        // selecting a door that hasnt been connected
        foreach (Door door in doorsToTry)
        {
            // find new room pos
            // find available rooms that work with this door (findvalidrooms)
            // for each valid room option, "place" (add to occupied and add new doors to a updated doors list)
                // if its true place room and hallway for real and return true
                // if false, remove from occupied

            Vector2Int newRoomCoord = GetNextRoomCoord(roomCoord, door);

            // Skip if side is occuppied
            if (occupied.ContainsKey(newRoomCoord))
            {
                continue;
            }

            foreach (Room room in GetValidRooms(door))
            {
                occupied.Add(newRoomCoord, room);
                if (GenerateWithBacktracking(newRoomCoord, room.GetDoors(), depth + 1))
                {
                    Room newRoom = PlaceNewRoom(roomCoord, door);
                    return true;
                }
                else
                {
                    occupied.Remove(newRoomCoord);
                }
            }
        }


        return false;
    }

    // Alternative valid rooms helper function
    // Returns a valid room that's has the corresponding side
    private List<Room> GetValidRooms(Door door)
    {
        List<Room> validRooms = new List<Room>();

        foreach (Room room in rooms)
        {
            if (room.HasDoorOnSide(door.GetMatchingDirection()))
            {
                validRooms.Add(room);
            }
        }

        Shuffle(validRooms);
        return validRooms;
    }

    private Vector2Int GetNextRoomCoord(Vector2Int currentRoomCoord, Door door)
    {
        Vector2Int direction = door.GetDirection() switch
        {
            Door.Direction.NORTH => Vector2Int.up,
            Door.Direction.SOUTH => Vector2Int.down,
            Door.Direction.EAST => Vector2Int.right,
            Door.Direction.WEST => Vector2Int.left,
            _ => Vector2Int.zero
        };

        return currentRoomCoord + direction;
    }

    private Room PlaceNewRoom(Vector2Int roomCoord, Door door)
    {
        List<Room roomToPlace = GetValidRoom(roomCoord, door);

        if (roomToPlace == null) 
        {
            return null;
        }

        (door.IsHorizontal() ? horizontal_hallway : vertical_hallway).Place(door);
        return roomToPlace.Place(roomCoord).GetComponent<Room>();
    }

    // finding valid rooms helper function
    public List<Room> FindValidRooms(Vector2Int pos, Dictionary<Vector2Int, Room> occupied)
    {
        // positions of adjacent tiles
        Vector2Int eastPos = pos + Vector2Int.right;
        Vector2Int westPos = pos + Vector2Int.left;
        Vector2Int northPos = pos + Vector2Int.up;
        Vector2Int southPos = pos + Vector2Int.down;

        // get rooms from the occupied dictionary
        Room east = occupied.GetValueOrDefault(eastPos);
        Room west = occupied.GetValueOrDefault(westPos);
        Room north = occupied.GetValueOrDefault(northPos);
        Room south = occupied.GetValueOrDefault(southPos);

        List<Room> options = new();
        
        // iterate over room options and check validity
        foreach (var room in rooms)
        {
            if (east && (room.HasDoorOnSide(Door.Direction.EAST) != east.HasDoorOnSide(Door.Direction.WEST))) continue;
            if (west && (room.HasDoorOnSide(Door.Direction.WEST) != west.HasDoorOnSide(Door.Direction.EAST))) continue;
            if (north && (room.HasDoorOnSide(Door.Direction.NORTH) != north.HasDoorOnSide(Door.Direction.SOUTH))) continue;
            if (south && (room.HasDoorOnSide(Door.Direction.SOUTH) != south.HasDoorOnSide(Door.Direction.NORTH))) continue;

            options.Add(room);
        }

        // shuffle and return list of valid options
        Shuffle(options);
        return options;

    }

    // shuffle helper function
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generated_objects = new List<GameObject>();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            Generate();
    }
}
