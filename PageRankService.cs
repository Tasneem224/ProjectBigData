using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectBigData.Services
{
    public class PageRankService
    {
        private readonly string _connectionString;
        private readonly string _filePath;

        public PageRankService(string connectionString, string filePath)
        {
            _connectionString = connectionString;
            _filePath = filePath;
        }

        public void CalculateAndStorePageRank()
        {
            // 1. قراءة الملف
            string[] lines = File.ReadAllLines(_filePath);
            Dictionary<string, List<(string url, int freq)>> wordToDocs = new();

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                var word = parts[0].Trim();

                var docFreqs = parts.Skip(1)
                    .Select(p =>
                    {
                        var split = p.Trim().Split(' ');
                        return (url: split[0], freq: int.Parse(split[1]));
                    })
                    .ToList();

                wordToDocs[word] = docFreqs;
            }

            // 2. بناء الجراف
            Dictionary<string, HashSet<string>> links = new();

            foreach (var pair in wordToDocs.Values)
            {
                foreach (var (docA, _) in pair)
                {
                    if (!links.ContainsKey(docA))
                        links[docA] = new HashSet<string>();

                    foreach (var (docB, _) in pair)
                    {
                        if (docA != docB)
                            links[docA].Add(docB);
                    }
                }
            }

            // 3. حساب PageRank
            Dictionary<string, double> pageRanks = links.Keys.ToDictionary(doc => doc, doc => 1.0);
            const double damping = 0.85;
            const int iterations = 20;

            for (int i = 0; i < iterations; i++)
            {
                var newRanks = new Dictionary<string, double>();

                foreach (var doc in links.Keys)
                {
                    double rank = (1 - damping);
                    foreach (var otherDoc in links.Keys)
                    {
                        if (links[otherDoc].Contains(doc))
                        {
                            rank += damping * (pageRanks[otherDoc] / links[otherDoc].Count);
                        }
                    }
                    newRanks[doc] = rank;
                }

                pageRanks = newRanks;
            }

            // 4. إدخال النتائج في قاعدة البيانات
            using (SqlConnection conn = new(_connectionString))
            {
                conn.Open();

                foreach (var kvp in pageRanks)
                {
                    var cmd = new SqlCommand("INSERT INTO PageRanks (url, score) VALUES (@url, @score)", conn);
                    cmd.Parameters.AddWithValue("@url", kvp.Key);
                    cmd.Parameters.AddWithValue("@score", kvp.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}