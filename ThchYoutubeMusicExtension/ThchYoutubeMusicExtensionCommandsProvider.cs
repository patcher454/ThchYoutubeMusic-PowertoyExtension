// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ThchYoutubeMusicExtension.Util;

namespace ThchYoutubeMusicExtension;

public partial class ThchYoutubeMusicExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();


    public ThchYoutubeMusicExtensionCommandsProvider()
    {
        DisplayName = "th-ch/youtube-music";
        Icon = IconHelpers.FromRelativePath("Assets\\youtube-music.png");
        Settings = _settingsManager.Settings;

        _commands = [
            new CommandItem(new ThchYoutubeMusicExtensionPage(_settingsManager))
            {
                Title = DisplayName,
                MoreCommands =
                [
                    new CommandContextItem(Settings.SettingsPage)
                ]
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
