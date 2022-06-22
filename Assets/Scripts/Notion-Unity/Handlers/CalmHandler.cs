using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;

namespace Notion.Unity
{
    public class CalmHandler : IMetricHandler
    {
        public Metrics Metric => Metrics.Awareness;
        public string Label => "calm";

        public Action<float> OnCalmUpdated { get; set; }

        public void Handle(string json)
        {
            BaseMetric metric = JsonConvert.DeserializeObject<BaseMetric>(json);
            //Debug.Log($"Handling {metric.Label} : {metric.Probability}");
            OnCalmUpdated?.Invoke(metric.Probability);
        }
    }
}