using System;
using System.Collections.Generic;

[Serializable]
public class GbifRoot
{
    public int offset { get; set; }
    public int limit { get; set; }
    public bool endOfRecords { get; set; }
    public List<GbifSpeciesObj> results { get; set; }
}


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
[Serializable]
public class GbifSpeciesObj : Poco
{
    public int key { get; set; }
    public int nubKey { get; set; } // taxonKey in other response lists
    public int nameKey { get; set; }
    public string taxonID { get; set; }
    public int sourceTaxonKey { get; set; }
    public string kingdom { get; set; }
    public string phylum { get; set; }
    public string order { get; set; }
    public string family { get; set; }
    public string genus { get; set; }
    public string species { get; set; }
    public int kingdomKey { get; set; }
    public int phylumKey { get; set; }
    public int classKey { get; set; }
    public int orderKey { get; set; }
    public int familyKey { get; set; }
    public int genusKey { get; set; }
    public int speciesKey { get; set; }
    public string datasetKey { get; set; }
    public string constituentKey { get; set; }
    public int parentKey { get; set; }
    public string parent { get; set; }
    public string scientificName { get; set; }
    public string canonicalName { get; set; }
    public string vernacularName { get; set; }
    public string authorship { get; set; }
    public string nameType { get; set; }
    public string rank { get; set; }
    public string origin { get; set; }
    public string taxonomicStatus { get; set; }
    public List<object> nomenclaturalStatus { get; set; }
    public string remarks { get; set; }
    public int numDescendants { get; set; }
    public DateTime lastCrawled { get; set; }
    public DateTime lastInterpreted { get; set; }
    public List<object> issues { get; set; }
    public bool synonym { get; set; }
    public string @class { get; set; }
}





