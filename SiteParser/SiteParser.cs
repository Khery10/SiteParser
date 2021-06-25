using SiteParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SiteParser.Tools;

namespace SiteParser
{
    public abstract class SiteParser<TResult> where TResult : class
    {
        private readonly IResultSaver<TResult> _resultSaver;

        public SiteParser(IResultSaver<TResult> resultSaver)
        {
            _resultSaver = resultSaver
                ?? throw new ArgumentNullException(nameof(resultSaver));
        }

        public async Task StartAsync(string startListUrl)
        {
            if (string.IsNullOrEmpty(startListUrl))
                throw new ArgumentNullException(nameof(startListUrl));

            var listContent = await ParseListAsync(startListUrl);

            // string listUrl;
            // while (!string.IsNullOrEmpty(listUrl = GetNextListUrl(listContent)))
            //     listContent = await ParseListAsync(listUrl);

            await _resultSaver.SaveResultsAsync();
        }

        private async Task<string> ParseListAsync(string listUrl)
        {
            var listContent = await GetListContent(listUrl);
            foreach (var itemUrl in GetItemsUrl(listContent))
            {
                var itemContent = await GetItemContent(itemUrl);
                Console.WriteLine(itemUrl);
                await ParseItemAsync(itemContent);
            }

            return listContent;
        }

        private async Task ParseItemAsync(string itemContent)
        {
            var result = await GetParsingResult(itemContent);

            if (result != null)
                await _resultSaver.WriteResultAsync(result);
        }

        protected abstract string GetNextListUrl(string listContent);
        protected abstract IEnumerable<string> GetItemsUrl(string listContent);

        protected abstract Task<string> GetListContent(string listUrl);
        protected abstract Task<string> GetItemContent(string itemUrl);
     
        protected abstract Task<TResult> GetParsingResult(string itemContent);       
    }
}
