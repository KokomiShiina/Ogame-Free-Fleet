﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FreeFleet.Model.Ogame;
using FreeFleet.Resources;
using FreeFleet.Services.Web;
using FreeFleet.ViewModels;
using FreeFleet.Views;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;

namespace FreeFleet.Core
{
    public class GameManager : BindableBase
    {
        private bool _isInGame;
        private bool _autoRelogin = true;

        public ServerAccount LoggedInUser { get; set; }
        public string LoggedInServerHost { get; private set; }
        public ObservableCollection<EventFleet> EventFleets { get; } = new ObservableCollection<EventFleet>();

        // Timer related
        private DateTime _lastUpdateTime;
        private int _nextUpdateLeft;
        private int _updateInterval = 180; // 3 minutes
        private int _randomUpToSeconds = 30;
        private int _randomizer = 0;

        public GameManager()
        {
            EventFleets.CollectionChanged += EventFleetChangedHandler;
        }

        public async Task<bool> Login(ServerAccount account = null)
        {
            // If account not given, used the saved one
            if (account == null) account = LoggedInUser;

            // Login user
            var login = await DependencyService.Get<IHttpService>().LoginAccountAsync(account);
            GamePage.Instance.GameViewNavigateTo(login.Url);
            LoggedInUser = account;
            LoggedInServerHost = new Uri(login.Url).Host;
            IsLogin = true;
            return true;
        }

        /// <summary>
        /// Start the event fleets monitor
        /// </summary>
        public void StartMonitor()
        {
            var seconds = TimeSpan.FromSeconds(1);
            _lastUpdateTime = DateTime.Now;
            Device.StartTimer(seconds, RefreshTimerHandler);
        }

        /// <summary>
        /// Update the event fleet from server
        /// </summary>
        public async void UpdateEventFleets()
        {
            if(!IsLogin) return; // Not logged in, do nothing
            var fleets = await DependencyService.Get<IHttpService>().GetEventFleetsAsync(LoggedInServerHost);

            // Check if request failed
            if (fleets == null)
            {
                // Relogin if failed
                GamePage.Instance.GameViewNavigateTo("https://" + UriList.OgameLobbyHost);
                return;
            }

            // Remove flags no longer exists
            var removeFleets = EventFleets.Where(ef => fleets.All(f => f.Id != ef.Id)).ToArray();
            foreach (var removeFleet in removeFleets)
            {
                EventFleets.Remove(removeFleet);
            }

            // Add new event fleets
            foreach (var fleet in fleets)
            {
                if (EventFleets.All(f => f.Id != fleet.Id))
                {
                    EventFleets.Add(fleet);
                }
            }
        }

        #region Handlers

        /// <summary>
        /// Handles when timer ticks
        /// </summary>
        /// <returns></returns>
        private bool RefreshTimerHandler()
        {
            var timeNow = DateTime.Now;
            var requiredInterval = _updateInterval + _randomizer;
            var interval = (int)(timeNow - _lastUpdateTime).TotalSeconds;
            NextUpdateLeft = requiredInterval - interval;
            if (interval > requiredInterval)
            {
                // Update fleet
                UpdateEventFleets();
                _lastUpdateTime = timeNow;

                // Generate some randomness
                var rnd1 = new Random();
                _randomizer = rnd1.Next(0, _randomUpToSeconds);
            }
            return true; // Repeat the timer
        }

        /// <summary>
        /// Handles when EventFleets is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void EventFleetChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            if (!(e.NewItems[0] is EventFleet eventFleet)) return; // Type mismatch, return

            MissionType[] offensiveMission = { MissionType.AcsAttack, MissionType.Attack, MissionType.MoonDestruction };
            if (!offensiveMission.Contains(eventFleet.MissionType)) return; // Not an offensive mission, return

            // Offensive mission, play alert
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var audioStream = assembly.GetManifestResourceStream("FreeFleet.Resources.Audio." + "alarm.mp3");
            var player = CrossSimpleAudioPlayer.Current;
            player.Loop = true;
            player.Load(audioStream);
            player.Play();
        }

        #endregion

        #region Auto Properties

        /// <summary>
        /// Gets or Sets the value of how frequent the timer will update in seconds
        /// </summary>
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                _updateInterval = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the value of how long until the next update will run in seconds
        /// </summary>
        public int NextUpdateLeft
        {
            get => _nextUpdateLeft;
            private set
            {
                _nextUpdateLeft = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets the value of how many seconds of randomness the user want in the update
        /// </summary>
        public int RandomUpToSeconds
        {
            get => _randomUpToSeconds;
            set
            {
                _randomUpToSeconds = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets whether the game manager will auto re-login
        /// </summary>
        public bool AutoRelogin
        {
            get => _autoRelogin;
            set
            {
                _autoRelogin = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or Sets whether the user is currently in game.
        /// </summary>
        public bool IsInGame
        {
            get => _isInGame;
            set
            {
                _isInGame = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets whether the game manager is logged in.
        /// </summary>
        public bool IsLogin { get; private set; }

        #endregion
    }
}