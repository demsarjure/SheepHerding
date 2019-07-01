/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using System.IO;
using UnityEngine;

public class FpsLogger : MonoBehaviour
{
  // save metrics
  [Header("Settings")]
  public float refreshRate;
  public int frameLimit;
  public int sheepIncrement;
  public int sheepLimit;

  // helper variables
  private string fileName;
  private int frame = 1;
  private float refreshTimer;
  private float fps;

  // Game Manager
  private GameManager GM;
  void Start()
  {
    // GameManager 
    GM = FindObjectOfType<GameManager>();

    // name
    string dir = "DataAnalysis\\" + GM.sheepBehaviour + "\\FPS\\";
    if (!Directory.Exists(dir))
    {
      Directory.CreateDirectory(dir);
    }

    fileName = dir + "fps.csv";

    using (StreamWriter sw = File.CreateText(fileName))
    {
      sw.WriteLine("N,frame,fps");
    }
  }

  void Update()
  {
    // decrease timer
    refreshTimer -= Time.deltaTime;

    if (refreshTimer < .0f)
    {
      // calculate fps
      fps = 1.0f / Time.deltaTime;

      // reset timer
      refreshTimer = refreshRate;

      if (frame > frameLimit)
      {
        GM.nOfSheep += sheepIncrement;

        GM.Reset();

        if (GM.nOfSheep > sheepLimit)
        {
#if UNITY_EDITOR
          UnityEditor.EditorApplication.isPlaying = false;
#else
          Application.Quit();
#endif
        }
        else
          frame = 1;
      }
      else
      {
        using (StreamWriter sw = File.AppendText(fileName))
        {
          sw.WriteLine(GM.nOfSheep + "," + frame + "," + fps);
        }

        // next frame
        frame++;
      }
    }
  }
}