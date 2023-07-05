﻿using Chateo.Pages.Components;
using Chateo.Resources.Styles;
using MauiReactor;
using System;

namespace Chateo.Pages;

enum Page
{
    Contacts,

    Chats,

    Settings
}

class HomePageState
{
    public Page CurrentPage { get; set; }
}

class HomePage : Component<HomePageState>
{
    public override VisualNode Render()
    {
        return new Shell
        {
            new ContentPage("Home")
            {
                new Grid("*, 83", "*")
                {
                    RenderCurrentPage(),

                    new TabBarComponent()
                        .Page(State.CurrentPage)
                        .OnPageChanged(page => SetState(s => s.CurrentPage = page))
                        .GridRow(1)
                }
            }
            .Set(MauiControls.Shell.NavBarIsVisibleProperty, false)
        }
        .BackgroundColor(Theme.Current.Background)
        ;
    }

    VisualNode RenderCurrentPage()
        => State.CurrentPage switch
        {
            Page.Contacts => new ContactsPage(),
            Page.Chats => new ChatsPage(),
            Page.Settings => new SettingsPage(),
            _ => throw new NotSupportedException(),
        };
}