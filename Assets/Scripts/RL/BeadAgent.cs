using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BeadAgent : Agent
{
    public NewMoleculeCreator creator; 
    public List<GameObject> dihedral;
    public List<GameObject> bondAngle;
    public int idx;
    public Rigidbody rgBody;
    public List<GameObject> exclude; // includes neighbors that are 2 bonds away or less.
    public static float FORCE_MULTIPLIER = 1000;

    public override void OnEpisodeBegin() 
    {
        creator.ResetEnvironment();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var diPos = dihedral.Select(x => x.transform.position).ToArray();
        var bondPos = bondAngle.Select(x => x.transform.position).ToArray();
        var bond_a = BondedAngle.CalculateBondAngle(bondPos[0], bondPos[1], bondPos[2]);
        var dihedral_a = BondedDihedral.CalculateDihedralAngle(diPos[0], diPos[1], diPos[2], diPos[3]);
        Debug.Log(string.Format("Bead {0}: Theta({1}), Phi({2})", idx, bond_a, dihedral_a));
        sensor.AddObservation(bond_a);
        sensor.AddObservation(dihedral_a);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var diPos = dihedral.Select(x => x.transform.position).ToList();
        var bondPos = bondAngle.Select(x => x.transform.position).ToList();
        var bangle_cos = Mathf.Cos(BondedAngle.CalculateBondAngle(bondPos[0], bondPos[1], bondPos[2]));
        var dangle_cos = Mathf.Cos(BondedDihedral.CalculateDihedralAngle(diPos[0], diPos[1], diPos[2], diPos[3]));

        var F_c = BondedAngle.processFa(bangle_cos, bondAngle) +
                  BondedDihedral.processFd(dangle_cos, dihedral) +
                  NonBondedForce.F_nb(bondAngle[1], exclude, creator.creator.BEADS);
        var dir = F_c.normalized;
        var actions = actionsOut.ContinuousActions;
        actions[0] = F_c.magnitude/FORCE_MULTIPLIER;
        actions[1] = dir.x;
        actions[2] = dir.y;
        actions[3] = dir.z;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var actions = actionBuffers.ContinuousActions;
        

        // Actions
        // agent force on bead
        var F_a = new Vector3(actions[1], actions[2], actions[3]) * (actions[0] * FORCE_MULTIPLIER);
        

        // Rewards
        var diPos = dihedral.Select(x => x.transform.position).ToList();
        var bondPos = bondAngle.Select(x => x.transform.position).ToList();
        var bangle_cos = Mathf.Cos(BondedAngle.CalculateBondAngle(bondPos[0], bondPos[1], bondPos[2]));
        var dangle_cos = Mathf.Cos(BondedDihedral.CalculateDihedralAngle(diPos[0], diPos[1], diPos[2], diPos[3]));

        var F_c = BondedAngle.processFa(bangle_cos, bondAngle) +
                  BondedDihedral.processFd(dangle_cos, dihedral) +
                  NonBondedForce.F_nb(bondAngle[1], exclude, creator.creator.BEADS);  //total force on bead
        
        

        if (float.IsNaN(F_a.magnitude))
        {
            Debug.Log(string.Format("Bead {0} is NaN somewhere, somehow...", idx));
            return;
        }
        
        rgBody.AddForce(F_a, ForceMode.Force);
        
        // SetReward(10000 / (totalForce - estimatedForce).magnitude);
        if (!float.IsNaN(F_c.magnitude))
        {
            Debug.Log("_____Rewards   "+ -(F_c - F_a).magnitude*5);
            AddReward(-(F_c - F_a).magnitude*5);
            Debug.Log(string.Format("Bead {0} Error: {1}", idx, (F_c - F_a).magnitude));
        }

        // We do not need to define when the Episode stops because that is defined on a separate
        // file where max steps is set to a defined number.
    }

    private void FixedUpdate()
    {
        RequestDecision();
    }
}
