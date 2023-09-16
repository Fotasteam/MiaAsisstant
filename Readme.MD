# Mia Asisstant

![Sample photo](sample.png)

MiaAsisstant is a simple and still customizable chat-gpt implementation in C#. It is intended to be just an easy way to start learning about [Azure OpenAI API](https://www.nuget.org/packages/Azure.AI.OpenAI/1.0.0-beta.5?_src=template).

## Information about the project:

### Intended use:

I wanted to create an easily-understandable project that people can use to learn more about using ChatGPT in C#. It is great to understand the basics. For more information go to the [OpenAI Website](https://openai.com/) or [various sample projects available](https://platform.openai.com/examples).

### Other Information:

This project is running on the UWP Framework. It also uses [WinUI 2.8.2](https://microsoft.github.io/microsoft-ui-xaml/) for esthetical purposes. 

### Features:

- **API Key TextBox** - You can test this application without modifying its code just by pasting your API Key [found here:](https://platform.openai.com/account/api-keys).
- **Fake Memory** - Mia can "remember" past conversations. In reality, every conversation is saved using the ApplicationDataContainer. Every time on-launch the AI is fed this information using the ChatCompletionsOptions. You can always reset its memory by clicking the **AI Memory Reset** Button.
- **Choose AI Model** - You can easily change AI models by selecting a different AI in the ComboBox.
- **Temperature Selection** - Easily adjustable AI Temperature via a slider. You can read more about Temperature [here](https://platform.openai.com/docs/api-reference/chat). 
- **Custom Initialization Message** - Make the AI believe whatever you want.
