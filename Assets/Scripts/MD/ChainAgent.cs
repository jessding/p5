using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(ChainObserver))]
[RequireComponent(typeof(NewMoleculeCreator))]
public class ChainAgent : Agent
{
    public static float MAX_STEPS = 1000;
    public static float DIST_MULTIPLIER = 10;
    private ChainCreator creator;
    private ChainObserver observer;

    // Start is called before the first frame update
    void Start()
    {
        // observer = this.GetComponent<ChainObserver>();
        // creator = observer.chainCreator;
    }

    public override void OnEpisodeBegin()
    {
        // creator.ResetEnvironment();
    }

    

    public override void CollectObservations(VectorSensor sensor) 
    {
        // sensor.AddObservation(observer.rg_sq);
        // // sensor.AddObservation of chain location
        // sensor.AddObservation(creator.BEADS);
    }

    // public int StepCount { get => step; }
    // public override void Heuristic(in ActionBuffers actionsOut)
    // public void AddReward(float increment)

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // carry out the received action, add to reward, check for success, increment step (do i have to explicitly do this or does ml-agents do for me)

        var actions = actionBuffers.ContinuousActions;
        var displacement = new Vector3(actions[1], actions[2], actions[3]) * (actions[0] * DIST_MULTIPLIER);
        // need to actually carry it out


        // reward: 
        // if no displacement, - , 
        // if "ensemble rebuilding" - how to quantify??, +, 
        // add reward for every {distance between two neighboring beads under certain threshold} 
        // look at martinimodel createbond to find out length of bond cylinder object for distance btwn beads threshold

        var beads_dist = 0;
        var rebuild_threshold = 0;
        // if at RG state (how to tell??), + big
        // hardcoded threshold [50, 150]
        var rg_range_lower = 50f;
        var rg_range_upper = 150f;
        var at_rg_state = (observer.rg_sq <= rg_range_upper && observer.rg_sq >= rg_range_lower);

        if (displacement.Equals(Vector3.zero)) 
        {
            AddReward(-0.01f);
        }

        else if (beads_dist < rebuild_threshold)
        {
            AddReward(0.01f);
        }

        else if (at_rg_state)
        {
            AddReward(1f);
        }

        // check for if at goal state, or if StepCount() == max steps

        // if (at_rg_state || StepCount >= MAX_STEPS)
        // {
            
        // }

    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        RequestDecision();
    }

}