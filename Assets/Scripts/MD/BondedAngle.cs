using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;

public class BondedAngle : MonoBehaviour
{
    /// This collection contains all lists of bond angles in the MARTINI chain.
    public List< List<GameObject> > angles = new();

    /// This dictionary will contain the molecule in a more accesible graph format
    public static Dictionary< GameObject, List<GameObject> > graph = new();

    /// This dictionary will provide the values for K_a and theta_a
    public static Dictionary< List<int>, List<float> > values = new();
    
    /// This dictionary establishes the equilibrium angles depending on the types of the beads
    /// forming the angle
    public static IReadOnlyDictionary<List<(string, string)>, float> equilibriumAngles = new Dictionary<List<(string, string)>, float>() {
        {new List<(string, string)>() {("P", "4"), ("N", "a"), ("P", "1")}, 166f},
        {new List<(string, string)>() {("P", "1"), ("N", "a"), ("P", "4")}, 85f},
        {new List<(string, string)>() {("P", "4"), ("N", "a"), ("P", "4")}, 85f},
        {new List<(string, string)>() {("N", "a"), ("P", "4"), ("P", "1")}, 113f},
        {new List<(string, string)>() {("N", "a"), ("P", "4"), ("N", "a")}, 80f},
        {new List<(string, string)>() {("N", "a"), ("P", "4"), ("N", "a")}, 165f}
    };

    /// <summary>
    /// Find all the non-repeating list of atoms that form a bond angle.
    /// Takes arguments in the form <code>(beads, bonds)</code>
    /// </summary>
    /// <param name="graph"> Graph representation of the beads </param>
    /// <param name="bead"> Starting bead from which construct the path </param>
    /// <param name="length"> Length of the sub-path in the graph </param>
    /// <param name="paths"> List of pats </param>
    /// <param name="path"> Current path </param>
    public static void findAngles (Dictionary< GameObject, List<GameObject> > graph, GameObject bead, int length, List< List<GameObject> > paths, List<GameObject> path = null) {
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
                Debug.Log(path.Count);
                
                foreach (GameObject neighbour in graph[bead]) {
                    List<GameObject> copy = new List<GameObject>(path);
                    findAngles(graph, neighbour, length, paths, copy);
                }
            }
        }
    }

    /// <summary>
    /// Computes the bond angle force
    /// Takes arguments in the form <code>(angle_cos, angle_beads, distances)</code>
    /// It returns a tuple <code>(F1, F2, F3)</code> with the vector forces corresponding to each bead
    /// </summary>
    /// <param name="angle_cos"> The cosine of the angle input </param>
    /// <param name="angle_beads"> The list of beads that form the angle </param>
    /// <param name="K_a"> Force constant of bond angle </param>
    /// <param name="theta_a"> Equilibrium bond angle </param>
    /// <param name="distances"> The list of Vector3 containing the positions of the beads that form the angle </param>
    public static List<Vector3> F_a(float angle_cos, List<GameObject> angle_beads, float K_a, float theta_a, List<Vector3> distances) {
        Vector3 r1 = distances[0];
        Vector3 r2 = distances[1];
        Vector3 r3 = distances[2];

        // All these equations were obtained using matrix calculus
        // Will need correction if I made a mistake in a derivative 
        float f = Vector3.Dot(r1 - r2, r3 - r2);
        float g = (r1 - r2).magnitude * (r3 - r2).magnitude;

        Vector3 f_1 = r3 - r2;
        Vector3 g_1 = (r1 - r2) * (r3 - r2).magnitude/(r1 - r2).magnitude;
        Vector3 f_2 = (r2 - r3) + (r2 - r1);
        Vector3 g_2 = - (r1 - r2) * (r3 - r2).magnitude/(r1 - r2).magnitude - (r3 - r2) * (r1 - r2).magnitude/(r3 - r2).magnitude;
        Vector3 f_3 = r1 - r2;
        Vector3 g_3 = (r3 - r2) * (r1 - r2).magnitude/(r3 - r2).magnitude;
        Vector3 F1 = K_a * (angle_cos - MathF.Cos(theta_a)) * ( ((f_1*g) - (f*g_1)) / (g * g) );
        Vector3 F2 = K_a * (angle_cos - MathF.Cos(theta_a)) * ( ((f_2*g) - (f*g_2)) / (g * g) );
        Vector3 F3 = K_a * (angle_cos - MathF.Cos(theta_a)) * ( ((f_3*g) - (f*g_3)) / (g * g) );

        List<Vector3> result = new List<Vector3>();
        result.Add(F1); result.Add(F2); result.Add(F3);
        return result;
    }
    
    void Start()
    {
        MartiniModel chain = gameObject.GetComponent<MartiniModel>();
        values = chain.ANGLES;

        // We construct a graph representation of the entire molecule by
        // making a dictionary with structure
        // keyBead : [neighbourBeads]
        for (int i = 0; i < chain.BEADS.Count; i++) {
            graph[chain.BEADS.ElementAt(i)] = chain._edges[i].Select(x => chain._beads[x]).ToList();
        }

        // We use the findAngles function to get the subpaths of size n that we want
        List< List<GameObject> > paths = new List< List<GameObject> >();
        int length = 3;
        foreach (GameObject bead in graph.Keys) {
            findAngles(graph, bead, length, paths);
        }
        angles = paths;
    }

    void FixedUpdate()
    {
        for (int i = 0; i < angles.Count; i++) {
            List<GameObject> angle_beads = angles[i];
            Vector3 one = (angle_beads[0].transform.position);
            Vector3 two = (angle_beads[1].transform.position);
            Vector3 three = (angle_beads[2].transform.position);

            Vector3 r_1 = one - two;
            Vector3 r_2 = three - two;

            // Computes the list of indexes that form the dihedral
            List<int> indexes = new List<int>();
            for (int j = 0; j < angle_beads.Count; j++) {
                indexes.Add(angle_beads[j].GetComponent<EmbeddedBead>().Index);
            }

            // Computes the cosine of the angle among the three beads
            float K_a = values[indexes][1] * 1000;
            float theta_a = values[indexes][0];
            float angle_cos = (Vector3.Dot(r_1, r_2)) / (r_1.magnitude * r_2.magnitude);
            List<Vector3> vectors = new List<Vector3>();
            vectors.Add(one); vectors.Add(two); vectors.Add(three);

            // Compute the force of the bonded angle
            var F = F_a(angle_cos, angle_beads, K_a, theta_a, vectors);
            
            Debug.Log(string.Format("{0}: {1}", i, string.Join(" , ", F)));

            // Applies the force calculated to each bead in the angle
            angle_beads[0].GetComponent<Rigidbody>().AddForce(F[0]);
            angle_beads[1].GetComponent<Rigidbody>().AddForce(F[1]);
            angle_beads[2].GetComponent<Rigidbody>().AddForce(F[2]);
        }
    }

    public static float CalculateBondAngle(Vector3 one, Vector3 two, Vector3 three)
    {
        return Vector3.Angle(one - two, three - two);
        // Vector3 r_1 = one - two;
        // Vector3 r_2 = three - two;
        //
        // // Computes the cosine of the angle among the three beads
        // float angle_cos = (Vector3.Dot(r_1, r_2)) / (r_1.magnitude * r_2.magnitude);
        //
        // return Mathf.Acos(angle_cos);
    }

    public static Vector3 processFa(float angle_cos, List<GameObject> angle_beads) {
            // Computes the list of indexes that form the dihedral
            List<int> indexes = new List<int>();
            for (int j = 0; j < angle_beads.Count; j++) {
                indexes.Add(angle_beads[j].GetComponent<EmbeddedBead>().Index);
            }

            var typeAndSub = angle_beads.Select(x => x.GetComponent<EmbeddedBead>().TypeAndSubType).ToList();

            var bondPos = angle_beads.Select(x => x.transform.position).ToList();

            var param = (values.Keys.Any(x => x.Contains(indexes[1]))) ? values[indexes] : new List<float>() { equilibriumAngles.GetValueOrDefault(typeAndSub, 0f), 50f/1000};

            // Computes the cosine of the angle among the three beads
            float K_a = param[1] * 1000;
            float theta_a = param[0];
            return F_a(angle_cos, angle_beads, K_a, theta_a, bondPos)[1];
    }
}