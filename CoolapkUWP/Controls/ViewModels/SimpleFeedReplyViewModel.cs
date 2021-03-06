﻿using Newtonsoft.Json.Linq;

namespace CoolapkUWP.Controls.ViewModels
{
    class SimpleFeedReplyViewModel
    {
        public SimpleFeedReplyViewModel(JObject token)
        {
            Id = token.Value<int>("id");
            Uurl = $"/u/{token.Value<int>("uid")}";
            Username = token.Value<string>("username");
            IsFeedAuthor = token.Value<int>("isFeedAuthor") == 1;
            Rurl = $"/u/{token.Value<int>("ruid")}";
            Rusername = token.Value<string>("rusername");
            if (ShowRuser)
                Message = $"<a href=\"{Uurl}\" type=\"user-detail\">{Username}{(IsFeedAuthor ? "[楼主]" : string.Empty)}</a>@<a href=\"{Rurl}\" type=\"user-detail\">{Rusername}</a>: {token.Value<string>("message")}";
            else Message = $"<a href=\"{Uurl}\" type=\"user-detail\">{Username}{(IsFeedAuthor ? "[楼主]" : string.Empty)}</a>: {token.Value<string>("message")}";
            ShowPic = token.TryGetValue("pic", out JToken value) && !string.IsNullOrEmpty(value.ToString());
            if (ShowPic)
            {
                PicUrl = value.ToString();
                Message += $" <a href=\"{PicUrl}\">查看图片</a>";
            }
        }
        public bool ShowRuser { get => !string.IsNullOrEmpty(Rusername); }
        public string Rusername { get; private set; }
        public string Rurl { get; private set; }
        public double Id { get; private set; }
        public string Uurl { get; private set; }
        public string Username { get; private set; }
        public string Message { get; private set; }
        public bool IsFeedAuthor { get; private set; }
        public bool ShowPic { get; private set; }
        public string PicUrl { get; private set; }
    }
}
