using System;
using UnityEngine;

/// <summary>
/// Class chứa data cần save - PHẢI có [Serializable]
/// </summary>
[Serializable]
public class GameHomePositionData
{
    //position in GameHome
    public float[] position;

    public GameHomePositionData(GameObject player)
    {    
        //position
        position = new float[3];
        position[0] = player.transform.position.x;
        position[1] = player.transform.position.y;
        position[2] = player.transform.position.z;

    }
}
