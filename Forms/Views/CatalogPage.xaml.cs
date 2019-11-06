﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Jammit.Model;

namespace Jammit.Forms.Views
{
  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class CatalogPage : ContentPage
  {
    public static List<SongInfo> Catalog { get; private set; }

    public CatalogPage()
    {
      InitializeComponent();
    }

    #region Page overrides

    protected override void OnAppearing()
    {
      base.OnAppearing();

      Device.BeginInvokeOnMainThread(async () => await LoadCatalog());
    }

    #endregion Page overrides

    private async Task LoadCatalog()
    {
      try
      {
        Catalog = await App.Client.LoadCatalog();

        AuthPopup.IsVisible = false;
      }
      catch (System.Net.Http.HttpRequestException ex)
      {
        Catalog = default;
        await DisplayAlert("Error", ex.Message, "Cancel");

        if (App.Client.AuthStatus == Jammit.Client.AuthorizationStatus.Rejected)
        {
          Settings.Credentials = default;
          AuthPopup.IsVisible = true;
        }
      }
      catch (Exception ex)
      {
        Catalog = default;
        await DisplayAlert("Error", ex.Message, "Cancel");
      }

      //TODO: Move back into XAML bindings.
      this.CatalogView.ItemsSource = Catalog;
    }

    private async void LoadButton_Clicked(object sender, EventArgs e)
    {
      await LoadCatalog();
    }

    private async void DownloadButton_Clicked(object sender, EventArgs e)
    {
      if (null == CatalogView.SelectedItem)
        return;

      // Download song
      var selectedSong = CatalogView.SelectedItem as SongInfo;
      try
      {
        // Make sure Downloads directory exists.
        var downloadsDir = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(App.DataDirectory, "Downloads"));
        var zipPath = System.IO.Path.Combine(downloadsDir.FullName, selectedSong.Id.ToString().ToUpper() + ".zip");

        await App.Client.DownloadSong(selectedSong, zipPath);
        var downloadedStream = System.IO.File.OpenRead(zipPath);
        var song = App.Library.AddSong(downloadedStream);
        System.IO.File.Delete(zipPath); // Delete downloaded file.

        //TODO: Assert selected item and downloaded content metadata are equal.

        await DisplayAlert("Downloaded Song", song.ToString(), "OK");

        DownloadProgressBar.Progress = 0;
      }
      catch (Exception ex)
      {
        await DisplayAlert("Error", $"Could not download or install song with ID [{selectedSong.Id}].", "OK");
      }
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
      CatalogView.BeginRefresh();

      if (string.IsNullOrWhiteSpace(e.NewTextValue))
        CatalogView.ItemsSource = Catalog;
      else
        CatalogView.ItemsSource = Catalog.Where(
          s => s.Title.IndexOf(e.NewTextValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
          s.Artist.IndexOf(e.NewTextValue, StringComparison.OrdinalIgnoreCase) >= 0 ||
          s.Album.IndexOf(e.NewTextValue, StringComparison.OrdinalIgnoreCase) >= 0
        );

      CatalogView.EndRefresh();
    }

    private async void AuthButton_Clicked(object sender, EventArgs e)
    {
      Settings.Credentials = AuthUser.Text + ':' + AuthPassword.Text;

      await LoadCatalog();
    }
  }
}
