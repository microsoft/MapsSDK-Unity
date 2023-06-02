using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMechanics : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnDestroy()
    {
        Debug.Log("Removing all liaisons");
        gameObject.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
    }
}
