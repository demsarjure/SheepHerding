/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

public class Event
{
  public int id;
  public float duration;
  public float area;
  // packing or dispersing event
  public Enums.HerdState type;

  public Event(int _id, float _duration, float _area, Enums.HerdState _type)
  {
    id = _id;
    duration = _duration;
    area = _area;
    type = _type;
  }

  public override string ToString()
  {
    return id + "," + duration + "," + area + "," + type;
  }
}