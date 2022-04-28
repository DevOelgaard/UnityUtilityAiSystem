﻿using System;
using System.Collections.Generic;
using UnityEngine;


// https://stackoverflow.com/questions/412019/math-optimization-in-c-sharp
// https://en.wikipedia.org/wiki/Logistic_function
public class LogisticFunction : ResponseFunction
{
    public LogisticFunction() : base(TypeToName.RF_Logistic)
    {
    }

    protected override List<Parameter> GetParameters()
    {
        return new List<Parameter>()
        {
            new Parameter("Growth Rate", 10f),
            new Parameter("Mid Point", 0.5f),
        };
    }

    protected override float CalculateResponseInternal(float x)
    {
        // L / 1 + e^-k(x-x0)
        return Convert.ToSingle(Max.Value) / (1.0f + Mathf.Exp(-Convert.ToSingle(GetParameter("Growth Rate").Value) * 
                                                                      (x - Convert.ToSingle(GetParameter("Mid Point").Value))));
    }
}
