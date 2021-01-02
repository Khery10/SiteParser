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
            HtmlDocument listHtml = GetHtmlDoc(listContent);

            HtmlNode node = listHtml.DocumentNode.QuerySelector(".next-paginator-button");
            return node != null ? _siteUrl + node.GetAttributeValue<string>("href", null) : null;
        }

        protected override IEnumerable<string> GetItemsUrl(string listContent)
        {
            HtmlDocument listHtml = GetHtmlDoc(listContent);

            var nodes = listHtml.DocumentNode.QuerySelectorAll(".plp-item__info__title");
            foreach (HtmlNode node in nodes)
            {
                string itemUrl = node.GetAttributeValue<string>("href", null);
                if (!string.IsNullOrEmpty(itemUrl))
                {
                    string fullUrl = _siteUrl + itemUrl;
                    yield return fullUrl;
                }
            }
        }

        protected override async Task<string> GetListContent(string listUrl)
            => await HttpClient.GetStringAsync(listUrl);

        protected override Task<string> GetItemContent(string itemUrl)
            => Task.FromResult(GetWebDriverHtml(itemUrl));

        protected override Task<LMProduct> GetParsingResult(string itemContent)
        {
            LMProduct product = new LMProduct();
            HtmlDocument itemHtml = GetHtmlDoc(itemContent);

            product.Name = itemHtml.DocumentNode.QuerySelector("h1.header-2")?.InnerText;

            HtmlNode colorTitleNode = itemHtml.DocumentNode.QuerySelectorAll("dt.def-list__term").FirstOrDefault(node => node.InnerText.Contains("Цвет"));
            if (colorTitleNode != null)
                product.Color = colorTitleNode.ParentNode.QuerySelector("dd")?.InnerText;

            HtmlNode pictureNode = itemHtml.DocumentNode.QuerySelector("[id='picture-box-id-generated-0']").QuerySelector("source");
            product.Image = pictureNode.GetAttributeValue<string>("srcset", null);

            return Task.FromResult(product);
        }

        private HtmlDocument GetHtmlDoc(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                throw new ArgumentNullException(nameof(htmlContent));

            HtmlDocument document = new HtmlDocument();
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
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("headless");

            return new ChromeDriver(options);
        }

        private HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            };

            HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);

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
