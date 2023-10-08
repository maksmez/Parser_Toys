
using AngleSharp;
using AngleSharp.Dom;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Reflection.Metadata;
using AngleSharp.Text;
using System.Diagnostics;
using AngleSharp.Io;
using static System.Formats.Asn1.AsnWriter;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;



string url = "https://www.toy.ru/catalog/boy_transport/";



var config = Configuration.Default.WithDefaultLoader();
var context = BrowsingContext.New(config);
Dictionary<string, string> cookie = new Dictionary<string, string>();
cookie.Add("Санкт-Петербург", "PHPSESSID=tqi2hi1prfik76ufb846ug41o5; ipp_uid=1696791033122/1o8xvJVhiI7VX97M/3jTYqcKCWUUQnQKtgfQBxA==; rerf=AAAAAGUi+fmb0PJ5A2esAg==");
cookie.Add("Ростов", "PHPSESSID=tqi2hi1prfik76ufb846ug41o5; ipp_uid=1696791033122/1o8xvJVhiI7VX97M/3jTYqcKCWUUQnQKtgfQBxA==; rerf=AAAAAGUi+fmb0PJ5A2esAg==; BITRIX_SM_country=; BITRIX_SM_country_zip=; BITRIX_SM_country_name=; BITRIX_SM_country_city=; BITRIX_SM_country_fias=; BITRIX_SM_city=61000001000");





async Task<IDocument> OpenSite(string url, string cookie = "")
{
    HttpClient hc = new HttpClient();
    hc.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/4.0 (Windows NT 10.0; Win32; x32; rv:109.0) Gecko/20100100 Firefox/117.0");
    hc.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie);

    var response = await hc.GetAsync(url);
    var html = await response.Content.ReadAsStringAsync();
    var parser = new HtmlParser();
    IHtmlDocument document = parser.ParseDocument(html);

    if (document.Title == "403 Forbidden")
    {
        Console.WriteLine("Ошибка при загрузке страницы!");
        Console.WriteLine("Ошибка: " + document.Title);
        Process.GetCurrentProcess().Kill();
    }
    return document;
}

 void NewParse(IDocument document_old, string cookie)
{
    var store = document_old.QuerySelectorAll("div.card-preview a.img-block.gtm-click");
    int num = 1;
    int count = 0;
    string text = "";
    Console.WriteLine($"{Thread.CurrentThread.Name}" + " обрабатывается");
    foreach (var item in store)
    {
        num++;
        string newurl = "https://www.toy.ru" + item.GetAttribute("href");
        try
        {

            var result = OpenSite(newurl, cookie);
            IDocument document = result.Result;
            if (document.Title == "403 Forbidden")
            {
                Console.WriteLine("Доступ к странице запрещен!\r\n" + document.Title + "\r\n" + newurl + "\r\n");
                continue;
            }
            string city = document.QuerySelector(".city-selection > span").TextContent.Trim();
            string name = document.QuerySelector("h1").TextContent;
            string itemurl = document.Url;
            var bread = document.QuerySelectorAll(".breadcrumb-item");
            string breads = "";

            foreach (var item_one in bread)
            {
                if (!item.IsLastChild())
                {
                    breads += item_one.TextContent + "/";
                }
                else
                {
                    breads += item_one.TextContent;
                }
            }

            string images = "";
            var img = document.QuerySelectorAll("div.swiper-product-thumbs div.swiper-wrapper img");

            foreach (var item_two in img)
            {
                images += item_two.GetAttribute("src") + ";";
            }
            string price = "";
            
            if (document.QuerySelector(".not-in-stock-text") != null)
            {
                string not_stock = document.QuerySelector(".not-in-stock-text").TextContent.Trim();
                text += city + ";" + breads + ";" + name  + ";" + not_stock + ";" + images + ";" + newurl + ";" + "\r\n";

            }
            else
            {
                price = document.QuerySelector("[itemprop='price']").GetAttribute("content");
                text += city + ";" + breads + ";" + name + ";" + price + ";" + images + ";" + newurl + ";" + "\r\n";
            }

            
            count++;

        }
        catch (Exception ex)
        {
            Console.WriteLine("Не удается считать данные со страницы!\r\n" + newurl + "\r\n");
            Console.WriteLine(ex);

        }
    }
    Console.WriteLine("\r\n" + $"{Thread.CurrentThread.Name}" + " обработана" + "\r\nКарточек на странице: " + Convert.ToString(num-1) + "\r\nОбработано карточек: " + count+ "\r\n");
    using (StreamWriter writer = new StreamWriter("Result.csv", true, System.Text.Encoding.UTF8))
    {
        writer.WriteLine(text);
    }
}

void Start(int pagination, string url, string cookie = "")
{
    int num = 0;
    string pagging_url = "";
    if (pagination != 0)
    {
        for (int i = 1; i < pagination + 1; i++)
        {
            num++;
            pagging_url = "";
            pagging_url = url + "?PAGEN_1=" + i;
            var document = OpenSite(pagging_url, cookie).Result;

            Thread myNewThread = new Thread(() => NewParse(document, cookie));
            myNewThread.Name = "Страница номер " + Convert.ToString(num);
            myNewThread.Start();
        }
    }
    else
    {
        var document = OpenSite(url, cookie).Result;
        Thread myNewThread = new Thread(() => NewParse(document, cookie));
        myNewThread.Name = "Страница";
        myNewThread.Start();
    }

}


void CheckPagination(string cookie)
{
    var document = OpenSite(url, cookie).Result;
    int pagination = 0;

    if (document.QuerySelector("nav ul.pagination li:nth-last-child(2)") == null)
    {
        Start(pagination, url, cookie);
    }
    else
    {
        pagination = int.Parse(document.QuerySelector("nav ul.pagination li:nth-last-child(2)").TextContent);
        Start(pagination, url, cookie);
    }

}
Console.WriteLine("Запуск программы");
Console.WriteLine("Выберите город\r\n1 - Санкт-Петербург\r\n2 - Ростов-на-Дону\r\n");
string choise = Console.ReadLine();
switch (choise)
{
    case "1":
    CheckPagination(cookie["Санкт-Петербург"]);
        break;
    case "2":
    CheckPagination(cookie["Ростов"]);
        break;
    default:
        Console.WriteLine("Ошибка! Попробуйте снова");
        break;

}
