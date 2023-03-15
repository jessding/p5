using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NonBondedForce : MonoBehaviour
{
    /// This collection contains all lists of non-bonded pairs in the MARTINI chain.
    public List< List<GameObject> > pairs = new();

    // We compute the Interaction Matrix
    public static IReadOnlyDictionary<(string, string), int> typeMappings = new Dictionary<(string,string), int>() {
            {("Q", "da"), 0}, {("Q", "d"), 1}, {("Q", "a"), 2}, {("Q", "0"), 3},
            {("P", "5"), 4}, {("P", "4"), 5}, {("P", "3"), 6}, {("P", "2"), 7}, {("P", "1"), 8},
            {("N", "da"), 9}, {("N", "d"), 10}, {("N", "a"), 11}, {("N", "0"), 12},
            {("C", "5"), 13}, {("C", "4"), 14}, {("C", "3"), 15}, {("C", "2"), 16}, {("C", "1"), 17},
            {("BP", "4"), 18}
        };

    // Interaction Matrix for interaction levels (epsilon)
    public static readonly string[,] levels = new string[,]
    {
        {"O", "O", "O", "II", "O", "O", "O", "I", "I", "I", "I", "I", "IV", "V", "VI", "VII", "IX", "IX", "O"},
        {"O", "I", "O", "II", "O", "O", "O", "I", "I", "I", "III", "I", "IV", "V", "VI", "VII", "IX", "IX", "O"},
        {"O", "O", "I", "II", "O", "O", "O", "I", "I", "I", "I", "III", "IV", "V", "VI", "VII", "IX", "IX", "O"},
        {"II", "II", "II", "IV", "I", "O", "I", "II", "III", "III", "III", "III", "IV", "V", "VI", "VII", "IX", "IX", "O"},
        {"O", "O", "O", "I", "O", "O", "O", "O", "O", "I", "I", "I", "IV", "V", "VI", "VI", "VII", "VIII", "O"},
        {"O", "O", "O", "O", "O", "I", "I", "II", "II", "III", "III", "III", "IV", "V", "VI", "VI", "VII", "VIII", "O"},
        {"O", "O", "O", "I", "O", "I", "I", "II", "II", "II", "II", "II", "IV", "IV", "V", "V", "VI", "VII", "O"},
        {"I", "I", "I", "II", "O", "II", "II", "II", "II", "II", "II", "II", "III", "IV", "IV", "V", "VI", "VII", "O"},
        {"I", "I", "I", "III", "O", "II", "II", "II", "II", "II", "II", "II", "III", "IV", "IV", "IV", "V", "VI", "O"},
        {"I", "I", "I", "III", "I", "III", "II", "II", "II", "II", "II", "II", "IV", "IV", "V", "VI", "VI", "VI", "O"},
        {"I", "III", "I", "III", "I", "III", "II", "II", "II", "II", "III", "II", "IV", "IV", "V", "VI", "VI", "VI", "O"},
        {"I", "I", "III", "III", "I", "III", "II", "II", "II", "II", "II", "III", "IV", "IV", "V", "VI", "VI", "VI", "O"},
        {"IV", "IV", "IV", "IV", "IV", "IV", "IV", "III", "III", "IV", "IV", "IV", "IV", "IV", "IV", "IV", "V", "VI", "O"},
        {"V", "V", "V", "V", "V", "V", "IV", "IV", "IV", "IV", "IV", "IV", "IV", "IV", "IV", "IV", "V", "V", "O"},
        {"VI", "VI", "VI", "VI", "VI", "VI", "V", "IV", "IV", "V", "V", "V", "IV", "IV", "IV", "IV", "V", "V", "O"},
        {"VI", "VI", "VI", "VI", "VI", "VI", "V", "IV", "IV", "V", "V", "V", "IV", "IV", "IV", "IV", "V", "V", "O"},
        {"VII", "VII", "VII", "VII", "VI", "VI", "V", "V", "IV", "VI", "VI", "VI", "IV", "IV", "IV", "IV", "IV", "IV", "O"},
        {"IX", "IX", "IX", "IX", "VII", "VII", "VI", "VI", "V", "VI", "VI", "VI", "V", "V", "V", "IV", "IV", "IV", "O"},
        {"IX", "IX", "IX", "IX", "VIII", "VIII", "VII", "VII", "VI", "VI", "VI", "VI", "VI", "V", "V", "IV", "IV", "IV", "O"},
        {"O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O"}
    };

    // Interaction Levels with their numeric values assigned to them in a scale [ε] = kJ/mol
    public static IReadOnlyDictionary<string, float> interactionLevels = new Dictionary<string, float>() {
            {"O", 5.6f}, {"I", 5f}, {"II", 4.5f}, {"III", 4f}, {"IV", 3.5f},
            {"V", 3.1f}, {"VI", 2.7f}, {"VII", 2.3f}, {"VIII", 2.0f}, {"IX", 2.0f}
        };

    // Computes the interaction level (epsilon) between two beads
    private static float interactionLevel(GameObject one, GameObject two) {
        // We get the embedded info
        EmbeddedBead beadOne = one.GetComponent<EmbeddedBead>();
        EmbeddedBead beadTwo = two.GetComponent<EmbeddedBead>();

        // We get the types and subtypes
        string level = levels[typeMappings[beadOne.TypeAndSubType], typeMappings[beadTwo.TypeAndSubType]];

        if (beadOne.Size == "S" || beadTwo.Size == "S") {
            return interactionLevels[level] * 1000f * 0.75f;
        }
        else {
            return interactionLevels[level] * 1000f; // returns in J/mol     
        }
    }
    
    // Computes the effective size (sigma) between two beads
    private static float effectiveSize(GameObject one, GameObject two) {
        // We get the embedded info
        EmbeddedBead beadOne = one.GetComponent<EmbeddedBead>();
        EmbeddedBead beadTwo = two.GetComponent<EmbeddedBead>();

        if (beadOne.Type == "Q" && beadTwo.Type == "Q")
            return (0.62f)*10; // * Mathf.Pow(10, -9));
        else if ((beadOne.TypeAndSubType == ("C", "1") || beadOne.TypeAndSubType == ("C", "2"))
                 && (beadTwo.TypeAndSubType == ("C", "1") || beadTwo.TypeAndSubType == ("C", "2")))
            return (0.62f)*10;// * Mathf.Pow(10, -9));
        else if (beadOne.TypeAndSubType == ("BP", "4") || beadTwo.TypeAndSubType == ("BP", "4"))
            return (0.57f*10); // * Mathf.Pow(10, -9));
        else if (beadOne.Size == "S" || beadTwo.Size == "S") {
            return (0.43f*10);
        }
        else
            return (0.47f*10);// * Mathf.Pow(10, -9));

    }

    // Checks if two beads are bonded
    private bool areTheyBonded(GameObject one, GameObject two, Dictionary<(int, int), GameObject> joints) {
        return joints.ContainsKey((one.GetComponent<EmbeddedBead>().Index, two.GetComponent<EmbeddedBead>().Index))
        || joints.ContainsKey((two.GetComponent<EmbeddedBead>().Index, one.GetComponent<EmbeddedBead>().Index));
    }

    /// <summary>
    /// Calculates the Lennard-Jones force between 2 entities.
    /// Takes arguments in the form <code>(r, sigma, epsilon)</code>
    /// </summary>
    /// <param name="sigma"> The sigma variable </param>
    /// <param name="epsilon"> The epsilon variable </param>
    /// <param name="distances"> List of position vectors of the beads </param>
    public static List<Vector3> Fnb(float sigma, float epsilon, List<Vector3> distances) {
        Vector3 r = distances[1] - distances[0];
        Vector3 dir = r.normalized;

        Vector3 F1 = (48 * epsilon * Mathf.Pow(sigma, 12) / Mathf.Pow(r.magnitude, 13) 
        - 24 * epsilon * Mathf.Pow(sigma, 6) / Mathf.Pow(r.magnitude, 7)) * dir;
        
        Vector3 F2 = (-48 * epsilon * Mathf.Pow(sigma, 12) / Mathf.Pow(r.magnitude, 13) 
        - 24 * epsilon * Mathf.Pow(sigma, 6) / Mathf.Pow(r.magnitude, 7)) * dir;

        List<Vector3> result = new List<Vector3>();
        result.Add(F1); result.Add(F2); 
        return result;
    }

    public static Vector3 F_nb(GameObject tgtBead, List<GameObject> exclude, List<GameObject> beads)
    {
        var F_nb = Vector3.zero;
        var pos = beads.Select(x => x.transform.position).ToList();
        var idx = beads.IndexOf(tgtBead);
        
        foreach (var (bead, i) in beads.Except(exclude).Select((x, i) => (x, i)))
        {
            var dir = (pos[i] - pos[idx]).normalized; //dir is a force
            var distance = (pos[i] - pos[idx]).magnitude;
            float intLevel = interactionLevel(tgtBead, bead);
            float effSize = effectiveSize(tgtBead, bead);

            var F_LJ = 48 * intLevel * Mathf.Pow(effSize, 12) 
            / Mathf.Pow(distance, 13) - 24 * intLevel * Mathf.Pow(effSize, 6) / Mathf.Pow(distance, 7);
            F_nb += F_LJ * dir;
        }

        return F_nb;
    }

    void Start()
    {
        MartiniModel chain = gameObject.GetComponent<MartiniModel>();

        // We find the existing pairs of beads
        List< List<GameObject> > paths = new List< List<GameObject> >();

        foreach (GameObject bead in chain.BEADS) {
            // For each bead, we check the neighboring beads that are not bonded
            Collider[] others = Physics.OverlapSphere(bead.transform.position, 12f);

            foreach (var other in others) {
                if (other.gameObject.tag == "Bead" && other.gameObject != bead && !areTheyBonded(bead, other.gameObject, chain.JOINTS)) {
                    List<GameObject> pairing = new List<GameObject>();
                    pairing[0] = bead;
                    pairing[1] = other.gameObject;
                    List<GameObject> rpairing = new List<GameObject>(pairing);
                    rpairing.Reverse();
                    if (!paths.Contains(pairing) && !paths.Contains(rpairing)) {
                        paths.Add(pairing);
                    }
                }
            }
        }
        pairs = paths;
    }

    // FixedUpdate is called in time with the physics simulation
    // using Update will break this code.
    void FixedUpdate()
    {
        for (int i = 0; i < pairs.Count; i++) {
            List<GameObject> bond_beads = pairs[i];
            Vector3 one = (bond_beads[0].transform.position);
            Vector3 two = (bond_beads[1].transform.position);

            // Computes the values of sigma and epsilon
            float C = 1000  * (1 / CONSTANTS.AVOGADRO_NUM) * 10e20f;
            float epsilon = (float) interactionLevel(bond_beads[0], bond_beads[1]) * C;
            float sigma = (float) effectiveSize(bond_beads[0], bond_beads[1]);
            List<Vector3> vectors = new List<Vector3>();
            vectors.Add(one); vectors.Add(two);

            // Computer the force of the potential angle
            List<Vector3> F = Fnb(sigma, epsilon, vectors);
            float distance = Vector3.Distance(one, two);
            float U_LJ = 4 * epsilon * ((float) Math.Pow((sigma / distance), 12) - (float) Math.Pow((sigma / distance), 6));

            Debug.Log(string.Format("{0}: {1}", i, string.Join(" , ", F)));

            // Applies the force calculated to each bead in the dihedral
            bond_beads[0].GetComponent<Rigidbody>().AddForce(F[0]);
            bond_beads[1].GetComponent<Rigidbody>().AddForce(F[1]);
        }
    }
}