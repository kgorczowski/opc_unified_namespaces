// Copyright (c) 2025 vm.pl

namespace OPCGateway.Admin.Client.Wpf.Views;

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OPCGateway.Admin.Client.Wpf.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            if (viewModel.CurrentView is ServerListViewModel slvm)
                await slvm.LoadServersCommand.ExecuteAsync(null);
        };
    }
}
