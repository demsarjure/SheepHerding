/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.Collections.Generic;
using UnityEngine;

public class HybridController : MonoBehaviour
{
  // update frequency
  private float updateFrequency = 1.0f;
  
  // update frequency
  private float updateTimer = 0;

  // id
  [HideInInspector]
  public int id;
  
  // state
  [HideInInspector]
  public Enums.SheepState sheepState;

  [HideInInspector]
  public Enums.SheepState previousSheepState;

  // GameManager
  private GameManager GM;

  // Hybrid Manager
  [HideInInspector]
  public HybridManager HM;


  // heading
  private float theta;
  private float desiredTheta = .0f;
  private float maxDeltaTheta = .1f;

  // speed
  private float desiredV = .0f;
  private float v;
  private float maxDeltaV = .1f;

  // fence drive only near borders
  private float minFenceBoundary;
  private float maxFenceBoundary;

  // probabilities
  // mimicking parameter alpha for idle to walking
  private float alpha = 15.0f;

  // idle <-> walking
  private float tau_iw = 35f;
  private float tau_wi = 8.0f;

  // params for transition to running
  private float A_r = 14f;
  private float d_s = 0.8f;

  // helper vars for transition
  private int n_idle = 0, n_walking = 0;

  [HideInInspector]
  public float p_iwr = .0f, p_ri = .0f, p_wi = .0f, p_iw = .0f;
  // cell
  [HideInInspector]
  public Cell currentCell;

  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // boundaries
    minFenceBoundary = GM.r_f;
    maxFenceBoundary = GM.fieldSize - GM.r_f;

    // speed
    switch (sheepState)
    {
      case Enums.SheepState.Idle:
        desiredV = 0f;
        break;
      case Enums.SheepState.Walking:
        desiredV = GM.v_1;
        break;
      case Enums.SheepState.Running:
        desiredV = GM.v_2;
        break;
    }
    v = desiredV;    // speed

    // random heading
    theta = Random.Range(-Mathf.PI, Mathf.PI);
    desiredTheta = theta;
    transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;

    // max accelration and turn rate speedup
    maxDeltaTheta = Mathf.Min(2f * Mathf.PI, maxDeltaTheta * GM.speedup);
    maxDeltaV = Mathf.Min(GM.v_2, maxDeltaV * GM.speedup);
  }

  void Update()
  {
    // timer countdown
    updateTimer -= Time.deltaTime;

    if (updateTimer < .0f)
    {
      // calculate new state
      UpdateState();

      // drives update
      // only change speed and heading if not idle
      if (sheepState == Enums.SheepState.Walking || sheepState == Enums.SheepState.Running)
        DrivesUpdate();

      updateTimer = updateFrequency / GM.speedup;
    }

    // dT
    float dT = Time.deltaTime * GM.speedup;

    // update speed
    v = Mathf.MoveTowards(v, desiredV, maxDeltaV * dT);

    // update heading
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, maxDeltaTheta * dT);
    theta = desiredTheta;

    // ensure angle remains in [-Pi,Pi]
    theta = (theta + Mathf.PI) % (2f * Mathf.PI) - Mathf.PI;
    transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;

    // update position
    transform.position += dT * v * transform.forward;      
  }

  void UpdateState()
  {
    previousSheepState = sheepState;

    // calculate local probabilities
    // transition parameters
    n_idle = 0;
    n_walking = 0;

    // always separate from metric neighbours
    foreach (HybridController neighbour in currentCell.hcList)
    {
      if (neighbour.id == id) continue; // exclude self from count

      Vector3 offset = transform.position - neighbour.transform.position;

      if (offset.sqrMagnitude < GM.r_o2)
      {
        // state counter
        switch (neighbour.sheepState)
        {
          // idle metric neighbours
          case Enums.SheepState.Idle:
            n_idle++;
            break;
          // walking metric neighbours
          case Enums.SheepState.Walking:
            n_walking++;
            break;
        }
      }
    }

    // idle -> walking
    p_iw = (1 + alpha * n_walking) / tau_iw;
    p_iw = 1 - Mathf.Exp(-p_iw);

    // walking -> idle
    p_wi = (1 + alpha * n_idle) / tau_wi;
    p_wi = 1 - Mathf.Exp(-p_wi);

    // idle/walking -> running
    p_iwr = Mathf.Exp(HM.area - (A_r * GM.nOfSheep)) * (1 + (alpha * HM.m_torunning));
    p_iwr = 1 - Mathf.Exp(-p_iwr);

    // dist to centre of mas
    float distToCom = (HM.com - transform.position).sqrMagnitude;

    // running -> idle
    p_ri = (1 / Mathf.Exp(distToCom - (d_s * GM.nOfSheep))) * (1 + (alpha * HM.m_toidle));
    p_ri = 1 - Mathf.Exp(-p_ri);

    // test states
    float random = .0f;
    // first test the transition between idle and walking and viceversa
    if (sheepState == Enums.SheepState.Idle)
    {
      random = Random.Range(.0f, 1.0f);
      if (random < p_iw)
      {
        // change state to walking and update speed
        sheepState = Enums.SheepState.Walking;
        desiredV = GM.v_1;
      }
    }
    else if (sheepState == Enums.SheepState.Walking)
    {
      random = Random.Range(.0f, 1.0f);
      if (random < p_wi)
      {
        // change state to idle and update speed
        sheepState = Enums.SheepState.Idle;
        desiredV = .0f;
      }
    }

    // second test the transition to running
    // which has the same rate regardless if you start from walking or idle
    if (sheepState == Enums.SheepState.Idle || sheepState == Enums.SheepState.Walking)
    {
      random = Random.Range(.0f, 1.0f);
      if (random < p_iwr)
      {
        // change state to running and update speed
        sheepState = Enums.SheepState.Running;
        desiredV = GM.v_2;
      }
    }
    // test the transition from running to standing
    else if (sheepState == Enums.SheepState.Running)
    {
      random = Random.Range(.0f, 1.0f);
      if (random < p_ri)
      {
        // change state to idle and update speed
        sheepState = Enums.SheepState.Idle;
        desiredV = .0f;
      }
    }
  }

  void DrivesUpdate()
  {
    // desired heading in vector form
    Vector3 desiredThetaVector = new Vector3();
    // noise
    float eps = 0f;

    // declarations
    Vector3 e_ij, a_i;
    float f_ij, d_ij;

    if (GM.gamma > .0f)
    {
      // perform check only if individuals are in cells that near the edge of the fence
      if (transform.position.x <= minFenceBoundary || transform.position.x >= maxFenceBoundary ||
        transform.position.z <= minFenceBoundary || transform.position.z >= maxFenceBoundary)
      {
        // fences repulsion
        List<Vector3> fenceProjections = new List<Vector3>();
        fenceProjections.Add(new Vector3(0, 0, transform.position.z));
        fenceProjections.Add(new Vector3(transform.position.x, 0, 0));
        fenceProjections.Add(new Vector3(GM.fieldSize, 0, transform.position.z));
        fenceProjections.Add(new Vector3(transform.position.x, 0, GM.fieldSize));

        // fences repulsion
        foreach (Vector3 projectionOnFence in fenceProjections)
        {
          // get dist
          e_ij = projectionOnFence - transform.position;
          
          if (e_ij.sqrMagnitude < GM.r_f2)
          {
            // distance to nearest point on fence
            d_ij = Vector3.Magnitude(e_ij);

            // repulsion strength
            f_ij = GM.r_f / d_ij;

            // vector towards centre
            a_i = (GM.fieldCentre - transform.position).normalized;

            desiredThetaVector += GM.gamma * f_ij * a_i;
          }
        }
      }
    }

    // if walking
    if (sheepState == Enums.SheepState.Walking)
    {
      int metricNeighboursCount = 0;

      // always separate from metric neighbours
      foreach (HybridController neighbour in currentCell.hcList) // cells overlap by the amount of r_o so currentCell.sheepList contains all neighbours
      {
        // don't test self
        if (neighbour.id != id) continue;

        e_ij = transform.position - neighbour.transform.position;
        d_ij = Vector3.Magnitude(e_ij);
        if (d_ij < GM.r_o)
        {
          metricNeighboursCount++;

          // allignment
          desiredThetaVector += neighbour.transform.forward;

          // separation
          f_ij = Mathf.Min(0f, (d_ij - GM.r_o) / GM.r_o);
          desiredThetaVector += GM.beta * f_ij * e_ij.normalized;
        }
      }

      // for sheep with no Metric neighbours set desiredTheta to current forward i.e. no change
      if (metricNeighboursCount == 0)
      {
        desiredThetaVector += transform.forward;
      }

      // noise
      eps += GM.eta * Random.Range(-Mathf.PI, Mathf.PI);
    }
    // if running
    else if (sheepState == Enums.SheepState.Running)
    {
      int metricNeighboursCount = 0;

      // always separate from metric neighbours
      foreach (HybridController neighbour in currentCell.hcList) // cells overlap by the amount of r_o so currentCell.sheepList contains all neighbours
      {
        // don't test self
        if (neighbour.id != id) continue;

        e_ij = transform.position - neighbour.transform.position;
        d_ij = Vector3.Magnitude(e_ij);
        if (d_ij < GM.r_o)
        {
          metricNeighboursCount++;

          // allignment with running sheep only
          if (neighbour.sheepState == Enums.SheepState.Running)
            desiredThetaVector += neighbour.transform.forward;

          // separation
          f_ij = Mathf.Min(0f, (d_ij - GM.r_o) / GM.r_o);
          desiredThetaVector += GM.beta * f_ij * e_ij.normalized;
        }
      }

      // for sheep with no Metric neighbours set desiredTheta to current forward i.e. no change
      if (metricNeighboursCount == 0)
      {
        desiredThetaVector += transform.forward;
      }

      // extract cohesion from field flow
      desiredThetaVector += currentCell.cohesionForce;
    }
    // if idle
    else if (sheepState == Enums.SheepState.Idle)
    {
      // for idle sheep there is no change
      desiredThetaVector += transform.forward;
    }

    // extract desired heading
    desiredTheta = Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps;
  }
}