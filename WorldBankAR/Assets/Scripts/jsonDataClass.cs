using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class jsonDataClass
{
    public List<Rank> results;
   
}

[Serializable]
public class Rank
{
    public int nubKey;
    public string kingdom;
    public string rank;
    public string scientificName;
}