using Microsoft.Extensions.Hosting;
using Rag.Service.Services.Interfaces;
using Rag.TelegramBot.Wrapper.Interface;
using Rag.TelegramBot.Wrapper.Models.Requests;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Rag.TelegramBot.HostService
{
    public class TelegramBotHostedService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IRagWebApiWrapper _ragWebApiWrapper;
        private readonly ITelegramConversationService _telegramConversationService;
        public TelegramBotHostedService(ITelegramBotClient botClient,IRagWebApiWrapper ragWebApiWrapper,ITelegramConversationService telegramConversationService)
        {
            _botClient = botClient;
            _ragWebApiWrapper = ragWebApiWrapper;
            _telegramConversationService = telegramConversationService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>() // receive all update types
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            Console.WriteLine("Bot is started...");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            var chatId = update.Message.Chat.Id;
            using var typingCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var typingTask = Task.Run(async () =>
            {
                try
                {
                    while (!typingCts.Token.IsCancellationRequested)
                    {
                        await bot.SendChatAction(
                            chatId: chatId,
                            action: ChatAction.Typing,
                            cancellationToken: typingCts.Token
                        );
                        await Task.Delay(2000, typingCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            });
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { new KeyboardButton("🆕 New Chat") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
            try
            {
                if (update.Type != UpdateType.Message || string.IsNullOrWhiteSpace(update.Message?.Text))
                    return;

                
                var userMessage = update.Message.Text.Trim();
                var telegramUserId = update.Message.From?.Id ?? 0;

                Log.Information("Received message from ChatId: {ChatId}, UserId: {UserId}, Message: {Message}",
                    chatId, telegramUserId, userMessage);

                var tgConversation = await _telegramConversationService.CreateOrGetTelegramConversationByUserIdAsync(telegramUserId);
                var sessionId = tgConversation.SessionId;

                if (userMessage.Equals("🆕 New Chat", StringComparison.OrdinalIgnoreCase))
                {
                    var newSessionId = await _telegramConversationService.EditSessionIdByUserIdAsync(telegramUserId);
                    await bot.SendMessage(
                        chatId: chatId,
                        text: $"🆕 New chat session started!",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: replyKeyboard,
                        cancellationToken: token
                    );
                    return;
                }
               
                var queryRequest = new TelegramQueryRequest
                {
                    Query = userMessage,
                    SessionId = sessionId,
                    UserId = tgConversation.TelegramUserGuid
                };

                var response = await _ragWebApiWrapper.GetAnswerAsync(queryRequest);
                string answerText = response?.Answer ?? "Sorry, I couldn’t generate an answer.";

                typingCts.Cancel();
                await typingTask;

                await bot.SendMessage(
                    chatId: chatId,
                    text: answerText,
                    replyMarkup: replyKeyboard,
                    cancellationToken: token
                );
            }
            catch (Exception ex)
            {
                typingCts.Cancel();
                await typingTask;

                Log.Error(ex, "Error processing message for ChatId: {ChatId}", chatId);
                await bot.SendMessage(
                    chatId: chatId,
                    text: "⚠️ Something went wrong, please try again later.",
                    replyMarkup: replyKeyboard,
                    cancellationToken: token
                );
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
        {
            Log.Error(exception, "Telegram Bot Error");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Bot is stopping...");
            return Task.CompletedTask;
        }
    }
}
