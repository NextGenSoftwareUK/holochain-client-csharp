﻿global using System.Collections.Immutable;
global using System.Windows.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Windows.Networking.Connectivity;
global using Windows.Storage;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Navigation;
global using Microsoft.Extensions.Options;
global using UnoApp.Business.Models;
global using UnoApp.Infrastructure;
global using UnoApp.Presentation;
global using UnoApp.DataContracts;
global using UnoApp.DataContracts.Serialization;
global using UnoApp.Services.Caching;
global using UnoApp.Services.Endpoints;
#if MAUI_EMBEDDING
global using UnoApp.MauiControls;
#endif
global using Uno.UI;
global using Windows.ApplicationModel;
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
