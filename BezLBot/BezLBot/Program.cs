using Telegram.Bot;
using System;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Collections.Concurrent;
using System.Linq;

namespace BezLBot
{
    internal class Program
    {
        
        class Chat
        {
            public long ChatId { get; set; }
            public ChatState State { get; set; }
            public ChatType Type { get; set; }
            public string Code { get; set; }
        }

        private static ITelegramBotClient botClient;
        private static HttpClient httpClient = new HttpClient();

        private static readonly ConcurrentBag<Chat> _chats = [];

        private static readonly Dictionary<long, ChatState> chatStates = [];
        private static readonly Dictionary<long, ChatType> chatTypes = [];
        private static readonly Dictionary<long, string> usersChatCodes = [];

        static async Task Main()
        {
            botClient = new TelegramBotClient("7965761291:AAFF_yg2PMTe1Gpww7e8NMUBSa3xss_u1qk");
            botClient.StartReceiving(Update, Error);

            Console.ReadLine();
        }

        private static async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message == null || message.Type != MessageType.Text || message.Text == null)
            {
                return;
            }
            var text = message.Text;
            var chatId = message.Chat.Id;

            await MoveBack(text, chatId, token);

            await CheckIfStartAndHello(text, chatId, token);
            await SelectGroupStyle(text, chatId, token);
            await CheckIfCode(text, chatId, token);
            await SendPartyTasks(text, chatId, token);
            await PlayGame(text, chatId);
        }

        private static async Task PlayGame(string text, long chatId)
        {
            if (chatStates[chatId] == ChatState.SelectedChatType && chatTypes[chatId] == ChatType.Group)
            {
                // Get task from db
                await botClient.SendTextMessageAsync(chatId, "Тепер пишіть мені в особисті, подобавляйте всього що можна і напишете в цій групі 'пуск'");
                if (text == "пуск")
                {
                    await botClient.SendTextMessageAsync(chatId, "Для того щоб почати приколяс напишіть 'йоу' 5 разів");
                    chatStates[chatId] = ChatState.GamePlaying;
                    await Task.Delay(3000);

                    return;
                }
                return;
            }
            if (chatStates[chatId] == ChatState.GamePlaying && chatTypes[chatId] == ChatType.Group)
            {
                // Get task from db
                // тут луп який тягне постійно з бази завдання і відправляє їх в чат
                // якщо завдання закінчились - відправляє повідомлення про кінець гри
                // але має працювати на /b
                await botClient.SendTextMessageAsync(chatId, "Завдання #1");
                return;
            }
        }

        private static async Task SendPartyTasks(string text, long chatId, CancellationToken token)
        {
            if (chatStates[chatId] == ChatState.SelectedChatType && text == "таск")
            {
                await botClient.SendTextMessageAsync(chatId, "Ну тепер можеш загадувати завдання (можливості редагування немає)", cancellationToken: token);
                chatStates[chatId] = ChatState.AddingTasks;
                return;
            }
            if (chatStates[chatId] == ChatState.AddingTasks)
            {
                // Add task to db

                await botClient.SendTextMessageAsync(chatId, "Завдання додано", cancellationToken: token);
                return;
            }
        }

        private static async Task MoveBack(string text, long chatId, CancellationToken token)
        {
            if (text == "/b" && chatStates.ContainsKey(chatId))
            {
                if (chatStates[chatId] == ChatState.SelectedChatType || chatStates[chatId] == ChatState.AddingTasks)
                {
                    await botClient.SendTextMessageAsync(chatId, "Вернув", cancellationToken: token);
                    await Task.Delay(1000);
                    await botClient.SendTextMessageAsync(chatId, "Вибери тип чату", cancellationToken: token);
                    chatStates[chatId] = ChatState.Started;
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "А я так не хочу (задалеко - це максимум)", cancellationToken: token);
                    return;
                }
            }
        }

        private static async Task CheckIfCode(string code, long chatId, CancellationToken token)
        {
            if (chatStates[chatId] == ChatState.CodeWaiting)
            {
                var me = await botClient.GetMeAsync();
                if (usersChatCodes.Values.Contains(code))
                {
                    await botClient.SendTextMessageAsync(chatId, "Єп біч, сенкс, конект пішов чекай 20 секунд поки скачаю твої дані пж", cancellationToken: token);
                    await Task.Delay(2500);
                    await botClient.SendTextMessageAsync(chatId, "Жарт, ніц не буде. сміхуйовинка просто)", cancellationToken: token);
                    await Task.Delay(2000);
                    await botClient.SendTextMessageAsync(chatId, "Напиши 'таск' щоб продовжити", cancellationToken: token);

                    chatStates[chatId] = ChatState.SelectedChatType;
                    chatTypes[chatId] = ChatType.Private;
                    return;
                }

                if (!me.IsBot && !usersChatCodes.Values.Contains(code))
                {
                    await botClient.SendTextMessageAsync(chatId, "Хуйня якась, код невірний, починай заново", cancellationToken: token);
                    chatStates.Remove(chatId);
                    await CheckIfStartAndHello("/start", chatId, token);
                    return;
                }
            }
        }

        private static async Task CheckIfStartAndHello(string message, long chatId, CancellationToken token)
        {
            if (message == "/start" && !chatStates.ContainsKey(chatId))
            {
                chatStates[chatId] = ChatState.Started;
                await botClient.SendTextMessageAsync(chatId, "Цейво.. шо за двіж ?", cancellationToken: token);
                return;
            }
        }

        private static async Task SelectGroupStyle(string message, long chatId, CancellationToken token)
        {
            if (chatStates[chatId] == ChatState.Started && message.Equals("групачька", StringComparison.CurrentCultureIgnoreCase))
            {
                var guid = Guid.NewGuid().ToString();

                await botClient.SendTextMessageAsync(chatId, "Єбаа вас тут", cancellationToken: token);
                await Task.Delay(1000);

                await botClient.SendTextMessageAsync(chatId, "Кидаю код цієї групи", cancellationToken: token);
                await Task.Delay(1000);

                if (!usersChatCodes.ContainsKey(chatId))
                {
                    usersChatCodes.Add(chatId, guid);
                }
                else
                {
                    guid = usersChatCodes[chatId];
                    await botClient.SendTextMessageAsync(chatId, "Тут вже існує код групи", cancellationToken: token);
                }
                await botClient.SendTextMessageAsync(chatId, guid, cancellationToken: token);

                chatStates[chatId] = ChatState.SelectedChatType;
                chatTypes[chatId] = ChatType.Group;
                return;
            }
            if (chatStates[chatId] == ChatState.Started && message.Equals("приватка", StringComparison.CurrentCultureIgnoreCase))
            {
                await botClient.SendTextMessageAsync(chatId, "Оооо мутки тут робиш?) молодець", cancellationToken: token);
                await Task.Delay(1000);
                await botClient.SendTextMessageAsync(chatId, "Вводь той дурний код давай", cancellationToken: token);
                chatStates[chatId] = ChatState.CodeWaiting;
                return;
            }
        }

        private static async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine(exception.Message);
        }

        enum ChatState
        {
            None,
            Started,
            SelectedChatType,
            CodeWaiting,
            AddingTasks,
            GamePlaying,
            GameStarted
        }
    }
}
