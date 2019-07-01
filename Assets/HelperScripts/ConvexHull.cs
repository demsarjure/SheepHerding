/*
 * Copyright (c) 2016 masphei
 * 
 * https://github.com/masphei/ConvexHull/blob/master/ConvexHull/jarvis_march.cs
 * 
 * */

using System;
using System.Collections.Generic;
using UnityEngine;

public class ConvexHull
{
  const int TURN_LEFT = 1;
  const int TURN_RIGHT = -1;
  const int TURN_NONE = 0;

  public static int Turn(Vector3 p, Vector3 q, Vector3 r)
  {
    return ((q.x - p.x) * (r.z - p.z) - (r.x - p.x) * (q.z - p.z)).CompareTo(0);
  }

  public static void KeepLeft(List<Vector3> hull, Vector3 r)
  {
    while (hull.Count > 1 && Turn(hull[hull.Count - 2], hull[hull.Count - 1], r) != TURN_LEFT)
      hull.RemoveAt(hull.Count - 1);

    if (hull.Count == 0 || hull[hull.Count - 1] != r)
      hull.Add(r);
  }

  public static double GetAngle(Vector3 p1, Vector3 p2)
  {
    float xDiff = p2.x - p1.x;
    float yDiff = p2.z - p1.z;
    return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
  }

  public static List<Vector3> MergeSort(Vector3 p0, List<Vector3> arrPoint)
  {
    if (arrPoint.Count == 1)
    {
      return arrPoint;
    }
    List<Vector3> arrSortedInt = new List<Vector3>();
    int middle = (int)arrPoint.Count / 2;
    List<Vector3> leftArray = arrPoint.GetRange(0, middle);
    List<Vector3> rightArray = arrPoint.GetRange(middle, arrPoint.Count - middle);
    leftArray = MergeSort(p0, leftArray);
    rightArray = MergeSort(p0, rightArray);
    int leftptr = 0;
    int rightptr = 0;
    for (int i = 0; i < leftArray.Count + rightArray.Count; i++)
    {
      if (leftptr == leftArray.Count)
      {
        arrSortedInt.Add(rightArray[rightptr]);
        rightptr++;
      }
      else if (rightptr == rightArray.Count)
      {
        arrSortedInt.Add(leftArray[leftptr]);
        leftptr++;
      }
      else if (GetAngle(p0, leftArray[leftptr]) < GetAngle(p0, rightArray[rightptr]))
      {
        arrSortedInt.Add(leftArray[leftptr]);
        leftptr++;
      }
      else
      {
        arrSortedInt.Add(rightArray[rightptr]);
        rightptr++;
      }
    }
    return arrSortedInt;
  }

  public static List<Vector3> CreateConvexHull(List<Vector3> points)
  {
    Vector3 p0 = new Vector3();
    foreach (Vector3 value in points)
    {
      if (p0 == Vector3.zero)
        p0 = value;
      else
      {
        if (p0.z > value.z)
          p0 = value;
      }
    }
    List<Vector3> order = new List<Vector3>();
    foreach (Vector3 value in points)
    {
      if (p0 != value)
        order.Add(value);
    }

    order = MergeSort(p0, order);

    List<Vector3> result = new List<Vector3>();
    result.Add(p0);
    result.Add(order[0]);
    order.RemoveAt(0);
    if (order.Count > 0)
    {
      result.Add(order[0]);
      order.RemoveAt(0);
    }

    foreach (Vector3 value in order)
    {
      KeepLeft(result, value);
    }

    return result;
  }

  public static float PolygonArea(List<Vector3> polygon)
  {
    int i, j;
    float area = .0f;

    for (i = 0; i < polygon.Count; i++)
    {
      j = (i + 1) % polygon.Count;

      area += polygon[i].x * polygon[j].z;
      area -= polygon[i].z * polygon[j].x;
    }

    area /= 2;
    return (area < 0 ? -area : area);
  }

  public static Vector3 PolygonCentroid(List<Vector3> polygon)
  {
    Vector3 centroid = new Vector3();

    foreach (Vector3 point in polygon)
    {
      centroid += point;
    }

    return centroid / polygon.Count;
  }

  public static float GetArea(List<GameObject> gameObjects)
  {
    float area = .0f;

    List<Vector3> positions = new List<Vector3>();

    foreach(GameObject GO in gameObjects)
    {
      positions.Add(GO.transform.position);
    }

    if (positions.Count > 0)
    {
      area = ConvexHull.PolygonArea(CreateConvexHull(positions));
    }

    return area;
  }
}
