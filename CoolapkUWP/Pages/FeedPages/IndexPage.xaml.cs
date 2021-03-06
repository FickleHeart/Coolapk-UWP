﻿using CoolapkUWP.Controls.ViewModels;
using CoolapkUWP.Helpers;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace CoolapkUWP.Pages.FeedPages
{
    public sealed partial class IndexPage : Page
    {
        int page = 0;
        readonly List<int> pages = new List<int>();
        string pageUrl;
        readonly ObservableCollection<Entity> Collection = new ObservableCollection<Entity>();
        int index;
        readonly List<string> urls = new List<string>();
        readonly ObservableCollection<ObservableCollection<Entity>> Feeds2 = new ObservableCollection<ObservableCollection<Entity>>();

        public bool CanLoadMore { get => Collection.Count != 0; }
        public IndexPage() => this.InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            object[] vs = e.Parameter as object[];

            pageUrl = vs[0] as string;
            if (pageUrl.Contains("&title=")) TitleBar.Title = pageUrl.Substring(pageUrl.LastIndexOf("&title=") + 7);
            if (pageUrl.IndexOf("/page") == -1 && pageUrl != "/main/indexV8") pageUrl = "/page/dataList?url=" + pageUrl;
            else if (pageUrl.IndexOf("/page") == 0 && !pageUrl.Contains("/page/dataList")) pageUrl = pageUrl.Replace("/page", "/page/dataList");
            pageUrl = pageUrl.Replace("#", "%23");
            index = -1;
            if ((bool)vs[1])
            {
                TitleBar.Visibility = Visibility.Collapsed;
            }
            GetUrlPage();
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if ((bool)vs[1])
                    {
                        listView.Padding = new Thickness(0);
                    }
                    (VisualTree.FindDescendantByName(listView, "ScrollViewer") as ScrollViewer).ViewChanged += ScrollViewer_ViewChanged;
                });
            });
        }

        public async void GetUrlPage(int p = -1)
        {
            if (index == -1)
            {
                if (!await GetUrlPage(p == -1 ? ++page : p, pageUrl, Collection))
                    page--;
            }
            else if (p == -1)
            {
                if (!await GetUrlPage(page = p == -1 ? ++pages[index] : p, urls[index], Feeds2[index]))
                    pages[index]--;
            }
        }

        async Task<bool> GetUrlPage(int page, string url, ObservableCollection<Entity> FeedsCollection)
        {
            UIHelper.ShowProgressBar();
            JArray array = (JArray)await DataHelper.GetData(DataUriType.GetIndexPage, url, url == "/main/indexV8" ? "?" : "&", page);
            if (array != null && array.Count > 0)
                if (page == 1)
                {
                    int n = 0;
                    if (FeedsCollection.Count > 0)
                    {
                        var needDeleteItems = (from b in FeedsCollection
                                               from c in array
                                               where b.EntityId == c.Value<string>("entityId").Replace("\"", string.Empty)
                                               select b).ToArray();
                        foreach (var item in needDeleteItems)
                            Collection.Remove(item);
                        n = (from b in FeedsCollection
                             where b.EntityFixed
                             select b).Count();
                    }
                    int k = 0;
                    for (int i = 0; i < array.Count; i++)
                    {
                        JObject jo = array[i] as JObject;
                        if (index == -1 && jo.TryGetValue("entityTemplate", out JToken t) && t?.ToString() == "configCard")
                        {
                            JObject j = JObject.Parse(jo.Value<string>("extraData"));
                            TitleBar.Title = j.Value<string>("pageTitle");
                            continue;
                        }
                        if (jo.TryGetValue("entityTemplate", out JToken tt) && tt?.ToString() == "fabCard") continue;
                        else if (tt?.ToString() == "feedCoolPictureGridCard")
                        {
                            foreach (JObject item in jo.Value<JArray>("entities"))
                            {
                                var entity = GetEntity(item);
                                if (entity != null)
                                {
                                    FeedsCollection.Insert(n + k, entity);
                                    k++;
                                }
                            }
                        }
                        else
                        {
                            var entity = GetEntity(jo);
                            if (entity != null)
                            {
                                FeedsCollection.Insert(n + k, entity);
                                k++;
                            }
                        }
                    }
                    UIHelper.HideProgressBar();
                    return true;
                }
                else
                {
                    if (array.Count != 0)
                    {
                        foreach (JObject i in array)
                        {
                            if (i.TryGetValue("entityTemplate", out JToken tt) && tt?.ToString() == "feedCoolPictureGridCard")
                            {
                                foreach (JObject item in i.Value<JArray>("entities"))
                                {
                                    var entity = GetEntity(item);
                                    if (entity != null) FeedsCollection.Add(entity);
                                }
                            }
                            else
                            {
                                var entity = GetEntity(i);
                                if (entity != null)
                                    FeedsCollection.Add(entity);
                            }
                        }
                        UIHelper.HideProgressBar();
                        return true;
                    }
                    else
                    {
                        UIHelper.HideProgressBar();
                        return false;
                    }
                }
            return false;
        }

        Entity GetEntity(JObject jo)
        {
            switch (jo.Value<string>("entityType"))
            {
                case "feed": return new FeedViewModel(jo, pageUrl == "/main/indexV8" ? FeedDisplayMode.isFirstPageFeed : FeedDisplayMode.normal);
                case "user": return new UserViewModel(jo);
                case "topic": return new TopicViewModel(jo);
                case "dyh": return new DyhViewModel(jo);
                case "card":
                default:
                    if (jo.TryGetValue("entityTemplate", out JToken v1))
                    {
                        switch (v1.Value<string>())
                        {
                            case "imageTextGridCard":
                            case "imageSquareScrollCard":
                            case "iconScrollCard":
                            case "iconGridCard":
                            case "feedScrollCard":
                            case "imageTextScrollCard":
                            case "iconMiniLinkGridCard":
                            case "iconMiniGridCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.Others);
                            case "imageCarouselCard_1": //return new IndexPageHasEntitiesViewModel(jo, EntitiesType.Image_1);
                            case "imageCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.Image);
                            case "iconLinkGridCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.IconLink);
                            case "feedGroupListCard":
                            case "textLinkListCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.TextLink);
                            case "textCard":
                            case "messageCard": return new IndexPageMessageCardViewModel(jo);
                            case "refreshCard": return new IndexPageOperationCardViewModel(jo, OperationType.Refresh);
                            case "unLoginCard": return new IndexPageOperationCardViewModel(jo, OperationType.Login);
                            case "titleCard": return new IndexPageOperationCardViewModel(jo, OperationType.ShowTitle);
                            case "iconTabLinkGridCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.TabLink);
                            case "selectorLinkCard": return new IndexPageHasEntitiesViewModel(jo, EntitiesType.SelectorLink);
                            default: return null;
                        }
                    }
                    else return null;
            }
        }

        private void FeedListViewItem_Tapped(object sender, TappedRoutedEventArgs e) => UIHelper.OpenLink((sender as FrameworkElement).Tag as string);

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer VScrollViewer = sender as ScrollViewer;
            if (!e.IsIntermediate && VScrollViewer.VerticalOffset == VScrollViewer.ScrollableHeight && CanLoadMore)
                GetUrlPage();
        }

        public void RefreshPage() => GetUrlPage(1);
        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e) => GetUrlPage(1);
        private void TitleBar_BackButtonClick(object sender, RoutedEventArgs e) => Frame.GoBack();

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element.Tag is string s)
            {
                if (!string.IsNullOrEmpty(s))
                    UIHelper.OpenLink(s);
            }
            else if (element.Tag is IHasUriAndTitle k)
            {
                if (!k.HasUrl || string.IsNullOrEmpty(k.Url) || k.Url == "/topic/quickList?quickType=list") return;
                string str = k.Url;
                if (str.IndexOf("/page") == 0)
                {
                    str = str.Replace("/page", "/page/dataList");
                    str += $"&title={k.Title}";
                    UIHelper.Navigate(typeof(IndexPage), new object[] { str, false });
                }
                else if (str.IndexOf('#') == 0) UIHelper.Navigate(typeof(IndexPage), new object[] { $"{str}&title={k.Title}", false });
                else UIHelper.OpenLink(str);
            }
        }

        private void ListViewItem_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            IndexPageViewModel model = (sender as FrameworkElement).DataContext as IndexPageViewModel;
            if (Feeds2.Count > 0)
            {
                ObservableCollection<Entity> feeds = Feeds2[0];
                var needDeleteItems = (from b in feeds
                                       where b.EntityType == "feed"
                                       select b).ToArray();
                foreach (var item in needDeleteItems)
                    feeds.Remove(item);
                urls[0] = $"/page/dataList?url={model.Url}&title={model.Title}";
                urls[0] = urls[0].Replace("#", "%23");
                pages[0] = 0;

            }
            else
            {
                ObservableCollection<Entity> feeds = Collection;
                var needDeleteItems = (from b in feeds
                                       where b.EntityType == "topic"
                                       select b).ToArray();
                foreach (var item in needDeleteItems)
                    feeds.Remove(item);
                pageUrl = $"/page/dataList?url={model.Url}&title={model.Title}";
                pageUrl = pageUrl.Replace("#", "%23");
                page = 0;
            }
            GetUrlPage();
        }

        public void ChangeTabView(string u)
        {
            pageUrl = u;
            page = 0;
            Collection.Clear();
            GetUrlPage();
        }

        private void Pivot_Loaded(object sender, RoutedEventArgs e)
        {
            Pivot element = sender as Pivot;
            index = element.SelectedIndex;
            if (element.Items.Count == 0)
            {
                Entity[] f = element.Tag as Entity[];
                Style style = new Style(typeof(ListViewItem));
                style.Setters.Add(new Setter(TemplateProperty, Application.Current.Resources["ListViewItemTemplate1"] as ControlTemplate));
                style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
                style.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
                style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
                for (int j = 0; j < f.Length; j++)
                {
                    IndexPageViewModel model = f[j] as IndexPageViewModel;
                    var ff = new ObservableCollection<Entity>();
                    var l = new ListView
                    {
                        Style = Application.Current.Resources["ListViewStyle"] as Style,
                        ItemContainerStyle = style,
                        ItemTemplateSelector = Resources["FTemplateSelector"] as DataTemplateSelector,
                        ItemsSource = ff,
                        ItemsPanel = Windows.UI.Xaml.Markup.XamlReader.Load("<ItemsPanelTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:c=\"using:CoolapkUWP.Controls\"><c:GridPanel DesiredColumnWidth=\"384\" CubeInSameHeight=\"False\"/></ItemsPanelTemplate>") as ItemsPanelTemplate,
                        SelectionMode = ListViewSelectionMode.None
                    };
                    l.SetValue(ScrollViewer.VerticalScrollModeProperty, ScrollMode.Disabled);
                    var i = new PivotItem
                    {
                        Tag = f[j],
                        Content = l,
                        Header = model.Title
                    };
                    element.Items.Add(i);
                    pages.Add(1);
                    Feeds2.Add(ff);
                    urls.Add("/page/dataList?url=" + model.Url.Replace("#", "%23") + $"&title={model.Title}");
                    if (j == 0) Load(element, i);
                }
                return;
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e) => Load(sender as Pivot);

        void Load(Pivot element, PivotItem i = null)
        {
            PivotItem item = i is null ? element.SelectedItem as PivotItem : i;
            IndexPageViewModel model = item.Tag as IndexPageViewModel;
            ListView view = item.Content as ListView;
            ObservableCollection<Entity> feeds = view.ItemsSource as ObservableCollection<Entity>;
            string u = model.Url;
            if (model.Url.IndexOf("/page") != 0)
            {
                u = u.Replace("#", "%23");
                u = "/page/dataList?url=" + u + $"&title={model.Title}";
            }
            _ = GetUrlPage(1, u, feeds);
        }

        private void LoginCard_Tapped(object sender, TappedRoutedEventArgs e) => UIHelper.Navigate(typeof(BrowserPage), new object[] { true, null });
    }
}