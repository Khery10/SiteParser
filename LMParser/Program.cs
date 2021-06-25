using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SiteParser.Implementations;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LMParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var resultSaver = new CsvResultSaver<LMProduct>("laminat.txt");

            var parser = new LMParser("https://leroymerlin.ru", resultSaver);
            parser.StartAsync("https://leroymerlin.ru/search/?sortby=8&page=1&tab=products&q=%D0%BB%D0%B0%D0%BC%D0%B8%D0%BD%D0%B0%D1%82").Wait();

            Console.WriteLine("End");
        }
    }
}
