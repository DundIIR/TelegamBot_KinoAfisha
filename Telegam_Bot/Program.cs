using HtmlAgilityPack;
using System;
using System.Net;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Telegram.Bot.Types.InputFiles;
using static System.Net.WebRequestMethods;
using static System.Net.Mime.MediaTypeNames;

namespace Telegram_Bot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Dictionary<string, Dictionary<string, string>> result = Parsing(url: "https://surkino.ru");
            Dictionary<int, string> callBackId = new Dictionary<int, string>();
            Dictionary<long, Dictionary<string, int>> reg = new Dictionary<long, Dictionary<string, int>>();
            Dictionary<long, List<int>> menu = new Dictionary<long, List<int>>(); 
            Dictionary<long, int> messageIdBot = new Dictionary<long, int>();
            
            var botClient = new TelegramBotClient("6237482814:AAFFX12o_3A3cc5F6yuNOt6Ce9Ot2Tol6Qo");

            using CancellationTokenSource cts = new();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();

            Console.ReadLine();

            cts.Cancel();

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                
                switch (update.Type)
                {
                    case UpdateType.Message:

                        if (update.Message is not { } message)
                            return;
                        string text;

                        var chatId = message.Chat.Id;

                        List<int> numbers = new List<int>();
                        //if (!messageIdBot.TryGetValue(chatId, out int value))
                        //    messageIdBot.Add(chatId, 0);

                        switch (message.Text.ToLower())
                        {
                            case "/start":
                            case "/help":
                                try
                                {
                                    await botClient.DeleteMessageAsync(
                                    chatId: chatId,
                                    messageId: message.MessageId,
                                    cancellationToken: cancellationToken);
                                }
                                catch (Exception ex) { Console.WriteLine(ex.Message); }

                                var messageId = await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Привет, я помогу тебе узнать какие фильмы в прокате сейчас есть, просто нажми Смотреть или напиши это в чат",
                                replyMarkup: GetButtons("main"),
                                cancellationToken: cancellationToken);
                                //if (messageIdBot.ContainsKey(chatId))
                                //    messageIdBot[chatId] = messageId.MessageId;
                                break;
                            case "смотреть😃":
                            case "смотреть":
                            case "/kino":
                                if (!menu.ContainsKey(chatId))
                                {
                                    menu.Add(chatId, numbers);
                                }
                                if (result != null)
                                {
                                    numbers = new List<int>();
                                    numbers.Add(message.MessageId);
                                    if (menu[chatId].Count == 0)
                                    {

                                        foreach (var item in result.Reverse().Skip(result.Count - 10))
                                        {
                                            text = null;
                                            if (item.Value["country"].ToLower() == "россия")
                                            {
                                                text = "💳Пушкин💳";
                                            }
                                            
                                            var m = await botClient.SendPhotoAsync(
                                            chatId: chatId,
                                            photo: item.Value["image"],
                                            caption: "<b>" + item.Key.ToUpper() + "</b>" + "\n\n" + item.Value["genre"] + "\n\n" + text,
                                            parseMode: ParseMode.Html,
                                            replyMarkup: GetInlineButtons(item.Value["buyTicket"], item.Key, "card"),
                                            cancellationToken: cancellationToken);

                                            numbers.Add(m.MessageId);
                                            
                                        }
                                        
                                        var m1 = await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Топ 10 фильмов сегодня",
                                        replyMarkup: GetButtons("still"),
                                        cancellationToken: cancellationToken);
                                        numbers.Add(m1.MessageId);
                                        menu[chatId] = numbers;
                                    }
                                    else
                                    {
                                        await botClient.DeleteMessageAsync(chatId, message.MessageId);
                                        await botClient.SendTextMessageAsync(chatId, "Чтобы обновить запрос, нужно сначла скрыть", replyMarkup: GetButtons("still"), cancellationToken: cancellationToken);
                                    }
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "На данную дату еще нет сеансов!",
                                    cancellationToken: cancellationToken);
                                }
                                break;
                            case "скрыть":
                                if (menu.ContainsKey(chatId))
                                {
                                    foreach (var itemId in menu[chatId])
                                    {
                                        try
                                        {
                                            await botClient.DeleteMessageAsync(
                                            chatId: chatId,
                                            messageId: itemId,
                                            cancellationToken: cancellationToken);
                                        }
                                        catch (Exception ex) { Console.WriteLine(ex.Message); }

                                    }
                                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                                }
                                await botClient.SendTextMessageAsync(chatId, "Готово!", replyMarkup: GetButtons("main"), cancellationToken: cancellationToken);
                                menu[chatId] = new List<int>();
                                break;
                            case "другие😃":
                            case "другие":
                                if (menu.ContainsKey(chatId))
                                {
                                    foreach (var itemId in menu[chatId])
                                    {

                                        try
                                        {
                                            await botClient.DeleteMessageAsync(
                                            chatId: chatId,
                                            messageId: itemId,
                                            cancellationToken: cancellationToken);
                                        }
                                        catch (Exception ex) { Console.WriteLine(ex.Message); }

                                    }
                                    await botClient.DeleteMessageAsync(chatId, message.MessageId);

                                }
                                menu[chatId] = new List<int>();
                                if (!menu.ContainsKey(chatId))
                                {
                                    menu.Add(chatId, numbers);
                                }
                                if (result != null)
                                {
                                    numbers = new List<int>();
                                    numbers.Add(message.MessageId);
                                    if (menu[chatId].Count == 0)
                                    {

                                        foreach (var item in result.Skip(10).Reverse())
                                        {
                                            text = null;
                                            if (item.Value["country"].ToLower() == "россия")
                                            {
                                                text = "💳Пушкин💳";
                                            }

                                            var m = await botClient.SendPhotoAsync(
                                            chatId: chatId,
                                            photo: item.Value["image"],
                                            caption: "<b>" + item.Key.ToUpper() + "</b>" + "\n\n" + item.Value["genre"] + "\n\n" + text,
                                            parseMode: ParseMode.Html,
                                            replyMarkup: GetInlineButtons(item.Value["buyTicket"], item.Key, "card"),
                                            cancellationToken: cancellationToken);

                                            numbers.Add(m.MessageId);
                                            
                                        }
                                        
                                        var m1 = await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Другие",
                                        replyMarkup: GetButtons("still"),
                                        cancellationToken: cancellationToken);
                                        numbers.Add(m1.MessageId);
                                        menu[chatId] = numbers;
                                    }
                                    else
                                    {
                                        await botClient.DeleteMessageAsync(chatId, message.MessageId);
                                        await botClient.SendTextMessageAsync(chatId, "Чтобы обновить запрос, нужно сначла скрыть", replyMarkup: GetButtons("still"), cancellationToken: cancellationToken);
                                    }
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "На данную дату еще нет сеансов!",
                                    cancellationToken: cancellationToken);
                                }
                                break;

                        }
                        break;
                    case UpdateType.CallbackQuery:
                        var callBack = update.CallbackQuery.Data;
                        if (callBack.Contains("back"))
                        {
                            break;
                        }
                        if (callBack.Contains("buy"))
                        {
                            break;
                        }
                        if (callBack.Length <= 15)
                        {
                            foreach(var item in result.Keys)
                            {
                                if (item.Contains(callBack))
                                {
                                    callBack = item;
                                    break;
                                }
                            }
                        }
                        if (result.ContainsKey(callBack))
                        {
                            var itemCallBack = result[callBack];
                            text = null;
                            if (itemCallBack["country"].ToLower() == "россия")
                            {
                                text = "💳Пушкин💳\n\n";
                            }
                            await botClient.SendTextMessageAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            text: "<b>" + callBack.ToUpper() + "</b>" + "\n\n" + itemCallBack["genre"] + "\n\n" + itemCallBack["description"] + "\n\n" + text + itemCallBack["trailer"] ,
                            parseMode: ParseMode.Html,
                            replyMarkup: GetInlineButtons(null, null, "description"),
                            cancellationToken: cancellationToken);
                        }

                        break;       
                }
            }
            
            
            Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }
        }


        private static void youtubeParser()
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyAihUgaFl8s8BvydfILTnT4zkptpGpv_uc",
                ApplicationName = "YoutubeParser"
            });

            // Получение ID видео из ссылки
            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var videoId = videoUrl.Split("v=")[1];

            try
            {
                // Парсинг видео по ID
                var videoRequest = youtubeService.Videos.List("snippet");
                videoRequest.Id = videoId;
                var videoResponse = videoRequest.Execute();

                // Вывод информации о видео
                //var video = videoResponse.Items[0];
                //var title = video.Snippet.Title;
                //var description = video.Snippet.Description;
                //Console.WriteLine($"Название: {title}");
                //Console.WriteLine($"Описание: {description}");
            }
            catch (Exception ex)
            {
                // Вывод сообщения об ошибке
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }


        private static IReplyMarkup GetButtons(string key)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = null;
            switch (key) 
            {
                case "main":
                    replyKeyboardMarkup = new(new[]
                    {
                        new KeyboardButton[] { "СМОТРЕТЬ😃" },
                        new KeyboardButton[] { "жанр", "лучшее" },
                    })
                    {
                        ResizeKeyboard = true
                    };
                    break;
                case "still":
                    replyKeyboardMarkup = new(new[]
                    {
                        new KeyboardButton[] { "ДРУГИЕ😃" },
                        new KeyboardButton[] { "скрыть" },
                    })
                    {
                        ResizeKeyboard = true
                    };
                    break;
            }

            return replyKeyboardMarkup;
        }
        private static IReplyMarkup? GetInlineButtons(string buyLink, string id, string key)
        {
            InlineKeyboardMarkup inlineKeyboardMarkup = null;
            if (id != null && id.Length > 15)
            {
                id = id.Substring(0, 15);
            }
            switch (key) 
            {
                case "card":
                    if(buyLink != null)
                    {
                        inlineKeyboardMarkup =  new InlineKeyboardMarkup(
                            new[]
                            {
                                    InlineKeyboardButton.WithUrl(text: "купить", url: buyLink),
                                    InlineKeyboardButton.WithCallbackData("подробнее", id),
                            });
                    }
                    else
                    {
                        inlineKeyboardMarkup = new InlineKeyboardMarkup(
                            new[]
                            {
                                    InlineKeyboardButton.WithCallbackData("купить", "buy" + id),
                                    InlineKeyboardButton.WithCallbackData("подробнее", id),
                            });
                    }
                    break;
                case "description":
                    inlineKeyboardMarkup = new InlineKeyboardMarkup( InlineKeyboardButton.WithCallbackData("назад", "back" + id));
                    break;
            }
            
            return inlineKeyboardMarkup;
        }

        private static Dictionary<string, Dictionary<string, string>> Parsing(string url)
        {
            try
            {
                var result = new Dictionary<string, Dictionary<string, string>>();
                using (var client = new WebClient())
                {

                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    var web = new HtmlWeb();
                    web.AutoDetectEncoding = true;
                    web.OverrideEncoding = Encoding.GetEncoding("Windows-1251");
                    var doc = web.Load(url);


                    var cardsFilm = doc.DocumentNode.SelectNodes(".//div[@id='allfilm']/div[@class='film']");
                    if (cardsFilm != null && cardsFilm.Count > 0)
                    {
                        foreach (var card in cardsFilm)
                        {
                            var titleCard = card.SelectSingleNode(".//div[@class='filmname']//a").InnerText;
                            {
                                string img = card.SelectSingleNode(".//div[@class='poster']//a//img").OuterHtml;
                                int startIndex = img.IndexOf("src=") + 5;
                                int endIndex = img.IndexOf("\" ", startIndex);
                                img = img.Substring(startIndex, endIndex - startIndex);
                                var imgCard = "https://surkino.ru/" + img;
                                if (imgCard != null)
                                {
                                    string descriptionCard = card.SelectSingleNode(".//div[@class='descr']").InnerHtml;
                                    string country = card.SelectSingleNode(".//div[@class='descr']//div[@class='filmsect']").InnerHtml;
                                    if (descriptionCard != null && country != null)
                                    {
                                        var genreCard = descriptionCard.Substring(0, descriptionCard.IndexOf('<'));
                                        startIndex = descriptionCard.IndexOf("<hr>") + 4;
                                        endIndex = descriptionCard.IndexOf("<", startIndex);
                                        string descriptionCardFilm = descriptionCard.Substring(startIndex, endIndex - startIndex).Replace("\n", "").Replace("\t", "");

                                        startIndex = country.IndexOf("<br>") + 4;
                                        endIndex = country.IndexOf("<", startIndex);
                                        string countryFilm = country.Substring(startIndex, endIndex - startIndex);

                                        var BuyTicket = card.SelectSingleNode(".//div[@class='descr']/div[@class='kassa-cont']/a");
                                        string linkBuyTicket = null;
                                        if (BuyTicket != null)
                                        {
                                            string jsCodeBuyTicket = BuyTicket.Attributes["href"].Value;
                                            startIndex = jsCodeBuyTicket.IndexOf(", ") + 2;
                                            endIndex = jsCodeBuyTicket.IndexOf(",", startIndex);
                                            linkBuyTicket = "https://www.afisha.ru/w/scheduleforsubject/movie/" + jsCodeBuyTicket.Substring(startIndex, endIndex - startIndex) + "/2565/cb205986-ddde-450d-be1a-54a9584b819a/https%253A%252F%252Fsurkino.ru%252F?apiUrl=https%3A%2F%2Fmapi.kassa.rambler.ru%2Fapi%2Fv21%2F";
                                        }
                                        //if (linkBuyTicket != null)
                                        //{
                                        var trailerFilm = card.SelectSingleNode(".//div[@class='botredlink']//a").Attributes["href"].Value;
                                        if (trailerFilm != null)
                                        {
                                            var res = new Dictionary<string, string>
                                                {
                                                    {"image", imgCard},
                                                    {"genre", genreCard},
                                                    {"description", descriptionCardFilm},
                                                    {"buyTicket", linkBuyTicket},
                                                    {"trailer", trailerFilm},
                                                    {"country", countryFilm}
                                                };
                                            result.Add(titleCard, res);

                                        }

                                        //}

                                    }
                                }
                            }
                        }

                        return result;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return null;
        }
        

    }
}

