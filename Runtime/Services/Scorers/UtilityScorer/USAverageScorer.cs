﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class USAverageScorer : IUtilityScorer
{
    private string name = Consts.Name_USAverageScore;
    private string description = Consts.Description_USAverageScore;
    public float CalculateUtility(List<Consideration> considerations, AiContext context)
    {
        if (considerations.Count == 0)
            return 0;

        var sum = 0f;
        var amountOfScorers = 0;
        var modifier = 1f;
        foreach (var consideration in considerations)
        {
            var score = consideration.CalculateScore(context);
            
            // If any consideration fails, the decision/bucket can't be executed
            if (score <= 0)
            {
                return score;
            }
            if (consideration.IsScorer)
            {
                amountOfScorers++;
                sum += score;
            }
            if (consideration.IsModifier)
            {
                if (score > modifier)
                {
                    modifier = score;
                }
            }
        }
        if(amountOfScorers <= 0) // Only ConsiderationsBools have been calculated. If they failed they would have returned false
        {
            return 1;
        } else
        {
            return (sum / amountOfScorers) * modifier;
        }
    }

    public string GetDescription() => description;

    public string GetName() => name;
}
