using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SiteParser.Interfaces
{
    public interface IResultSaver<TResult> where TResult : class
    {
        Task WriteResultAsync(TResult result);

        Task SaveResultsAsync();

    }
}
