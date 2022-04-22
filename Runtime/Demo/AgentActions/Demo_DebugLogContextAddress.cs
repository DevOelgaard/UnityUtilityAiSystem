﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

internal class Demo_DebugLogContextAddress : AgentAction
{
    public Demo_DebugLogContextAddress() : base()
    {
    }

    protected override List<Parameter> GetParameters()
    {
        return new List<Parameter>()
        {
        };
    }

    public override void OnGoing(AiContext context)
    {
        DebugService.Log(GetContextAddress(context),this);
    }
}