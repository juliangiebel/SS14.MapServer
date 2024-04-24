# Build Configuration

The build configuration section dictates if, how and where the map renderer is built and run.
Most configuration values in this section shouldn't be changed except for [MapRendererCommand](#maprenderercommand) and 
possibly [ProcessTimeoutMinutes](#processtimeoutminutes).

```yaml
Build:
  [[[Enabled|#enabled]]]: [true|false]
  [[[Runner|#runner]]]: <enum BuildRunnerName>
  [[[RelativeOutputPath|#relativeoutputpath]]]: <string>
  [[[MapRendererProjectName|#maprendererprojectname]]]: <string>
  [[[MapRendererCommand|#maprenderercommand]]]: <string>
  [[[MapRendererOptionsString|#maprendereroptionsstring]]]: <string>
  [[[ProcessTimeoutMinutes|#processtimeoutminutes]]]: <int>
  [[[RelativeMapFilesPath|#relativemapfilespath]]]: <string>
  [[[MapDataFileName|#mapdatafilename]]]: <string>
  [[[CleanMapFolderAfterImport|#cleanmapfolderafterimport]]]: [true|false]
```

## Configuration Options

### Enabled
`[true|false] default=true`

Disables fetching and building the game content.  
The map server can only be used to host map images if building is disabled.


### Runner
`<enum BuildRunnerName> default=Local`

The type of runner to use. 

#### Possible values:

Local  
: Builds and executes the map renderer locally.  
Requires the correct dotnet version to be installed on the host.

Container
: Uses a docker container for building and executing the map renderer.

> Currently ony `Local` is supported.
{style="note"}

### RelativeOutputPath
`<string> default="bin"`

The output path relative to the runner specific build directory.

### MapRendererProjectName
`<string> default="Content.MapRenderer"`

The name of the C# project that contains the map renderer.

### MapRendererCommand
`<string> default="Content.MapRenderer.exe"`

The command to execute for rendering maps.

> This needs to be `Content.MapRenderer` on linux
{style="note"}

### MapRendererOptionsString
`<string> default="--format webp --viewer -f"`

The options provided to the map renderer command when rendering maps.

### ProcessTimeoutMinutes
`<int> default="10"`

The time the build and render process can run before getting timed out.

> Building the game and running the map renderer take a while. The timeout shouldn't be set to low.

### RelativeMapFilesPath
`<string> default="Resources/MapImages"`

The path the rendered map images end up after running the map renderer. Relative to the runners build directory.

### MapDataFileName
`<string> default="map.json"`

The name of the json file that gets generated for each map containing information about them.

### CleanMapFolderAfterImport
`[true|false] default=true`

Controls whether the contents of the [RelativeMapFilesPath](#relativemapfilespath)should be
deleted after ingesting the rendered map images.

This is enabled to save space on the hardrive but can be disabled to allow other programs to use the generated map images.

<seealso>
    <!--Provide links to related how-to guides, overviews, and tutorials.-->
</seealso>