using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine;

public class DunGen : MonoBehaviour
{
    #region Variables Declaration
    //Map
    [Header("Map Settings")]
    public int mapHeight;
    public int mapWidth;
    public int groundSpace;
    public int wallSpace;

    //Room
    [Header("Rooms Settings")]
    public Vector2Int roomNormalWidth;
    public Vector2Int roomNormalHeight;

    public Vector2Int roomStartWidth;
    public Vector2Int roomStartHeight;

    public Vector2Int roomEndWidth;
    public Vector2Int roomEndHeight;

    List<Vector4> rooms = new List<Vector4>();
    List<Vector2> grids = new List<Vector2>();

    //Tilemap
    [Header("Tilemap Settings")]
    public Tilemap tilemapGround;
    public Tilemap tilemapShadow;
    public Tilemap tilemapWallForeground;
    public Tilemap tilemapWallBackground;

    public List<TileBase> shadow;
    public List<TileBase> groundNormal;
    public List<TileBase> roomEndFloor;
    public List<TileBase> roomNormalFloor;
    public List<TileBase> roomStartFloor;
    public List<TileBase> wallNormalForeground;
    public List<TileBase> wallNormalBackground;
    public List<TileBase> wallCornerForeground;
    public List<TileBase> wallCornerBackground;
    public List<TileBase> wallIndividualForeground;
    public List<TileBase> wallIndividualBackground;

    //Game props
    [Header("Game Props")]
    public Transform propsLowLayer;
    public Transform propsHighLayer;

    public GameObject firePot;
    public GameObject doorWayVertical;
    public GameObject doorWayHorizontal;

    //Demo Codes
    [Header("Demo")]
    public GameObject player;
    public RectTransform tagUI;
    public void SetMapHeight(float value) => mapHeight = (int)value;
    public void SetMapWidth(float value) => mapWidth = (int)value;
    public void SetGroundSpace(float value) => groundSpace = (int)value;
    //End of demo codes

    //Map array
    int[,] rawMap;
    int[,] exportMap;

    //Stack and counter
    int markedCells;
    List<Vector2Int> coordinatesStack = new List<Vector2Int>();
    List<List<Vector2Int>> areaStack = new List<List<Vector2Int>>();
    List<Vector2Int> areaStackTemp = new List<Vector2Int>();

    #region Bitwise Consts
    //Room index
    const int INDEX_ROOM_START = 0;
    const int INDEX_ROOM_END = 1;

    //Bitwise consts in raw map
    const int CELL_PATH_N      = 0x0001;
    const int CELL_PATH_E      = 0x0002;
    const int CELL_PATH_S      = 0x0004;
    const int CELL_PATH_W      = 0x0008;                     
    const int CELL_DOOR_N      = 0x0010;
    const int CELL_DOOR_E      = 0x0020;
    const int CELL_DOOR_S      = 0x0040;
    const int CELL_DOOR_W      = 0x0080;
    const int CELL_MARKED      = 0x0100;
    const int CELL_LINKED      = 0x0200;
    const int CELL_ROOM_NORMAL = 0x0400;
    const int CELL_ROOM_START  = 0x0800;
    const int CELL_ROOM_END    = 0x1000;

    //Bitwise consts in export map
    const int CELL_DOOR_HORIZONTAL  = 0x001;
    const int CELL_DOOR_VERTICAL    = 0x002;
    const int CELL_GROUND_NORMAL    = 0x004;
    const int CELL_GROUND_GRID      = 0x008;
    const int CELL_ROOMFLOOR_NORMAL = 0x010;
    const int CELL_ROOMFLOOR_START  = 0x020;
    const int CELL_ROOMFLOOR_END    = 0x040;
    const int CELL_WALL_NORMAL      = 0x080;
    const int CELL_WALL_CORNER      = 0x100;
    const int CELL_WALL_INDIVIDUAL  = 0x200;
    const int CELL_SHADOW           = 0x400;
    #endregion
    #endregion

    //Generate at the beginning
    public void Start() => StartGenerate();

    //Returns a new instance of list with same value as the given list
    public List<Vector2Int> CloneList(List<Vector2Int> list)
    {
        //Generate a new list
        List<Vector2Int> value = new List<Vector2Int>();

        //Copy all units on the new list
        foreach (Vector2Int unit in list)
        {
            value.Add(unit);
        }

        //Return the new list
        return value;
    }

    // Start Generate Dungeon
    public void StartGenerate()
    {
        //Fill array with value
        void SetArray(int[,] array, int value)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    array[x, y] = value;
                }
            }
        }

        //Clear the props in map
        foreach (Transform child in propsLowLayer) { Destroy(child.gameObject); }
        foreach (Transform child in propsHighLayer) { Destroy(child.gameObject); }

        //Clear the tilemap
        tilemapWallForeground.ClearAllTiles();
        tilemapWallBackground.ClearAllTiles();
        tilemapGround.ClearAllTiles();
        tilemapShadow.ClearAllTiles();

        //Reset mark counter
        markedCells = 0;

        //Reset the Arrays
        int width = mapWidth * (wallSpace + groundSpace) + wallSpace;
        int height = mapHeight * (wallSpace + groundSpace) + wallSpace;
        exportMap = new int[width, height];
        rawMap = new int[mapWidth, mapHeight];
        SetArray(exportMap, 0x00);
        SetArray(rawMap, 0x00);

        //Clean the stacks
        rooms.Clear();
        grids.Clear();
        coordinatesStack.Clear();
        areaStack.Clear();
        areaStackTemp.Clear();

        //Check if the wall data is complete
        if (wallNormalForeground.Count != wallNormalBackground.Count) { Debug.LogError("Foreground and background tile counts are not equal, did you missed to assign any tile?"); return; }
        if (wallCornerForeground.Count != wallCornerBackground.Count) { Debug.LogError("Foreground and background tile counts are not equal, did you missed to assign any tile?"); return; }
        if (wallIndividualForeground.Count != wallIndividualBackground.Count) { Debug.LogError("Foreground and background tile counts are not equal, did you missed to assign any tile?"); return; }

        //Generate dungeon
        CreateRooms();
        GeneratePathWay();
        CreateConnection();
        FindDeadEnds();
        ExportMap();
        GenerateRoomFloor();
        BakeShadow();
        Spawn();
    }

    //Place rooms in the map
    public void CreateRooms()
    {
        int maxtries = 500;
        bool startRoomCreated = false;
        bool endRoomCreated = false;

        for (int i = 0; i < maxtries; i++)
        {
            int ranWidth, ranHeight, ranPosX, ranPosY;

            //Generate start room properties
            if (!startRoomCreated)
            {
                //Get a random position, width and height
                ranWidth = Random.Range(roomStartWidth.x, roomStartWidth.y);
                ranHeight = Random.Range(roomStartHeight.x, roomStartHeight.y);
                ranPosX = Random.Range(0, mapWidth - roomStartWidth.y);
                ranPosY = Random.Range(0, mapHeight - roomStartHeight.y);
                //Create room if not overlapped
                if (!CheckOverlap(ranPosX, ranPosY, ranWidth, ranHeight))
                {
                    startRoomCreated = true;
                    WriteOnArray(ranPosX, ranPosY, ranWidth, ranHeight, CELL_ROOM_START);
                }
            }
            //Generate end room properties
            else if (!endRoomCreated)
            {
                //Get a random position, width and height
                ranWidth = Random.Range(roomEndWidth.x, roomEndWidth.y);
                ranHeight = Random.Range(roomEndHeight.x, roomEndHeight.y);
                ranPosX = Random.Range(0, mapWidth - roomEndWidth.y);
                ranPosY = Random.Range(0, mapHeight - roomEndHeight.y);
                //Create room if not overlapped
                if (!CheckOverlap(ranPosX, ranPosY, ranWidth, ranHeight))
                {
                    endRoomCreated = true;
                    WriteOnArray(ranPosX, ranPosY, ranWidth, ranHeight, CELL_ROOM_END);
                }
            }
            //Generate normal room properties
            else
            {
                //Get a random position, width and height
                ranWidth = Random.Range(roomNormalWidth.x, roomNormalWidth.y);
                ranHeight = Random.Range(roomNormalHeight.x, roomNormalHeight.y);
                ranPosX = Random.Range(0, mapWidth - roomNormalWidth.y);
                ranPosY = Random.Range(0, mapHeight - roomNormalHeight.y);
                //Create room if not overlapped
                if (!CheckOverlap(ranPosX, ranPosY, ranWidth, ranHeight))
                    WriteOnArray(ranPosX, ranPosY, ranWidth, ranHeight, CELL_ROOM_NORMAL);
            }

            void WriteOnArray(int ranPosX, int ranPosY, int ranWidth, int ranHeight, int value)
            {
                //Record the four points of the room
                rooms.Add(new Vector4(ranPosX, ranPosY, ranPosX + ranWidth, ranPosY + ranHeight));

                for (int w = 0; w < ranWidth; w++)
                {
                    for (int h = 0; h < ranHeight; h++)
                    {
                        //Mark all room cells
                        rawMap[ranPosX + w, ranPosY + h] |= CELL_MARKED;

                        //Set room type on each cell
                        rawMap[ranPosX + w, ranPosY + h] |= value;

                        if (w != 0)
                            rawMap[ranPosX + w, ranPosY + h] |= CELL_PATH_W;

                        if (h != 0)
                            rawMap[ranPosX + w, ranPosY + h] |= CELL_PATH_S;

                        if (h != ranHeight - 1)
                            rawMap[ranPosX + w, ranPosY + h] |= CELL_PATH_N;

                        if (w != ranWidth - 1)
                            rawMap[ranPosX + w, ranPosY + h] |= CELL_PATH_E;

                        //Add the room to list
                        areaStackTemp.Add(new Vector2Int(ranPosX + w, ranPosY + h));
                    }
                }
                markedCells += areaStackTemp.Count;
                areaStack.Add(CloneList(areaStackTemp));
                areaStackTemp.Clear();
            }
        }
    }

    //Check if any cell in the area is already marked
    private bool CheckOverlap(int positionX, int positionY, int width, int height)
    {
        for (int w = 0; w < width; w++)
        {
            for (int h = 0; h < height; h++)
            {
                //If cell marked
                if ((rawMap[positionX + w, positionY + h] & CELL_MARKED) != 0)
                {
                    //Return overlapped
                    return true;
                }
            }
        }
        return false;
    }

    //Find and setup a new point in a new area
    public bool SetupNewCoordinate()
    {
        //Check across the whole map
        for (int x = 0; x < rawMap.GetLength(0); x++)
        {
            for (int y = 0; y < rawMap.GetLength(1); y++)
            {
                //Find a new cell that is not marked
                if ((rawMap[x, y] & CELL_MARKED) == 0)
                {
                    //Mark the cell
                    rawMap[x, y] |= CELL_MARKED;

                    //Add cell to the current area cell list
                    areaStackTemp.Add(new Vector2Int(x, y));

                    //Add cell to the top of the coordinates stack
                    coordinatesStack.Add(new Vector2Int(x, y));

                    return true;
                }
            }
        }
        return false;
    }

    //Generate pathway between rooms using maze algorithm
    public void GeneratePathWay()
    {
        ///Setup the first coordinate in the stack
        ///If can not setup, it means the rooms have fulfilled the map
        ///We can jump straight to the connection setup in this situation
        if (!SetupNewCoordinate()) return;

        //Loop until all cells are marked
        while (markedCells < mapHeight * mapWidth)
        {
            //Get top stack local function
            Vector2Int TopStack(List<Vector2Int> stack) => stack[stack.Count - 1];

            //Get x and y of the top coordinate stack
            int xt = TopStack(coordinatesStack).x;
            int yt = TopStack(coordinatesStack).y;

            //Create a set of unmarked neighbours
            List<int> neighbours = new List<int>();

            if (yt < rawMap.GetLength(1) - 1 && (rawMap[xt + 0, yt + 1] & CELL_MARKED) == 0)
                neighbours.Add(0);
            if (xt < rawMap.GetLength(0) - 1 && (rawMap[xt + 1, yt + 0] & CELL_MARKED) == 0)
                neighbours.Add(1);
            if (yt > 0 && (rawMap[xt + 0, yt - 1] & CELL_MARKED) == 0)
                neighbours.Add(2);
            if (xt > 0 && (rawMap[xt - 1, yt + 0] & CELL_MARKED) == 0)
                neighbours.Add(3);

            //If any neighbour is not marked
            if (neighbours.Count > 0)
            {
                //Pick a random neighbour
                int randomIndex = Random.Range(0, neighbours.Count);
                int randomDirection = neighbours[randomIndex];

                ///Create a path between neighbour and cell:
                ///1.Mark direction on both cells
                ///2.Mark the neighbour cell
                ///3.Add neighbour cell to the top of the coordinates stack
                ///4.Add neighbour cell to the current area cell list

                switch (randomDirection)
                {
                    //North
                    case 0:
                        rawMap[xt + 0, yt + 0] |= CELL_PATH_N;
                        rawMap[xt + 0, yt + 1] |= CELL_PATH_S;
                        rawMap[xt + 0, yt + 1] |= CELL_MARKED;
                        areaStackTemp.Add(new Vector2Int(xt + 0, yt + 1));
                        coordinatesStack.Add(new Vector2Int(xt + 0, yt + 1));
                        break;
                    //East
                    case 1:
                        rawMap[xt + 0, yt + 0] |= CELL_PATH_E;
                        rawMap[xt + 1, yt + 0] |= CELL_PATH_W;
                        rawMap[xt + 1, yt + 0] |= CELL_MARKED;
                        areaStackTemp.Add(new Vector2Int(xt + 1, yt + 0));
                        coordinatesStack.Add(new Vector2Int(xt + 1, yt + 0));
                        break;
                    //South
                    case 2:
                        rawMap[xt + 0, yt + 0] |= CELL_PATH_S;
                        rawMap[xt + 0, yt - 1] |= CELL_PATH_N;
                        rawMap[xt + 0, yt - 1] |= CELL_MARKED;
                        areaStackTemp.Add(new Vector2Int(xt + 0, yt - 1));
                        coordinatesStack.Add(new Vector2Int(xt + 0, yt - 1));
                        break;
                    //West
                    case 3:
                        rawMap[xt + 0, yt + 0] |= CELL_PATH_W;
                        rawMap[xt - 1, yt + 0] |= CELL_PATH_E;
                        rawMap[xt - 1, yt + 0] |= CELL_MARKED;
                        areaStackTemp.Add(new Vector2Int(xt - 1, yt + 0));
                        coordinatesStack.Add(new Vector2Int(xt - 1, yt + 0));
                        break;
                }
            }

            //If no neighbour is found
            else
            {
                //Pop the stack to track back and fill all spaces available
                coordinatesStack.Remove(TopStack(coordinatesStack));

                //If the stack is empty, meaning the area is fulfilled
                if (coordinatesStack.Count <= 0)
                {
                    //Add number of all cells in area to the count
                    markedCells += areaStackTemp.Count;

                    //Add the area cell list to the main area list
                    areaStack.Add(CloneList(areaStackTemp));

                    //Clear the area cell list
                    areaStackTemp.Clear();

                    //Try to setup a new coordinate in the stack
                    SetupNewCoordinate();
                }
            }
        }
    }

    //Mark all cell in the area main area
    public void UnifyArea(List<Vector2Int> area)
    {
        //Mark all cell in the area linked to the main
        foreach (Vector2Int cell in area)
        {
            rawMap[cell.x, cell.y] |= CELL_LINKED;
        }

        //Remove the area from the check list
        areaStack.Remove(area);
    }

    //Create connection between main area and other connected areas
    public void CreateConnection()
    {
        //Choose the first main area
        UnifyArea(areaStack[0]);

        //Loop until all areas are connected
        while (areaStack.Count > 0)
        {
            //All cells in the area that is linked to the main area
            List<Vector3Int> linkedCells = new List<Vector3Int>();

            //The top stack of the area list
            List<Vector2Int> TopStack(List<List<Vector2Int>> stack) => stack[stack.Count - 1];

            //Go through every cell in the top stack area
            foreach (Vector2Int cell in TopStack(areaStack))
            {
                //Create a set of main area neighbours
                List<int> neighbours = new List<int>();

                if (cell.y < mapHeight - 1 && (rawMap[cell.x + 0, cell.y + 1] & CELL_LINKED) != 0)
                    neighbours.Add(0);
                if (cell.x < mapWidth - 1 && (rawMap[cell.x + 1, cell.y + 0] & CELL_LINKED) != 0)
                    neighbours.Add(1);
                if (cell.y > 0 && (rawMap[cell.x + 0, cell.y - 1] & CELL_LINKED) != 0)
                    neighbours.Add(2);
                if (cell.x > 0 && (rawMap[cell.x - 1, cell.y + 0] & CELL_LINKED) != 0)
                    neighbours.Add(3);

                //If it can be connected to the main area
                if (neighbours.Count > 0)
                {
                    //Pick a random direction to access the main area
                    int randomIndex = Random.Range(0, neighbours.Count);
                    int randomDir = neighbours[randomIndex];

                    //Add the cell to the possible connection list
                    linkedCells.Add(new Vector3Int(cell.x, cell.y, randomDir));
                }
            }

            //If any cell is linked to the main area
            if (linkedCells.Count > 0)
            {
                //Choose a random connections cell from all possible connections
                int randomCell = Random.Range(0, linkedCells.Count);
                Vector3Int connectCell = linkedCells[randomCell];

                //Get xy of the cell
                int xc = connectCell.x;
                int yc = connectCell.y;

                //Create connections of cells
                switch (connectCell.z)
                {
                    //North
                    case 0:
                        //Set ground
                        rawMap[xc + 0, yc + 0] |= CELL_PATH_N;
                        rawMap[xc + 0, yc + 1] |= CELL_PATH_S;
                        //Set doors
                        rawMap[xc + 0, yc + 0] |= CELL_DOOR_N;
                        rawMap[xc + 0, yc + 1] |= CELL_DOOR_S;
                        break;
                    //East
                    case 1:
                        //Set ground
                        rawMap[xc + 0, yc + 0] |= CELL_PATH_E;
                        rawMap[xc + 1, yc + 0] |= CELL_PATH_W;
                        //Set doors
                        rawMap[xc + 0, yc + 0] |= CELL_DOOR_E;
                        rawMap[xc + 1, yc + 0] |= CELL_DOOR_W;
                        break;
                    //South
                    case 2:
                        //Set ground
                        rawMap[xc + 0, yc + 0] |= CELL_PATH_S;
                        rawMap[xc + 0, yc - 1] |= CELL_PATH_N;
                        //Set doors
                        rawMap[xc + 0, yc + 0] |= CELL_DOOR_S;
                        rawMap[xc + 0, yc - 1] |= CELL_DOOR_N;
                        break;
                    //West
                    case 3:
                        //Set ground
                        rawMap[xc + 0, yc + 0] |= CELL_PATH_W;
                        rawMap[xc - 1, yc + 0] |= CELL_PATH_E;
                        //Set doors
                        rawMap[xc + 0, yc + 0] |= CELL_DOOR_W;
                        rawMap[xc - 1, yc + 0] |= CELL_DOOR_E;
                        break;
                }

                //Unify the area
                UnifyArea(TopStack(areaStack));
            }

            else
            {
                //Copy it to the bottom of the stack
                areaStack.Insert(0, CloneList(TopStack(areaStack)));

                //Remove the area from the check list
                areaStack.Remove(TopStack(areaStack));
            }
        }
    }

    //Find and Eliminate all dead ends in the map
    public void FindDeadEnds()
    {
        int deadEnds = 1;

        while (deadEnds > 0)
        {
            //Reset the counter
            deadEnds = 0;

            //Reset the clean list
            List<Vector3Int> cleanPathList = new List<Vector3Int>();

            //Go through each cell in the list
            for (int x = 0; x < rawMap.GetLength(0); x++)
            {
                for (int y = 0; y < rawMap.GetLength(1); y++)
                {
                    //Create a list of all available path directions
                    List<int> direction = new List<int>();

                    if ((rawMap[x, y] & CELL_PATH_N) != 0)
                        direction.Add(0);
                    if ((rawMap[x, y] & CELL_PATH_E) != 0)
                        direction.Add(1);
                    if ((rawMap[x, y] & CELL_PATH_S) != 0)
                        direction.Add(2);
                    if ((rawMap[x, y] & CELL_PATH_W) != 0)
                        direction.Add(3);

                    //If there is only one way, its a dead end
                    if (direction.Count == 1)
                    {
                        //Add dead ends counter
                        deadEnds++;

                        //Cut all connections of nearby cells
                        switch (direction[0])
                        {
                            //North
                            case 0:
                                cleanPathList.Add(new Vector3Int(x + 0, y + 1, 0));
                                break;
                            //East                                
                            case 1:
                                cleanPathList.Add(new Vector3Int(x + 1, y + 0, 1));
                                break;
                            //South                                  
                            case 2:
                                cleanPathList.Add(new Vector3Int(x + 0, y - 1, 2));
                                break;
                            //West                                     
                            case 3:
                                cleanPathList.Add(new Vector3Int(x - 1, y + 0, 3));
                                break;
                        }

                        //Clean itself
                        cleanPathList.Add(new Vector3Int(x, y, 4));
                    }
                }
            }

            //Clean the deadends marked in the index list
            foreach (Vector3Int i in cleanPathList)
            {
                switch (i.z)
                {
                    //North
                    case 0:
                        rawMap[i.x, i.y] ^= CELL_PATH_S;
                        if ((rawMap[i.x, i.y] & CELL_DOOR_S) != 0) rawMap[i.x, i.y] ^= CELL_DOOR_S;
                        break;
                    //East
                    case 1:
                        rawMap[i.x, i.y] ^= CELL_PATH_W;
                        if ((rawMap[i.x, i.y] & CELL_DOOR_W) != 0) rawMap[i.x, i.y] ^= CELL_DOOR_W;
                        break;
                    //South
                    case 2:
                        rawMap[i.x, i.y] ^= CELL_PATH_N;
                        if ((rawMap[i.x, i.y] & CELL_DOOR_N) != 0) rawMap[i.x, i.y] ^= CELL_DOOR_N;
                        break;
                    //West
                    case 3:
                        rawMap[i.x, i.y] ^= CELL_PATH_E;
                        if ((rawMap[i.x, i.y] & CELL_DOOR_E) != 0) rawMap[i.x, i.y] ^= CELL_DOOR_E;
                        break;
                    //Clear
                    case 4:
                        rawMap[i.x, i.y] = 0;
                        break;
                }
            }
        }
    }

    //Export the full map as a detailed 2d array
    public void ExportMap()
    {
        //For every valid cell, fill each direction with walls and paths
        for(int x = 0; x < rawMap.GetLength(0); x++) 
        {
            for (int y = 0; y < rawMap.GetLength(1); y++)
            {
                //Raw position to export position
                int xe = x * (wallSpace + groundSpace) + wallSpace;
                int ye = y * (wallSpace + groundSpace) + wallSpace;

               
                if (rawMap[x, y] != 0)
                {
                    //Set the current cell as path
                    for (int xp = 0; xp < groundSpace; xp++)
                    {
                        for (int yp = 0; yp < groundSpace; yp++)
                        {
                            exportMap[xe + xp, ye + yp] |= CELL_GROUND_NORMAL;
                        }
                    }

                    //Check surrounding cells and add walls or paths
                    for (int p = 0; p < groundSpace; p++)
                    {
                        for (int w = 0; w < wallSpace; w++)
                        {
                            //North
                            if ((rawMap[x, y] & CELL_DOOR_N) != 0) { exportMap[xe + p, ye + groundSpace + w] |= CELL_DOOR_HORIZONTAL; }
                            if ((rawMap[x, y] & CELL_PATH_N) != 0) { exportMap[xe + p, ye + groundSpace + w] |= CELL_GROUND_NORMAL; }
                            else if ((rawMap[x, y] & CELL_PATH_N) == 0) { exportMap[xe + p, ye + groundSpace + w] |= CELL_WALL_NORMAL; }

                            //East
                            if ((rawMap[x, y] & CELL_DOOR_E) != 0) { exportMap[xe + groundSpace + w, ye + p] |= CELL_DOOR_VERTICAL; }
                            if ((rawMap[x, y] & CELL_PATH_E) != 0) { exportMap[xe + groundSpace + w, ye + p] |= CELL_GROUND_NORMAL; }
                            else if ((rawMap[x, y] & CELL_PATH_E) == 0) { exportMap[xe + groundSpace + w, ye + p] |= CELL_WALL_NORMAL; }

                            //South
                            if ((rawMap[x, y] & CELL_DOOR_S) != 0) { exportMap[xe + p, ye - wallSpace + w] |= CELL_DOOR_HORIZONTAL; }
                            if ((rawMap[x, y] & CELL_PATH_S) != 0) { exportMap[xe + p, ye - wallSpace + w] |= CELL_GROUND_NORMAL; }
                            else if ((rawMap[x, y] & CELL_PATH_S) == 0) { exportMap[xe + p, ye - wallSpace + w] |= CELL_WALL_NORMAL; }

                            //West
                            if ((rawMap[x, y] & CELL_DOOR_W) != 0) { exportMap[xe - wallSpace + w, ye + p] |= CELL_DOOR_VERTICAL; }
                            if ((rawMap[x, y] & CELL_PATH_W) != 0) { exportMap[xe - wallSpace + w, ye + p] |= CELL_GROUND_NORMAL; }
                            else if ((rawMap[x, y] & CELL_PATH_W) == 0) { exportMap[xe - wallSpace + w, ye + p] |= CELL_WALL_NORMAL; }
                        }
                    }
                }
            }
        }

        //Fill the gap at the corner of each cell
        for (int x = 0; x < rawMap.GetLength(0) + 1; x++)
        {
            for (int y = 0; y < rawMap.GetLength(1) + 1; y++)
            {
                //Position to real position in the export map
                int xe = x * (wallSpace + groundSpace);
                int ye = y * (wallSpace + groundSpace);

                //Count the wall and path around the gap
                List<int> wall = new List<int>();
                List<int> path = new List<int>();

                //North
                if (ye + wallSpace < exportMap.GetLength(1) - 1 && (exportMap[xe + 0, ye + wallSpace] & CELL_WALL_NORMAL) != 0) { wall.Add(0); }
                else if (ye + wallSpace < exportMap.GetLength(1) - 1 && (exportMap[xe + 0, ye + wallSpace] & CELL_GROUND_NORMAL) != 0) { path.Add(0); }
                //East
                if (xe + wallSpace < exportMap.GetLength(0) - 1 && (exportMap[xe + wallSpace, ye + 0] & CELL_WALL_NORMAL) != 0) { wall.Add(1); }
                else if (xe + wallSpace < exportMap.GetLength(0) - 1 && (exportMap[xe + wallSpace, ye + 0] & CELL_GROUND_NORMAL) != 0) { path.Add(1); }
                //South
                if (ye - wallSpace > 0 && (exportMap[xe + 0, ye - wallSpace] & CELL_WALL_NORMAL) != 0) { wall.Add(2); }
                else if (ye - wallSpace > 0 && (exportMap[xe + 0, ye - wallSpace] & CELL_GROUND_NORMAL) != 0) { path.Add(2); }
                //West
                if (xe - wallSpace > 0 && (exportMap[xe - wallSpace, ye + 0] & CELL_WALL_NORMAL) != 0) { wall.Add(3); }
                else if (xe - wallSpace > 0 && (exportMap[xe - wallSpace, ye + 0] & CELL_GROUND_NORMAL) != 0) { path.Add(3); }

                //Fill the Gap using wall diameter
                for (int xw = 0; xw < wallSpace; xw++)
                {
                    for (int yw = 0; yw < wallSpace; yw++)
                    {
                        //If there are all path around the gap, its swallowed
                        //As these places are perfect for crates, they will be stored
                        if (path.Count > 3)
                        {
                            exportMap[xe + xw, ye + yw] |= CELL_GROUND_GRID;
                            exportMap[xe + xw, ye + yw] |= CELL_GROUND_NORMAL;
                            grids.Add(new Vector2(xe + xw, ye + yw));
                        }

                        if (wall.Count > 0)
                        {
                            //If there is only one wall found, its the edge of wall
                            if (wall.Count == 1)
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_INDIVIDUAL;
                            
                            //If the wall turned, its has a corner
                            else if (wall.Exists(a => a == 0) && wall.Exists(a => a == 1))
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_CORNER;
                            else if (wall.Exists(a => a == 1) && wall.Exists(a => a == 2))
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_CORNER;
                            else if (wall.Exists(a => a == 2) && wall.Exists(a => a == 3))
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_CORNER;
                            else if (wall.Exists(a => a == 3) && wall.Exists(a => a == 0))
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_CORNER;
                            
                            //If its not a corner or edge, it is a normal wall
                            else
                                exportMap[xe + xw, ye + yw] |= CELL_WALL_NORMAL;
                        }
                    }
                }
            }
        }
    }

    //Generate room floors using room data
    public void GenerateRoomFloor()
    {
        int ConvertRawToExport(float coordinate) => (int)((coordinate + 1) * wallSpace + coordinate * groundSpace);

        for(int x = 0; x < exportMap.GetLength(0); x++)
        {
            for(int y = 0; y < exportMap.GetLength(1); y++)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (x >= ConvertRawToExport(rooms[i].x) - 1 && x < ConvertRawToExport(rooms[i].z) && y >= ConvertRawToExport(rooms[i].y) - 1 && y < ConvertRawToExport(rooms[i].w))
                        exportMap[x, y] |= i == INDEX_ROOM_START ? CELL_ROOMFLOOR_START : i == INDEX_ROOM_END ? CELL_ROOMFLOOR_END : CELL_ROOMFLOOR_NORMAL;
                }
            }
        }
    }

    //Bake the shadows of the map (Assuming the light from the top)
    public void BakeShadow()
    {
        //For every cell in exported map, find each wall, if the below cell is ground, bake cell
        for (int x = 0; x < exportMap.GetLength(0); x++)
        {
            for (int y = 0; y < exportMap.GetLength(1); y++)
            {
                if (y - 1 > 0)
                    if ((exportMap[x, y] & CELL_WALL_NORMAL) != 0 ||
                        (exportMap[x, y] & CELL_WALL_INDIVIDUAL) != 0 ||
                        (exportMap[x, y] & CELL_WALL_CORNER) != 0)
                        if ((exportMap[x, y - 1] & CELL_GROUND_NORMAL) != 0)
                            exportMap[x, y - 1] |= CELL_SHADOW;
            }
        }
    }

    //Spawn the actural in-game map using the 2d array
    public void Spawn()
    {
        for (int x = 0; x < exportMap.GetLength(0) ; x++)
        {
            for (int y = 0; y < exportMap.GetLength(1); y++)
            {
                //Ground
                if((exportMap[x, y] & CELL_ROOMFLOOR_NORMAL) != 0)
                {
                    tilemapGround.SetTile(new Vector3Int(x, y, 0), roomNormalFloor[Random.Range(0, roomNormalFloor.Count)]);
                }
                else if((exportMap[x, y] & CELL_ROOMFLOOR_START) != 0)
                {
                    tilemapGround.SetTile(new Vector3Int(x, y, 0), roomStartFloor[Random.Range(0, roomStartFloor.Count)]);
                }
                else if((exportMap[x, y] & CELL_ROOMFLOOR_END) != 0)
                {
                    tilemapGround.SetTile(new Vector3Int(x, y, 0), roomEndFloor[Random.Range(0, roomEndFloor.Count)]);
                }
                else if((exportMap[x, y] & CELL_GROUND_NORMAL) != 0)
                {
                    tilemapGround.SetTile(new Vector3Int(x, y, 0), groundNormal[Random.Range(0, groundNormal.Count)]);
                }

                //Wall
                if ((exportMap[x, y] & CELL_WALL_NORMAL) != 0)
                {
                    int wallIndex = Random.Range(0, wallNormalForeground.Count);
                    tilemapWallForeground.SetTile(new Vector3Int(x, y, 0), wallNormalForeground[wallIndex]);
                    //Hide the back ground if it is hidden in wall
                    if (y - 1 < 0 || exportMap[x, y - 1] == 0 || (exportMap[x, y - 1] & CELL_GROUND_NORMAL) != 0)
                        tilemapWallBackground.SetTile(new Vector3Int(x, y, 0), wallNormalBackground[wallIndex]);
                }
                else if ((exportMap[x, y] & CELL_WALL_CORNER) != 0)
                {
                    int wallIndex = Random.Range(0, wallCornerForeground.Count);
                    tilemapWallForeground.SetTile(new Vector3Int(x, y, 0), wallCornerForeground[wallIndex]);
                    //Hide the back ground if it is hidden in wall
                    if (y - 1 < 0 || exportMap[x, y - 1] == 0 || (exportMap[x, y - 1] & CELL_GROUND_NORMAL) != 0)
                        tilemapWallBackground.SetTile(new Vector3Int(x, y, 0), wallCornerBackground[wallIndex]);
                }
                else if ((exportMap[x, y] & CELL_WALL_INDIVIDUAL) != 0)
                {
                    int wallIndex = Random.Range(0, wallIndividualForeground.Count);
                    tilemapWallForeground.SetTile(new Vector3Int(x, y, 0), wallIndividualForeground[wallIndex]);
                    //Hide the back ground if it is hidden in wall
                    if (y - 1 < 0 || exportMap[x, y - 1] == 0 || (exportMap[x, y - 1] & CELL_GROUND_NORMAL) != 0)
                        tilemapWallBackground.SetTile(new Vector3Int(x, y, 0), wallIndividualBackground[wallIndex]);
                    Instantiate(firePot, new Vector3(x, y, 0), Quaternion.identity, propsHighLayer);
                }

                //Doors
                if ((exportMap[x, y] & CELL_DOOR_HORIZONTAL) != 0)
                {
                    Instantiate(doorWayHorizontal, new Vector3(x, y, 0), Quaternion.identity, propsLowLayer);
                }
                else if ((exportMap[x, y] & CELL_DOOR_VERTICAL) != 0)
                {
                    Instantiate(doorWayVertical, new Vector3(x, y, 0), Quaternion.identity, propsLowLayer);
                }

                //Shadows
                if ((exportMap[x, y] & CELL_SHADOW) != 0)
                {
                    tilemapShadow.SetTile(new Vector3Int(x, y, 0), shadow[Random.Range(0, shadow.Count)]);
                }

                //Player(Demo)
                int ConvertRawToExport(float coordinate) => (int)((coordinate + 1) * wallSpace + coordinate * groundSpace);
                player.transform.position = new Vector3(Random.Range(ConvertRawToExport(rooms[0].x), ConvertRawToExport(rooms[0].z - 1)), Random.Range(ConvertRawToExport(rooms[0].y), ConvertRawToExport(rooms[0].w - 1)), 0);
                CancelInvoke(); InvokeRepeating(nameof(UIFollowPlayer), 0, 0.01f);
            }
        }
    }

    //Demo
    void UIFollowPlayer() => tagUI.position = Camera.main.WorldToScreenPoint(player.transform.position + new Vector3(0, 0.65f, 0), Camera.MonoOrStereoscopicEye.Mono);

}
