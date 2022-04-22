﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using UniRxExtension;

internal class WindowOpener: EditorWindow
{
    public static Rect windowPosition = new Rect(0f, 0f, 1920*0.6f, 1080f / 2f);
    private static EditorWindow _templateManager;

    [MenuItem(Consts.MenuName + Consts.Window_AiInspector_Name)]
    public static void OpenRuntimInspector()
    {
        RunTimeInspector wnd = GetWindow<RunTimeInspector>();
        wnd.titleContent = new GUIContent(Consts.Window_AiInspector_Name);
        wnd.Show();
        wnd.position = windowPosition;
    }


    [MenuItem(Consts.MenuName + Consts.Window_TemplateManager_Name)]
    internal static void OpenTemplateManager()
    {
        if (string.IsNullOrEmpty(ProjectSettingsService.Instance.GetCurrentProjectDirectory()))
        {
            var wnd = GetWindow<SelectProjectWindow>();
            wnd.titleContent = new GUIContent(Consts.Window_SelectProject_Name);
            wnd.SetOnComplete(OpenTemplateManagerPrivate);
            wnd.Show();
            wnd.position = windowPosition;
        } else
        {
            OpenTemplateManagerPrivate();
        }
    }

    private static void OpenTemplateManagerPrivate()
    {
        TemplateManager wnd = GetWindow<TemplateManager>();
        wnd.titleContent = new GUIContent(Consts.Window_TemplateManager_Name);
        wnd.Show();
        wnd.position = windowPosition;
        _templateManager = wnd;
    }

    [MenuItem(Consts.MenuName + Consts.Window_Logger_Name)]
    internal static void OpenAiLogger()
    {
        AiLogWindow wnd = GetWindow<AiLogWindow>();
        wnd.titleContent = new GUIContent(Consts.Window_Logger_Name);
        wnd.Show();
        wnd.position = windowPosition;
    }

    [MenuItem(Consts.MenuName + Consts.Window_AiTickerManager_Name)]
    public static void OpenAiTickerManager()
    {
        AiTickerSettingsWindow wnd = GetWindow<AiTickerSettingsWindow>();
        wnd.titleContent = new GUIContent(Consts.Window_AiTickerManager_Name);
        wnd.Show();
        wnd.position = windowPosition;
    }

    public static ResponseCurveWindow OpenResponseCurve()
    {
        ResponseCurveWindow wnd = GetWindow<ResponseCurveWindow>();
        wnd.titleContent = new GUIContent("Response Curve");
        wnd.Show();

        if(_templateManager != null)
        {
            wnd.position = _templateManager.position;

        } else
        {
            wnd.position = windowPosition;
        }
        return wnd;
    }
}
