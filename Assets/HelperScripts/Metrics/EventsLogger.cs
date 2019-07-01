/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EventsLogger : MonoBehaviour
{
  // number of events per iteration
  [Header("Number of Eventes")]
  public int eventsLimit;

  // file name
  private string fileName;

  // state
  private float dispersingTimeThreshold = 2.0f;
  private float dispersingThreshold = .9f;
  private float areaThreshold = .1f;
  private Enums.HerdState state;
  private Event dispersingEvent;
  private Event packingEvent;

  // Game Manager
  private GameManager GM;

  // appropriate manager
  GinelliManager GiM;
  HybridManager HM;

  void Start()
  {
    // GameManager 
    GM = FindObjectOfType<GameManager>();

    Reset();

    // set proper manager
    switch (GM.sheepBehaviour)
    {
      case Enums.SheepBehaviour.Ginelli:
        GiM = GM.GetComponent<GinelliManager>();
        break;
      case Enums.SheepBehaviour.Hybrid:
        HM = GM.GetComponent<HybridManager>();
        break;
    }
  }

  public void Reset()
  {
    string dir = "DataAnalysis\\" + GM.sheepBehaviour + "\\Event\\";
    if (!Directory.Exists(dir))
    {
      Directory.CreateDirectory(dir);
    }

    // name
    fileName = dir + GM.iterationCount + "_" + GM.nOfSheep + ".csv";

    using (StreamWriter sw = File.CreateText(fileName))
    {
      sw.WriteLine("id,duration,area,event");
    }

    // init first state and event
    state = Enums.HerdState.Packing;
    dispersingEvent = new Event(0, -float.MaxValue, .0f, Enums.HerdState.Dispersing);
    packingEvent = new Event(0, Time.time, .0f, Enums.HerdState.Packing);
  }

  private Enums.HerdState CalculateState()
  {
    float n_R = .0f;

    // calculate state
    switch (GM.sheepBehaviour)
    {
      case Enums.SheepBehaviour.Ginelli:
        foreach (GinelliController GC in GiM.sheepList)
        {
          if (GC.sheepState == Enums.SheepState.Running)
            n_R++;
        }
        break;
      case Enums.SheepBehaviour.Hybrid:
        foreach (HybridController HC in HM.sheepList)
        {
          if (HC.sheepState == Enums.SheepState.Running)
            n_R++;
        }
        break;
    }

    n_R /= (float)GM.nOfSheep;

    if ((1 - n_R) > dispersingThreshold)
      return Enums.HerdState.Dispersing;
    else
      return Enums.HerdState.Packing;
  }

  void Update()
  {
    // get state
    state = CalculateState();

    // track dispersing event
    if (state == Enums.HerdState.Dispersing)
    {
      if (dispersingEvent.area != .0f)
      {
        // area and timestamp when packing ended
        packingEvent.area = ConvexHull.GetArea(GM.sheepList);
        float requiredChange = dispersingEvent.area * areaThreshold;
        float areaChange = dispersingEvent.area - packingEvent.area;

        // check if packing was strong enough
        float dispersingDuration = packingEvent.duration - dispersingEvent.duration;
        // valid packing event
        if (areaChange > requiredChange && dispersingDuration > dispersingTimeThreshold)
        {
          // set timers
          dispersingEvent.duration = dispersingDuration;
          packingEvent.duration = Time.time - packingEvent.duration;

          // save previous dispersing and current packing
          if (dispersingEvent.id != 0 && dispersingEvent.id <= 50)
          {
            using (StreamWriter sw = File.AppendText(fileName))
            {
              dispersingEvent.duration *= GM.speedup;
              packingEvent.duration *= GM.speedup;

              sw.WriteLine(dispersingEvent.ToString());
              sw.WriteLine(packingEvent.ToString());
            }
          }
          if (dispersingEvent.id >= eventsLimit)
          {
            GM.Reset();

            if (GM.iterationCount > GM.iterations)
            {
#if UNITY_EDITOR
              UnityEditor.EditorApplication.isPlaying = false;
#else
              Application.Quit();
#endif
            }
            else
              Reset();
          }
          else
          {
            dispersingEvent = new Event(packingEvent.id + 1, Time.time, .0f, Enums.HerdState.Dispersing);
            packingEvent = new Event(dispersingEvent.id + 1, .0f, .0f, Enums.HerdState.Packing);
          }
        }
        // false event
        else
        {
          dispersingEvent.area = .0f;
        }
      }
    }
    // track packing event
    else
    {
      // area and timestamp when packing triggered
      if (dispersingEvent.area == .0f)
      {
        packingEvent.duration = Time.time;
        dispersingEvent.area = ConvexHull.GetArea(GM.sheepList);
      }
    }
  }
}