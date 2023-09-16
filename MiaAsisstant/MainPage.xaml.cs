using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Azure.AI.OpenAI;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;
using Windows.Media.Audio;
using System.Threading;
using Windows.System.Threading;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.ViewManagement;
using System.Drawing;
using Windows.Graphics.Display;

namespace MiaAsisstant
{
    public sealed partial class MainPage : Page
    {
        string mainChatContent;
        Stopwatch timer;
        List<string[]> conversationsList = new List<string[]>();
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        string receivedResponse = string.Empty;

        Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
        double scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

        OpenAIClient client;
        ChatCompletionsOptions chatCompletionsOptions;

        List<string> AIModels = new List<string>() { "gpt-3.5-turbo",
            "gpt-3.5-turbo-0301", "gpt-3.5-turbo-0613", "gpt-3.5-turbo-16k",
            "gpt-3.5-turbo-16k-0613"};

        public MainPage()
        {
            if (localSettings.Values["ApiKey"] != null && localSettings.Values["ApiKey"].ToString() != "")
                client = new OpenAIClient(localSettings.Values["ApiKey"].ToString());

            if (localSettings.Values["numberOfConversations"] == null)
            {
                localSettings.Values["numberOfConversations"] = 0;
            }
            if (localSettings.Values["PrematureWarning"] == null)
            {
                localSettings.Values["PrematureWarning"] = "0";
            }
            if (localSettings.Values["Temperature"] == null)
            {
                localSettings.Values["Temperature"] = "1";
            }
            if (localSettings.Values["ApiKeyAutoSave"] == null)
            {
                localSettings.Values["ApiKeyAutoSave"] = "1";
            }
            if (localSettings.Values["ApiKey"] == null)
            {
                localSettings.Values["ApiKey"] = "";
            }
            if (localSettings.Values["CustomModel"] == null)
            {
                localSettings.Values["CustomModel"] = "gpt-3.5-turbo";
            }
            if (localSettings.Values["CustomModelEnabled"] == null)
            {
                localSettings.Values["CustomModelEnabled"] = "0";
            }
            if (localSettings.Values["CustomInitializationMessageEnabled"] == null)
            {
                localSettings.Values["CustomInitializationMessageEnabled"] = "0";
            }
            if (localSettings.Values["CustomInitializationMessage"] == null)
            {
                localSettings.Values["CustomInitializationMessage"] = "";
            }

            this.InitializeComponent();

            //for (int i = 1; i <= int.Parse(localSettings.Values["numberOfConversations"].ToString()); ++i)
            //{
            //    ComboBoxConversationsBrowser.Items.Add("Conversation " + i.ToString() + " MIA");
            //    ComboBoxConversationsBrowser.Items.Add("Conversation " + i.ToString() + " User");
            //}

            foreach (string AIModel in AIModels)
                ComboBoxAIModel.Items.Add(AIModel);

            if (localSettings.Values["CustomModelEnabled"].ToString() == "0")
                ComboBoxAIModel.IsEnabled = false;
            else
            {
                CheckBoxAIModel.IsChecked = true;
                ComboBoxAIModel.SelectedItem = localSettings.Values["CustomModel"].ToString();
            }

            TextBoxApiKey.Text = localSettings.Values["ApiKey"].ToString();
            if (localSettings.Values["ApiKeyAutoSave"].ToString() == "1") CheckBoxApiKey.IsChecked = true;
            textBoxApiKeyValueLoaded = true;

            if (localSettings.Values["numberOfConversations"] == null || localSettings.Values["numberOfConversations"].ToString() == "0")
            {
                buttonDeleteMemory.IsEnabled = false;
            }

            var size = new Windows.Foundation.Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
            GridCenterBackground.Height = size.Height;
            GridLeftBackground.Height = size.Height - 300;
            //GridRightBackground.Height = size.Height - 300;

            SliderTemperature.Value = double.Parse(localSettings.Values["Temperature"].ToString());

            mainChatContent = mainChat.Text;
            timer = new Stopwatch();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (localSettings.Values["CustomInitializationMessageEnabled"].ToString() == "1")
            {
                chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                {
                    new ChatMessage(ChatRole.System, 
                    "You are a helpful assistant. Your name is Mia." +
                    " You are an AI capable of anything and everything." +
                    " You are supposed to help the user with their tasks," +
                    " problems and answer their quesions. Speak freely, and be spontaneous." +
                    " Do not hesitate to answer any quesion. Speak in an easy-to-understand language," +
                    " make your responses short and make your responses seem human."),
                }
                };
            }
            else
            {
                chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages =
                {
                    new ChatMessage(ChatRole.System, localSettings.Values["CustomInitializationMessage"].ToString()),
                }
                };
            }


            if (AlternativeTemperatureValueLoading != 0)
                chatCompletionsOptions.Temperature = AlternativeTemperatureValueLoading;

            if (int.Parse(localSettings.Values["numberOfConversations"].ToString()) == 0)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, " Introduce yourself briefly."));
            }
            else
            {
                mainChat.Text += localSettings.Values["numberOfConversations"].ToString();
                for (int i = 1; i <= int.Parse(localSettings.Values["numberOfConversations"].ToString()); ++i)
                {
                    string[] temporaryArray = { localSettings.Values["conv" + i.ToString() + "USER"].ToString(), localSettings.Values["conv" + i.ToString() + "MIA"].ToString() };

                    conversationsList.Add(temporaryArray);
                }
                //mainChat.Text += ("Ammount= " + localSettings.Values["numberOfConversations"]);
                //mainChat.Text += ("CONV1USER " + localSettings.Values["conv1USER"]);
                //mainChat.Text += ("CONV1MIA" + localSettings.Values["conv1MIA"]);

                foreach (var conversation in conversationsList)
                {
                    try
                    {
                        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, conversation[0]));
                    }
                    catch (Exception ex)
                    {
                        InfoBarPrematurelyExiting.Message = "Warning, numberOfConversations suggests more conversations should be recorded in memory. Forcing settings reset might solve this issue. Exception Code: " + ex;
                        InfoBarPrematurelyExiting.Title = "Memory tampering detected!";
                        InfoBarPrematurelyExiting.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                        InfoBarPrematurelyExiting.IsOpen = true;
                    }

                    try
                    {
                        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, conversation[1]));
                    }
                    catch (Exception ex)
                    {
                        InfoBarPrematurelyExiting.Message = "Warning, numberOfConversations suggests more conversations should be recorded in memory. Forcing settings reset might solve this issue. Exception Code: " + ex;
                        InfoBarPrematurelyExiting.Title = "Memory tampering detected!";
                        InfoBarPrematurelyExiting.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                        InfoBarPrematurelyExiting.IsOpen = true;
                    }
                    //mainChat.Text += conversation[0];
                    //mainChat.Text += " MIA - " + conversation[1];
                }
            }

            receiveResponse();
        }

        async void receiveResponse()
        {
            if (localSettings.Values["ApiKey"] != null && localSettings.Values["ApiKey"].ToString() != "")
            {
                if (!timer.IsRunning)
                {
                    timer.Start();
                    buttonASK.IsEnabled = false;
                    loadingRing.Visibility = Visibility.Visible;
                }

                Azure.Response<StreamingChatCompletions> chatCompletionsResponse = null;

                if (localSettings.Values["CustomModelEnabled"].ToString() != "1")
                {
                    chatCompletionsResponse = await client.GetChatCompletionsStreamingAsync(
                    deploymentOrModelName: "gpt-3.5-turbo",
                    chatCompletionsOptions);
                }
                else
                {
                    chatCompletionsResponse = await client.GetChatCompletionsStreamingAsync(
                    deploymentOrModelName: localSettings.Values["CustomModel"].ToString(),
                    chatCompletionsOptions);
                }

                var chatResponseBuilder = new StringBuilder();
                await foreach (var chatChoice in chatCompletionsResponse.Value.GetChoicesStreaming())
                {
                    await foreach (var chatMessage in chatChoice.GetMessageStreaming())
                    {
                        chatResponseBuilder.AppendLine(chatMessage.Content);
                        mainChat.Text += chatMessage.Content;
                        receivedResponse += chatMessage.Content;
                        await Task.Delay(TimeSpan.FromMilliseconds(200));
                    }
                }

                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, chatResponseBuilder.ToString()));

                if (timer.Elapsed.TotalSeconds > 0.5)
                {
                    timer.Stop();
                    buttonASK.IsEnabled = true;
                    loadingRing.Visibility = Visibility.Collapsed;
                    localSettings.Values["conv" + localSettings.Values["numberOfConversations"].ToString() + "MIA"] = receivedResponse;
                }
            }
            else
            {
                InfoBarPrematurelyExiting.Message = "Api Key is either invalid, expired, or not set. AI Response generation aborted.";
                InfoBarPrematurelyExiting.Title = "OpenAI Api Key is invalid.";
                InfoBarPrematurelyExiting.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                InfoBarPrematurelyExiting.IsOpen = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string conversation = "Failed Saving User Message";

            mainChat.Text += Environment.NewLine + "You - " + message.Text;
            conversation = Environment.NewLine + "You - " + message.Text;

            var userMessage = message.Text;
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, userMessage));
            message.Text = string.Empty;
            mainChat.Text += Environment.NewLine + "Mia - ";

            localSettings.Values["numberOfConversations"] = int.Parse(localSettings.Values["numberOfConversations"].ToString()) + 1;
            localSettings.Values["conv" + localSettings.Values["numberOfConversations"].ToString() + "USER"] = conversation;

            receivedResponse = string.Empty;
            receiveResponse();

            if (localSettings.Values["PrematureWarning"].ToString() == "0")
            {
                InfoBarPrematurelyExiting.Message = "Shutting down during response generation may require the AI to reset its memory.";
                InfoBarPrematurelyExiting.Title = "Do not shut down this program!";
                InfoBarPrematurelyExiting.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                InfoBarPrematurelyExiting.IsOpen = true;
            }
        }

        private void buttonDeleteMemory_Click(object sender, RoutedEventArgs e)
        {
            bool warning = false;
            if (localSettings.Values["PrematureWarning"].ToString() == "1") warning = true;
            string apiKey = localSettings.Values["ApiKey"].ToString();
            string apiKeyAutoSave = localSettings.Values["ApiKeyAutoSave"].ToString();
            float Temperature = float.Parse(localSettings.Values["Temperature"].ToString());
            
            localSettings.Values.Clear();

            if (warning)
            {
                localSettings.Values["PrematureWarning"] = "1";
                localSettings.Values["ApiKey"] = apiKey;
                localSettings.Values["ApiKeyAutoSave"] = apiKeyAutoSave;
                localSettings.Values["Temperature"] = Temperature;
            }
        }

        private void InfoBarPrematurelyExiting_CloseButtonClick(Microsoft.UI.Xaml.Controls.InfoBar sender, object args)
        {
            localSettings.Values["PrematureWarning"] = "1";
        }

        bool sliderTemperatureValueLoaded = false;
        float AlternativeTemperatureValueLoading = 0;

        private void SliderTemperature_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!sliderTemperatureValueLoaded) sliderTemperatureValueLoaded = true;
            else
                localSettings.Values["Temperature"] = SliderTemperature.Value.ToString();
            if (localSettings.Values["ApiKey"] != null && localSettings.Values["ApiKey"].ToString() != "")
            {
                AlternativeTemperatureValueLoading = float.Parse(SliderTemperature.Value.ToString());
            }
        }

        private void CheckBoxApiKey_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CheckBoxApiKey.IsChecked)
            {
                localSettings.Values["ApiKeyAutoSave"] = "1";
            }
            else
            {
                localSettings.Values["ApiKeyAutoSave"] = "0";
            }
        }

        bool textBoxApiKeyValueLoaded = false;

        private void TextBoxApiKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!textBoxApiKeyValueLoaded)
                textBoxApiKeyValueLoaded = true;
            else
                if (localSettings.Values["ApiKeyAutoSave"].ToString() == "1")
                    localSettings.Values["ApiKey"] = TextBoxApiKey.Text;
        }

        private void buttonDeleteSettings_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values.Clear();
        }

        private void CheckBoxAIModel_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CheckBoxAIModel.IsChecked)
            {
                localSettings.Values["CustomModelEnabled"] = "1";
                ComboBoxAIModel.IsEnabled = true;
            }
            else
            {
                localSettings.Values["CustomModelEnabled"] = "0";
                ComboBoxAIModel.IsEnabled = false;
            }
        }

        private void ComboBoxAIModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (localSettings.Values["CustomModelEnabled"].ToString() == "1")
                localSettings.Values["CustomModel"] = ComboBoxAIModel.SelectedItem.ToString();
        }

        private void CheckBoxInitializationMessage_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CheckBoxInitializationMessage.IsChecked)
            {
                localSettings.Values["CustomInitializationMessageEnabled"] = "1";
            }
            else
            {
                localSettings.Values["CustomInitializationMessageEnabled"] = "0";
            }
        }

        bool textBoxInitializationMessageValueLoaded = false;

        private void TextBoxInitializationMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!textBoxInitializationMessageValueLoaded)
                textBoxApiKeyValueLoaded = true;
            else
                if (localSettings.Values["CustomInitializationMessageEnabled"].ToString() == "1")
                localSettings.Values["CustomInitializationMessage"] = TextBoxInitializationMessage.Text;
        }

        private void ComboBoxConversationsBrowser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string unchangedID = ComboBoxConversationsBrowser.SelectedItem.ToString();
            //unchangedID = unchangedID.Replace("Conversation ", "conv");
            //unchangedID = unchangedID.Replace(" MIA", "MIA");
            //unchangedID = unchangedID.Replace(" USER", "USER");
            //TextBoxConversationsBrowser.Text = localSettings.Values[unchangedID].ToString();
        }

        private void ButtonConversationsBrowser_Click(object sender, RoutedEventArgs e)
        {
            //if (ComboBoxConversationsBrowser.SelectedItem != null || ComboBoxConversationsBrowser.SelectedItem.ToString() != "")
            //{
            //    string unchangedID = ComboBoxConversationsBrowser.SelectedItem.ToString();
            //    unchangedID = unchangedID.Replace("Conversation ", "");
            //    unchangedID = unchangedID.Replace(" MIA", "");
            //    unchangedID = unchangedID.Replace(" USER", "");

            //    //algorytm usuwajacy jeden value z localsettings i przesuwajacy liczbe o jeden w dol, po tym refresh wartosci
            //    //w comboboxie na nowe. Uwaga! Usuwanie jednej z konwersacji o tym samym id spowoduje usunięcie też drugiej.

            //    int valueDeleted = int.Parse(unchangedID);
            //    int valueMax = int.Parse(localSettings.Values["numberOfConversations"].ToString());

            //    if (valueDeleted == valueMax)
            //        localSettings.Values.Remove("conv" + valueDeleted + "MIA");
            //    else
            //    {
                    
            //    }

            //    ComboBoxConversationsBrowser.Items.Remove(ComboBoxConversationsBrowser.SelectedItem);
            //}
        }

        private void TextBoxConversationsBrowser_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (ComboBoxConversationsBrowser.SelectedItem.ToString() != "" || ComboBoxConversationsBrowser.SelectedItem.ToString() != null)
            //{

            //}
        }
    }
}
