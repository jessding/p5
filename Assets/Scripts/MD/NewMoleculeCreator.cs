using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NewMoleculeCreator : MonoBehaviour
{
    [SerializeField] public ChainCreator creator;

    [SerializeField] string formula = "CC(=O)OCC1C(C(C(C(O1)O)OC(=O)C)O)O";

    private (Vector3, Quaternion)[] initBeads, initBonds;
    // Start is called before the first frame update
    void Start()
    {
        creator.IngestFormula(gameObject, formula);

        initBeads = creator.BEADS.Select(x => x.transform.position)
            .Zip(creator.BEADS.Select(x => x.transform.rotation), (p, q) => (p, q)).ToArray();
        
        initBonds = creator.JOINTS.Values.Select(x => x.transform.position)
            .Zip(creator.JOINTS.Values.Select(x => x.transform.rotation), (p, q) => (p, q)).ToArray();

        // gameObject.AddComponent<NonBondedForce>();
        // gameObject.AddComponent<BondedPotential>();
        // gameObject.AddComponent<BondedAngle>();
        // gameObject.AddComponent<BondedDihedral>();
    }

    public void ResetEnvironment()
    {
        for (var i = 0; i < creator.NUM_BEADS; i++)
        {
            var bead = creator.BEADS[i];
            var (pos, rot) = initBeads[i];
            var rgBody = bead.GetComponent<Rigidbody>();
            bead.transform.position = pos;
            bead.transform.rotation = rot;
            rgBody.velocity = rgBody.angularVelocity = Vector3.zero;
        }
        
        for (var i = 0; i < creator.NUM_BONDS; i++)
        {
            var bond = creator.JOINTS.Values.ElementAt(i);
            var (pos, rot) = initBonds[i];
            bond.transform.position = pos;
            bond.transform.rotation = rot;
        }
    }
}
