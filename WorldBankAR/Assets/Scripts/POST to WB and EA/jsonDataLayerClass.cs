using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class jsonDataLayerClass
{
    public Map treeCover;
}

[Serializable]
public class Map
{
    public string mapId1;
    public string token1;
    public legend legend;
}

[Serializable]
public class legend
{
    public int min;
    public int max;
    public string palette;
    public string values;
    public string discreteValues;
}
