using System.Collections.Generic;
using UnityEngine;
using Python.Runtime;

public class MoleculeCreator : MonoBehaviour
{
    public string MOLECULE_SMILES = "CC(=O)OCC1C(C(C(C(O1)O)OC(=O)C)O)O";
    public float POS_SCALE = 1f;

    public GameObject atomModel;
    public GameObject bondModel;
    GameObject bondFolder;
    GameObject atomFolder;

    private Dictionary<int, GameObject> atoms = new Dictionary<int, GameObject>();
    private Dictionary<(int, int), GameObject> bonds = new Dictionary<(int, int), GameObject>();

    private dynamic mol, conformer, chem, rdChem, allChem, rdKit; // rdkit imports kept here to

    // Start is called before the first frame update
    void Start()
    {
        using (Py.GIL())
        {
            // rdkit imports
            rdKit = Py.Import("rdkit");
            chem = Py.Import("rdkit.Chem"); ;
            rdChem = Py.Import("rdkit.Chem.rdchem");
            allChem = Py.Import("rdkit.Chem.AllChem");

            dynamic confGenParams = allChem.ETKDGv3(); // conformer generation parameters

            mol = chem.MolFromSmiles(MOLECULE_SMILES); // generate acetylcellulose molecule
            mol = chem.AddHs(mol);

            bondFolder = new("Bonds");
            bondFolder.transform.parent = gameObject.transform;

            atomFolder = new("Atoms");
            atomFolder.transform.parent = gameObject.transform;

            // TODO: Tie-in with polyply to generate simulation initial conformation.
            // allChem.EmbedMultipleConfs(mol, Py.kw("numConfs", 3), Py.kw("params", confGenParams)); // generate conformers
            allChem.EmbedMolecule(mol, confGenParams);
            //allChem.MMFFOptimizeMolecule(mol);
            conformer = mol.GetConformer();

            foreach (dynamic atom in mol.GetAtoms())
            {
                int idx = atom.GetIdx();

                var atomObject = createAtom(atom);

                // Create bonds and bonded atoms from this atom
                foreach (dynamic bondedAtom in atom.GetNeighbors())
                {
                    createAtom(bondedAtom);
                    createBond(idx, (int)bondedAtom.GetIdx());
                }
            }

        }
    }

    GameObject createAtom(dynamic atom)
    {
        using (Py.GIL())
        {
            int idx = atom.GetIdx();

            if (atoms.ContainsKey(idx)) return atoms[idx];

            dynamic pos = conformer.GetAtomPosition(atom.GetIdx());
            pos = new Vector3((float)pos.x * POS_SCALE, (float)pos.y * POS_SCALE, (float)pos.z * POS_SCALE);

            GameObject obj = Instantiate(atomModel, pos, Quaternion.identity, atomFolder.transform);
            obj.gameObject.name = string.Format("{0}: {1}", atom.GetIdx(), atom.GetSymbol());

            var renderer = obj.GetComponent<Renderer>();
            switch ((int)atom.GetAtomicNum())
            {
                case 6:
                    renderer.material.color = Color.black;
                    break;
                case 7:
                    renderer.material.color = Color.blue;
                    break;
                case 8:
                    renderer.material.color = Color.red;
                    break;
            }

            obj.GetComponent<Rigidbody>().mass = (float)atom.GetMass();
            obj.transform.localScale = obj.transform.localScale*((float) chem.GetPeriodicTable().GetRvdw(atom.GetAtomicNum()));

            atoms[idx] = obj;
            return obj;
        }
    }

    GameObject createBond(int fIdx, int tIdx)
    {
        if (bonds.ContainsKey((fIdx, tIdx))) return bonds[(fIdx, tIdx)];

        var fAtom = atoms[fIdx]; var tAtom = atoms[tIdx];

        var parentObj = new GameObject(string.Format("{0} -> {1}", fIdx, tIdx));
        parentObj.transform.position = Vector3.Lerp(fAtom.transform.position, tAtom.transform.position, 0.5f);
        parentObj.transform.parent = bondFolder.transform;

        var bondObj = Instantiate(bondModel, parentObj.transform);
        bondObj.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        parentObj.transform.LookAt(tAtom.transform.position);
        parentObj.transform.localScale = new Vector3(1f, 1f, Vector3.Distance(fAtom.transform.position, tAtom.transform.position));

        bonds[(fIdx, tIdx)] = bondObj;
        bonds[(tIdx, fIdx)] = bondObj;

        var fJoint = bondObj.AddComponent<HingeJoint>();
        fJoint.anchor = fJoint.axis = -Vector3.up;
        fJoint.connectedBody = fAtom.GetComponent<Rigidbody>();

        var tJoint = bondObj.AddComponent<HingeJoint>();
        tJoint.anchor = tJoint.axis = Vector3.up;
        tJoint.connectedBody = tAtom.GetComponent<Rigidbody>();

        Physics.IgnoreCollision(fAtom.GetComponent<Collider>(), tAtom.GetComponent<Collider>());

        //var bondComp = parentObj.AddComponent<Bond>();
        //bondComp.fAtom = fAtom;
        //bondComp.tAtom = tAtom;

        return bondObj;
    }
}
