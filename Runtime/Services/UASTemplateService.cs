﻿using UnityEngine;
using UniRx;
using UniRxExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

internal class UASTemplateService: RestoreAble
{
    private CompositeDisposable subscriptions = new CompositeDisposable();
    private Dictionary<string, ReactiveList<AiObjectModel>> collectionsByLabel = new Dictionary<string, ReactiveList<AiObjectModel>>();

    public ReactiveListNameSafe<AiObjectModel> AIs;
    public ReactiveListNameSafe<AiObjectModel> Buckets;
    public ReactiveListNameSafe<AiObjectModel> Decisions;
    public ReactiveListNameSafe<AiObjectModel> Considerations;
    public ReactiveListNameSafe<AiObjectModel> AgentActions;
    public ReactiveListNameSafe<AiObjectModel> ResponseCurves;

    public IObservable<ReactiveList<AiObjectModel>> OnCollectionChanged => onCollectionChanged;
    private Subject<ReactiveList<AiObjectModel>> onCollectionChanged = new Subject<ReactiveList<AiObjectModel>>();

    private bool autoSaveLoaded = false;

    private static UASTemplateService instance;
    public UASTemplateService()
    {
        Init(true);
    }


    private void Init(bool restore)
    {
        Debug.Log("Init UAS TEmplate");
        AIs = new ReactiveListNameSafe<AiObjectModel>();
        Buckets = new ReactiveListNameSafe<AiObjectModel>();
        Decisions = new ReactiveListNameSafe<AiObjectModel>();
        Considerations = new ReactiveListNameSafe<AiObjectModel>();
        AgentActions = new ReactiveListNameSafe<AiObjectModel>();
        ResponseCurves = new ReactiveListNameSafe<AiObjectModel>();

        collectionsByLabel = new Dictionary<string, ReactiveList<AiObjectModel>>();
        collectionsByLabel.Add(Consts.Label_UAIModel, AIs);
        collectionsByLabel.Add(Consts.Label_BucketModel, Buckets);
        collectionsByLabel.Add(Consts.Label_DecisionModel, Decisions);
        collectionsByLabel.Add(Consts.Label_ConsiderationModel, Considerations);
        collectionsByLabel.Add(Consts.Label_AgentActionModel, AgentActions);
        collectionsByLabel.Add(Consts.Label_ResponseCurve, ResponseCurves);

        foreach(var collection in collectionsByLabel)
        {
            collection.Value.OnValueChanged
                .Subscribe(col => onCollectionChanged.OnNext(collection.Value))
                .AddTo(subscriptions);
        }

        //if (restore)
        //{
        //    LoadCurrentProject();
        //}
    }

    internal void LoadCurrentProject(bool backup = false)
    {
        ClearCollectionNoNotify();
        Debug.Log("Loading Current project");
        var perstistAPI = PersistenceAPI.Instance;
        UASTemplateServiceState state;
        if (!backup)
        {
            state = perstistAPI.LoadObjectPath<UASTemplateServiceState>(ProjectSettingsService.Instance.GetCurrentProjectPath()).LoadedObject;
            autoSaveLoaded = true;
        }
        else
        {
            state = perstistAPI.LoadObjectPath<UASTemplateServiceState>(Consts.File_UASTemplateService_BackUp + Consts.FileExtension_JSON).LoadedObject;
        }
        if (state == null)
        {
            ClearCollectionNotify();
        }

        else
        {
            try
            {
                Restore(state);
            }
            catch (Exception ex)
            {
                throw new Exception("UAS Template Service Restore Failed : ", ex);
                //Debug.LogWarning("UASTemplateService Restore failed: " + ex);
            }
        }
    }

    internal void Save(bool backup = false)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        var perstistAPI = PersistenceAPI.Instance;
        if (!backup)
        {
            perstistAPI.SaveDestructiveObjectPath(this, ProjectSettingsService.Instance.GetCurrentProjectDirectory(), ProjectSettingsService.Instance.GetCurrentProjectName(true));
        } else
        {
            perstistAPI.SaveObjectPath(this, Consts.File_UASTemplateService_BackUp, "BackUp");
        }
        //Debug.Log("Autosave completed in: " + sw.ElapsedMilliseconds + "ms");
    }

    protected override string GetFileName()
    {
        return "UASProject";
    }

    internal void Reset()
    {
        subscriptions.Clear();
        ClearCollectionNoNotify();
        Init(false);
    }

    internal static UASTemplateService Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UASTemplateService();
            }
            return instance;
        }
    }

    internal Ai GetAiByName(string name, bool isPLayMode = false)
    {
        var aiTemplate = AIs.Values
            .Cast<Ai>()
            .FirstOrDefault(ai => ai.Name == name && ai.IsPLayable);

        if (aiTemplate == null)
        {
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning("Ai: " + name + " not found, returning default Ai");
            }
            aiTemplate = AIs.Values.Cast<Ai>().First(ai => ai.IsPLayable);
            if (aiTemplate == null)
            {
                Debug.LogError("No ai found");
                throw new Exception("No default Ai found AiName: " + name + " is the ai playable?");
            }
        }
        var clone = aiTemplate.Clone() as Ai;

        return clone;
    }

    internal ReactiveList<AiObjectModel> GetCollection(string label)
    {
        if (collectionsByLabel.ContainsKey(label))
        {
            return collectionsByLabel[label];
        } else
        {
            return null;
        }
    }

    internal ReactiveList<AiObjectModel> GetCollection(Type t)
    {
        if (t.IsAssignableFrom(typeof(Ai)))
        {
            return AIs;
        }
        if (t.IsAssignableFrom(typeof(Bucket)))
        {
            return Buckets;
        }
        if (t.IsAssignableFrom(typeof(Decision)))
        {
            return Decisions;
        }
        if (t.IsAssignableFrom(typeof(Consideration)))
        {
            return Considerations;
        }

        if (t.IsAssignableFrom(typeof(AgentAction)))
        {
            return AgentActions;
        }

        if (t.IsAssignableFrom(typeof(ResponseCurve)))
        {
            return ResponseCurves;
        }
        return null;
    }

    private void OnEnable()
    {
        subscriptions.Clear();
    }

    private void OnDisable()
    {
        Debug.Log("Disable");
        
        subscriptions.Clear();
    }

    private void OnDestroy()
    {
        subscriptions.Clear();
    }

    ~UASTemplateService()
    {
        subscriptions.Clear();
    }

    internal override RestoreState GetState()
    {
        return new UASTemplateServiceState(collectionsByLabel, AIs, Buckets, Decisions, Considerations, AgentActions, this);
    }

    protected override void InternalSaveToFile(string path, IPersister destructivePerister, RestoreState state)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!path.Contains(Consts.FileExtension_UasProject))
        {
            path += "." + Consts.FileExtension_UasProject;
        }
        destructivePerister.SaveObject(state, path) ;

        // Guard if saving destructively. Should only happen for Project level
        var persister = new JSONPersister();
        foreach(Ai a in AIs.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_Ais;
            a.SaveToFile(subPath, persister);
        }

        foreach (Bucket b in Buckets.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_Buckets;
            b.SaveToFile(subPath, persister);
        }

        foreach (Decision d in Decisions.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_Decisions;
            d.SaveToFile(subPath, persister);
        }

        foreach (Consideration c in Considerations.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_Considerations;
            c.SaveToFile(subPath, persister);
        }

        foreach (AgentAction aa in AgentActions.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_AgentActions;
            aa.SaveToFile(subPath, persister);
        }

        foreach (ResponseCurve rc in ResponseCurves.Values)
        {
            var subPath = directoryPath + "/" + Consts.FolderName_ResponseCurves;
            rc.SaveToFile(subPath, persister);
        }
    }
     
    internal void Restore(UASTemplateServiceState state)
    {
        RestoreInternal(state, false);
    }

    internal void Add(AiObjectModel model)
    {
        var collection = GetCollection(model);
        collection.Add(model);
    }

    internal void Remove(AiObjectModel model)
    {
        var collection = GetCollection(model);
        collection.Remove(model);
    }

    private void ClearCollectionNotify()
    {
        subscriptions?.Clear();
        AIs?.Clear();
        Buckets?.Clear();
        Decisions?.Clear();
        Considerations?.Clear();
        AgentActions?.Clear();
        ResponseCurves?.Clear();
    }

    private void ClearCollectionNoNotify() {
        subscriptions?.Clear();
        AIs?.ClearNoNotify();
        Buckets?.ClearNoNotify();
        Decisions?.ClearNoNotify();
        Considerations?.ClearNoNotify();
        AgentActions?.ClearNoNotify();
        ResponseCurves?.ClearNoNotify();
    }

    private ReactiveList<AiObjectModel> GetCollection(AiObjectModel model)
    {
        var type = model.GetType();

        if (TypeMatches(type, typeof(Consideration)))
        {
            return Considerations;
        }
        else if (TypeMatches(type,typeof(Decision)))
        {
            return Decisions;
        } else if (TypeMatches(type, typeof(AgentAction)))
        {
            return AgentActions;
        } else if (TypeMatches(type, typeof(Bucket)))
        {
            return Buckets;
        } else if (TypeMatches(type, typeof(Ai)))
        {
            return AIs;
        } else if (TypeMatches(type, typeof(ResponseCurve)))
        {
            return ResponseCurves;
        }
        return null;
    }

    private bool TypeMatches(Type a, Type b)
    {
        return a.IsAssignableFrom(b) || a.IsSubclassOf(b);
    }

    protected override void RestoreInternal(RestoreState s, bool restoreDebug)
    {
        ClearCollectionNotify();
        var state = (UASTemplateServiceState)s;
        if (state == null)
        {
            return;
        }
        var directory = ProjectSettingsService.Instance.GetCurrentProjectDirectory();
        
        var ais = RestoreAbleService.GetAiObjects<Ai>(directory + Consts.FolderName_Ais, restoreDebug);
        AIs.Add(RestoreAbleService.SortByName(state.AIs, ais));

        var buckets = RestoreAbleService.GetAiObjects<Bucket>(directory + Consts.FolderName_Buckets, restoreDebug);
        Buckets.Add(RestoreAbleService.SortByName(state.Buckets, buckets));

        var decisions = RestoreAbleService.GetAiObjects<Decision>(directory + Consts.FolderName_Decisions, restoreDebug);
        Decisions.Add(RestoreAbleService.SortByName(state.Decisions, decisions));

        var considerations = RestoreAbleService.GetAiObjects<Consideration>(directory + Consts.FolderName_Considerations, restoreDebug);
        Considerations.Add(RestoreAbleService.SortByName(state.Considerations, considerations));

        var agentActions = RestoreAbleService.GetAiObjects<AgentAction>(directory + Consts.FolderName_AgentActions, restoreDebug);
        AgentActions.Add(RestoreAbleService.SortByName(state.AgentActions, agentActions));

        var responseCurves = RestoreAbleService.GetAiObjects<ResponseCurve>(directory + Consts.FolderName_ResponseCurves,restoreDebug);
        ResponseCurves.Add(RestoreAbleService.SortByName(state.ResponseCurves, responseCurves));
    }
}

[Serializable]
public class UASTemplateServiceState : RestoreState
{
    public List<string> AIs;
    public List<string> Buckets;
    public List<string> Decisions;
    public List<string> Considerations;
    public List<string> AgentActions;
    public List<string> ResponseCurves;
    public UASTemplateServiceState() : base()
    {
    }

    internal UASTemplateServiceState(
        Dictionary<string, ReactiveList<AiObjectModel>> collectionsByLabel, ReactiveList<AiObjectModel> aiS,
        ReactiveList<AiObjectModel> buckets, ReactiveList<AiObjectModel> decisions,
        ReactiveList<AiObjectModel> considerations, ReactiveList<AiObjectModel> agentActions, UASTemplateService model) : base(model)
    {
        AIs = RestoreAbleService.NamesToList(aiS.Values);

        Buckets = RestoreAbleService.NamesToList(buckets.Values);

        Decisions = RestoreAbleService.NamesToList(decisions.Values);

        Considerations = RestoreAbleService.NamesToList(considerations.Values);

        AgentActions = RestoreAbleService.NamesToList(agentActions.Values);

        ResponseCurves = RestoreAbleService.NamesToList(model.ResponseCurves.Values);
    }

    internal static UASTemplateServiceState LoadFromFile()
    {
        Debug.Log("Change this");
        var p = PersistenceAPI.Instance;
        var state = p.LoadObjectPanel<UASTemplateServiceState>();
        return state.LoadedObject;
    }
}
