using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BondedDihedral : MonoBehaviour
{
    /// This collection contains all lists of dihedrals in the MARTINI chain.
    public List< List<GameObject> > dihedrals = new();

    /// This dictionary will contain the molecule in a more accesible graph format
    public static Dictionary< GameObject, List<GameObject> > graph = new();

    /// This dictionary will provide the values for K_d and phi_d
    public static Dictionary< List<int>, List<float> > values = new();

    /// <summary>
    /// Find all the non-repeating list of atoms that form a bond angle.
    /// Takes arguments in the form <code>(beads, bonds)</code>
    /// </summary>
    /// <param name="graph"> Graph representation of the beads </param>
    /// <param name="bead"> Starting bead from which construct the path </param>
    /// <param name="length"> Length of the sub-path in the graph </param>
    /// <param name="paths"> List of pats </param>
    /// <param name="path"> Current path </param>
    public static void findDihedrals (Dictionary< GameObject, List<GameObject> > graph, GameObject bead, int length, List< List<GameObject> > paths, List<GameObject> path = null) {
        path = path ?? new List<GameObject>();
        if (path != null) {
            path.Add(bead);
            List<GameObject> pathr = new List<GameObject>(path);
            pathr.Reverse();
            if (path.Count == length) {
                if (!paths.Contains(path) && !paths.Contains(pathr)) {
                    paths.Add(path);
                }
            }
            else {
                foreach (GameObject neighbour in graph[bead]) {
                    List<GameObject> copy = new List<GameObject>(path);
                    findDihedrals(graph, neighbour, length, paths, copy);
                }
            }
        }
    }

    /// <summary>
    /// Computes the bond angle force
    /// Takes arguments in the form <code>(angle_cos, angle_beads, distances)</code>
    /// It returns a tuple <code>(F1, F2, F3, F4)</code> with the vector forces corresponding to each bead
    /// </summary>
    /// <param name="dangle_cos"> The cosine of the dihedral input </param>
    /// <param name="dangle_beads"> The list of beads that form the dihedral </param>
    /// <param name="K_d"> Force constant of dihedral angle </param>
    /// <param name="phi_d"> Equilibrium dihedral angle </param>
    /// <param name="distances"> The list of Vector3 containing the positions of the beads that form the dihedral </param>
    public static List<Vector3> F_d(float dangle_cos, List<GameObject> dangle_beads, float K_d, float phi_d, List<Vector3> distances) {
        float d_angle = Mathf.Acos(dangle_cos);
        Vector3 r1 = distances[0];
        Vector3 r2 = distances[1];
        Vector3 r3 = distances[2];
        Vector3 r4 = distances[3];

        // All these equations were obtained using matrix calculus
        // Will need correction if I made a mistake in a derivative 
        float f = Vector3.Dot(Vector3.Cross((r2 - r1), (r3 - r2)), Vector3.Cross((r3 - r2), (r4 - r3)));
        float g = Vector3.Cross((r2 - r1), (r3 - r2)).magnitude * Vector3.Cross((r3 - r2), (r4 -r3)).magnitude;

        Vector3 half1 = Vector3.Cross(r2 - r1, r3 - r2).normalized;
        Vector3 half2 = Vector3.Cross(r3 - r2, r4 - r3).normalized;

        // here the differential equations need to be added in order to calculate the dihedral potential angles
        // this code until the vectors need to be replaced, because the result from the equations is not currently in the form of a vector
        // the derivation inputed here is probably wrong
        // ask Greg!

        float f_1 = Vector3.Dot((r2 - r3), Vector3.Cross((r3 - r2), (r4 - r3)));
        float g_1 = Vector3.Dot(half1, r2 - r3) * Vector3.Cross(r3 - r2, r4 - r3).magnitude;
        float f_2 = Vector3.Dot(r3 - r2 + r1 - r2, Vector3.Cross(r3 - r2, r4 - r3)) + Vector3.Dot(Vector3.Cross(r2- r1, r3 - r2), r3 - r4);
        float g_2 = Vector3.Dot(half1, r3 - r2 + r1 - r2) +  Vector3.Dot(half2, r3 - r4);
        float f_3 = Vector3.Dot(Vector3.Cross(r2 - r1, r3), Vector3.Cross(r3 - r2, r4 - r3)) + Vector3.Dot(Vector3.Cross(r2 - r1, r3 - r2), r4 - r3 + r2 - r3);
        float g_3 = Vector3.Dot(half1, r2 - r1) + Vector3.Dot(half2, r4 - r3 + r2 - r3);
        float f_4 = Vector3.Dot(Vector3.Cross(r2 - r1, r3 - r2), r3 - r2);
        float g_4 = Vector3.Cross(r2 - r1, r3 - r2).magnitude * Vector3.Dot(half1, r3 - r2);

        // Vector3 F1 = -K_d * (MathF.Sin(d_angle) - phi_d) * ( ((f_1*g) - (f*g_1)) / (g * g) ) * r1;
        // Vector3 F2 = -K_d * (MathF.Sin(d_angle) - phi_d) * ( ((f_2*g) - (f*g_2)) / (g * g) ) * r2;
        // Vector3 F3 = -K_d * (MathF.Sin(d_angle) - phi_d) * ( ((f_3*g) - (f*g_3)) / (g * g) ) * r3;
        // Vector3 F4 = -K_d * (MathF.Sin(d_angle) - phi_d) * ( ((f_4*g) - (f*g_4)) / (g * g) ) * r4;
        Vector3 F1 = 10 * r1;
        Vector3 F2 = 10 * r2;
        Vector3 F3 = 10 * r3;
        Vector3 F4 = 10 * r4;

        // the differential equations need to replace up until here

        List<Vector3> result = new List<Vector3>();
        result.Add(F1); result.Add(F2); result.Add(F3); result.Add(F4);
        return result;
    }

    void Start()
    {
        MartiniModel chain = gameObject.GetComponent<MartiniModel>();
        values = chain.DIHEDRALS;

        // We construct a graph representation of the entire molecule by
        // making a dictionary with structure
        // keyBead : [neighbourBeads]
        for (int i = 0; i < chain.BEADS.Count; i++) {
            graph[chain.BEADS.ElementAt(i)] = chain._edges[i].Select(x => chain._beads[x]).ToList();
        }

        // We use the findAngles function to get the subpaths of size n that we want
        List< List<GameObject> > paths = new List< List<GameObject> >();
        int length = 4;
        foreach (GameObject bead in graph.Keys) {
            findDihedrals(graph, bead, length, paths);
        }
        dihedrals = paths;
    }

    void FixedUpdate()
    {
        for (int i = 0; i < dihedrals.Count; i++) {
            List<GameObject> torsion_beads = dihedrals[i];
            Vector3 one = (torsion_beads[0].transform.position);
            Vector3 two = (torsion_beads[1].transform.position);
            Vector3 three = (torsion_beads[2].transform.position);
            Vector3 four = (torsion_beads[3].transform.position);

            Vector3 u_1 = two - one;
            Vector3 u_2 = three - two;
            Vector3 u_3 = four - three;

            // Computes the list of indexes that form the dihedral
            List<int> indexes = new List<int>();
            for (int j = 0; j < torsion_beads.Count; j++) {
                indexes.Add(torsion_beads[j].GetComponent<EmbeddedBead>().Index);
            }

            // Computes the dihedral angle among the 4 beads
            float K_d = values[indexes][1] * 1000;
            float phi_d = values[indexes][0];
            float dangle_cos = (   (Vector3.Dot(Vector3.Cross(u_1, u_2), Vector3.Cross(u_2, u_3))) 
            / (Vector3.Cross(u_1, u_2).magnitude * Vector3.Cross(u_2, u_3).magnitude)    ); 
            List<Vector3> vectors = new List<Vector3>();
            vectors.Add(one); vectors.Add(two); vectors.Add(three); vectors.Add(four);

            // Computer the force of the dihedral angle
            List<Vector3> F = F_d(dangle_cos, torsion_beads, K_d, phi_d, vectors);

            Debug.Log(string.Format("{0}: {1}", i, string.Join(" , ", F)));

            // Applies the force calculated to each bead in the dihedral
            torsion_beads[0].GetComponent<Rigidbody>().AddForce(F[0]);
            torsion_beads[1].GetComponent<Rigidbody>().AddForce(F[1]);
            torsion_beads[2].GetComponent<Rigidbody>().AddForce(F[2]);
            torsion_beads[3].GetComponent<Rigidbody>().AddForce(F[3]);
        }
    }

    public static float CalculateDihedralAngle(Vector3 one, Vector3 two, Vector3 three, Vector3 four)
    {
        Vector3 u_1 = two - one;
        Vector3 u_2 = three - two;
        Vector3 u_3 = four - three;

        return Vector3.Angle(Vector3.Cross(u_1, u_2), Vector3.Cross(u_2, u_3));

        // Computes the dihedral angle among the 4 beads
        //float dangle_cos = ((Vector3.Dot(Vector3.Cross(u_1, u_2), 
        //Vector3.Cross(u_2, u_3)))/ 
        //(Vector3.Cross(u_1, u_2).magnitude * Vector3.Cross(u_2, u_3).magnitude));
        //return Mathf.Acos(dangle_cos);
    }

    public static Vector3 processFd(float dangle_cos, List<GameObject> dangle_beads) {
            // Computes the list of indexes that form the dihedral
            List<int> indexes = new List<int>();
            for (int j = 0; j < dangle_beads.Count; j++) {
                indexes.Add(dangle_beads[j].GetComponent<EmbeddedBead>().Index);
            }

            var diPos = dangle_beads.Select(x => x.transform.position).ToList();

            float K_d = 0f;
            float phi_d = 0f;
            
            // Computes the cosine of the angle among the three beads
            if (values.ContainsKey(indexes))
            {
                K_d = values[indexes][1] * 1000;
                phi_d = values[indexes][0];
            }

            return F_d(dangle_cos, dangle_beads, K_d, phi_d, diPos)[1];
    }
}