using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using SiteParser;
using SiteParser.Implementations;
using SiteParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SiteParser.Tools;


namespace LMParser
{
    public class LMParser : SiteParser<LMProduct>
    {
        private readonly Lazy<ChromeDriver> _webDriver;
        private readonly Lazy<HttpClient> _httpClient;

        private readonly string _siteUrl;

        public LMParser(string siteUrl, IResultSaver<LMProduct> resultSaver) : base(resultSaver)
        {
            if (string.IsNullOrEmpty(siteUrl))
                throw new ArgumentNullException(nameof(siteUrl));

            _siteUrl = siteUrl;
            _webDriver = new Lazy<ChromeDriver>(CreateChromeDriver);
            _httpClient = new Lazy<HttpClient>(CreateHttpClient);
        }

        protected ChromeDriver WebDriver => _webDriver.Value;
        protected HttpClient HttpClient => _httpClient.Value;

        protected override string GetNextListUrl(string listContent)
        {
            var listHtml = GetHtmlDoc(listContent);

            var node = listHtml.DocumentNode.QuerySelector(".bex6mjh_plp.s15wh9uj_plp.l7pdtbg_plp.r1yi03lb_plp.sj1tk7s_plp");
            return node != null ? _siteUrl + node.GetAttributeValue<string>("href", null) : null;
        }

        protected override IEnumerable<string> GetItemsUrl(string listContent)
        {
            var listHtml = GetHtmlDoc(listContent);

            var nodes = listHtml.DocumentNode
                .QuerySelectorAll(".bex6mjh_plp.b1f5t594_plp.p5y548z_plp.pblwt5z_plp.nf842wf_plp")
                .ToArray();
            
            foreach (var node in nodes)
            {
                var itemUrl = node.GetAttributeValue<string>("href", null);
                if (string.IsNullOrEmpty(itemUrl)) continue;
                
                var fullUrl = _siteUrl + itemUrl;
                yield return fullUrl;
            }
        }

        protected override Task<string> GetListContent(string listUrl)
            => Task.FromResult(GetWebDriverHtml(listUrl));

        protected override Task<string> GetItemContent(string itemUrl)
            => Task.FromResult(GetWebDriverHtml(itemUrl));

        protected override Task<LMProduct> GetParsingResult(string itemContent)
        {
            var product = new LMProduct();
            var itemHtml = GetHtmlDoc(itemContent);

            product.Name = itemHtml.DocumentNode
                .QuerySelector("h1.header-2")
                ?.InnerText
                ?.NormalizeInnerText();

            var colorTitleNode = itemHtml.DocumentNode.QuerySelectorAll("dt.def-list__term").FirstOrDefault(node => node.InnerText.Contains("Цвет"));
            if (colorTitleNode != null)
                product.Color = colorTitleNode.ParentNode.QuerySelector("dd")
                    ?.InnerText
                    ?.NormalizeInnerText();

            var pictureNode = itemHtml.DocumentNode.QuerySelector("[id='picture-box-id-generated-0']").QuerySelector("source");
            product.Image = pictureNode.GetAttributeValue<string>("srcset", null);

            return Task.FromResult(product);
        }

        private HtmlDocument GetHtmlDoc(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                throw new ArgumentNullException(nameof(htmlContent));

            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            return document;
        }

        private string GetWebDriverHtml(string url)
        {
            WebDriver.Navigate().GoToUrl(url);
            return WebDriver.PageSource;
        }

        private ChromeDriver CreateChromeDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("headless");

            return new ChromeDriver(options);
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient {Timeout = TimeSpan.FromSeconds(10)};
            return client;
        }
    }

    [ParseResult]
    public class LMProduct
    {
        [ParseFieldResult("Название")]
        public string Name { get; set; }

        [ParseFieldResult("Цвет")]
        public string Color { get; set; }

        [ParseFieldResult("Изображение")]
        public string Image { get; set; }
    }
}
