﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LinearFunction : ResponseFunction
{
    public LinearFunction() : base(TypeToName.RF_Linear)
    {
    }

    protected override List<Parameter> GetParameters()
    {
        return new List<Parameter>()
        {
            new Parameter("a",1f),
            new Parameter("b",0f)
        };
    }

    protected override float CalculateResponseInternal(float x)
    {
        return (float) GetParameter("a").Value * x + (float) GetParameter("b").Value;
    }
}
