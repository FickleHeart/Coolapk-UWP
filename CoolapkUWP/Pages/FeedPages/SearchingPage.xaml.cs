﻿using CoolapkUWP.Controls.ViewModels;
using CoolapkUWP.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace CoolapkUWP.Pages.FeedPages
{
    public sealed partial class SearchingPage : Page
    {
        int[] pages = new int[3];
        string[] lastItems = new string[3];
        public SearchingPage()
        {
            this.InitializeComponent();
            FeedList.ItemsSource = new ObservableCollection<FeedViewModel>();
            UserList.ItemsSource = new ObservableCollection<UserViewModel>();
            TopicList.ItemsSource = new ObservableCollection<TopicViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is object[] vs)
            {
                if (!string.IsNullOrEmpty(vs[1] as string))
                {
                    SearchText.Text = vs[1] as string;
                    SearchTypeComboBox.SelectedIndex = Convert.ToInt32(vs[0]);
                    StartSearch();
                }
            }
        }

        async void SearchFeeds(string keyWord)
        {
            UIHelper.ShowProgressBar();
            string feedType = string.Empty;
            string sortType = string.Empty;
            switch (SearchFeedTypeComboBox.SelectedIndex)
            {
                case 0: feedType = "all"; break;
                case 1: feedType = "feed"; break;
                case 2: feedType = "feedArticle"; break;
                case 3: feedType = "rating"; break;
                case 4: feedType = "picture"; break;
                case 5: feedType = "question"; break;
                case 6: feedType = "answer"; break;
                case 7: feedType = "video"; break;
                case 8: feedType = "ershou"; break;
                case 9: feedType = "vote"; break;
            }
            switch (SearchFeedSortTypeComboBox.SelectedIndex)
            {
                case 0: sortType = "default"; break;
                case 1: sortType = "hot"; break;
                case 2: sortType = "reply"; break;
            }
            JArray array = (JArray)await DataHelper.GetData(DataUriType.SearchFeeds,
                                                            feedType,
                                                            sortType,
                                                            keyWord,
                                                            (++pages[0]).ToString(),
                                                            pages[0] > 1 ? "&lastItem=" + lastItems[0] : string.Empty);
            ObservableCollection<FeedViewModel> FeedsCollection = FeedList.ItemsSource as ObservableCollection<FeedViewModel>;
            if (pages[0] == 1) FeedsCollection.Clear();
            if (array.Count != 0)
            {
                lastItems[0] = array.Last.Value<string>("id");
                foreach (var i in array)
                    FeedsCollection.Add(new FeedViewModel(i as JObject));
            }
            else pages[0]--;
            UIHelper.HideProgressBar();
        }

        async void SearchUsers(string keyWord)
        {
            UIHelper.ShowProgressBar();
            ObservableCollection<UserViewModel> infos = UserList.ItemsSource as ObservableCollection<UserViewModel>;
            JArray array = (JArray)await DataHelper.GetData(DataUriType.SearchUsers, keyWord, ++pages[1], pages[1] > 1 ? "&lastItem=" + lastItems[1] : string.Empty);
            if (array.Count > 0)
            {
                lastItems[1] = array.Last.Value<string>("uid");
                if (infos.Count > 0)
                {
                    var d = (from a in infos
                             from b in array
                             where a.UserName == b.Value<string>("username")
                             select a).ToArray();
                    foreach (var item in d)
                        infos.Remove(item);
                }
                for (int i = 0; i < array.Count; i++)
                {
                    JToken t = array[i];
                    infos.Add(new UserViewModel((JObject)t));
                }
            }
            else pages[1]--;
            UIHelper.HideProgressBar();
        }

        async void SearchTopic(string keyWord)
        {
            UIHelper.ShowProgressBar();
            JArray array = (JArray)await DataHelper.GetData(DataUriType.SearchTags, keyWord, ++pages[2], pages[2] > 1 ? "&lastItem=" + lastItems[2] : string.Empty);
            ObservableCollection<TopicViewModel> FeedsCollection = TopicList.ItemsSource as ObservableCollection<TopicViewModel>;
            if (pages[2] == 1) FeedsCollection.Clear();
            if (array.Count != 0)
            {
                lastItems[2] = array.Last.Value<string>("id");
                foreach (JObject i in array)
                    FeedsCollection.Add(new TopicViewModel(i));
            }
            else pages[2]--;
            UIHelper.HideProgressBar();
        }

        void StartSearch()
        {
            if (string.IsNullOrEmpty(SearchText?.Text))
                DetailPivot.Visibility = Visibility.Collapsed;
            else
            {
                switch (DetailPivot.SelectedIndex)
                {
                    case 0:
                        SearchFeeds(SearchText.Text);
                        break;
                    case 1:
                        SearchUsers(SearchText.Text);
                        break;
                    case 2:
                        SearchTopic(SearchText.Text);
                        break;
                }
                DetailPivot.Visibility = Visibility.Visible;
            }
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e) => UIHelper.OpenLink((sender as FrameworkElement).Tag as string);

        private void BackButton_Click(object sender, RoutedEventArgs e) => Frame.GoBack();

        private void SearchTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTypeComboBox.SelectedIndex != -1 && DetailPivot != null)
                DetailPivot.SelectedIndex = SearchTypeComboBox.SelectedIndex;
            if (SearchTypeComboBox.SelectedIndex + 1 == SearchTypeComboBox.Items.Count || pages[SearchTypeComboBox.SelectedIndex] == 0)
                StartSearch();
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            if (!e.IsIntermediate && viewer.VerticalOffset == viewer.ScrollableHeight)
                StartSearch();
        }

        private void SearchFeedTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pages[0] = 0;
            lastItems[0] = string.Empty;
            StartSearch();
        }

        #region 搜索框相关
        private void SearchTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                SearchText_QuerySubmitted(null, null);
        }
        private void SearchText_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            pages = new int[3];
            lastItems = new string[3];
            (FeedList.ItemsSource as ObservableCollection<FeedViewModel>).Clear();
            (UserList.ItemsSource as ObservableCollection<UserViewModel>).Clear();
            (TopicList.ItemsSource as ObservableCollection<TopicViewModel>).Clear();
            StartSearch();
        }
        private async void SearchText_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                JArray array = (JArray)await DataHelper.GetData(DataUriType.GetSearchWords, sender.Text);
                if (array != null && array.Count > 0)
                    sender.ItemsSource = array.Select(i => new SearchWord(i as JObject));
                else
                    sender.ItemsSource = null;
            }
        }
        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is SearchWord m)
                sender.Text = m.GetTitle();
        }
        #endregion

        private void DetailPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTypeComboBox.SelectedIndex != DetailPivot.SelectedIndex)
            {
                SearchTypeComboBox.SelectedIndex = DetailPivot.SelectedIndex;
                if (SearchTypeComboBox.SelectedIndex + 1 == SearchTypeComboBox.Items.Count || pages[SearchTypeComboBox.SelectedIndex] == 0)
                    StartSearch();
            }
        }
    }
}
