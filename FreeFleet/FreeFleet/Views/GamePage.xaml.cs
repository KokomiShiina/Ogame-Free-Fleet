﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FreeFleet.Core;
using FreeFleet.Resources;
using FreeFleet.Services.Web;
using FreeFleet.ViewModels.Home;
using FreeFleet.Views.Modal;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeFleet.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class GamePage : ContentPage
	{
        // Singleton
	    public static GamePage Instance = new GamePage();
        
	    private GamePageViewModel _vm;
	    public GameManager GameManger => _vm.GameManager;

		public GamePage ()
		{
			InitializeComponent ();
		    _vm = (GamePageViewModel) BindingContext;
		    GameView.Source = UriList.LandingUrl;
        }

        /// <summary>
        /// Make GameView navigate to the given URL
        /// </summary>
        /// <param name="url"></param>
	    public void GameViewNavigateTo(string url)
        {
            GameView.Source = url;
        }

	    #region Buttons

	    /// <summary>
	    /// Handles refresh button on clicked
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
	    private void RefreshBtn_OnClick(object sender, EventArgs e)
	    {
	        GameView.Source = (GameView.Source as UrlWebViewSource).Url;
	    }

	    /// <summary>
	    /// Handles back button on clicked
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
	    private void NavigateBackBtn_OnClick(object sender, EventArgs e)
	    {
	        if (GameView.CanGoBack)
	        {
	            GameView.GoBack();
	        }
	    }

	    /// <summary>
	    /// Handles forward button on clicked
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
	    private void NavigateForwardBtn_OnClick(object sender, EventArgs e)
	    {
	        if (GameView.CanGoForward)
	        {
	            GameView.GoForward();
	        }
	    }

        /// <summary>
        /// Handles stop alarm button on clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
	    private void StopAlarmBtn_OnClicked(object sender, EventArgs e)
	    {
	        CrossSimpleAudioPlayer.Current.Stop();
	    }

        #endregion

        #region Browser Handlers

        /// <summary>
        /// Handles GameView OnNavigating events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameView_OnNavigating(object sender, WebNavigatingEventArgs e)
	    {
	        _vm.MainUrl = e.Url;
	    }

        /// <summary>
        /// Handles GameView OnNavigated events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GameView_OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            var uri = new Uri(e.Url);
            var gm = _vm.GameManager;

            // Check if in Lobby
            if (uri.Host == UriList.OgameLobbyHost)
	        {
                // if already logged in and re-login enabled
	            if (gm.IsLogin && gm.AutoRelogin)
	            {
	                await GameManger.Login();
	            }
	            else
	            {
	                // if in lobby, get accounts
	                await Navigation.PushModalAsync(new AccountSelectionModal());
	            }
	        }

            // Check if in Game
            var r = new Regex(@"s\d+-[a-z]+.ogame.gameforge.com");
            var m = r.Match(uri.Host);
            gm.IsInGame = m.Success;
            if (m.Success)
            {
                // Load event fleet when user refreshes
                gm.UpdateEventFleets();
                gm.StartMonitor();
            }
        }

	    #endregion
	}
}