using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient; // استخدام Microsoft.Data.SqlClient

namespace ProjectBigData.Services
{
    public class DataImportService
    {
        private readonly string _connectionString; private readonly string _filePath;
        public DataImportService(string connectionString, string filePath)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public (int insertedCount, List<string> errors) ImportData()
        {
            try
            {
                var records = ReadFileRecords();
                return InsertRecordsToDatabase(records);
            }
            catch (Exception ex)
            {
                return (0, new List<string> { $"حدث خطأ أثناء استيراد البيانات: {ex.Message}" });
            }
        }

        private List<(string pk, string fileCount)> ReadFileRecords()
        {
            var records = new List<(string pk, string fileCount)>();

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"الملف غير موجود: {_filePath}");
            }

            string[] lines = File.ReadAllLines(_filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var record = ParseLine(line, i + 1);
                if (record.HasValue)
                { 
                    records.Add(record.Value);
                }
            }

            return records;
        }

        private (string pk, string fileCount)? ParseLine(string line, int lineNumber)
        {
            string[] parts = line.Split(' ');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            {
                throw new FormatException($"تنسيق السطر {lineNumber} غير صحيح: {line}");
            }

            return (parts[0].Trim(), parts[1].Trim());
        }

        private (int insertedCount, List<string> errors) InsertRecordsToDatabase(List<(string pk, string fileCount)> records)
        {
            int insertedCount = 0;
            var errors = new List<string>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var record in records)
                {
                    try
                    {
                        if (InsertSingleRecord(connection, record.pk, record.fileCount))
                        {
                            insertedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"فشل إدخال السجل '{record.pk}': {ex.Message}");
                    }
                }

                connection.Close();
            }

            return (insertedCount, errors);
        }

        private bool InsertSingleRecord(SqlConnection connection, string pk, string fileCount)
        {
            const string query = "INSERT INTO world (pk, [file_count]) VALUES (@pk, @fileCount)";
             
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@pk", pk);
                command.Parameters.AddWithValue("@fileCount", fileCount);
                command.ExecuteNonQuery();
            }

            return true;
        }
    }


}
