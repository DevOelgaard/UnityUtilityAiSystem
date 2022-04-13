﻿using UnityEngine;
using UniRx;
using UniRxExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

internal class UasTemplateService: RestoreAble
{
    private readonly CompositeDisposable subscriptions = new CompositeDisposable();
    private Dictionary<string, ReactiveList<AiObjectModel>> collectionsByLabel = new Dictionary<string, ReactiveList<AiObjectModel>>();

    public ReactiveListNameSafe<AiObjectModel> AIs;
    public ReactiveListNameSafe<AiObjectModel> Buckets;
    public ReactiveListNameSafe<AiObjectModel> Decisions;
    public ReactiveListNameSafe<AiObjectModel> Considerations;
    public ReactiveListNameSafe<AiObjectModel> AgentActions;
    public ReactiveListNameSafe<AiObjectModel> ResponseCurves;

    public IObservable<ReactiveList<AiObjectModel>> OnCollectionChanged => onCollectionChanged;
    private readonly Subject<ReactiveList<AiObjectModel>> onCollectionChanged = new Subject<ReactiveList<AiObjectModel>>();

    // private bool autoSaveLoaded = false;
    private string projectDirectory;

    private bool includeDemos = true;
    public bool IncludeDemos
    {
        get => includeDemos;
        set
        {
            includeDemos = value; 
            onIncludeDemosChanged?.OnNext(value);
        }
    }
    public IObservable<bool> OnIncludeDemosChanged => onIncludeDemosChanged;
    private readonly Subject<bool> onIncludeDemosChanged = new Subject<bool>();

    private static UasTemplateService _instance;
    internal static UasTemplateService Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new UasTemplateService();
            #pragma warning disable CS4014
            _instance.Init(true);
            #pragma warning restore CS4014
            return _instance;
        }
    }

    private UasTemplateService()
    {
    }

    private async Task Init(bool restore)
    {
        AIs = new ReactiveListNameSafe<AiObjectModel>();
        Buckets = new ReactiveListNameSafe<AiObjectModel>();
        Decisions = new ReactiveListNameSafe<AiObjectModel>();
        Considerations = new ReactiveListNameSafe<AiObjectModel>();
        AgentActions = new ReactiveListNameSafe<AiObjectModel>();
        ResponseCurves = new ReactiveListNameSafe<AiObjectModel>();

        collectionsByLabel = new Dictionary<string, ReactiveList<AiObjectModel>>
        {
            {Consts.Label_UAIModel, AIs},
            {Consts.Label_BucketModel, Buckets},
            {Consts.Label_DecisionModel, Decisions},
            {Consts.Label_ConsiderationModel, Considerations},
            {Consts.Label_AgentActionModel, AgentActions},
            {Consts.Label_ResponseCurve, ResponseCurves}
        };

        SubscribeToCollectionChanges();

        if (restore)
        {
            await LoadCurrentProject(true);
        }
    }

    private void SubscribeToCollectionChanges()
    {
        foreach(var (_, value) in collectionsByLabel)
        {
            value.OnValueChanged
                .Subscribe(_ => 
                    onCollectionChanged.OnNext(value))
                .AddTo(subscriptions);
        }
    }

    internal async Task LoadCurrentProject(bool backup = false)
    {
        var loadPath = backup
            ? ProjectSettingsService.Instance.GetProjectBackupPath()
            : ProjectSettingsService.Instance.GetCurrentProjectPath();
        projectDirectory = ProjectSettingsService.Instance.GetDirectory(loadPath);

        if (string.IsNullOrEmpty(projectDirectory))
        {
            return;
        }
        
        ClearCollectionNoNotify();
        var perstistAPI = PersistenceAPI.Instance;
        
        var state = perstistAPI.LoadObjectPath<UasTemplateServiceState>(loadPath).LoadedObject;
        if (state == null)
        {
            ClearCollectionNotify();
        }
        else
        {
            try
            {
                await Restore(state);
                Save(true);
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
        perstistAPI.SaveDestructiveObjectPath(this,
            !backup
                ? ProjectSettingsService.Instance.GetCurrentProjectDirectory()
                : ProjectSettingsService.Instance.GetBackupDirectory(),
            ProjectSettingsService.Instance.GetCurrentProjectName(true));
    }

    protected override string GetFileName()
    {
        return "UASProject";
    }

    internal async Task Reset()
    {
        subscriptions.Clear();
        ClearCollectionNoNotify();
        await Init(false);
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

    ~UasTemplateService()
    {
        subscriptions.Clear();
    }

    internal override RestoreState GetState()
    {
        return new UasTemplateServiceState(AIs, Buckets, Decisions, Considerations, AgentActions, this);
    }

    protected override void InternalSaveToFile(string path, IPersister destructivePersister, RestoreState state)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!path.Contains(Consts.FileExtension_UasProject))
        {
            path += "." + Consts.FileExtension_UasProject;
        }
        destructivePersister.SaveObject(state, path) ;

        // Guard if saving destructively. Should only happen for Project level
        var persister = new JsonPersister();
        foreach(var aiObjectModel in AIs.Values)
        {
            var a = (Ai) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_Ais;
            a.SaveToFile(subPath, persister);
        }

        foreach (var aiObjectModel in Buckets.Values)
        {
            var b = (Bucket) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_Buckets;
            b.SaveToFile(subPath, persister);
        }

        foreach (var aiObjectModel in Decisions.Values)
        {
            var d = (Decision) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_Decisions;
            d.SaveToFile(subPath, persister);
        }

        foreach (var aiObjectModel in Considerations.Values)
        {
            var c = (Consideration) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_Considerations;
            c.SaveToFile(subPath, persister);
        }

        foreach (var aiObjectModel in AgentActions.Values)
        {
            var aa = (AgentAction) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_AgentActions;
            aa.SaveToFile(subPath, persister);
        }

        foreach (var aiObjectModel in ResponseCurves.Values)
        {
            var rc = (ResponseCurve) aiObjectModel;
            var subPath = directoryPath + "/" + Consts.FolderName_ResponseCurves;
            rc.SaveToFile(subPath, persister);
        }
    }

    private async Task Restore(UasTemplateServiceState state)
    {
        await RestoreInternalAsync(state);
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

    protected override async Task RestoreInternalAsync(RestoreState s, bool restoreDebug = false)
    {
        ClearCollectionNotify();
        SubscribeToCollectionChanges();
        var state = (UasTemplateServiceState)s;
        if (state == null)
        {
            return;
        }

        var tasks = new List<Task>
        {
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<Ai>(projectDirectory + Consts.FolderName_Ais, state.aIs,AIs,restoreDebug)),
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<Bucket>(projectDirectory + Consts.FolderName_Buckets, state.buckets,Buckets,restoreDebug)),
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<Decision>(projectDirectory + Consts.FolderName_Decisions, state.decisions,Decisions,restoreDebug)),
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<Consideration>(projectDirectory + Consts.FolderName_Considerations, state.considerations,Considerations,restoreDebug)),
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<AgentAction>(projectDirectory + Consts.FolderName_AgentActions, state.agentActions,AgentActions,restoreDebug)),
            Task.Factory.StartNew(() => RestoreAbleService.LoadObjectsAndSortToCollection<ResponseCurve>(projectDirectory + Consts.FolderName_ResponseCurves, state.responseCurves,ResponseCurves,restoreDebug))
        };

        await Task.WhenAll(tasks);
    }
}

[Serializable]
public class UasTemplateServiceState : RestoreState
{
    public List<string> aIs;
    public List<string> buckets;
    public List<string> decisions;
    public List<string> considerations;
    public List<string> agentActions;
    public List<string> responseCurves;
    public UasTemplateServiceState()
    {
    }

    internal UasTemplateServiceState(
        ReactiveList<AiObjectModel> aiS,
        ReactiveList<AiObjectModel> buckets, ReactiveList<AiObjectModel> decisions,
        ReactiveList<AiObjectModel> considerations, ReactiveList<AiObjectModel> agentActions, UasTemplateService model) : base(model)
    {
        aIs = RestoreAbleService.NamesToList(aiS.Values);

        this.buckets = RestoreAbleService.NamesToList(buckets.Values);

        this.decisions = RestoreAbleService.NamesToList(decisions.Values);

        this.considerations = RestoreAbleService.NamesToList(considerations.Values);

        this.agentActions = RestoreAbleService.NamesToList(agentActions.Values);

        responseCurves = RestoreAbleService.NamesToList(model.ResponseCurves.Values);
    }

    internal static UasTemplateServiceState LoadFromFile()
    {
        Debug.Log("Change this");
        var p = PersistenceAPI.Instance;
        var state = p.LoadObjectPanel<UasTemplateServiceState>();
        return state.LoadedObject;
    }
}
