using Unity.VisualScripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;
using Python.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

public class AtomisticChain : ChainCreator
{
    
    // CLASS MEMEBERS
    [SerializeField] private GameObject beadModel;
    [SerializeField] private GameObject bondModel;
    [SerializeField] private float posScale;
    
    [ItemCanBeNull] private GameObject[] _beads;
    private Dictionary<(int, int), GameObject> _bonds = new();

    public override List<GameObject> BEADS
    {
        get => _beads.AsReadOnlyCollection() as List<GameObject>;
    }

    public override Dictionary<(int, int), GameObject> JOINTS
    {
        get => _bonds;
    }

    public override List< (GameObject, GameObject) > PAIRS 
    {
        get => new();
    }

    public override Dictionary< List<int>, List<float>> BONDS { get => null; }
    public override Dictionary< List<int>, List<float> > ANGLES { get => null; } 
    public override Dictionary< List<int>, List<float> > DIHEDRALS { get => null; }

    private GameObject bondFolder;
    private GameObject beadFolder;

    private dynamic pyScope = new ExpandoObject();
    public override void IngestFormula(GameObject owner, dynamic formula)
    {
        try
        {
            var smiles = (string)formula;

            beadFolder = new GameObject("Beads");
            bondFolder = new GameObject("Bonds");
            bondFolder.transform.parent = owner.transform;
            beadFolder.transform.parent = owner.transform;

            using (Py.GIL())
            {
                pyScope.rdkit = Py.Import("rdkit");
                pyScope.chem = Py.Import("rdkit.Chem");
                pyScope.rdChem = Py.Import("rdkit.Chem.rdchem");
                pyScope.allChem = Py.Import("rdkit.Chem.AllChem");

                pyScope.mol = pyScope.chem.MolFromSmiles(smiles);
                pyScope.mol = pyScope.chem.AddHs(pyScope.mol);

                pyScope.allChem.EmbedMolecule(pyScope.mol, pyScope.allChem.ETKDGv3());
                pyScope.conformer = pyScope.mol.GetConformer();

                _beads = new GameObject[pyScope.conformer.GetNumAtoms()];

                foreach (dynamic atom in pyScope.mol.GetAtoms())
                {
                    GameObject atomObj = CreateBead(atom, 0);

                    foreach (dynamic bondedAtom in atom.GetNeighbors())
                    {
                        CreateBead(bondedAtom, 0);
                        CreateBond(atom.GetIdx(), bondedAtom.GetIdx());
                    }
                }

            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Debug.LogError(e);
            throw;
        }
    }

    public override GameObject CreateBead(dynamic atom, int ind)
    {
        using (Py.GIL())
        {
            int idx = atom.GetIdx();

            if (_beads[idx] != null) return _beads[idx];

            dynamic pos = pyScope.conformer.GetAtomPosition(idx);
            pos = new Vector3((float)pos.x * posScale, (float)pos.y * posScale, (float)pos.z * posScale) as object;

            GameObject atomObj = Instantiate(beadModel, pos, Quaternion.identity, beadFolder.transform);
            atomObj.name = string.Format("{0}: {1}", idx as object, atom.GetSymbol());
            
            var renderer = atomObj.GetComponent<Renderer>();
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
            
            atomObj.GetComponent<Rigidbody>().mass = (float)atom.GetMass();
            atomObj.transform.localScale = atomObj.transform.localScale*((float) pyScope.chem.GetPeriodicTable().GetRvdw(atom.GetAtomicNum()));

            _beads[idx] = atomObj;

            return atomObj;
        }
    }

    public override GameObject CreateBond(dynamic from, dynamic to)
    {
        var (fIdx, tIdx) = ((int) from , (int) to);

        if (_bonds.ContainsKey((fIdx, tIdx))) return _bonds[(fIdx, tIdx)];
        
        var fAtom = _beads[fIdx]; var tAtom = _beads[tIdx];

        var parentObj = new GameObject(string.Format("{0} -> {1}", fIdx as object, tIdx as object));
        parentObj.transform.position = Vector3.Lerp(fAtom.transform.position, tAtom.transform.position, 0.5f);
        parentObj.transform.parent = bondFolder.transform;

        var bondObj = Instantiate(bondModel, parentObj.transform);
        bondObj.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        parentObj.transform.LookAt(tAtom.transform.position);
        parentObj.transform.localScale = new Vector3(1f, 1f, Vector3.Distance(fAtom.transform.position, tAtom.transform.position));

        _bonds[(fIdx, tIdx)] = bondObj;
        _bonds[(tIdx, fIdx)] = bondObj;

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
