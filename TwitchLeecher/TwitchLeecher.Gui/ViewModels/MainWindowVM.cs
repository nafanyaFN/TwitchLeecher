﻿using CefSharp;
using CefSharp.Wpf;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TwitchLeecher.Core.Enums;
using TwitchLeecher.Core.Events;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Gui.Events;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.Commands;
using TwitchLeecher.Shared.Events;
using TwitchLeecher.Shared.Extensions;
using TwitchLeecher.Shared.IO;
using TwitchLeecher.Shared.Notification;
using TwitchLeecher.Shared.Reflection;

namespace TwitchLeecher.Gui.ViewModels
{
    public class MainWindowVM : BindableBase
    {
        #region Fields

        private bool _showDonationButton;
        private bool _isAuthenticated;

        private int _videosCount;
        private int _downloadsCount;

        private ViewModelBase _mainView;

        private readonly IEventAggregator _eventAggregator;
        private readonly IAuthService _authService;
        private readonly ITwitchService _twitchService;
        private readonly IDialogService _dialogService;
        private readonly IDonationService _donationService;
        private readonly IFolderService _folderService;
        private readonly INavigationService _navigationService;
        private readonly IRuntimeDataService _runtimeDataService;
        private readonly ISearchService _searchService;
        private readonly IPreferencesService _preferencesService;
        private readonly IUpdateService _updateService;

        private ICommand _showSearchCommand;
        private ICommand _showDownloadsCommand;
        private ICommand _showPreferencesCommand;
        private ICommand _donateCommand;
        private ICommand _showInfoCommand;
        private ICommand _revokeCommand;
        private ICommand _doMinimizeCommand;
        private ICommand _doMmaximizeRestoreCommand;
        private ICommand _doCloseCommand;
        private ICommand _requestCloseCommand;

        private readonly object _commandLockObject;

        #endregion Fields

        #region Constructors

        public MainWindowVM(
            IEventAggregator eventAggregator,
            IAuthService authService,
            ITwitchService twitchService,
            IDialogService dialogService,
            IDonationService donationService,
            IFolderService folderService,
            INavigationService navigationService,
            IRuntimeDataService runtimeDataService,
            ISearchService searchService,
            IPreferencesService preferencesService,
            IUpdateService updateService)
        {
            AssemblyUtil au = AssemblyUtil.Get;

            Title = au.GetProductName() + " " + au.GetAssemblyVersion().Trim();

            _eventAggregator = eventAggregator;
            _authService = authService;
            _twitchService = twitchService;
            _dialogService = dialogService;
            _donationService = donationService;
            _folderService = folderService;
            _navigationService = navigationService;
            _runtimeDataService = runtimeDataService;
            _searchService = searchService;
            _preferencesService = preferencesService;
            _updateService = updateService;

            _commandLockObject = new object();

            _eventAggregator.GetEvent<AuthenticatedChangedEvent>().Subscribe(AuthenticatedChanged);
            _eventAggregator.GetEvent<AuthenticationResultEvent>().Subscribe(AuthenticationResultAction);
            _eventAggregator.GetEvent<PreferencesSavedEvent>().Subscribe(PreferencesSaved);
            _eventAggregator.GetEvent<VideosCountChangedEvent>().Subscribe(VideosCountChanged);
            _eventAggregator.GetEvent<DownloadsCountChangedEvent>().Subscribe(DownloadsCountChanged);
            _eventAggregator.GetEvent<ShowViewEvent>().Subscribe(ShowView);

            _showDonationButton = _preferencesService.CurrentPreferences.AppShowDonationButton;
        }

        #endregion Constructors

        #region Properties

        public int VideosCount
        {
            get
            {
                return _videosCount;
            }
            private set
            {
                SetProperty(ref _videosCount, value, nameof(VideosCount));
            }
        }

        public int DownloadsCount
        {
            get
            {
                return _downloadsCount;
            }
            private set
            {
                SetProperty(ref _downloadsCount, value, nameof(DownloadsCount));
            }
        }

        public string Title { get; }

        public bool ShowDonationButton
        {
            get
            {
                return _showDonationButton;
            }

            private set
            {
                SetProperty(ref _showDonationButton, value, nameof(ShowDonationButton));
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return _isAuthenticated;
            }

            private set
            {
                SetProperty(ref _isAuthenticated, value, nameof(IsAuthenticated));
            }
        }

        public ViewModelBase MainView
        {
            get
            {
                return _mainView;
            }
            set
            {
                SetProperty(ref _mainView, value, nameof(MainView));
            }
        }

        public ICommand ShowSearchCommand
        {
            get
            {
                if (_showSearchCommand == null)
                {
                    _showSearchCommand = new DelegateCommand(ShowSearch);
                }

                return _showSearchCommand;
            }
        }

        public ICommand ShowDownloadsCommand
        {
            get
            {
                if (_showDownloadsCommand == null)
                {
                    _showDownloadsCommand = new DelegateCommand(ShowDownloads);
                }

                return _showDownloadsCommand;
            }
        }

        public ICommand ShowPreferencesCommand
        {
            get
            {
                if (_showPreferencesCommand == null)
                {
                    _showPreferencesCommand = new DelegateCommand(ShowPreferences);
                }

                return _showPreferencesCommand;
            }
        }

        public ICommand DonateCommand
        {
            get
            {
                if (_donateCommand == null)
                {
                    _donateCommand = new DelegateCommand(Donate);
                }

                return _donateCommand;
            }
        }

        public ICommand ShowInfoCommand
        {
            get
            {
                if (_showInfoCommand == null)
                {
                    _showInfoCommand = new DelegateCommand(ShowInfo);
                }

                return _showInfoCommand;
            }
        }

        public ICommand RevokeCommand
        {
            get
            {
                if (_revokeCommand == null)
                {
                    _revokeCommand = new DelegateCommand(RevokeAuthentication);
                }

                return _revokeCommand;
            }
        }

        public ICommand DoMinimizeCommand
        {
            get
            {
                if (_doMinimizeCommand == null)
                {
                    _doMinimizeCommand = new DelegateCommand<Window>(DoMinimize);
                }

                return _doMinimizeCommand;
            }
        }

        public ICommand DoMaximizeRestoreCommand
        {
            get
            {
                if (_doMmaximizeRestoreCommand == null)
                {
                    _doMmaximizeRestoreCommand = new DelegateCommand<Window>(DoMaximizeRestore);
                }

                return _doMmaximizeRestoreCommand;
            }
        }

        public ICommand DoCloseCommand
        {
            get
            {
                if (_doCloseCommand == null)
                {
                    _doCloseCommand = new DelegateCommand<Window>(DoClose);
                }

                return _doCloseCommand;
            }
        }

        public ICommand RequestCloseCommand
        {
            get
            {
                if (_requestCloseCommand == null)
                {
                    _requestCloseCommand = new DelegateCommand(() => { }, CloseApplication);
                }

                return _requestCloseCommand;
            }
        }

        #endregion Properties

        #region Methods

        private void ShowSearch()
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (_videosCount > 0)
                    {
                        _navigationService.ShowSearchResults();
                    }
                    else
                    {
                        _navigationService.ShowSearch();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ShowDownloads()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowDownloads();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ShowPreferences()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowPreferences();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void Donate()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _donationService.OpenDonationPage();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ShowInfo()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowInfo();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void RevokeAuthentication()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _twitchService.Pause();

                    MessageBoxResult? result = null;

                    if (_twitchService.CanShutdown())
                    {
                        result = _dialogService.ShowMessageBox("Do you really want to logout?", "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    }
                    else
                    {
                        result = _dialogService.ShowMessageBox("Do you want to abort all running downloads and logout?", "Logout", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        _twitchService.Shutdown();
                        _authService.RevokeAuthentication();
                        _navigationService.ShowAuth();
                    }
                    else
                    {
                        _twitchService.Resume();
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DoMinimize(Window window)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (window == null)
                    {
                        throw new ArgumentNullException(nameof(window));
                    }

                    window.WindowState = WindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DoMaximizeRestore(Window window)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (window == null)
                    {
                        throw new ArgumentNullException(nameof(window));
                    }

                    window.WindowState = window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DoClose(Window window)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (window == null)
                    {
                        throw new ArgumentNullException(nameof(window));
                    }

                    window.Close();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void AuthenticatedChanged(bool isAuthenticated)
        {
            this.IsAuthenticated = isAuthenticated;
        }

        private void ShowView(ViewModelBase contentVM)
        {
            if (contentVM != null)
            {
                MainView = contentVM;
            }
        }

        private void PreferencesSaved()
        {
            try
            {
                ShowDonationButton = _preferencesService.CurrentPreferences.AppShowDonationButton;
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void VideosCountChanged(int count)
        {
            VideosCount = count;
        }

        private void DownloadsCountChanged(int count)
        {
            DownloadsCount = count;
        }

        public void Loaded()
        {
            try
            {
                InitializeCef();

                if (!CheckAuthentication())
                {
                    _navigationService.ShowAuth();
                }
                else
                {
                    ShowWelcomeOrSearch();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        public void Shown()
        {
            ShowUpdateDialog();
        }

        private void InitializeCef()
        {
            Cef.EnableHighDPISupport();

            if (!Cef.IsInitialized)
            {
                string cachePath = Path.Combine(_folderService.GetAppDataFolder(), "CefCache");

                FileSystem.CreateDirectory(cachePath);

                CefSettings settings = new CefSettings()
                {
                    RemoteDebuggingPort = 9222,
                    CachePath = cachePath
                };

                Cef.Initialize(settings);
            }
        }

        private bool CheckAuthentication()
        {
            string accessToken = _runtimeDataService.RuntimeData.AccessToken;

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return _authService.ValidateAuthentication(accessToken);
            }

            return false;
        }

        private void AuthenticationResultAction(bool success)
        {
            if (success)
            {
                ShowWelcomeOrSearch();
            }
            else
            {
                _navigationService.ShowAuth();
            }
        }

        private void ShowWelcomeOrSearch()
        {
            if (!SearchOnStartup())
            {
                _navigationService.ShowWelcome();
            }
        }

        private bool SearchOnStartup()
        {
            Preferences currentPrefs = _preferencesService.CurrentPreferences.Clone();

            if (currentPrefs.SearchOnStartup)
            {
                currentPrefs.Validate();

                if (!currentPrefs.HasErrors)
                {
                    SearchParameters searchParams = new SearchParameters(SearchType.Channel)
                    {
                        Channel = currentPrefs.SearchChannelName,
                        VideoType = currentPrefs.SearchVideoType,
                        LoadLimitType = currentPrefs.SearchLoadLimitType,
                        LoadFrom = DateTime.Now.Date.AddDays(-currentPrefs.SearchLoadLastDays),
                        LoadFromDefault = DateTime.Now.Date.AddDays(-currentPrefs.SearchLoadLastDays),
                        LoadTo = DateTime.Now.Date,
                        LoadToDefault = DateTime.Now.Date,
                        LoadLastVods = currentPrefs.SearchLoadLastVods
                    };

                    _searchService.PerformSearch(searchParams);

                    return true;
                }
            }

            return false;
        }

        private void ShowUpdateDialog()
        {
            Preferences currentPrefs = _preferencesService.CurrentPreferences.Clone();

            if (currentPrefs.AppCheckForUpdates)
            {
                Task.Run(() => _updateService.CheckForUpdate()).ContinueWith(t =>
                {
                    if (!t.IsFaulted && t.Result != null)
                    {
                        _dialogService.ShowUpdateInfoDialog(t.Result);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private bool CloseApplication()
        {
            try
            {
                _twitchService.Pause();

                if (!_twitchService.CanShutdown())
                {
                    MessageBoxResult result = _dialogService.ShowMessageBox("Do you want to abort all running downloads and exit the application?", "Exit Application", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        _twitchService.Resume();
                        return false;
                    }
                }

                _twitchService.Shutdown();
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }

            return true;
        }

        #endregion Methods
    }
}