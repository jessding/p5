using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// TODO: inherit from Agent, bypass BeadAgent ML/put that here
[RequireComponent(typeof(NewMoleculeCreator))]
public class ChainObserver : MonoBehaviour
{
    private ChainCreator chainCreator;
    private GameObject locator;
    
    Vector3 centerOfMass = Vector3.zero;
    private float rg_sq = 0f;

    // Start is called before the first frame update
    void Start()
    {
        chainCreator = this.GetComponent<NewMoleculeCreator>().creator;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        var beads = chainCreator.BEADS;
        var sumPos = Vector3.zero;

        foreach (var bead in beads)
        {
            sumPos += bead.transform.position * bead.GetComponent<Rigidbody>().mass;
        }

        centerOfMass = sumPos / chainCreator.MASS;
        rg_sq = 0f;
        foreach (var bead in beads)
        {
            rg_sq += MathF.Pow(Vector3.Distance(bead.transform.position, centerOfMass), 2f);
        }

        rg_sq /= beads.Count;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 256, 100), string.Format("Polymer Stats\nCenter of mass: {0}\nRadius of Gyration: {1}", centerOfMass, rg_sq));
        GUILayout.EndArea();
    }
}
