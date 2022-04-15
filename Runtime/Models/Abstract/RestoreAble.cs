﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

public abstract class RestoreAble
{
    public string FileName => GetFileName();
    public readonly Type DerivedType;
    protected RestoreAble()
    {
        DerivedType = GetType();
    }

    public virtual string GetTypeDescription()
    {
        return DerivedType.ToString();
    }

    protected abstract string GetFileName();

    public string CurrentDirectory;
    protected abstract Task RestoreInternalAsync(RestoreState state, bool restoreDebug = false);
    public static T Restore<T>(RestoreState state, bool restoreDebug = false) where T:RestoreAble
    {
        // var type = Type.GetType(state.DerivedTypeString);
        var type = state.DerivedType;
        T element = default(T);
        if (type == null)
        {
            element = AssetDatabaseService.GetInstanceOfType<T>(state.AssemblyQualifiedName);
        } else
        {
            element = (T)InstantiaterService.CreateInstance(type,true);
        }
        element.CurrentDirectory = state.FolderLocation + "/" + state.FileName + "/";
        element.RestoreInternalAsync(state, restoreDebug);
        return element;
    }

    public static RestoreAble Restore(RestoreState state, Type type, bool restoreDebug = false)
    {
        var element = (RestoreAble)InstantiaterService.CreateInstance(type, true);
        element.CurrentDirectory = state.FolderLocation + "/" + state.FileName + "/";

        element.RestoreInternalAsync(state, restoreDebug);
        return element;
    }

    internal virtual void SaveToFile(string path, IPersister persister, int index = -1, string className = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        var state = GetState();
        // state.FolderLocation = path;
        // FileName = className ?? GetFileName();
        path = path + "/" + FileName;
        state.Index = index;

        InternalSaveToFile(path, persister, state);
    }


    protected abstract void InternalSaveToFile(string path, IPersister persister, RestoreState state);

    internal abstract RestoreState GetState();
}

public abstract class RestoreState
{
    public string FileName;
    public string DerivedTypeString;
    public string FolderLocation;
    public Type DerivedType;
    public int Index;
    public string AssemblyName;
    public string AssemblyQualifiedName;

    public RestoreState()
    {
    }

    public RestoreState(RestoreAble o)
    {
        FileName = o.FileName;
        DerivedType = o.DerivedType;
        DerivedTypeString = o.DerivedType.ToString();
        AssemblyName = o.DerivedType.Assembly.FullName;
        AssemblyQualifiedName = o.DerivedType.AssemblyQualifiedName;
    }

    public Type OriginalType => Type.GetType(AssemblyQualifiedName);
}
