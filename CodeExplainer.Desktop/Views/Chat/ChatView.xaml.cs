using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CodeExplainer.Desktop.Views.Chat;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        DataContextChanged += ChatView_DataContextChanged;
    }

    private void ChatView_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        // Try to subscribe to Messages collection change so we can auto-scroll
        if (e.OldValue is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= Vm_PropertyChanged;
        }

        if (e.NewValue is INotifyPropertyChanged vm)
        {
            vm.PropertyChanged += Vm_PropertyChanged;
            TrySubscribeMessages(vm);
        }
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Messages" && sender is INotifyPropertyChanged vm)
        {
            TrySubscribeMessages(vm);
        }
    }

    private void TrySubscribeMessages(INotifyPropertyChanged vm)
    {
        var prop = vm.GetType().GetProperty("Messages");
        if (prop == null) return;
        var val = prop.GetValue(vm) as INotifyCollectionChanged;
        if (val == null) return;

        val.CollectionChanged += Messages_CollectionChanged;
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to end on the UI thread
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                MessagesScrollViewer?.ScrollToEnd();
            }
            catch { }
        }));
    }
}
