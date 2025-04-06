# ThchYoutubeMusic PowerToys Extension

A Microsoft PowerToys extension that integrates with the [th-ch/youtube-music](https://github.com/th-ch/youtube-music) desktop client. This extension allows you to search for songs, control playback, and manage your queue directly from PowerToys Run.

## Features

- Search for songs directly from PowerToys Run
- Add songs to the queue (after current or at the end)
- Search history management
- Playback controls:
  - Play/Pause toggle
  - Next/Previous track
  - Like/Dislike
  - Seek forward/backward
  - Volume control
  - Shuffle and repeat mode control
- Queue management:
  - View current queue
  - Add songs to queue
  - Remove songs from queue
  - Reorder queue items
  - Set queue index

## Prerequisites

- Windows 10/11
- [PowerToys](https://github.com/microsoft/PowerToys) installed
- [th-ch/youtube-music](https://github.com/th-ch/youtube-music) desktop client installed and running

## Installation

### Building from Source

1. Clone this repository
2. Open the solution in Visual Studio
3. Build the solution
4. Install the resulting package

## Usage

1. Launch PowerToys Run (default: Alt+Space)
2. Type "ytm" followed by your search query
3. Select a song from the results to play it or add it to your queue
4. Use the context menu (right-click) for additional options

## Configuration

The extension provides several configurable settings:

- API Server Address: The address where your th-ch/youtube-music client is running
- History Settings: Configure how your search history is maintained
- Other playback and display preferences

## API Communication

This extension communicates with the th-ch/youtube-music desktop client through its REST API. The client must be running for the extension to work properly.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) team for the extension framework
- [th-ch/youtube-music](https://github.com/th-ch/youtube-music) for the excellent YouTube Music desktop client