using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using MD.Parser;
using Python.Runtime;
using UnityEngine;

public class MartiniModel : ChainCreator
{
    // VdW Diameter for beads:
    // Normal: 5.2 Angstroms
    // Small (in a ring, prefixed by 'S'): 4.7 Angstroms

    /// DICTIONARIES/LISTS OF BEADS AND MARTINI MODEL PARAMETERS
    public Dictionary<int, GameObject> _beads = new();                // Dictionary of beads organized by index
    public Dictionary< List<int>, List<float> > _bonds = new();             // Dictionary of bonds to parameters
    public Dictionary< List<int>, List<float> > _angles = new();      // Dictionary of angles to parameters
    public Dictionary< List<int>, List<float> > _dihedrals = new();   // Dictionary of dihedrals to parameters

    public List<(GameObject, GameObject)> _pairs = new();             // List of pairs of beads
    public Dictionary<(int, int), GameObject> _joints = new();        // Dictionary that maps pairs of beads to their joints
    public Dictionary<int, List<int>> _edges = new();                 // Graph representation of monomer
    public GameObject AgentModel = null;

    /// GET METHODS
    public override List<GameObject> BEADS { get => new List<GameObject>(_beads.Values); }
    public override Dictionary< List<int>, List<float> > BONDS { get => _bonds; }
    public override Dictionary< List<int>, List<float> > ANGLES { get => _angles; }
    public override Dictionary< List<int>, List<float> > DIHEDRALS { get => _dihedrals; }
    public override List< (GameObject, GameObject) > PAIRS { get => _pairs; }
    public override Dictionary< (int, int), GameObject > JOINTS { get => _joints; }

    private GameObject beadFolder;
    private GameObject bondFolder;
    public int numMonomer = 10;

    /// <summary>
    /// Generates the MARTINI representation of the polymer chain by taking a SMILES
    /// representation of the monomer; creating the beads and the bonds.
    /// </summary>
    /// <param name="owner"> GameObject corresponding to the polymer chain </param>
    /// <param name="formula"> Monomer SMILES representation formula </param>
    public override void IngestFormula(GameObject owner, dynamic formula)
    {
        var itp = ITPParser.read((string)formula+".itp");

        List<String> itpKeys = itp.Keys.ToList();
        List<String> atomKeys = new();

        for (int i = 0; i < itp.Keys.Count; i++) {
            if (itpKeys[i] == "atoms") {
                atomKeys = itp["atoms"].Keys.ToList();
            }
        }

        var gro = GROParser.read((string)formula + "_CG.gro");
        owner.name = "CG Chain: " + itp["moleculetype"]["molname"][0];
        
        beadFolder = new GameObject("Beads");
        bondFolder = new GameObject("Bonds");
        bondFolder.transform.parent = owner.transform;
        beadFolder.transform.parent = owner.transform;

        // create beads,
        // TODO: break out into own function (declared below)
        int numBeads = itp["atoms"]["id"].Count;
        
        // initial monomer
        for (int i = 0; i < numBeads; i++)
        {
            dynamic bead = new ExpandoObject();
            bead.smiles = itp["atoms"][atomKeys[7]][i]; // atomKeys[7] == "smiles"
            bead.s_type = itp["atoms"]["type"][i];
            bead.i = i;
            bead.pos = gro[i];

            // create the bead
            var beadObj = CreateBead(bead, i);
        }
        
        // create bonds
        foreach (var (i, j) in itp["bonds"]["i"].Zip(itp["bonds"]["j"], (a, b) => (int.Parse(a) - 1, int.Parse(b) - 1)))
            CreateBond(i, j);
        
        foreach (var (i, j) in itp["constraints"]["i"].Zip(itp["constraints"]["j"], (a, b) => (int.Parse(a) - 1, int.Parse(b) - 1)))
            CreateBond(i, j);

        var offset = _beads[numBeads - 5].transform.position;
        offset.x -= _beads[2].transform.position.x;
        offset.z -= _beads[2].transform.position.z;

        var monomerDir = (_beads[numBeads - 5].transform.position - _beads[2].transform.position).normalized;
        // here we can change the position of the beads in space


        // TODO: play with this num 2.46
        offset += monomerDir * 2.46f; // NOTE: I made this number up. Replace with bond length between two polymerized ends.

        // Make all the other monomers (i.e. NUM_MONOMERS - 1)
        for (int i = 1; i < numMonomer; i++)
        {
            for (int j = 0; j < numBeads; j++)
            {
                dynamic bead = new ExpandoObject();
                bead.smiles = itp["atoms"][atomKeys[7]][j];
                bead.s_type = itp["atoms"]["type"][j];
                bead.i = j + numBeads * i;
                bead.pos = gro[j] + offset*i;

                // create the bead
                var beadObj = CreateBead(bead, j);
            }

            var force_idx = itp["bonds"].Keys.ToList()[4];
            
            // Creates the BONDS, ANGLES, and DIHEDRALS dictionaries
            int numBonds = itp["bonds"]["i"].Count;
            for (int n = 0; n < numBonds; n++) {
                List<int> bondN = new List<int> {int.Parse(itp["bonds"]["i"][n]), int.Parse(itp["bonds"]["j"][n])};
                _bonds[bondN] = new List<float> {float.Parse(itp["bonds"]["length"][n])*10, float.Parse(itp["bonds"][force_idx][n])/100};
            }
            int numAngles = itp["angles"]["i"].Count;
            for (int n = 0; n < numAngles; n++) {
                List<int> bond_angle = new List<int> {int.Parse(itp["angles"]["i"][n]), int.Parse(itp["angles"]["j"][n]), int.Parse(itp["angles"]["k"][n])};
                _angles[bond_angle] = new List<float> {float.Parse(itp["angles"]["angle"][n]), float.Parse(itp["angles"][force_idx][n])};
            }
            int numDihedrals = itp["dihedrals"]["i"].Count;
            for (int n = 0; n < numDihedrals; n++) {
                List<int> bond_dihedral = new List<int> {int.Parse(itp["dihedrals"]["i"][n]), int.Parse(itp["dihedrals"]["j"][n]), int.Parse(itp["dihedrals"]["k"][n])}; //har coded for 7 beads
                _dihedrals[bond_dihedral] = new List<float> {float.Parse(itp["dihedrals"]["angle"][n]), float.Parse(itp["dihedrals"][force_idx][n])};
            }

            //CreateBond(_beads.Count - numBeads - 2, _beads.Count - numBeads); // (in progress to not be hard coded) Hard-coded polymerization
            // that would work only if we start buldings the beads from the beads that do monomer-monomer bond 
            //CreateBond(3,5); // pretty bad hard coded, using beads n4 and n3
            //Debug.Log("NUMBER OF BEADS   "+ numBeads);
            //Debug.Log("NUMBER OF BEADS FROM BEAD COUNT   "+ _beads.Count);
            var monMonBond1 = _beads.Count - numBeads - 5; // has to be equal to 3 
            var monMonBond2 = _beads.Count - numBeads + 4; // has to be equal to 4
            //Debug.Log("monomer bond 1  "+ monMonBond1);
            //Debug.Log("monomer bond 2   "+ monMonBond2);
            CreateBond(monMonBond1, monMonBond2);
        
            // create bonds
            foreach (var (f, t) in itp["bonds"]["i"].Zip(itp["bonds"]["j"], (a, b) => (int.Parse(a) - 1, int.Parse(b) - 1)))
                CreateBond(_beads.Count - numBeads + f, _beads.Count - numBeads + t);
        
            foreach (var (f, t) in itp["constraints"]["i"].Zip(itp["constraints"]["j"], (a, b) => (int.Parse(a) - 1, int.Parse(b) - 1)))
                CreateBond(_beads.Count - numBeads + f, _beads.Count - numBeads + t);


            // TODO: This section attaches the beadagent script at runtime to specific beads, will have to delete if using chainobserver
            // brings the prefab that has the Agent script attached
            
            // manually attach chainagent to molecule, instead of beadagent script
        }
    }


    /// <summary>
    /// Given an atom's information and its index
    /// it generates the GameObject representation of that bead.
    /// </summary>
    /// <param name="atom"> Atom's information </param>
    /// <param name="index"> Auto_Martini index given to the current atom in the monomer </param>
    public override GameObject CreateBead(dynamic atom, int index)
    {
        int i = atom.i;
        string smiles = atom.smiles;
        string s_type = atom.s_type;
        Vector3 pos = atom.pos;
        // create the bead
        var beadObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beadObj.name = String.Format("{0}: {1}, {2}",i, s_type, smiles);
        beadObj.transform.parent = beadFolder.transform;
        beadObj.transform.position = pos;
        beadObj.tag = "Bead";
            
        // Attach components
        var embBead = beadObj.GuaranteeGetComponent<EmbeddedBead>();
        //beadObj.AddComponent<NonBondedForce>().chain = this;
            
        // bead information
        {
            var (type, subtype) = (s_type[^2], s_type[^1]);
            embBead.Index = index;
            embBead.Type = type.ToString();
            embBead.SubType = subtype.ToString();
            if (s_type.Length > 2) embBead.Size = s_type[0].ToString();
        }

        beadObj.transform.localScale = s_type[0] == 'S' ? new Vector3(4.7f,4.7f,4.7f)/2 : new Vector3(5.2f,5.2f,5.2f)/2;
            
        // color the bead with what is basically a random color based on bead type. 
        {
            var hash = new Hash128(); foreach (var chr in s_type) hash.Append(chr);
            ColorUtility.TryParseHtmlString("#" + hash.ToString().Substring(0, 6), out Color color);
            beadObj.GetComponent<Renderer>().material.color = color;
        }

        using (Py.GIL())
        {
            dynamic chem = Py.Import("rdkit.Chem");
            dynamic descriptors = Py.Import("rdkit.Chem.Descriptors");
                
            // get mass
            var rgBody = beadObj.GuaranteeGetComponent<Rigidbody>();
            rgBody.useGravity = false;
            rgBody.mass = (float) descriptors.MolWt(chem.MolFromSmiles(smiles));
            MASS += rgBody.mass;
            rgBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            //rgBody.detectCollisions = false;
        }

        _beads[i] = beadObj;
        _edges[i] = new List<int>();

        return beadObj;
    }


    /// <summary>
    /// Generates the bond between two neighboring beads.
    /// </summary>
    /// <param name="from"> First bead </param>
    /// <param name="to"> Second bead </param>
    public override GameObject CreateBond(dynamic from, dynamic to)
    {
        var i = (int)from;
        var j = (int)to;

        if (_joints.ContainsKey((i, j))) return _joints[(i,j)];
        
            
        var fBead = _beads[i]; var tBead = _beads[j];
        
        var bondParentObj = new GameObject(string.Format("{0} -> {1}", i, j));
        bondParentObj.tag = "Bond";
        bondParentObj.transform.position = Vector3.Lerp(fBead.transform.position, tBead.transform.position, 0.5f);
        bondParentObj.transform.parent = bondFolder.transform;

        var bondObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bondObj.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
        bondObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bondObj.transform.position = bondParentObj.transform.position;
        bondObj.transform.parent = bondParentObj.transform;
        var rgBody = bondObj.GuaranteeGetComponent<Rigidbody>();
        rgBody.detectCollisions = false;
        rgBody.useGravity = false;

        bondParentObj.transform.LookAt(tBead.transform);
        bondParentObj.transform.localScale = new Vector3(1f, 1f,
            Vector3.Distance(fBead.transform.position, tBead.transform.position));

        var fJoint = bondObj.AddComponent<HingeJoint>();
        fJoint.anchor = -Vector3.up;
        fJoint.connectedBody = fBead.GetComponent<Rigidbody>();
        fJoint.enableCollision = true;

        var tJoint = bondObj.AddComponent<HingeJoint>();
        tJoint.anchor = Vector3.up;
        tJoint.connectedBody = tBead.GetComponent<Rigidbody>();
        tJoint.enableCollision = true;
            
        //Physics.IgnoreCollision(fBead.GetComponent<Collider>(), tBead.GetComponent<Collider>());

        _joints[(i, j)] = bondParentObj;
        _joints[(j, i)] = bondParentObj;
        
        _edges[i].Add(j);
        _edges[j].Add(i);

        return bondParentObj;
    }
}