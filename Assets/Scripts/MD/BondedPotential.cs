using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BondedPotential : MonoBehaviour
{
    /// This collection contains all lists of bonds in the MARTINI chain.
    public List< List<GameObject> > bonds = new();

    /// This dictionary will contain the molecule in a more accesible graph format
    public static Dictionary< GameObject, List<GameObject> > graph = new();

    /// This dictionary will provide the values for K_b and d_b
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
    public static void findBonds (Dictionary< GameObject, List<GameObject> > graph, GameObject bead, int length, List< List<GameObject> > paths, List<GameObject> path = null) {
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
                    findBonds(graph, neighbour, length, paths, copy);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the Bonded force between 2 beads
    /// Takes arguments in the form <code>(r_ij, K_b, d_b)</code>
    /// </summary>
    /// <param name="Kb"> Vector3 from bead i to bead j </param>
    /// <param name="db"> The force constant </param>
    /// <param name="distances"> The equilibrium distance between two beads </param>
    public static List<Vector3> F_b(float Kb, float db, List<Vector3> distances) {
        Vector3 r = distances[1] - distances[0];

        Vector3 F1 = -2f * Kb * (r.magnitude - db) * r/r.magnitude;
        Vector3 F2 = -2f * Kb * (r.magnitude - db) * r/r.magnitude;

        List<Vector3> result = new List<Vector3>();
        result.Add(F1); result.Add(F2); 
        return result;
    }

    void Start()
    {
        MartiniModel chain = gameObject.GetComponent<MartiniModel>();
        values = chain.BONDS;

        // We construct a graph representation of the entire molecule by
        // making a dictionary with structure
        // keyBead : [neighbourBeads]
        for (int i = 0; i < chain.BEADS.Count; i++) {
            List<GameObject> neighbors = new List<GameObject>();
            for (int j = 0; j < chain._edges[i].Count; j++) {
                neighbors.Add(chain._beads[chain._edges[i][j]]);
            }
            graph[chain.BEADS[i]] = neighbors;
        }

        // We use the findAngles function to get the subpaths of size n that we want
        List< List<GameObject> > paths = new List< List<GameObject> >();
        int length = 2;
        foreach (GameObject bead in graph.Keys) {
            findBonds(graph, bead, length, paths);
        }
        bonds = paths;
    }

    void FixedUpdate()
    {
        for (int i = 0; i < bonds.Count; i++) {
            List<GameObject> bond_beads = bonds[i];
            Vector3 one = (bond_beads[0].transform.position);
            Vector3 two = (bond_beads[1].transform.position);

            // Computes the values of K_b and d_b
            List<int> indexes = new List<int>();
            indexes.Add(bond_beads[0].GetComponent<EmbeddedBead>().Index); indexes.Add(bond_beads[1].GetComponent<EmbeddedBead>().Index);
            float K_b = values[indexes][1] * 1000f;
            float d_b = values[indexes][0];
            List<Vector3> vectors = new List<Vector3>();
            vectors.Add(one); vectors.Add(two);

            // Computes the force of the potential angle
            List<Vector3> F = F_b(K_b, d_b, vectors);

            Debug.Log(string.Format("{0}: {1}", i, string.Join(" , ", F)));

            // Applies the force calculated to each bead in the dihedral
            bond_beads[0].GetComponent<Rigidbody>().AddForce(F[0]);
            bond_beads[1].GetComponent<Rigidbody>().AddForce(F[1]);
        }
    }
}