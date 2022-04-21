﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using UniRx;
using UniRxExtension;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

internal class AiComponent : AiObjectComponent 
{
    private readonly CompositeDisposable subscriptions = new CompositeDisposable();
    private IDisposable bucketTabSub;
    private readonly TemplateContainer root;
    private readonly DropdownContainerComponent<UtilityContainerSelector> bucketDropdown;
    private readonly DropdownContainerComponent<UtilityContainerSelector> decisionDropdown;
    private readonly DropdownDescriptionComponent<IUtilityScorer> utilityScorerDropdown;
    private Ai aiModel;
    private readonly VisualElement collectionsContainer;
    private readonly Toggle playableToggle;

    private readonly TabViewComponent tabView;
    private readonly Button bucketTab;
    private Button settingsTab;
    private readonly HelpBox playAbleHelpBox;

    private readonly CollectionComponent<Bucket> bucketCollection;

    internal AiComponent() : base()
    {
        root = AssetDatabaseService.GetTemplateContainer(GetType().FullName);
        Body.Clear();
        Body.Add(root);
        collectionsContainer = root.Q<VisualElement>("CollectionsContainer");
        playAbleHelpBox = new HelpBox("Not set to playable!", HelpBoxMessageType.Warning);
        tabView = new TabViewComponent();
        collectionsContainer.Add(tabView);

        bucketCollection = new CollectionComponent<Bucket>(UasTemplateService.Instance.Buckets, "Bucket", "Buckets");

        var settingsContainer = new VisualElement();

        playableToggle = new Toggle("Playable")
        {
            name = "Playable-Toggle"
        };

        bucketDropdown = new DropdownContainerComponent<UtilityContainerSelector>("Bucket Selector");
        settingsContainer.Add(bucketDropdown);
        decisionDropdown = new DropdownContainerComponent<UtilityContainerSelector>("Decision Selector");
        settingsContainer.Add(decisionDropdown);

        utilityScorerDropdown = new DropdownDescriptionComponent<IUtilityScorer>();
        settingsContainer.Add(utilityScorerDropdown);
        settingsContainer.name = "SettingsContainer";

        bucketDropdown.name = "DropdownScorerCollection";
        decisionDropdown.name = "DropdownScorerCollection";
        utilityScorerDropdown.name = "DropdownScorerCollection";

        bucketTab = tabView.AddTabGroup("Buckets", bucketCollection);
        settingsTab = tabView.AddTabGroup("Settings", settingsContainer);
         
        playableToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
        {
            if (aiModel == null) return;
            aiModel.IsPLayAble = evt.newValue;

            UpdateHelpBox(evt.newValue);
        });
    }

    private void UpdateHelpBox(bool hide)
    {
        if (hide)
        {
            playAbleHelpBox.style.display = DisplayStyle.None;
        }
        else
        {
            playAbleHelpBox.style.display = DisplayStyle.Flex;
        }
    }

    protected override void UpdateInternal(AiObjectModel model)
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        aiModel = model as Ai;

        ScoreContainer.Add(playableToggle);
        ScoreContainer.Add(playAbleHelpBox);

        Debug.Assert(aiModel != null, nameof(aiModel) + " != null");
        bucketTab.text = "Buckets (" + aiModel.Buckets.Count + ")";
        bucketTabSub?.Dispose();
        bucketTabSub = aiModel.Buckets.OnValueChanged
            .Subscribe(list => bucketTab.text = "Buckets (" + list.Count + ")");
        
        
        playableToggle.SetValueWithoutNotify(aiModel.IsPLayAble);
        UpdateHelpBox(playableToggle.value);

        if (aiModel == null)
        {
            bucketCollection.SetElements(new ReactiveList<Bucket>());
        }
        else
        {
            bucketCollection.SetElements(aiModel.Buckets);
        }
        subscriptions.Clear();

        var currentDecisionIndex = aiModel.DecisionSelectors.IndexOf(aiModel.CurrentDecisionSelector);
        decisionDropdown.UpdateUi(aiModel.DecisionSelectors, currentDecisionIndex);

        decisionDropdown
            .OnSelectedObjectChanged
            .Subscribe(selector => {
                if (aiModel != null)
                {
                    aiModel.CurrentDecisionSelector = selector;
                }
            })
            .AddTo(subscriptions);

        var currentBucketindex = aiModel.BucketSelectors.IndexOf(aiModel.CurrentBucketSelector);
        bucketDropdown.UpdateUi(aiModel.BucketSelectors,currentDecisionIndex);
        bucketDropdown
            .OnSelectedObjectChanged
            .Subscribe(selector =>
            {
                if (aiModel != null)
                {
                    aiModel.CurrentBucketSelector = selector;
                }
            })
            .AddTo(subscriptions);

        utilityScorerDropdown.UpdateUi(ScorerService.Instance.UtilityScorers, "Utility Scorer", aiModel.UtilityScorer.GetName());
        utilityScorerDropdown
            .OnDropdownValueChanged
            .Subscribe(uS =>
            {
                if (aiModel != null)
                {
                    aiModel.UtilityScorer = uS;
                }
            })
            .AddTo(subscriptions);
        TimerService.Instance.LogCall(sw.ElapsedMilliseconds, "UpdateInternal AI");
    }

    ~AiComponent()
    {
        bucketTabSub?.Dispose();
        subscriptions.Clear();
    }
}
