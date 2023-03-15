using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MyType {
        START, END, NONE
    }

public class EmbeddedBead : MonoBehaviour
{
    // The type will be assigned to be either P (Polar), N (Semipolar), A (Apolar), and Q (Charged) and BP (only for antifreeze particles)
    public string Type;

    // The type will be assigned to be one of two:
    //       * Hydrogen-bonding: a (acceptor), d (donor), da (both), 0 (none)
    //       * Polarity Level: 1 (lowest), 2, 3, 4, 5 (highest)
    public string SubType;

    // This sets the size of the bead. It can vary between two types: R for Regular and S for Special.
    public string Size = "R";

    // This sets the index of the bead with respect to its monomer in the polymer chain
    // as given by Auto_Martini
    public int Index;

    public MyType Cat = MyType.NONE; 

    public (string, string) TypeAndSubType
    {
        get => (Type, SubType);
    }
}
