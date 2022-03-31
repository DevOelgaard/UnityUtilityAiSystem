﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class AiContext
{
    public IAgent Agent;
    //internal AgentAction CurrentAction;
    private Dictionary<object, object> contextStringKey = new Dictionary<object, object>();
    //private Dictionary<AiContextKey, object> contextEnumKey = new Dictionary<AiContextKey, object>();
    public IUtilityScorer UtilityScorer = new USAverageScorer();
    public List<AgentAction> LastActions = new List<AgentAction>();
    public Decision LastSelectedDecision { get; internal set; }
    public Decision CurrentEvalutedDecision { get; internal set; }
    public Bucket LastSelectedBucket { get; internal set; }
    public Bucket CurrentEvaluatedBucket { get; internal set; }
    public TickMetaData TickMetaData { get; internal set; }

    public AiContext()
    {
    }

    /// <summary>
    /// This is slower but more versatile. Consider using GetContext(AiContextKey) instead.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T GetContext<T>(object key, UtilityContainer container = null)
    {
        if (container != null)
        {
            key = container.GetContextAddress(this) + key;
        }
        if (contextStringKey.ContainsKey(key))
        {
            return (T)contextStringKey[key];
        } else
        {
            return default(T);
        }
    }

    /// <summary>
    /// This is slower but more versatile. Consider using SetContext(AiContextKey) instead.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public void SetContext(object key, object value, UtilityContainer container = null)
    {
        if (container != null)
        {
            key = container.GetContextAddress(this) + key;
        }
        if (!contextStringKey.ContainsKey(key))
        {
            contextStringKey.Add(key, value);
        } else
        {
            contextStringKey[key] = value;
        }
    }

    /// <summary>
    /// This is slower but more versatile. Consider using RemoveContext(AiContextKey) instead.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public void RemoveContext(object key, UtilityContainer container = null)
    {
        if (container != null)
        {
            key = container.GetContextAddress(this) + key;
        }
        if (contextStringKey.ContainsKey(key))
        {
            contextStringKey.Remove(key);
        }
    }

    ///// <summary>
    ///// Use this if you have defined the needed Enum in AiContextKey otherwise use the string version
    ///// </summary>
    ///// <param name="key"></param>
    ///// <returns></returns>
    //public T GetContext<T>(AiContextKey key)
    //{
    //    if (contextEnumKey.ContainsKey(key))
    //    {
    //        return (T)contextEnumKey[key];
    //    }
    //    else
    //    {
    //        return default;
    //    }
    //}

    ///// <summary>
    ///// Use this if you have defined the needed Enum in AiContextKey otherwise use the string version
    ///// </summary>
    ///// <param name="key"></param>
    ///// <returns></returns>
    //public void SetContext(AiContextKey key, object value)
    //{
    //    if (!contextEnumKey.ContainsKey(key))
    //    {
    //        contextEnumKey.Add(key, value);
    //    }
    //    else
    //    {
    //        contextEnumKey[key] = value;
    //    }
    //}

    ///// <summary>
    ///// Use this if you have defined the needed Enum in AiContextKey otherwise use the string version
    ///// </summary>
    ///// <param name="key"></param>
    ///// <returns></returns>
    //public void RemoveContext(AiContextKey key)
    //{
    //    if (contextEnumKey.ContainsKey(key))
    //    {
    //        contextEnumKey.Remove(key);
    //    }
    //}
}
