using SiteParser.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SiteParser.Implementations
{
    public class CsvResultSaver<TResult> : IResultSaver<TResult> where TResult : class
    {
        private readonly string _filePath;
        private readonly ConcurrentQueue<TResult> _results = new ConcurrentQueue<TResult>();
        private readonly Type _resultType;

        public CsvResultSaver(string filePath)
        {
            _resultType = typeof(TResult);
            if (_resultType.GetCustomAttribute(typeof(ParseResultAttribute)) == null)
                throw new Exception($"Тип {_resultType.Name} не помечен атрибутом {nameof(ParseResultAttribute)}");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            _filePath = filePath;
        }

        public Task WriteResultAsync(TResult result)
        {
            _results.Enqueue(result);
            return Task.CompletedTask;
        }

        public async Task SaveResultsAsync()
        {
            var isNew = !File.Exists(_filePath);
            using (var fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    var fields = GetFields();

                    if (isNew)
                        await streamWriter.WriteLineAsync(GetHeaderString(fields));

                    while (_results.TryDequeue(out var result))
                    {
                        var resultString = GetResultString(result, fields);
                        await streamWriter.WriteLineAsync(resultString);
                    }
                }
            }
        }

        private ReadOnlyCollection<Tuple<PropertyInfo, string>> GetFields()
        {
            return _resultType
                 .GetProperties()
                 .Select(prop =>
                 {
                     var attribute = prop.GetCustomAttribute<ParseFieldResultAttribute>();

                     return attribute != null
                     ? new Tuple<PropertyInfo, string>(prop, attribute.Name)
                     : null;
                 })
                 .Where(el => el != null)
                 .ToList()
                 .AsReadOnly();
        }

        private string GetHeaderString(ReadOnlyCollection<Tuple<PropertyInfo, string>> fields)
            => string.Join(";", fields.Select(field => field.Item2));

        private string GetResultString(TResult result, ReadOnlyCollection<Tuple<PropertyInfo, string>> fields) 
            => string.Join(";", fields.Select(field => field.Item1.GetValue(result)?.ToString() ?? ""));
    }


    public class ParseResultAttribute : Attribute { }

    public class ParseFieldResultAttribute : Attribute
    {
        public ParseFieldResultAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public string Name { get;}
    }

}
