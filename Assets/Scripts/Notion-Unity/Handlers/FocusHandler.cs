using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;

namespace Notion.Unity
{
    public class FocusHandler : IMetricHandler
    {
        public Metrics Metric => Metrics.Awareness;
        public string Label => "focus";

        public Action<float> OnFocusUpdated { get; set; } 

        public void Handle(string json)
        {
            BaseMetric metric = JsonConvert.DeserializeObject<BaseMetric>(json);
            //Debug.Log($"Handling {metric.Label} : {metric.Probability}");
            OnFocusUpdated?.Invoke(metric.Probability);
        }
    }
}