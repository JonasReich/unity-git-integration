# unity-git-integration [![GitHub (pre-)release](https://img.shields.io/github/release/JonasReich/unity-git-integration/all.svg)](https://github.com/JonasReich/unity-git-integration/releases/)
Git Client Plugin for Unity3D

## Installation
Simply check out the repository and copy the [Assets/Plugins/git-integration](Assets/Plugins/git-integration) folder to your project.
Maybe I'll add a build as .unitypackage later.

## Features
The central feature of the plugin is an overlay for the project view:

![](screenshot_overlay.png)

There is also an editor window to stage files and submit commits:

![](screenshot_editorWindow.png)

The plugin is configured via the usual `$HOME/.gitconfig`, so in order to use the plugin,
your git installation has to be properly set up.

### Overlay Icons
|Icon|File Status|
|-|-|
|![](Assets/Plugins/git-integration/Resources/GitIcons/added.png)|added (_only staged changes_)|
|![](Assets/Plugins/git-integration/Resources/GitIcons/ignored.png)|ignored|
|![](Assets/Plugins/git-integration/Resources/GitIcons/modified.png)|modified (_only unstaged changes_)|
|![](Assets/Plugins/git-integration/Resources/GitIcons/modifiedAdded.png)|modified + added (_both staged and unstaged changes_)|
|![](Assets/Plugins/git-integration/Resources/GitIcons/moved.png)|moved|
|![](Assets/Plugins/git-integration/Resources/GitIcons/unresolved.png)|unresolved conflicts|
|![](Assets/Plugins/git-integration/Resources/GitIcons/untracked.png)|untracked|

Source: [git-icons.svg](Assets/Plugins/git-integration/Resources/git-icons.svg) is a vector image file,
which contains all of the icons on separate layers.

## Contribution
So far I've not thought about public contributions, but feel free to contact me
if you're interested in improving this plugin!

I'd love to see this little thing actualy be used in other peoples projects.

## License
[MIT License](LICENSE.md)
