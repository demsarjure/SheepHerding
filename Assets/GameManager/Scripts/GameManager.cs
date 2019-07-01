/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.Collections.Generic;
using UnityEngine;
using csDelaunay;

public class GameManager : MonoBehaviour
{
  // game settings
  [Header("Simulation Settings")]
  public Enums.SheepBehaviour sheepBehaviour = Enums.SheepBehaviour.Hybrid;
  public int nOfSheep = 100;
  // number of iterations
  public int iterations = 5;
  // trigger for iteration counter
  [HideInInspector]
  public int iterationCount = 1;
  // size of field
  public float fieldSize = 200.0f;
  // size of field
  public int flowPrecision = 40;
  [HideInInspector]
  public Vector3 fieldCentre;
  // speed up of simulations
  public float speedup = 20.0f;

  // sheep prefab
  [Header("Sheep")]
  public GameObject sheepPrefab;
  // list of sheep
  [HideInInspector]
  public List<GameObject> sheepList = new List<GameObject>();

  // fences
  [Header("Global Sheep Settings")]
  // grazing noise
  public float eta = 0.13f;
  // speeds
  public float v_1 = 0.15f;
  public float v_2 = 1.5f;
  // separation/cohesion factor
  public float beta = 0.8f;
  // metric neighbours
  public float r_o = 1.0f;
  [HideInInspector]
  public float r_o2;
  public float r_e = 1.0f;
  // fence interacion
  public float r_f = 10.0f;
  [HideInInspector]
  public float r_f2;
  public float gamma = 0.1f;
  public float omega = 10f;

  void Start()
  {
    // field centre
    fieldCentre = new Vector3(fieldSize / 2.0f, .0f, fieldSize / 2.0f);

    // squared distances
    r_f2 = r_f * r_f;
    r_o2 = r_o * r_o;

    // set proper behaviour
    switch (sheepBehaviour)
    {
      case Enums.SheepBehaviour.Ginelli:
        GinelliManager GiM = GetComponent<GinelliManager>();
        GiM.enabled = true;
        break;
      case Enums.SheepBehaviour.Hybrid:
        HybridManager HM = GetComponent<HybridManager>();
        HM.enabled = true;
        break;
    }
  }

  public void Reset()
  {
    iterationCount++;

    // clear
    sheepList.Clear();

    // 
    GameObject[] sheep = GameObject.FindGameObjectsWithTag("Sheep");
    foreach (GameObject go in sheep)
    {
      Destroy(go);
    }

    // set proper behaviour
    switch (sheepBehaviour)
    {
      case Enums.SheepBehaviour.Ginelli:
        GinelliManager GiM = GetComponent<GinelliManager>();
        GiM.Reset();
        break;
      case Enums.SheepBehaviour.Hybrid:
        HybridManager HM = GetComponent<HybridManager>();
        HM.Reset();
        break;
    }
  }
}