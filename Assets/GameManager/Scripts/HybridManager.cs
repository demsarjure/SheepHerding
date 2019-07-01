/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.Collections.Generic;
using UnityEngine;
using csDelaunay;

public class HybridManager : MonoBehaviour
{
  // link to game manager
  private GameManager GM;

  // flows
  [HideInInspector]
  public Cell[,] forceField;

  // size of 1 cell in flow grid in Unity units
  [HideInInspector]
  public float binSize;

  // attraction to adjacent sheep
  private float epsilon = .7f;

  // centre of mass
  [HideInInspector]
  public Vector3 com;

  // area of herd
  [HideInInspector]
  public float area;
  // convex hull points
  private List<Vector3> hull;
  // voronoi of cells
  public Voronoi voronoi;

  // vars for counting sheep states
  [HideInInspector]
  public float m_torunning = 0f;
  [HideInInspector]
  public float m_toidle = 0f;

  // list of sheep controllers
  [HideInInspector]
  public List<HybridController> sheepList;

  // update frequency
  private float updateFrequency = 1.0f;
  
  // update frequency
  private float updateTimer = 0;

  // centroid randomization vector
  private Vector2 rand;

  void Start()
  {
    // find GM
    GM = GetComponent<GameManager>();

    // size of 1 cell
    binSize = GM.fieldSize / GM.flowPrecision;

    // init the field
    forceField = new Cell[GM.flowPrecision, GM.flowPrecision];
    Vector3 coordinates;
    for (int i = 0; i < GM.flowPrecision; i++)
    {
      for (int j = 0; j < GM.flowPrecision; j++)
      {
        coordinates = new Vector3((binSize / 2) + (i * binSize), .0f, (binSize / 2) + (j * binSize));
        forceField[i, j] = new Cell(coordinates, i, j, epsilon);
      }
    }

    // set neighbours
    for (int i = 0; i < GM.flowPrecision; i++)
    {
      for (int j = 0; j < GM.flowPrecision; j++)
      {
        SetNeighbours(i, j);
      }
    }
    
    // set flow and sheep
    Reset();
  }

  public void Reset()
  {
    // clear list
    sheepList.Clear();

    // get circle radius from N of sheep
    float radius = Mathf.Sqrt(GM.nOfSheep);
    float mid = GM.fieldSize / 2;

    // spawn
    Vector3 position;
    Vector2 randCirc;
    GameObject newSheep;

    int i = 0;
    while (i < GM.nOfSheep)
    {
      // random position
      randCirc = Random.insideUnitCircle * radius;
      position = new Vector3(mid + randCirc.x, .0f, mid + randCirc.y);

      // instantiate
      newSheep = (GameObject)Instantiate(GM.sheepPrefab, position, Quaternion.identity);

      // find controller, add to list and enable
      HybridController HC = newSheep.GetComponent<HybridController>();

      // set initial state to running
      HC.sheepState = Enums.SheepState.Idle;
      sheepList.Add(HC);
      HC.enabled = true;

      // allocate sheep to cell
      AddSheepToField(HC);

      // link sheep with this HybridManager
      HC.HM = this;

      // set id and add to list
      HC.id = i;
      i++;
      GM.sheepList.Add(newSheep);
    }

    // update flows
    UpdateFlows();
  }

  void Update()
  {
    UpdateFlows();
  }

  void SetNeighbours(int x, int z)
  {
    Cell currentCell = forceField[x, z];

    // boundaries
    int xMin = Mathf.Max(0, x - 1);
    int xMax = Mathf.Min(x + 1, GM.flowPrecision - 1);
    int zMin = Mathf.Max(0, z - 1);
    int zMax = Mathf.Min(z + 1, GM.flowPrecision - 1);

    for (int i = xMin; i <= xMax; i++)
    {
      for (int j = zMin; j <= zMax; j++)
      {
        if (i != x || j != z)
          currentCell.neighbourCells.Add(forceField[i, j]);
      }
    }
  }

  void UpdateFlows()
  {
    // timer countdown
    updateTimer -= Time.deltaTime;

    if (updateTimer <= .0f)
    {
      // randomize com every update frequency
      rand = Random.insideUnitCircle * GM.omega;

      updateTimer = updateFrequency / GM.speedup;
    }
    
    // get com and n of sheep
    m_torunning = 0;
    m_toidle = 0;

    // get points
    List<Vector3> positions = new List<Vector3>();

    // centre of mass
    int totalMass = 0;
    com = new Vector3();
    // clear list and add full cells for area purposes
    for (int i = 0; i < GM.flowPrecision; i++)
    {
      for (int j = 0; j < GM.flowPrecision; j++)
      {
        // add cell to hull positions
        if (forceField[i, j].m_sheep > 0)      
          positions.Add(forceField[i, j].coordinates);

        // com
        totalMass += forceField[i, j].m_sheep;
        com += forceField[i, j].coordinates * forceField[i, j].m_sheep;

        // clear cell
        forceField[i, j].hcList.Clear();
        forceField[i, j].m_sheep = 0;
      }
    }

    // centre of mass
    com /= totalMass;

    // randomize
    com = new Vector3(com.x + rand.x, .0f, com.z + rand.y);

    // allocate sheep to cells
    foreach (HybridController HC in sheepList)
    {
      AddSheepToField(HC);
    }

    // convex hull
    hull = ConvexHull.CreateConvexHull(positions);
    area = ConvexHull.PolygonArea(hull);

    // if all cells are in a line
    if (area == 0)
      area = hull.Count * binSize * binSize;

    area -= binSize * binSize;

    // update flow field
    for (int i = 0; i < GM.flowPrecision; i++)
    {
      for (int j = 0; j < GM.flowPrecision; j++)
      {
        forceField[i, j].UpdateField(com);
      }
    }
  }

  void AddSheepToField(HybridController HC)
  {
    // add sheep to field
    float hc_x = HC.transform.position.x / binSize;
    float hc_z = HC.transform.position.z / binSize;

    // coordinates
    int x = Mathf.FloorToInt(hc_x);
    int z = Mathf.FloorToInt(hc_z);
    int x_0 = x;
    int z_0 = z;

    // add
    HC.currentCell = forceField[x, z];
    forceField[x, z].hcList.Add(HC);
    forceField[x, z].m_sheep++;

    // state count
    if (HC.sheepState == Enums.SheepState.Running && HC.previousSheepState != Enums.SheepState.Running)
      m_torunning++;
    else if (HC.sheepState == Enums.SheepState.Idle && HC.previousSheepState == Enums.SheepState.Running)
      m_toidle++;

    // add also to neighbour fields?
    hc_x = HC.transform.position.x - (x * binSize);
    hc_z = HC.transform.position.z - (z * binSize);

    // TL
    if (hc_x < GM.r_o && x > 0 && hc_z < GM.r_o && z > 0)
    { x = x - 1; z = z - 1; }
    // BL
    else if (hc_x < GM.r_o && x > 0 && hc_z > (binSize - GM.r_o) && z < (GM.flowPrecision - 1))
    { x = x - 1; z = z + 1; }
    // TR
    else if (hc_x > (binSize - GM.r_o) && x < (GM.flowPrecision - 1) && hc_z < GM.r_o && z > 0)
    { x = x + 1; z = z - 1; }
    // BR
    else if (hc_x > (binSize - GM.r_o) && x < (GM.flowPrecision - 1) && hc_z > (binSize - GM.r_o) && z < (GM.flowPrecision - 1))
    { x = x + 1; z = z + 1; }

    // left and right edge
    if (hc_x < GM.r_o && x > 0)
    { x = x - 1; }
    else if (hc_x > (binSize - GM.r_o) && x < (GM.flowPrecision - 1))
    { x = x + 1; }

    // top and bottom edge
    if (hc_z < GM.r_o && z > 0)
    { z = z - 1; }
    else if (hc_z > (binSize - GM.r_o) && z < (GM.flowPrecision - 1))
    { z = z + 1; }

    if (x != x_0 || z != z_0)
      forceField[x, z].hcList.Add(HC);
  }
}