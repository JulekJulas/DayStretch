using DayStretch;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayStretch
{
    public class StatPart_DayStretchMultiplier : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val /= Settings.Instance.TimeMultiplier;
        }

        public override string ExplanationPart(StatRequest req)
        {
            return $"DayStretch multiplier: /{Settings.Instance.TimeMultiplier}";
        }
    }

    public class StatPart_ReverseDayStretchMultiplier : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val *= Settings.Instance.TimeMultiplier;
        }

        public override string ExplanationPart(StatRequest req)
        {
            return $"DayStretch multiplier: x{Settings.Instance.TimeMultiplier}";
        }
    }

    public class StatPart_WorkDayStretchMultiplier : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val /= Settings.Instance.WorkMultiplier;
        }

        public override string ExplanationPart(StatRequest req)
        {
            return $"DayStretch multiplier: /{Settings.Instance.WorkMultiplier}";
        }
    }
}

