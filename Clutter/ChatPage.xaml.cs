using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Clutter.Models;
using Clutter.Services;
using Clutter.ViewModels;

namespace Clutter;

public partial class ChatPage
{
    public ChatPage(IBluetoothService bluetoothService)
    {
        InitializeComponent();

        var viewModel = new ChatPageViewModel(bluetoothService);
        BindingContext = viewModel;

        // Scroll to the latest message when a new message is added
        viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        if (MessagesList.ItemsSource is ObservableCollection<MessageModel> messages)
        {
            MessagesList.ScrollTo(messages.Count - 1);
        }
    }
}