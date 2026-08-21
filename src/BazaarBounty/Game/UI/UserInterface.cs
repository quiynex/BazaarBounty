using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.UI;
using AssetManagementBase;
using System.IO;
using System;

namespace BazaarBounty;

public class UserInterfaceManager
{
    public MenuWidget MainMenu { get; private set; }
    public StatusBarWidget StatusBar { get; private set; }
    public GameEndWidget EndWidget { get; private set; }
    public MenuWidget WinWidget { get; private set; }
    public TutorialWidget TutorialWidget { get; private set; }
    private Desktop desktop;

    public void LoadContent()
    {
        MyraEnvironment.Game = BazaarBountyGame.GetGameInstance();
        string dir = Path.Combine(Environment.CurrentDirectory, "Assets");
        MyraEnvironment.DefaultAssetManager = AssetManager.CreateFileAssetManager(dir);

        desktop = new Desktop();
        StatusBar = new StatusBarWidget();
        MainMenu = new MainMenuWidget();
        EndWidget = new GameEndWidget();
        WinWidget = new GameWinWidget();
        TutorialWidget = new TutorialWidget();

        AddWidget(MainMenu);
    }

    public void AddWidget(IWidgetBase widget)
    {
        desktop.Widgets.Add(widget as Widget);
        widget.Initialize();
    }

    public void RemoveWidget(IWidgetBase widget)
    {
        if (desktop.Widgets.Contains(widget as Widget))
        {
            desktop.Widgets.Remove(widget as Widget);
            widget.Deinitialize();
        }
    }

    public void Update()
    {
        // if(BazaarBountyGame.GetGameInstance().CurrentGameState == BazaarBountyGame.GameState.Running) StatusBar.Update();
        for (int i = 0; i < desktop.Widgets.Count; i++)
        {
            if (desktop.Widgets[i] is IWidgetBase widget)
            {
                widget.Update();
            }
        }
    }

    public void Draw()
    {
        desktop.Render();
    }
}

public interface IWidgetBase
{
    public void Initialize()
    {
    }

    public void Deinitialize()
    {
    }

    public void Update()
    {
    }
}
