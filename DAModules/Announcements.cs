using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.7.1
/// </summary>

public class Announcements : BattleBitModule
{
    public AnnouncementsConfiguration Configuration { get; set; }
    public AnnouncementStore Store { get; set; }

    public override Task OnConnected()
    {
        Task.Run(announcements);

        return Task.CompletedTask;
    }

    private async void announcements()
    {
        while (this.IsLoaded && this.Server.IsConnected)
        {
            int lastItem = this.doAnnouncement(this.Configuration.AnnounceLongDelay, this.Store.lastAnnounceLong, this.Store.lastAnnounceLongItem, this.Configuration.AnnounceLong, this.Server.AnnounceLong);
            if (lastItem != -1)
            {
                this.Store.lastAnnounceLong = DateTime.Now;
                this.Store.lastAnnounceLongItem = lastItem;
            }

            lastItem = this.doAnnouncement(this.Configuration.AnnounceShortDelay, this.Store.lastAnnounceShort, this.Store.lastAnnounceShortItem, this.Configuration.AnnounceShort, this.Server.AnnounceShort);
            if (lastItem != -1)
            {
                this.Store.lastAnnounceShort = DateTime.Now;
                this.Store.lastAnnounceShortItem = lastItem;
            }

            lastItem = this.doAnnouncement(this.Configuration.UILogOnServerDelay, this.Store.lastUILogOnServer, this.Store.lastUILogOnServerItem, this.Configuration.UILogOnServer, message => this.Server.UILogOnServer(message, this.Configuration.UILogOnserverTimeout));
            if (lastItem != -1)
            {
                this.Store.lastUILogOnServer = DateTime.Now;
                this.Store.lastUILogOnServerItem = lastItem;
            }

            lastItem = this.doAnnouncement(this.Configuration.MessageToPlayerDelay, this.Store.lastMessageToPlayer, this.Store.lastMessageToPlayerItem, this.Configuration.MessageToPlayer, message =>
            {
                foreach (RunnerPlayer player in this.Server.AllPlayers)
                {
                    player.Message(message, this.Configuration.MessageToPlayerTimeout);
                }
            });
            if (lastItem != -1)
            {
                this.Store.lastMessageToPlayer = DateTime.Now;
                this.Store.lastMessageToPlayerItem = lastItem;
            }

            lastItem = this.doAnnouncement(this.Configuration.SayToAllChatDelay, this.Store.lastSayToAllChat, this.Store.lastSayToAllChatItem, this.Configuration.SayToAllChat, this.Server.SayToAllChat);
            if (lastItem != -1)
            {
                this.Store.lastSayToAllChat = DateTime.Now;
                this.Store.lastSayToAllChatItem = lastItem;
            }

            this.Store.Save();

            await Task.Delay(1000);
        }
    }

    private int doAnnouncement(int delay, DateTime lastAnnounce, int lastItem, string[] messages, Action<string> action)
    {
        if (messages.Length == 0)
        {
            return -1;
        }

        if (DateTime.Now.Subtract(lastAnnounce).TotalSeconds < delay)
        {
            return -1;
        }

        if (lastItem >= messages.Length)
        {
            lastItem = 0;
        }

        action(messages[lastItem++]);

        return lastItem;
    }
}

public class AnnouncementsConfiguration : ModuleConfiguration
{
    public int AnnounceLongDelay { get; set; } = 600;
    public int AnnounceShortDelay { get; set; } = 300;
    public int UILogOnServerDelay { get; set; } = 60;
    public int UILogOnserverTimeout { get; set; } = 10;
    public int MessageToPlayerDelay { get; set; } = 60;
    public int MessageToPlayerTimeout { get; set; } = 10;
    public int SayToAllChatDelay { get; set; } = 60;

    public string[] AnnounceLong { get; set; } = Array.Empty<string>();
    public string[] AnnounceShort { get; set; } = Array.Empty<string>();
    public string[] UILogOnServer { get; set; } = Array.Empty<string>();
    public string[] MessageToPlayer { get; set; } = Array.Empty<string>();
    public string[] SayToAllChat { get; set; } = new[]
    {
        "We hope you enjoy our server!",
        "Feel free to write feedback in the chat!"
    };
}

public class AnnouncementStore : ModuleConfiguration
{
    public DateTime lastAnnounceLong { get; set; } = DateTime.MinValue;
    public DateTime lastAnnounceShort { get; set; } = DateTime.MinValue;
    public DateTime lastUILogOnServer { get; set; } = DateTime.MinValue;
    public DateTime lastMessageToPlayer { get; set; } = DateTime.MinValue;
    public DateTime lastSayToAllChat { get; set; } = DateTime.MinValue;

    public int lastAnnounceLongItem { get; set; } = 0;
    public int lastAnnounceShortItem { get; set; } = 0;
    public int lastUILogOnServerItem { get; set; } = 0;
    public int lastMessageToPlayerItem { get; set; } = 0;
    public int lastSayToAllChatItem { get; set; } = 0;
}
