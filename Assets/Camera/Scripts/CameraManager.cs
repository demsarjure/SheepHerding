/*
 * Copyright (c) 2017 Jure Demsar
 * 
 * MIT License - see LICENCE.TXT
 * 
 * */

using UnityEngine;

public class CameraManager : MonoBehaviour
{
  [Header("Cam Speeds")]
  // camera movement speed
  public float moveSpeed;
  // camera rotation speed
  public float lookSpeed;

  // interpolation speeds
  public float areaSpeed;
  public float centroidSpeed;

  // camera position parameters
  public float heightFactor;
  public float yOffset;
  public float distanceFactor;
  public float zOffset;

  
  // reference to GM for acquiring sheep
  [Header("Game Manager")]
  public GameManager GM;

  // bottom left and top right corners of the herd
  private Vector3 BL;
  private Vector3 TR;

  // helper variables
  private Vector3 newPos;
  private Vector3 lookAt;
  private Vector3 centroid;
  private float viewSize;

  void Start()
  {
    // init
    centroid = GM.fieldCentre;
    lookAt = GM.fieldCentre;
    viewSize = GM.fieldSize;
  }

  void Update()
  {
    // init vars to calculate new centroid
    Vector3 newCentroid = new Vector3();
    BL = new Vector3(GM.fieldSize, .0f, GM.fieldSize);
    TR = new Vector3();

    // calculate centroid
    Vector3 pos;
    foreach (GameObject GO in GM.sheepList)
    {
      pos = GO.transform.position;
      newCentroid += pos;
      BL.x = Mathf.Min(BL.x, pos.x);
      BL.z = Mathf.Min(BL.z, pos.z);
      TR.x = Mathf.Max(TR.x, pos.x);
      TR.z = Mathf.Max(TR.z, pos.z);
    }

    // centroid
    newCentroid /= GM.nOfSheep;

    // lerp old towards new centroid
    centroid = Vector3.Lerp(centroid, newCentroid, centroidSpeed);

    // height > width?
    float maxDimension = Mathf.Max(TR.x - BL.x, TR.z - BL.z);
    viewSize = Mathf.Lerp(viewSize, maxDimension, areaSpeed);

    // calculate new camera position
    newPos = new Vector3(centroid.x, viewSize * heightFactor + yOffset, centroid.z - ((GM.fieldSize - viewSize) * distanceFactor) - zOffset);
    // lerp current position towards new
    transform.position = Vector3.Lerp(transform.position, newPos, moveSpeed * (transform.position - newPos).sqrMagnitude);

    // rotate
    lookAt = Vector3.Lerp(lookAt, centroid, lookSpeed);
    transform.LookAt(lookAt);
  }
}