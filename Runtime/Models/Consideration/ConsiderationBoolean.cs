﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Use this to check true/false stuff like "Do i have a weapon"
/// CalculateBaseScore should return <=0 for false otherwise true
/// The score from this does not count towards the decision/buckets Score
/// </summary>
internal abstract class ConsiderationBoolean : Consideration
{
    public ConsiderationBoolean(): base()
    {
        IsScorer = false;
    }
}