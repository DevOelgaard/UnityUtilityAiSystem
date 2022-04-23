﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;


public class AgentMono : MonoBehaviour, IAgent
{
    [SerializeField]
    [InspectorName("Settings")]
    private AgentModel settings = new AgentModel();
    public AgentModel Model => settings;
    public string TypeIdentifier => GetType().FullName;

    [HideInInspector]
    public string defaultAiName = "";

    private DecisionScoreEvaluator decisionScoreEvaluator;
    private Ai ai;
    public Ai Ai
    {
        get => ai;
        set
        {
            ai = value;
            ai.Context.Agent = this;
        }
    }
    

    void Start()
    {
        Model.Name = SetAgentName();
        AgentManager.Instance.Register(this);
        var aiByName = PlayAbleAiService.Instance.GetAiByName(defaultAiName);
        SetAi(aiByName);
        decisionScoreEvaluator = new DecisionScoreEvaluator();
    }

    public void SetAi(Ai model)
    {
        if (model == null)
        {
            DebugService.LogWarning("Setting Ai of agent: " + name +" to null", this);
            throw new NullReferenceException();
        }
        DebugService.Log("Setting Ai of agent: " + model.Name, this);
        Ai = model;
    }

    void OnDestroy()
    {
        AgentManager.Instance?.Unregister(this);
    }

    /// <summary>
    /// Returns the desired AiAgent name, which is displayed in the UAS Tools
    /// By default set as the name of the attached MonoBehaviour
    /// </summary>
    /// <returns></returns>
    protected virtual string SetAgentName()
    {
        return gameObject.name;
    }

    public void Tick(TickMetaData metaData)
    {
        Ai.Context.TickMetaData = metaData;
        Model.LastTickMetaData = metaData;
        Model.LastTickTime = Time.time;
        Model.LastTickFrame = Time.frameCount;

        var actions = decisionScoreEvaluator.NextActions(Ai.Buckets.Values, Ai.Context, Ai);
        var oldActions = Ai.Context.LastActions;
        foreach(var action in actions)
        {
            if (oldActions.Contains(action))
            {
                action.OnGoing(Ai.Context);
                oldActions.Remove(action);
            } else
            {
                action.OnStart(Ai.Context);
            }
        }

        foreach(var action in oldActions)
        {
            action.OnEnd(Ai.Context);
        }

        Ai.Context.LastActions = actions;
    }

    public bool CanAutoTick()
    {
        if (Model.AutoTick == false) return false;
        if (Time.time - Model.LastTickTime < Model.MsBetweenTicks/1000f) return false;
        return Time.frameCount - Model.LastTickFrame >= Model.FramesBetweenTicks;
    }
}
