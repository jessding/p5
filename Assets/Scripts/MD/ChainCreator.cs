using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public abstract class ChainCreator: MonoBehaviour
{
    public abstract List<GameObject> BEADS { get; }
    public abstract Dictionary<(int, int), GameObject> JOINTS { get; }
    public abstract List<(GameObject, GameObject)> PAIRS { get; }
    public abstract Dictionary< List<int>, List<float>> BONDS { get; }
    public abstract Dictionary< List<int>, List<float> > ANGLES { get; } 
    public abstract Dictionary< List<int>, List<float> > DIHEDRALS { get; }

    public float MASS = 0f;
    
    public int NUM_BEADS
    {
        get => BEADS.Count;
    }
    
    public int NUM_BONDS
    {
        get => BONDS.Values.Count()/2;
    }
    public abstract void IngestFormula(GameObject owner, dynamic formula);
    public abstract GameObject CreateBead(dynamic atom, int idx);
    public abstract GameObject CreateBond(dynamic from, dynamic to);
}
