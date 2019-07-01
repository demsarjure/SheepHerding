/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.Collections.Generic;
using UnityEngine;

public class Cell
{
  // cell coordinates in grid
  public int x;
  public int z;

  // cell coordinates in world space
  public Vector3 coordinates;

  // list of sheep in this cell
  public List<HybridController> hcList;
  // list of sheep in this cell
  public List<GinelliController> gcList;

  // adjacent cells
  public List<Cell> neighbourCells;

  public Vector3 cohesionForce;

  // local cohesion importance
  private float epsilon;

  // number of sheep
  public int m_sheep = 0;

  public Cell(Vector3 _coordinates, int _x, int _z, float _epsilon)
  {
    // grid coordinates
    x = _x;
    z = _z;

    // world coordinates
    coordinates = _coordinates;

    // lists of sheep and neighbour cells
    hcList = new List<HybridController>();
    gcList = new List<GinelliController>();
    neighbourCells = new List<Cell>();

    // cohesion flow vector
    cohesionForce = new Vector3();

    // weight for cohesion towards nearby occupied cells
    epsilon = _epsilon;
}

  public void UpdateField(Vector3 com)
  {
    // new cohesion vector
    Vector3 newCohesionForce = new Vector3();

    // cohesion towards local centroids
    int n = 0;
    foreach (Cell c in neighbourCells)
    {
      if (c.hcList.Count > 0)
      {
        // weighted with number of sheep in an adjacent cell
        // use square power to keep in sinc with computation of com
        n += c.m_sheep;
        newCohesionForce += (c.coordinates - coordinates).normalized * c.m_sheep;
      }
    }

    // divide with n of sheep in nearby cells
    // take into account self, i.e. Moore neighbourhood includes self ... 
    // no cohesion to outside cells if own cell very crowded
    if (n > 0)
    {
      newCohesionForce /= (n + m_sheep);
      newCohesionForce *= epsilon;
    }

    // offset from centroid
    Vector3 offset = com - coordinates;

    // cohesion towards com
    newCohesionForce += (1f - epsilon) * offset.normalized;

    // set
    cohesionForce = newCohesionForce.normalized;
  }
}