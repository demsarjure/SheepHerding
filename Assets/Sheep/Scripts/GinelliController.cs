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

public class GinelliController : MonoBehaviour
{
  // delta time
  private float updateTimer = 0;
  private float dT = 1.0f;

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

  // heading
  private float desiredTheta = .0f;
  private float theta;

  // speed
  private float desiredV = .0f;
  private float v;

  // probabilities
  // mimicking parameter alpha
  private float alpha = 15.0f;
  // idle <-> walking
  private float tau_iw = 35f;
  private float tau_wi = 8.0f;
  // <-> running
  private float delta = 4.0f;
  private float d_R = 31.6f;
  private float d_S = 6.3f;

  // helper vars for transition
  private int n_idle = 0, n_walking = 0, m_running = 0, m_toidle = 0;
  private float tau_iwr;
  private float tau_ri;
  private float l_i = .0f;
  // probabilities
  private float p_iw, p_wi, p_iwr, p_ri;

  // cell
  [HideInInspector]
  public Cell currentCell;

  // neighbour list
  [HideInInspector]
  public List<GinelliController> metricNeighbours = new List<GinelliController>();
  [HideInInspector]
  public List<GinelliController> voronoiNeighbours = new List<GinelliController>();

  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

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
    v = desiredV;

    // transition parameters
    tau_iwr = GM.nOfSheep;
    tau_ri = GM.nOfSheep;

    // random heading
    theta = Random.Range(-Mathf.PI, Mathf.PI);
    desiredTheta = theta;
    transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;
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

      // update speed
      v = desiredV;

      // update heading
      theta = desiredTheta;

      // ensure angle remains in [-Pi,Pi)
      theta = (theta + Mathf.PI) % (2f*Mathf.PI) - Mathf.PI;
      transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;

      // update position
      transform.position += (dT * v * transform.forward);

      updateTimer = dT / GM.speedup;
    }
  }

  void NeighboursUpdate()
  {
    n_idle = 0;
    n_walking = 0;
    m_toidle = 0;
    m_running = 0;

    l_i = .0f;

    foreach (GinelliController neighbour in metricNeighbours)
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

    foreach (GinelliController neighbour in voronoiNeighbours)
    {
      // running neighbours
      if (neighbour.sheepState == Enums.SheepState.Running)
        m_running++;
      else
      {
        // neighbours that transitioned from running to idle
        if ((neighbour.sheepState == Enums.SheepState.Idle) && (neighbour.previousSheepState == Enums.SheepState.Running))
          m_toidle++;
      }

      // calculate mean distance to topologic neighbours
      l_i += (transform.position - neighbour.transform.position).magnitude;
    }

    // divide distance with number of topologic
    if (voronoiNeighbours.Count > 0)
      l_i /= voronoiNeighbours.Count;
    else
      l_i = .0f;
  }

  void UpdateState()
  {
    previousSheepState = sheepState;

    // refresh numbers of neighbours
    NeighboursUpdate();

    // idle -> walking
    p_iw = (1 + alpha * n_walking) / tau_iw;
    p_iw = 1 - Mathf.Exp(-p_iw * dT);

    // walking -> idle
    p_wi = (1 + alpha * n_idle) / tau_wi;
    p_wi = 1 - Mathf.Exp(-p_wi * dT);

    p_iwr = .0f;
    p_ri = .0f;
    if (l_i > .0f)
    {
      // idle/walking -> running
      p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + alpha * m_running), delta);
      p_iwr = 1 - Mathf.Exp(-p_iwr * dT);

      // running -> idle
      p_ri = (1 / tau_ri) * Mathf.Pow((d_S / l_i) * (1 + alpha * m_toidle), delta);
      p_ri = 1 - Mathf.Exp(-p_ri * dT);
    }

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
    Vector3 e_ij;
    float f_ij, d_ij;

    // fences repulsion
    if (GM.gamma > .0f)
    {
      List<Vector3> colliders = new List<Vector3>();
      colliders.Add(new Vector3(0, 0, transform.position.z));
      colliders.Add(new Vector3(transform.position.x, 0, 0));
      colliders.Add(new Vector3(GM.fieldSize, 0, transform.position.z));
      colliders.Add(new Vector3(transform.position.x, 0, GM.fieldSize));
      
      foreach (Vector3 closestPoint in colliders)
      {
        // get dist
        e_ij = closestPoint - transform.position;
        if (e_ij.sqrMagnitude < GM.r_f2)
        {
          // distance to nearest point on fence
          d_ij = Vector3.Magnitude(e_ij);

          // repulsion strength
          f_ij = Mathf.Max(0f, (GM.r_f - d_ij) / GM.r_f);

          desiredThetaVector += GM.gamma * (-f_ij * e_ij.normalized);
        }
      }
    }

    // if walking
    if (sheepState == Enums.SheepState.Walking)
    {
      foreach (GinelliController neighbour in metricNeighbours)
      {
        // allignment
        desiredThetaVector += neighbour.transform.forward;
      }

      // for sheep with no Metric neighbours set desiredTheta to current forward i.e. no change
      if (metricNeighbours.Count == 0)
      {
        desiredThetaVector += transform.forward;
      }

      // noise
      eps += GM.eta * Random.Range(-Mathf.PI, Mathf.PI);
    }
    // if running
    else if (sheepState == Enums.SheepState.Running)
    {
      // cohesion with shell neighbours
      foreach (GinelliController neighbour in voronoiNeighbours)
      {
        // allign with running neighbours
        if (neighbour.sheepState == Enums.SheepState.Running)
          desiredThetaVector += neighbour.transform.forward;

        // cohesion/repulsion force
        e_ij = neighbour.transform.position - transform.position;
        d_ij = Vector3.Magnitude(e_ij);
        f_ij = Mathf.Min(1, (d_ij - GM.r_e) / GM.r_e);
        desiredThetaVector += GM.beta * f_ij * e_ij.normalized;
      }

      // for sheep with no Voronoi neighbours set desiredTheta to current forward i.e. no change
      if (voronoiNeighbours.Count == 0)
      {
        desiredThetaVector += transform.forward;
      }
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