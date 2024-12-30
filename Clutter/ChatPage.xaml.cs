using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Clutter.Models;
using Clutter.Services;
using Clutter.ViewModels;

namespace Clutter;

public sealed partial class ChatPage
{
    public ChatPage(IBluetoothService bluetoothService, IConnectionService connectionService,
        IMessagingService messagingService, ISoundService soundService)
    {
        InitializeComponent();

        var viewModel = new ChatPageViewModel(bluetoothService, connectionService, messagingService, soundService);
        BindingContext = viewModel;

        if (viewModel.Messages != null) viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
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