# Git Configuration

This section contains all the settings related to working with the content git repository.

> Be carefull when changing configuration settings in this section as some are relevant for the servers security
{style="note"}

````yaml
yaml:
  [[[RepositoryUrl|#repositoryurl]]]: <string>
  [[[Branch|#branch]]]: <string>
  [[[RetrieveMapFilesFromDiff|#retrievemapfilesfromdiff]]]: [true|false]
  [[[MapFilePatterns|#mapfilepatterns]]]: <string[]>
  [[[MapFileExcludePatterns|#mapfileexcludepatterns]]]: <string[]>
  [[[DontRunWithCodeChanges|#dontrunwithcodechanges]]]: [true|false]
  [[[CodeChangePatterns|#codechangepatterns]]]: <string[]>
  [[[RunOnPullRequests|#runonpullrequests]]]: [true|false]
  [[[Identity|#identity]]]:
    Name: <string>
    Email: <string>
  [[[SshCommand|#sshcommand]]]: <string>
````

## Configuration Options
### RepositoryUrl

The url for the git repository to pull from.
This needs to be a valid url.

> Be sure that this is a repository you can trust not to have malicious code on the branch specified in [Branch](#branch).
{style="warning"}

### Branch
`<string> default="master"`

The default branch to pull changes from when a run is triggered.

### RetrieveMapFilesFromDiff
`[true|false] default=true`

If true the map server will retrieve the list of changed maps from the github diff api.
If this is false all maps will get updated on every push.

> Requires the map server to be installed as a github app.
> {style="note"}

### MapFilePatterns
`<string[]> default="master"`

Glob patterns of map files to check for.

### MapFileExcludePatterns
`<string[]>`

Glob patterns for excluding specific map files.

### DontRunWithCodeChanges
`[true|false] default=true`

Prevent updating maps when there where any c# files changed.

> Requires the map server to be installed as a github app.  
> This setting is recommended when the map server is configured to run for PRs
> as it prevents potentially malicious changes from being built and executed.
> {style="note"}

> Be **very** careful about turning this off as it may allow arbitrary code execution 
> if the map server is not configured with this option being turned off in mind!
> {style="warning"}

### CodeChangePatterns
`<string[]> default=["**/*.cs"]`

Glob patterns used for detecting code changed.

### RunOnPullRequests
`[true|false] default=true`

Setting this to true enables listening to the PullRequest event for putting the rendered map as a comment into the PR.

### Identity
`<GitIdentity> default={Name="ss14.mapserver", Email="git@mapserver.localhost"}`

The identity git will use to pull changes with. This doesn't have an effect on anything but is required for pulling 
changes in some situations.

### SshCommand
`<string>`

The ssh command used by git if set. Used for providing an ssh key to use.  

````Shell
ssh -i [path to ssh key]
````

<seealso>
    <a href="Github-Configuration.md"/>
</seealso>