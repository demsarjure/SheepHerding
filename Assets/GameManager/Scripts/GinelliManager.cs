/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * Behaviour implementation based on in Ginelli et al.'s manuscript:
 * Intermittent collective dynamics emerge from conflicting imperatives in sheep herds
 * doi: https://doi.org/10.1073/pnas.1503749112
 * */

using System.Collections.Generic;
using UnityEngine;
using csDelaunay;

public class GinelliManager : MonoBehaviour
{
  // link to game manager
  private GameManager GM;

  // list of sheep controllers
  [HideInInspector]
  public List<GinelliController> sheepList;

  void Start()
  {
      // find GM
      GM = GetComponent<GameManager>();

      // set SP and sheep
      Reset();
  }

  public void Reset()
  {
      // clear list
      sheepList.Clear();

      // get circle radius from N of sheep
      float radius = Mathf.Sqrt(GM.nOfSheep / Mathf.PI);
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
          GinelliController GC = newSheep.GetComponent<GinelliController>();
          // set initial state to idle
          GC.sheepState = Enums.SheepState.Idle;
          sheepList.Add(GC);
          GC.enabled = true;

          // set id and add to list
          GC.id = i;
          i++;
          GM.sheepList.Add(newSheep);
      }
  }

  void Update()
  {
    List<Vector2f> points = new List<Vector2f>();
    // prepare for Fortunes algorithm and clear neighbours
    foreach (GinelliController sheep in sheepList)
    {
        // prepare data for Voronoi
        Vector2 point = new Vector2(sheep.transform.position.x, sheep.transform.position.z);
        points.Add(new Vector2f(point.x, point.y, sheep.id));
    }

    // get metric neighbours
    List<GinelliController>[] metricNeighbours = new List<GinelliController>[sheepList.Count];
    foreach (GinelliController sheep in sheepList)
    {
      metricNeighbours[sheep.id] = new List<GinelliController>();
    }
    GinelliController firstSheep, secondSheep;
    for (int i = 0; i < sheepList.Count; i++)
    {
      firstSheep = sheepList[i];

      for (int j = i + 1; j < sheepList.Count; j++)
      {
        secondSheep = sheepList[j];

        // dist?
        if ((firstSheep.transform.position - secondSheep.transform.position).sqrMagnitude < GM.r_o2)
        {
          metricNeighbours[firstSheep.id].Add(secondSheep);
          metricNeighbours[secondSheep.id].Add(firstSheep);
        }
      }
    }
    foreach (GinelliController sheep in sheepList)
    {
      sheep.metricNeighbours = metricNeighbours[sheep.id];
    }

    // voronoi neighbours
    Rectf bounds = new Rectf(0f, 0f, GM.fieldSize, GM.fieldSize); // bounds actually irrelevant as Fortunes algoritghm does not use them 
    Voronoi voronoi = new Voronoi(points, bounds);

    foreach (Vector2f pt in points)
    {
        GinelliController sheep = sheepList[pt.id];
        List<GinelliController> voronoiNeighbours = new List<GinelliController>();
        foreach (Vector2f neighbourPt in voronoi.NeighborSitesForSite(pt))
        {
            GinelliController neighbour = sheepList[neighbourPt.id];
            voronoiNeighbours.Add(neighbour);
        }
        sheep.voronoiNeighbours = voronoiNeighbours;
    }
  }
}