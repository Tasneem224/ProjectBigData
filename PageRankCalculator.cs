using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBigData.Utilities
{
    public class PageRankCalculators
    {
        public static Dictionary<string, double> CalculatePageRank(Dictionary<string, List<string>> graph, double dampingFactor = 0.85, int maxIterations = 100, double epsilon = 1e-6)
        {
            // Get all unique nodes from graph keys and their links
            var allNodes = new HashSet<string>(graph.Keys);
            foreach (var node in graph.Keys)
            {
                foreach (var target in graph[node])
                {
                    allNodes.Add(target);
                }
            }

            int N = allNodes.Count;
            if (N == 0) return new Dictionary<string, double>();

            // Initialize PageRank for all nodes
            var pageRank = allNodes.ToDictionary(key => key, key => 1.0 / N);

            // Initialize transition dictionary for all nodes
            var transition = allNodes.ToDictionary(key => key, key => new Dictionary<string, double>());

            // Build transition probabilities
            foreach (var node in graph.Keys)
            {
                var validTargets = graph[node].Where(t => allNodes.Contains(t)).ToList();
                int outLinks = validTargets.Count;
                if (outLinks == 0) // Handle dangling nodes
                {
                    foreach (var otherNode in allNodes)
                    {
                        transition[otherNode][node] = 1.0 / N;
                    }
                }
                else
                {
                    foreach (var target in validTargets)
                    {
                        transition[target][node] = 1.0 / outLinks;
                    }
                }
            }

            // Power iteration to compute PageRank
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var newPageRank = new Dictionary<string, double>();
                double totalDiff = 0;

                foreach (var node in allNodes)
                {
                    double sum = 0;
                    foreach (var source in allNodes)
                    {
                        if (transition[node].ContainsKey(source))
                        {
                            sum += transition[node][source] * pageRank[source];
                        }
                    }
                    newPageRank[node] = (1 - dampingFactor) / N + dampingFactor * sum;
                    totalDiff += Math.Abs(newPageRank[node] - pageRank[node]);
                }

                pageRank = newPageRank;
                if (totalDiff < epsilon)
                {
                    break;
                }
            }

            return pageRank;
        }
    }
}